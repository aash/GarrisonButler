using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using Bots.DungeonBuddy.Helpers;
using JetBrains.Annotations;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.World;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static List<WoWPoint> _waypoints = new List<WoWPoint>();
        private static WoWPoint _target;
        private static float _lastDistance;
        private static readonly Stopwatch StuckWatch = new Stopwatch();
        //public static async Task<bool> MoveToHarvest(WoWGameObject gameObject, string destinationName = null)
        //{
        //    if (await MoveTo(gameObject.Location))
        //        return true;

        //    WoWMovement.ClickToMove(gameObject.Location);
        //}
        public static async Task<bool> MoveToSafe(WoWPoint destination, string destinationName = null)
        {
            return await MoveTo(Dijkstra.ClosestToNodes(destination), destinationName);
        }
        public static async Task<bool> MoveTo(WoWPoint destination, string destinationName = null)
        {
            if (Me.Location == destination)
                return false;

            if (_target != destination || _lastMoveTo == new WoWPoint())
            {
                _waypoints = Dijkstra.GetPath(Me.Location, destination);
                if (_waypoints.Count == 0)
                {
                    if (Me.Location.Distance(destination) > 5)
                        GarrisonBuddy.Warning("Couldn't generate path from " + Me.Location + " to " + destination);
                    return false;
                }
                _lastMoveTo = _waypoints.First();
                _target = destination;
                StuckWatch.Reset();
                StuckWatch.Start();
            }
            if (Me.Location.Distance(destination) > 5 + (Me.Mounted ? 3 : 0))
            {
                WoWPoint waypoint;
                if (Me.Location.Distance(_lastMoveTo) >= 2)
                {
                    waypoint = _lastMoveTo;
                    // HERE WE CAN CHECK FOR MAYBE A FURTHER WAYPOINTS? 
                    if (!furthestTrimmed && _waypoints.Count > 2)
                    {
                        _waypoints = await GetFurthestWaypoint(_waypoints);
                        furthestTrimmed = true;
                    }
                    //GarrisonBuddy.Diagnostic("Keeping next waypoint to " + destinationName + ": " + waypoint);
                }
                else
                {
                    if (_waypoints.Count == 0)
                    {
                        GarrisonBuddy.Diagnostic("Waypoints list empty, assuming at destination: " + destinationName);
                        return false;
                    }

                    // HERE WE CAN CHECK FOR MAYBE A FURTHER WAYPOINTS ? 
//                    _waypoints = await GetFurthestWaypoint(_waypoints);

                    waypoint = _waypoints.First();
                    furthestTrimmed = false;

                    _waypoints.Remove(waypoint);
                    StuckWatch.Reset();
                    StuckWatch.Start();
                    GarrisonBuddy.Diagnostic("Loading next waypoint to " + destinationName + ": " + waypoint);
                }
                _lastMoveTo = waypoint;
                if (Math.Abs(Me.Location.DistanceSqr(_lastMoveTo) - _lastDistance) < 2 &&
                    StuckWatch.Elapsed.TotalSeconds > 5)
                {
                    GarrisonBuddy.Log("Stuck! Starting Unstuck routine.");
                    await Buddy.Coroutines.Coroutine.Wait(3000, () =>
                    {
                        Navigator.NavigationProvider.StuckHandler.Unstick();
                        return false;
                    }
                        );
                    StuckWatch.Reset();
                    StuckWatch.Start();
                    _target = new WoWPoint(); // reset path
                }
                else
                {
                    if (Me.IsMoving && !Me.Mounted && Mount.CanMount())
                    {
                        WoWMovement.MoveStop();
                        await Buddy.Coroutines.Coroutine.Wait(5000, () => !Me.IsMoving);
                        Mount.GetMountSpell().Cast();
                        await CommonCoroutines.SleepForLagDuration();
                        await Buddy.Coroutines.Coroutine.Wait(5000, () => !Me.IsCasting);
                        await CommonCoroutines.SleepForLagDuration();
                    }
                    WoWMovement.ClickToMove(waypoint);
                }
                _lastDistance = Me.Location.DistanceSqr(_lastMoveTo);
                return true;
            }
            return false;
        }

        private static async Task<List<WoWPoint>> GetFurthestWaypoint([NotNull] List<WoWPoint> waypoints)
        {
            if (waypoints == null) throw new ArgumentNullException("waypoints");
            if (!waypoints.Any()) throw new ArgumentException("waypoints empty");
            if (waypoints.Count == 1) return waypoints;

            var left = new List<WoWPoint>();
            int maxTry = Math.Min(waypoints.Count, 8);
            int indexToKeep = 0;
            for (int index = 0; index < maxTry; index++)
            {
                indexToKeep = index;
                WoWPoint waypoint = waypoints[index];
                await Buddy.Coroutines.Coroutine.Yield();
                if (!IsValidWaypoint(Me.Location, waypoint))
                    break;
            }
            indexToKeep -= 1;
            left.AddRange(waypoints.Skip(indexToKeep));
            GarrisonBuddy.Diagnostic("ENS skipped " + (waypoints.Count - left.Count) + " waypoints.");
            return left.Any() ? left : waypoints;
        }

        private static double Slope(WoWPoint p1, WoWPoint p2)
        {
            double m = p2.Z - p1.Z;
            double xy = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            return RadianToDegree((Math.Atan2(m, xy)));
        }

        private static double RadianToDegree(double angle)
        {
            return angle*(180.0/Math.PI);
        }

        private static bool IsValidWaypoint(WoWPoint from, WoWPoint waypoint)
        {
            bool toKeep = false;
            var lines = new List<WorldLine>();
            for (float p = 0; p <= 1; p += (1/(from.Distance(waypoint))))
            {
                WoWPoint pOrigin = from + (waypoint - from)*p;
                for (float i = Me.BoundingHeight/3; i <= Me.BoundingHeight; i += Me.BoundingHeight/3)
                {
                    WoWPoint pTo = waypoint + new WoWPoint(0, 0, i);
                    pOrigin.Z = GetGroundZ(pTo) + i;

                    for (float j = -Me.BoundingRadius; j <= Me.BoundingRadius; j += Me.BoundingRadius/2)
                    {
                        WoWPoint perpTo = GetPerpPointsBeginning(pTo, pOrigin, j);
                        WoWPoint perpOrigin = GetPerpPointsBeginning(pOrigin, pTo, j);
                        perpOrigin.Z = GetGroundZ(perpTo) + i;
                        lines.Add(new WorldLine(perpOrigin, perpTo));
                    }
                }
            }
            bool[] resTrace;
            GameWorld.MassTraceLine(lines.ToArray(), TraceLineHitFlags.Collision, out resTrace);
            //for (int index = 0; index < lines.Count; index++)
            //{
            //    var worldLine = lines[index];
            //    var res = resTrace[index];

            //    //GarrisonBuddy.Diagnostic("worldline, from:" + worldLine.Start + "\nTo:" + worldLine.End + "\nRes:" + res);
            //}
            //GarrisonBuddy.Diagnostic("RESULT to: " + pTo + "\nTraceLine - Collision:" + col);
            //GarrisonBuddy.Diagnostic("Slope: " + Slope(pOrigin, pTo));
            if (resTrace.Any(res => res == true)) return false;
            //GarrisonBuddy.Diagnostic("resTrace passed");

            return Slope(lines.First().Start, lines.First().End) < 45;
        }

        private static WoWPoint GetPerpPointsBeginning(WoWPoint p1, WoWPoint p2, double distance)
        {
            double dx = p1.X - p2.X;
            double dy = p1.Y - p2.Y;
            double a = dy/dx;
            double b = p1.Y - a*p1.X;
            double a2 = -a;
            double b2 = p1.Y - a2*p1.X;
            double magnitude = Math.Sqrt(Math.Pow(1, 2) + Math.Pow(a2, 2));

            var N = new Vector2(1/(float) magnitude, (float) a2/(float) magnitude);
            var res1 = new WoWPoint(p1.X + distance*N.X, p1.Y + distance*N.Y, p1.Z);

            //GarrisonBuddy.Diagnostic("point: " + res1 + "\nof: " + p1 + "\nand: " + p2);
            //var res2 = new WoWPoint(p1.X - distance*dy, p1.Y + distance*dx, p1.X);
            return res1;
        }

        public static float GetGroundZ(WoWPoint p)
        {
            WoWPoint ground;

            GameWorld.TraceLine(new WoWPoint(p.X, p.Y, (p.Z + 0.5)), new WoWPoint(p.X, p.Y, (p.Z - 5)),
                TraceLineHitFlags.Collision, out ground);
            return ground != WoWPoint.Empty ? ground.Z : float.MinValue;
        }

        #region Dijkstra

        private static Graph _movementGraph;
        private static List<WoWPoint> _zonePoints;
        private static bool furthestTrimmed;

        public static void InitializationMove()
        {
            // Generate Garrison points based on garrison level and buildings level
            if (_zonePoints == null)
                _zonePoints = GetGarrisonPoints();

            // Generating graph from list of points
            _movementGraph = Dijkstra.GraphFromList(_zonePoints);

            // Init variables for movement system
            _lastMoveTo = new WoWPoint();
        }

        public class Dijkstra
        {
            public static Graph GraphFromList(List<WoWPoint> points)
            {
                var graph = new Graph();
                foreach (WoWPoint t in points)
                {
                    graph.AddNode(t);
                }
                List<WoWPoint> graphPoints = graph.Nodes.Keys.ToList();
                for (int i = 0; i < graphPoints.Count; i++)
                {
                    WoWPoint point1 = graphPoints[i];
                    for (int j = i + 1; j < graphPoints.Count; j++)
                    {
                        WoWPoint point2 = graphPoints[j];
                        float dist = point1.Distance(point2);
                        if (dist < 4)
                        {
                            graph.AddConnection(point1, point2, dist, true);
                        }
                    }
                }
                return graph;
            }

            public static WoWPoint ClosestToNodes(WoWPoint point)
            {
                //var closest = new WoWPoint();

                //float Mindistance = float.PositiveInfinity;
                var items = _movementGraph.Nodes.Keys.Select(p => new {Point = p, dist = p.Distance(point)});
                return items.Aggregate((a, b) => a.dist < b.dist ? a : b).Point;
                //foreach (var node in _movementGraph.Nodes)
                //{
                //    float distance = node.Key.Distance(point);
                //    if (distance < Mindistance)
                //    {
                //        closest = node.Key;
                //        Mindistance = distance;
                //    }
                //}
                //return closest;
            }

            public static List<WoWPoint> GetPath(WoWPoint from, WoWPoint to)
            {
                InitializationMove();
                WoWPoint starting = ClosestToNodes(from);
                WoWPoint ending = ClosestToNodes(to);
                if (_movementGraph.Nodes.All(n => n.Key != starting))
                    throw new ArgumentException("Starting node must be in graph.");

                InitialiseGraph(_movementGraph, starting);
                ProcessGraph(_movementGraph, starting);
                return ExtractPath(_movementGraph, ending);
            }

            private static void InitialiseGraph(Graph graph, WoWPoint startingNode)
            {
                foreach (Node node in graph.Nodes.Values)
                    node.DistanceFromStart = double.PositiveInfinity;
                graph.Nodes[startingNode].DistanceFromStart = 0;
            }

            private static void ProcessGraph(Graph graph, WoWPoint startingNode)
            {
                bool finished = false;
                List<Node> queue = graph.Nodes.Values.ToList();
                while (!finished)
                {
                    Node nextNode =
                        queue.OrderBy(n => n.DistanceFromStart)
                            .FirstOrDefault(n => !double.IsPositiveInfinity(n.DistanceFromStart));
                    if (nextNode != null)
                    {
                        ProcessNode(nextNode, queue);
                        queue.Remove(nextNode);
                    }
                    else
                    {
                        finished = true;
                    }
                }
            }

            private static void ProcessNode(Node node, List<Node> queue)
            {
                IEnumerable<NodeConnection> connections = node.Connections.Where(c => queue.Contains(c.Target));
                foreach (NodeConnection connection in connections)
                {
                    double distance = node.DistanceFromStart + connection.Distance;
                    if (distance < connection.Target.DistanceFromStart)
                    {
                        connection.Target.DistanceFromStart = distance;
                        connection.Target.Previous = node;
                    }
                }
            }

            private static IDictionary<WoWPoint, double> ExtractDistances(Graph graph)
            {
                return graph.Nodes.ToDictionary(n => n.Key, n => n.Value.DistanceFromStart);
            }

            private static List<WoWPoint> ExtractPath(Graph graph, WoWPoint target)
            {
                var path = new List<WoWPoint>();
                Node u = graph.Nodes.First(n => n.Key == target).Value;

                while (u.Previous != null)
                {
                    path.Add(u.Position);
                    u = u.Previous;
                }
                path.Reverse();
                return path;
            }
        }

        public class Graph
        {
            public Graph()
            {
                Nodes = new Dictionary<WoWPoint, Node>();
            }

            internal IDictionary<WoWPoint, Node> Nodes { get; private set; }

            public void AddNode(WoWPoint position)
            {
                if (Nodes.ContainsKey(position))
                    return;
                var node = new Node(position);
                Nodes.Add(position, node);
            }

            public void AddConnection(WoWPoint fromNode, WoWPoint toNode, float distance, bool twoWay)
            {
                Nodes[fromNode].AddConnection(Nodes[toNode], distance, twoWay);
            }
        }

        internal class Node
        {
            private readonly IList<NodeConnection> _connections;

            internal Node(WoWPoint position)
            {
                Position = position;
                Previous = null;
                _connections = new List<NodeConnection>();
            }

            internal WoWPoint Position { get; private set; }
            internal Node Previous { get; set; }

            internal double DistanceFromStart { get; set; }

            internal IEnumerable<NodeConnection> Connections
            {
                get { return _connections; }
            }

            internal void AddConnection(Node targetNode, double distance, bool twoWay)
            {
                if (targetNode == null) throw new ArgumentNullException("targetNode");
                if (targetNode == this)
                    throw new ArgumentException("Node may not connect to itself: " + targetNode.Position);
                if (distance <= 0) throw new ArgumentException("Distance must be positive.");

                _connections.Add(new NodeConnection(targetNode, distance));
                if (twoWay) targetNode.AddConnection(this, distance, false);
            }
        }

        internal class NodeConnection
        {
            internal NodeConnection(Node target, double distance)
            {
                Target = target;
                Distance = distance;
            }

            internal Node Target { get; private set; }
            internal double Distance { get; private set; }
        }

        #endregion
    }
}