#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GarrisonButler.Libraries;
using Priority_Queue;
using Styx;
using Styx.Pathing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Tripper.Tools.Math;

#endregion

namespace GarrisonButler.ButlerCoroutines
{
    partial class ButlerCoroutine
    {
        #region Dijkstra

        private static Graph _movementGraph;
        private static List<WoWPoint> _zonePoints;

        public static Graph MovementGraph { get { return _movementGraph; } }

        private static NavigationGaB _customNavigation;
        //internal static bool CustomNavigationLoaded = false;
        //internal static NavigationProvider NativeNavigation;
        internal static List<Buildings> BuildingsLoaded; 
        public static void InitializationMove()
        {            
            // Generate Garrison points based on garrison level and buildings level

            if (_zonePoints != null && !IsNewBuildingsToLoad()) return;

            using (var myLock = Styx.StyxWoW.Memory.AcquireFrame())
            {
                GetGarrisonPoints(ref _zonePoints, ref BuildingsLoaded);
                _movementGraph = Dijkstra.GraphFromList(_zonePoints);
            }
        }

        public static bool IsNewBuildingsToLoad()
        {
            
            var buildingNotLoaded = _buildings.GetEmptyIfNull().Where(b => !BuildingsLoaded.Contains((Buildings)b.Id));

            // Return if there is any in range
            return buildingNotLoaded.Any(building => ObjectManager.GetObjectsOfTypeFast<WoWGameObject>()
                .GetEmptyIfNull()
                .FirstOrDefault(o => building.buildingIDs.Contains(o.Entry)) != default(WoWGameObject));
            
        }

        public class Dijkstra
        {
            private static readonly Stopwatch PathGenerationStopwatch = new Stopwatch();

            public static Graph GraphFromList(List<WoWPoint> points)
            {

                const float pathPrecision = 3f;
                var graph = new Graph();
                foreach (var t in points)
                {
                    graph.AddNode(t);
                }

                var graphPoints = graph.Nodes.Keys.ToList();

                var forLoopFilterEndTime = DateTime.Now;
                var count = 0;
                var totalCount = 0;
                using (var myLock = Styx.StyxWoW.Memory.AcquireFrame())
                { 
                    for (var i = 0; i < graphPoints.Count; i++)
                    {
                        var point1 = graphPoints[i];
                        for (var j = i + 1; j < graphPoints.Count; j++)
                        {
                            totalCount++;
                            var point2 = graphPoints[j];
                            var dist = point1.Distance(point2);
                            if (dist > pathPrecision) continue;
                            graph.AddConnection(point1, point2, dist, true);
                            count++;
                        }
                    }
                }
                GarrisonButler.DiagnosticLogTimeTaken("Matching all with distance less than "
                                                      + pathPrecision
                                                      + " returned "
                                                      + count
                                                      + " connections, after " +
                                                      + totalCount
                                                      + " tests between nodes in graph.", DateTime.Now - forLoopFilterEndTime);
                return graph;
            }

            public static WoWPoint ClosestToNodes(WoWPoint point)
            {
                if (_movementGraph != null)
                {
                    var items = _movementGraph.Nodes.Keys.Select(p => new { Point = p, dist = p.Distance(point) });
                    return items.Aggregate((a, b) => a.dist < b.dist ? a : b).Point;
                }
                GarrisonButler.Diagnostic("[Dijkstra] Error Movement Graph null.");
                return default(WoWPoint);
            }
            public static WoWPoint ClosestToNodes(WoWPoint point, List<WoWPoint> blacklisted)
            {
                if (_movementGraph != null)
                {
                    var items = _movementGraph.Nodes.Keys.Where(p=> !blacklisted.Contains(p)).Select(p => new { Point = p, dist = p.Distance(point) });
                    return items.Aggregate((a, b) => a.dist < b.dist ? a : b).Point;
                }
                GarrisonButler.Diagnostic("[Dijkstra] Error Movement Graph null.");
                return default(WoWPoint);
            }

            public static Vector3[] GetPath2(WoWPoint from, WoWPoint to, int currentChild = 0, List<WoWPoint> blacklisted = null)
            {
                InitializationMove();
                PathGenerationStopwatch.Reset();
                PathGenerationStopwatch.Start();
                if (blacklisted == null)
                    blacklisted = new List<WoWPoint>();

                var starting = ClosestToNodes(@from, blacklisted);
                var ending = ClosestToNodes(to, blacklisted);
                GarrisonButler.Diagnostic("Starting path generation. Closest start:{0}, Closest end:{1}", starting, ending);

                if (_movementGraph.Nodes.All(n => n.Key != starting))
                    throw new ArgumentException("Starting node must be in graph.");

                if (_movementGraph.Nodes.All(n => n.Key != ending))
                    throw new ArgumentException("Ending node must be in graph.");

                ProcessGraph(_movementGraph, starting);

                var tempPath = ExtractPath(_movementGraph, ending);

                if (tempPath.Count() <= 0 && currentChild <= 50) // max 50 tries, might be too much in memory or not. 
                {
                    // let's try again with another node.
                    GarrisonButler.Diagnostic("Failed to generate path, blacklisting closest points used and trying again. attempt: {2}, start:{0}, end:{1}", starting, ending, currentChild);

                    blacklisted.Add(starting);
                    blacklisted.Add(ending);
                    return GetPath2(@from, to, currentChild + 1, blacklisted);
                }
                var res = new Vector3[tempPath.Count()];
                for (var index = 0; index < tempPath.Length; index++)
                {
                    res[index] = tempPath[index];
                }
                GarrisonButler.DiagnosticLogTimeTaken("Path generation",
                    (int) PathGenerationStopwatch.ElapsedMilliseconds);
                PathGenerationStopwatch.Stop();
                GarrisonButler.Diagnostic("Contains {0} waypoints.", tempPath.Count());

                return res;
            }

            public static WoWPoint[] GetPathWoW(WoWPoint from, WoWPoint to)
            {
                InitializationMove();
                PathGenerationStopwatch.Reset();
                PathGenerationStopwatch.Start();

                GarrisonButler.Diagnostic("Starting path generation.");

                var starting = ClosestToNodes(@from);
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

                var starting = ClosestToNodes(@from);
                if (_movementGraph.Nodes.All(n => n.Key != starting))
                    throw new ArgumentException("Starting node must be in graph.");

                ProcessGraph(_movementGraph, starting);

                var pathsList = new List<WoWPoint[]>();
                foreach (var t in to)
                {
                    var endingPoint = ClosestToNodes(t);
                    if (_movementGraph.Nodes.All(n => n.Key != endingPoint))
                        throw new ArgumentException("Ending node must be in graph.");
                    pathsList.Add(ExtractPathWoW(_movementGraph, endingPoint, t));
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

                var starting = ClosestToNodes(@from);
                if (_movementGraph.Nodes.All(n => n.Key != starting))
                    throw new ArgumentException("Starting node must be in graph.");

                ProcessGraph(_movementGraph, starting);
                var minDistance = Double.MaxValue;
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

            public static WoWGameObject GetClosestObjectSalesman(WoWPoint from, WoWGameObject[] objectsToArray)
            {
                InitializationMove();
                PathGenerationStopwatch.Reset();
                PathGenerationStopwatch.Start();

                GarrisonButler.Diagnostic("Starting path generation.");
                var starting = ClosestToNodes(@from);
                if (_movementGraph.Nodes.All(n => n.Key != starting))
                    throw new ArgumentException("Starting node must be in graph.");

                //Generating data
                var objectsTo = objectsToArray.OrderBy(o => from.Distance(o.Location)).Take(5).ToArray();
                var objectsCount = objectsTo.Count();
                var vertics = new int[objectsCount + 1];
                var matrix = new double[objectsCount + 1, objectsCount + 1];

                // Adding starting point
                vertics[0] = 0;
                matrix[0, 0] = 0;
                // Adding distance from starting point to all objects
                ProcessGraph(_movementGraph, starting);
                for (int index = 0; index < objectsTo.Length; index++)
                {
                    var gameObject = objectsTo[index];
                    var endPoint = ClosestToNodes(gameObject.Location);
                    if (_movementGraph.Nodes.All(n => n.Key != endPoint))
                        throw new ArgumentException("Ending node must be in graph.");
                    var distance = ExtractDistanceWoW(_movementGraph, endPoint);
                    matrix[0, index + 1] = distance;
                    matrix[index + 1, 0] = distance;
                }

                // Adding distances from every points to all others
                for (int index1 = 0; index1 < objectsTo.Length; index1++)
                {
                    vertics[index1 + 1] = index1 + 1;

                    starting = ClosestToNodes(objectsTo[index1].Location);
                    if (_movementGraph.Nodes.All(n => n.Key != starting))
                        throw new ArgumentException("Starting node must be in graph.");
                    ProcessGraph(_movementGraph, starting);

                    for (int index2 = index1; index2 < objectsTo.Length; index2++)
                    {
                        if (index1 == index2)
                            matrix[index1 + 1, index2 + 1] = 0.0;
                        else
                        {
                            var endPoint = ClosestToNodes(objectsTo[index2].Location);
                            if (_movementGraph.Nodes.All(n => n.Key != endPoint))
                                throw new ArgumentException("Ending node must be in graph.");
                            var distance = ExtractDistanceWoW(_movementGraph, endPoint);
                            matrix[index1 + 1, index2 + 1] = distance;
                            matrix[index2 + 1, index1 + 1] = distance;
                        }
                    }
                    GarrisonButler.Diagnostic("[Salesman] Processed node in {0}ms", PathGenerationStopwatch.ElapsedMilliseconds);
                }
                double cost;
                var salesman = new Salesman(vertics, matrix);
                var route = salesman.Solve(out cost).ToArray();

                PathGenerationStopwatch.Stop();
                GarrisonButler.Diagnostic("[Salesman] Tour found in {0}ms, cost={1}, route:", PathGenerationStopwatch.ElapsedMilliseconds, cost);
                ObjectDumper.WriteToHb(route, 3);

                return objectsTo[route[1] - 1];
            }
            public static WoWGameObject GetClosestObjectSalesmanHB(WoWPoint from, WoWGameObject[] objectsToArray)
            {
                PathGenerationStopwatch.Reset();
                PathGenerationStopwatch.Start();

                GarrisonButler.Diagnostic("Starting salesman algorithm.");

                //Generating data
                var objectsTo = objectsToArray.OrderBy(o => from.Distance(o.Location)).Take(5).ToArray();
                var objectsCount = objectsTo.Count();
                var vertics = new int[objectsCount + 1];
                var matrix = new double[objectsCount + 1, objectsCount + 1];

                // Adding starting point
                vertics[0] = 0;
                matrix[0, 0] = 0;
                // Adding distance from starting point to all objects
                for (int index = 0; index < objectsTo.Length; index++)
                {
                    var gameObject = objectsTo[index];
                    var distance = Navigator.PathDistance(@from, gameObject.Location) ?? float.MaxValue;
                    matrix[0, index + 1] = (float)distance;
                    matrix[index + 1, 0] = (float)distance;
                }

                // Adding distances from every points to all others
                for (int index1 = 0; index1 < objectsTo.Length; index1++)
                {
                    vertics[index1 + 1] = index1 + 1;

                    for (int index2 = index1; index2 < objectsTo.Length; index2++)
                    {
                        if (index1 == index2)
                            matrix[index1 + 1, index2 + 1] = 0.0;
                        else
                        {
                            var distance = Navigator.PathDistance(@from, objectsTo[index2].Location) ?? float.MaxValue;
                            matrix[index1 + 1, index2 + 1] = distance;
                            matrix[index2 + 1, index1 + 1] = distance;
                        }
                    }
                    GarrisonButler.Diagnostic("[Salesman] Processed node in {0}ms", PathGenerationStopwatch.ElapsedMilliseconds);
                }
                double cost;
                var salesman = new Salesman(vertics, matrix);
                var route = salesman.Solve(out cost).ToArray();

                PathGenerationStopwatch.Stop();
                GarrisonButler.Diagnostic("[Salesman] Tour found in {0}ms, cost={1}, route:", PathGenerationStopwatch.ElapsedMilliseconds, cost);
                ObjectDumper.WriteToHb(route, 3);

                return objectsTo[route[1] - 1];
            }

            private static void ProcessGraph(Graph graph, WoWPoint startingNode)
            {
                // Initialization of the data
                var priorityQueue = new HeapPriorityQueue<Node>(graph.Nodes.Count);
                foreach (var node in graph.Nodes.Values)
                {
                    node.DistanceFromStart = Double.PositiveInfinity;
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
                    if (Math.Abs(distance - Double.MaxValue) < 1)
                        distance = u.DistanceFromStart;
                    else distance += u.DistanceFromStart;
                    u = u.Previous;
                }
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