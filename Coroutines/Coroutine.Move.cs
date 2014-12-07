using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GreyMagic;
using JetBrains.Annotations;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.Helpers;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.World;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static List<WoWPoint> _currentWaypointsList = new List<WoWPoint>();
        private static WoWPoint _target;
        private static float _lastDistance;
        private static readonly Stopwatch StuckWatch = new Stopwatch();

        private static MoveResult lastMoveResult;

        //public static async Task<bool> MoveTo(WoWPoint destination, string destinationName = null)
        //{
        //    if (Me.Location.Distance(destination) > 5 + (Me.Mounted ? 3 : 0))
        //    {

        //        if (Navigator.CanNavigateFully(Me.Location, destination))
        //        {
        //            GarrisonBuddy.Diagnostic("Can use HB native movement system.");
        //            if (lastMoveResult.IsSuccessful())
        //                return false;

        //            GarrisonBuddy.Diagnostic("Getting last run status");
        //            Navigator.GetRunStatusFromMoveResult(lastMoveResult);
        //            Navigator.MoveTo(destination);
        //            return true;
        //        }
        //        GarrisonBuddy.Diagnostic("Using experimental movement system");
        //        return await MoveToSub(destination, destinationName);
        //    }
        //    else return false;
        //}

        public static async Task<bool> MoveTo(WoWPoint destination, string destinationName = null)
        {
            if (Me.Location == destination || Me.Location.Distance(destination) < 2)
                return false;

            if (_target != destination || _lastMoveTo == new WoWPoint())
            {
                var TaskResult = await Buddy.Coroutines.Coroutine.ExternalTask(Task.Run(() =>
                {
                    return Dijkstra.GetPath(Me.Location, destination);
                }),30000);
                if (!TaskResult.Completed)
                {
                    return true;
                }
                _currentWaypointsList = TaskResult.Result;
                if (_currentWaypointsList.Count == 0)
                {
                    if (Me.Location.Distance(destination) > 5)
                        GarrisonBuddy.Warning("Couldn't generate path from " + Me.Location + " to " + destination);
                    return false;
                }
                _lastMoveTo = _currentWaypointsList.First();
                _target = destination;
                StuckWatch.Reset();
                StuckWatch.Start();
            }
            if (Me.Location.Distance(destination) > 5 + (Me.Mounted ? 3 : 0))
            {
                WoWPoint waypoint;
                if (Me.Location.Distance(_lastMoveTo) >= 2 + (Me.Mounted ? 1 : 0))
                {
                    waypoint = _lastMoveTo;
                    //// HERE WE CAN CHECK FOR MAYBE A FURTHER WAYPOINTS? 
                    //if (!furthestTrimmed && _waypoints.Count > 2)
                    //{
                    //    _waypoints = await GetFurthestWaypoint(_waypoints);
                    //    furthestTrimmed = true;
                    //}
                    //GarrisonBuddy.Diagnostic("Keeping next waypoint to " + destinationName + ": " + waypoint);
                }
                else
                {
                    if (_currentWaypointsList.Count == 0)
                    {
                        GarrisonBuddy.Diagnostic("Waypoints list empty, assuming at destination: " + destinationName);
                        return false;
                    }

                    waypoint = _currentWaypointsList.First();
                    furthestTrimmed = false;

                    _currentWaypointsList.Remove(waypoint);
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
                    if (Me.IsMoving && !Me.Mounted && Mount.CanMount() &&
                        Me.Location.Distance(destination) >= CharacterSettings.Instance.MountDistance)
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
            int maxTry = Math.Min(waypoints.Count, 3);
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
        private async static Task<List<WoWPoint>>  GetFurthestWaypoint2(List<WoWPoint> waypoints)
        {
            if (waypoints == null) throw new ArgumentNullException("waypoints");
            if (waypoints.Count < 3) return waypoints;
            int cpt = 0;
            var res = new List<WoWPoint>(waypoints);
            for (int index = 2; index < res.Count - 1; index++)
            {
                //GarrisonBuddy.Diagnostic("ENS2 index: " + index + " - count: " + waypoints.Count);
                var waypoint = res[index];
                var old = res[index - 2];
                if (MinimalIsValidWaypoint(old, waypoint))
                {
                    if (IsValidWaypoint(old, waypoint))
                    {
                        res.RemoveAt(index - 1);
                        index--;
                        cpt++;
                    }
                }
                await Buddy.Coroutines.Coroutine.Yield();
            }
            GarrisonBuddy.Diagnostic("ENS2 skipped " + cpt + " waypoints.");
            return res;
        }

        private static double Slope(WoWPoint p1, WoWPoint p2)
        {
            double m = p2.Z - p1.Z;
            double xy = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
            return m/xy;
        }

        private static double RadianToDegree(double angle)
        {
            return angle*(180.0/Math.PI);
        }

        private static bool MinimalIsValidWaypoint(WoWPoint from, WoWPoint waypoint)
        {
            GarrisonBuddy.Diagnostic("height:" + Me.BoundingHeight);
            var height = Me.BoundingHeight/2;
            var tempFrom = from + new WoWPoint(0, 0, height);
            var tempWaypoint = waypoint + new WoWPoint(0, 0, height);

            var quickTest = GameWorld.TraceLine(tempFrom, tempWaypoint, TraceLineHitFlags.Collision);
            return !quickTest && Slope(from, waypoint) < 1.2;
        }
 private static bool IsValidWaypoint(WoWPoint from, WoWPoint waypoint)
        {
            var lines = new List<WorldLine>();
            var height = Me.BoundingHeight+Me.BoundingHeight/10;
            var width = Me.BoundingRadius +Me.BoundingHeight/10;
            for (float p = 0; p <= 1; p += (1/(from.Distance(waypoint))))
            {
                WoWPoint pOrigin = from + (waypoint - from)*p;
                for (float i = height/3; i <= height; i += height/10)
                {
                    WoWPoint pTo = waypoint + new WoWPoint(0, 0, i);
                    pOrigin.Z = GetGroundZ(pTo) + i;

                    for (float j = -width; j <= width; j += width / 10)
                    {
                        WoWPoint perpTo = GetPerpPointsBeginning(pTo, pOrigin, j);
                        WoWPoint perpOrigin = GetPerpPointsBeginning(pOrigin, pTo, j);
                        perpOrigin.Z = GetGroundZ(perpTo);
                        lines.Add(new WorldLine(perpOrigin, perpTo));
                    }
                }
            }
            bool[] resTrace;
            GameWorld.MassTraceLine(lines.ToArray(), TraceLineHitFlags.Collision, out resTrace);
            if (resTrace.Any(res => res)) return false;

            return Slope(lines.First().Start, lines.First().End) < 1.2;
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
                var items = _movementGraph.Nodes.Keys.Select(p => new {Point = p, dist = p.Distance(point)});
                return items.Aggregate((a, b) => a.dist < b.dist ? a : b).Point;
            }

            private static Stopwatch pathGenerationStopwatch = new Stopwatch();
            public static List<WoWPoint> GetPath(WoWPoint from, WoWPoint to)
            {
                InitializationMove();
                pathGenerationStopwatch.Reset();
                pathGenerationStopwatch.Start();

                GarrisonBuddy.Diagnostic("Starting path generation.");
                WoWPoint starting = ClosestToNodes(from);
                GarrisonBuddy.Diagnostic("Found ClosestToNodes in " + pathGenerationStopwatch.ElapsedMilliseconds + "ms.");
                WoWPoint ending = ClosestToNodes(to);
                GarrisonBuddy.Diagnostic("Found ClosestToNodes in " + pathGenerationStopwatch.ElapsedMilliseconds + "ms.");
                if (!_movementGraph.Nodes.Any(n => n.Key == starting))
                    throw new ArgumentException("Starting node must be in graph.");

                GarrisonBuddy.Diagnostic("Found Any in " + pathGenerationStopwatch.ElapsedMilliseconds + "ms.");
                InitialiseGraph(_movementGraph, starting);
                GarrisonBuddy.Diagnostic("Found InitialiseGraph in " + pathGenerationStopwatch.ElapsedMilliseconds + "ms.");
                ProcessGraph(_movementGraph, starting);
                GarrisonBuddy.Diagnostic("Found ProcessGraph in " + pathGenerationStopwatch.ElapsedMilliseconds + "ms.");
                var tempPath = ExtractPath(_movementGraph, ending);
                GarrisonBuddy.Diagnostic("Found ExtractPath in " + pathGenerationStopwatch.ElapsedMilliseconds + "ms.");
                //tempPath = await GetFurthestWaypoint2(tempPath);
                //GarrisonBuddy.Diagnostic("Found GetFurthestWaypoint2 in " + pathGenerationStopwatch.ElapsedMilliseconds + "ms.");

                pathGenerationStopwatch.Stop();
                GarrisonBuddy.Diagnostic("Path generated in " + pathGenerationStopwatch.ElapsedMilliseconds + "ms with " +
                              tempPath.Count + " waypoints.");
                return tempPath;
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