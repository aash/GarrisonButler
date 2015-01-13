#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Priority_Queue;
using Styx;
using Styx.Pathing;
using Styx.WoWInternals.WoWObjects;
using Tripper.Tools.Math;

#endregion

namespace GarrisonButler
{
    partial class Coroutine
    {
        #region Dijkstra

        private static Graph _movementGraph;
        private static List<WoWPoint> _zonePoints;

        private static NavigationGaB _customNavigation;
        internal static bool CustomNavigationLoaded = false;
        internal static NavigationProvider NativeNavigation;

        public static void InitializationMove()
        {
            // Generate Garrison points based on garrison level and buildings level
            if (_zonePoints == null)
            {
                _zonePoints = GetGarrisonPoints();

                //navigation.UpdateMaps();
            }

            // Generating graph from list of points
            if (_movementGraph == null)
                _movementGraph = Dijkstra.GraphFromList(_zonePoints);
        }

        public class Dijkstra
        {
            private static readonly Stopwatch PathGenerationStopwatch = new Stopwatch();

            public static Graph GraphFromList(List<WoWPoint> points)
            {
                var graph = new Graph();
                foreach (var t in points)
                {
                    graph.AddNode(t);
                }

                var graphPoints = graph.Nodes.Keys.ToList();

                var forLoopFilterEndTime = DateTime.Now;
                var count = 0;
                for (var i = 0; i < graphPoints.Count; i++)
                {
                    var point1 = graphPoints[i];
                    for (var j = i + 1; j < graphPoints.Count; j++)
                    {
                        var point2 = graphPoints[j];
                        var dist = point1.Distance(point2);
                        if (!(dist < 3)) continue;
                        graph.AddConnection(point1, point2, dist, true);
                        count++;
                    }
                }
                GarrisonButler.DiagnosticLogTimeTaken("Matching all with distance less than "
                                                      + 3
                                                      + " returned "
                                                      + count
                                                      + " elements USING 2x for loops", forLoopFilterEndTime);
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

                GarrisonButler.Diagnostic("Starting path generation.");
                var starting = ClosestToNodes(from);

                var ending = ClosestToNodes(to);

                if (_movementGraph.Nodes.All(n => n.Key != starting))
                    throw new ArgumentException("Starting node must be in graph.");

                if (_movementGraph.Nodes.All(n => n.Key != ending))
                    throw new ArgumentException("Ending node must be in graph.");

                ProcessGraph(_movementGraph, starting);

                var tempPath = ExtractPath(_movementGraph, ending);
                var res = new Vector3[tempPath.Count()];
                for (var index = 0; index < tempPath.Length; index++)
                {
                    res[index] = tempPath[index];
                }
                GarrisonButler.DiagnosticLogTimeTaken("Path generation",
                    (int) PathGenerationStopwatch.ElapsedMilliseconds);
                PathGenerationStopwatch.Stop();
                return res;
            }

            public static WoWPoint[] GetPathWoW(WoWPoint from, WoWPoint to)
            {
                InitializationMove();
                PathGenerationStopwatch.Reset();
                PathGenerationStopwatch.Start();

                GarrisonButler.Diagnostic("Starting path generation.");

                var starting = ClosestToNodes(from);
                if (_movementGraph.Nodes.All(n => n.Key != starting))
                    throw new ArgumentException("Starting node must be in graph.");

                var ending = ClosestToNodes(to);
                if (_movementGraph.Nodes.All(n => n.Key != ending))
                    throw new ArgumentException("Ending node must be in graph.");

                ProcessGraph(_movementGraph, starting);
                var tempPath = ExtractPathWoW(_movementGraph, ending, to);

                PathGenerationStopwatch.Stop();
                GarrisonButler.Diagnostic("Path generated in " + PathGenerationStopwatch.ElapsedMilliseconds + "ms.");
                return tempPath;
            }


            public static List<WoWPoint[]> GetMultiPaths(WoWPoint from, WoWPoint[] to)
            {
                InitializationMove();
                PathGenerationStopwatch.Reset();
                PathGenerationStopwatch.Start();

                GarrisonButler.Diagnostic("Starting path generation.");

                var starting = ClosestToNodes(from);
                if (_movementGraph.Nodes.All(n => n.Key != starting))
                    throw new ArgumentException("Starting node must be in graph.");

                ProcessGraph(_movementGraph, starting);

                var pathsList = new List<WoWPoint[]>();
                foreach (var t in to)
                {
                    var endingPoint = ClosestToNodes(t);
                    if (_movementGraph.Nodes.All(n => n.Key != endingPoint))
                        throw new ArgumentException("Ending node must be in graph.");
                    pathsList.Add(ExtractPathWoW(_movementGraph, endingPoint,t));
                }

                PathGenerationStopwatch.Stop();
                GarrisonButler.Diagnostic("Multi-Paths generated in " + PathGenerationStopwatch.ElapsedMilliseconds +
                                          "ms.");
                return pathsList;
            }

            public static WoWGameObject GetClosestObject(WoWPoint from, WoWGameObject[] objectsTo)
            {
                InitializationMove();
                PathGenerationStopwatch.Reset();
                PathGenerationStopwatch.Start();

                GarrisonButler.Diagnostic("Starting path generation.");

                var starting = ClosestToNodes(from);
                if (_movementGraph.Nodes.All(n => n.Key != starting))
                    throw new ArgumentException("Starting node must be in graph.");

                ProcessGraph(_movementGraph, starting);
                var minDistance = double.MaxValue;
                var temp = objectsTo.First();

                foreach (var gameObject in objectsTo)
                {
                    var endPoint = ClosestToNodes(gameObject.Location);
                    if (_movementGraph.Nodes.All(n => n.Key != endPoint))
                        throw new ArgumentException("Ending node must be in graph.");

                    var currentDistance = ExtractDistanceWoW(_movementGraph, endPoint);
                    if (!(currentDistance <= minDistance)) continue;
                    minDistance = currentDistance;
                    temp = gameObject;
                }

                PathGenerationStopwatch.Stop();
                GarrisonButler.Diagnostic("Multi-Paths generated in " + PathGenerationStopwatch.ElapsedMilliseconds +
                                          "ms.");
                return temp;
            }

            private static void ProcessGraph(Graph graph, WoWPoint startingNode)
            {
                // Initialization of the data
                var priorityQueue = new HeapPriorityQueue<Node>(graph.Nodes.Count);
                foreach (var node in graph.Nodes.Values)
                {
                    node.DistanceFromStart = double.PositiveInfinity;
                    node.Previous = null;
                    node.Visited = false;
                }
                graph.Nodes[startingNode].DistanceFromStart = 0;
                foreach (var node in graph.Nodes.Values)
                {
                    priorityQueue.Enqueue(node, node.DistanceFromStart);
                }
                // Processing graph
                while (priorityQueue.Count != 0)
                {
                    var u = priorityQueue.Dequeue();
                    u.Visited = true;
                    var connections = u.Connections.Where(c => c.Target.Visited == false);
                    foreach (var v in connections)
                    {
                        var distance = u.DistanceFromStart + v.Distance;
                        if (!(distance < v.Target.DistanceFromStart)) continue;
                        v.Target.DistanceFromStart = distance;
                        v.Target.Previous = u;
                        priorityQueue.UpdatePriority(v.Target, distance);
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
                return path.ToArray();
            }

            private static WoWPoint[] ExtractPathWoW(Graph graph, WoWPoint target, WoWPoint endReal)
            {
                var path = new List<WoWPoint> {endReal};

                var u = graph.Nodes.First(n => n.Key == target).Value;

                while (u.Previous != null)
                {
                    path.Add(u.Position);
                    u = u.Previous;
                }
                path.Reverse();
                return path.ToArray();
            }

            private static double ExtractDistanceWoW(Graph graph, WoWPoint target)
            {
                var distance = Double.MaxValue;
                var u = graph.Nodes.First(n => n.Key == target).Value;

                while (u.Previous != null)
                {
                    if (Math.Abs(distance - double.MaxValue) < 1)
                        distance = u.DistanceFromStart;
                    else distance += u.DistanceFromStart;
                    u = u.Previous;
                }
                GarrisonButler.Diagnostic("[Dijkstra] Distance to {0}: {1}", target.ToString(), distance);
                return distance;
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