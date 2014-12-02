using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        private static List<WoWPoint> _waypoints = new List<WoWPoint>();
        private static WoWPoint _target;

        //public static async Task<bool> MoveToHarvest(WoWGameObject gameObject, string destinationName = null)
        //{
        //    if (await MoveTo(gameObject.Location))
        //        return true;

        //    WoWMovement.ClickToMove(gameObject.Location);
        //}
        public static async Task<bool> MoveTo(WoWPoint destination, string destinationName = null)
        {
            if (Me.Location == destination)
                return false;

            if (_target != destination || _lastMoveTo == new WoWPoint())
            {
                _waypoints = Dijkstra.GetPath(Me.Location, destination);
                if (_waypoints.Count == 0)
                {
                    GarrisonBuddy.Warning("Couldn't generate path from " + Me.Location + " to " + destination);
                    return false;
                }
                _lastMoveTo = _waypoints.First();
                _target = destination;
            }
            if (Me.Location.Distance(destination) > 5)
            {
                WoWPoint waypoint;
                if (Me.Location.Distance(_lastMoveTo) >= 1)
                {
                    waypoint = _lastMoveTo;
                    //GarrisonBuddy.Diagnostic("Keeping next waypoint to " + destinationName + ": " + waypoint);
                }
                else
                {
                    if (_waypoints.Count == 0)
                    {
                        GarrisonBuddy.Diagnostic("Waypoints list empty, assuming at destination: " + destinationName);
                        return false;
                    }
                    waypoint = _waypoints.First();

                    _waypoints.Remove(waypoint);
                    GarrisonBuddy.Diagnostic("Loading next waypoint to " + destinationName + ": " + waypoint);
                }
                _lastMoveTo = waypoint;
                if (Me.Mounted)
                {
                    await CommonCoroutines.LandAndDismount("Mount not supported yet.");
                }
                WoWMovement.ClickToMove(waypoint);
                return true;
            }

            await Buddy.Coroutines.Coroutine.Wait(100, () => !Me.IsMoving);

            return false;
        }

        #region Dijkstra

        private static Graph _movementGraph;
        private static List<WoWPoint> _zonePoints;

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

            private static WoWPoint ClosestToNodes(WoWPoint point)
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