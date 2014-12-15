using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Priority_Queue;
using Styx;
using Styx.Pathing;
using Tripper.Tools.Math;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        #region Dijkstra

        private static Graph _movementGraph;
        private static List<WoWPoint> _zonePoints;

        private static NavigationGaB navigation;

        internal static NavigationProvider oldNavigation;

        public static void InitializationMove()
        {
            // Generate Garrison points based on garrison level and buildings level
            if (_zonePoints == null)
            {
                _zonePoints = GetGarrisonPoints();
                //navigation.UpdateMaps();
            }
            if (navigation == null)
            {
                navigation = new NavigationGaB();
                oldNavigation = Navigator.NavigationProvider;
                Navigator.NavigationProvider = navigation;
            }

            // Generating graph from list of points
            _movementGraph = Dijkstra.GraphFromList(_zonePoints);

            // Init variables for movement system
            _lastMoveTo = new WoWPoint();
        }

        public class Dijkstra
        {
            private static readonly Stopwatch PathGenerationStopwatch = new Stopwatch();

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

            public static Vector3[] GetPath2(WoWPoint from, WoWPoint to)
            {
                InitializationMove();
                PathGenerationStopwatch.Reset();
                PathGenerationStopwatch.Start();

                GarrisonBuddy.Diagnostic("Starting path generation.");
                WoWPoint starting = ClosestToNodes(from);

                WoWPoint ending = ClosestToNodes(to);

                if (_movementGraph.Nodes.All(n => n.Key != starting))
                    throw new ArgumentException("Starting node must be in graph.");

                if (_movementGraph.Nodes.All(n => n.Key != ending))
                    throw new ArgumentException("Ending node must be in graph.");

                ProcessGraph(_movementGraph, starting);

                WoWPoint[] tempPath = ExtractPath(_movementGraph, ending);
                Vector3[] res = new Vector3[tempPath.Count()];
                for (int index = 0; index < tempPath.Length; index++)
                {
                    res[index] = tempPath[index];
                }
                GarrisonBuddy.Diagnostic("Path generated in " + PathGenerationStopwatch.ElapsedMilliseconds + "ms.");
                PathGenerationStopwatch.Stop();
                return res;
            }

            public static WoWPoint[] GetPathWoW(WoWPoint from, WoWPoint to)
            {
                InitializationMove();
                PathGenerationStopwatch.Reset();
                PathGenerationStopwatch.Start();

                GarrisonBuddy.Diagnostic("Starting path generation.");
                WoWPoint starting = ClosestToNodes(from);
                WoWPoint ending = ClosestToNodes(to);
                if (_movementGraph.Nodes.All(n => n.Key != starting))
                    throw new ArgumentException("Starting node must be in graph.");

                ProcessGraph(_movementGraph, starting);
                WoWPoint[] tempPath = ExtractPathWoW(_movementGraph, ending);

                PathGenerationStopwatch.Stop();
                GarrisonBuddy.Diagnostic("Path generated in " + PathGenerationStopwatch.ElapsedMilliseconds + "ms.");
                return tempPath;
            }

            private static void ProcessGraph(Graph graph, WoWPoint startingNode)
            {
                // Initialization of the data
                var priorityQueue = new HeapPriorityQueue<Node>(graph.Nodes.Count);
                foreach (Node node in graph.Nodes.Values)
                {
                    node.DistanceFromStart = double.PositiveInfinity;
                    node.Previous = null;
                    node.Visited = false;
                }
                graph.Nodes[startingNode].DistanceFromStart = 0;
                foreach (Node node in graph.Nodes.Values)
                {
                    priorityQueue.Enqueue(node, node.DistanceFromStart);
                }
                int cpt = 0;
                // Processing graph
                while (priorityQueue.Count != 0)
                {
                    Node U = priorityQueue.Dequeue();
                    U.Visited = true;
                    IEnumerable<NodeConnection> connections = U.Connections.Where(c => c.Target.Visited == false);
                    foreach (NodeConnection V in connections)
                    {
                        double distance = U.DistanceFromStart + V.Distance;
                        if (distance < V.Target.DistanceFromStart)
                        {
                            V.Target.DistanceFromStart = distance;
                            cpt++;
                            V.Target.Previous = U;
                            priorityQueue.UpdatePriority(V.Target, distance);
                        }
                    }
                }
            }

            private static WoWPoint[] ExtractPath(Graph graph, WoWPoint target)
            {
                var path = new List<WoWPoint>();
                var u = graph.Nodes.First(n => n.Key == target).Value;

                while (u.Previous != null)
                {
                    path.Add(u.Position);
                    u = u.Previous;
                }
                path.Reverse();
                for (int index = 0; index < path.Count; index++)
                {
                    WoWPoint vector3 = path[index];
                }
                return path.ToArray();
            }

            private static WoWPoint[] ExtractPathWoW(Graph graph, WoWPoint target)
            {
                var path = new List<WoWPoint>();
                Node u = graph.Nodes.First(n => n.Key == target).Value;

                while (u.Previous != null)
                {
                    path.Add(u.Position);
                    u = u.Previous;
                }
                path.Reverse();
                for (int index = 0; index < path.Count; index++)
                {
                    WoWPoint vector3 = path[index];
                }
                return path.ToArray();
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

        internal class Node : PriorityQueueNode
        {
            private readonly IList<NodeConnection> _connections;

            internal Node(WoWPoint position)
            {
                Position = new WoWPoint(position.X, position.Y, position.Z);
                Previous = null;
                Visited = false;
                _connections = new List<NodeConnection>();
            }

            internal WoWPoint Position { get; private set; }
            internal Node Previous { get; set; }
            internal Boolean Visited { get; set; }

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