using System;
using System.Collections.Generic;
using System.Linq;
using Styx;
using Styx.Common;

namespace GarrisonBuddy
{
    partial class Coroutine
    {
        #region Dijkstra

        private static Graph _movementGraph;

        public static void InitializationMove()
        {
            _movementGraph = Dijkstra.GraphFromList(AllPoints);
        }

        public static class Dijkstra
        {
            public static Graph GraphFromList(List<WoWPoint> points)
            {
                var graph = new Graph();
                foreach (WoWPoint t in points)
                {
                    graph.AddNode(t);
                }
                for (int i = 0; i < points.Count; i++)
                {
                    WoWPoint point1 = points[i];
                    for (int j = i + 1; j < points.Count; j++)
                    {
                        WoWPoint point2 = points[j];
                        float dist = point1.Distance(point2);
                        if (dist < 2)
                        {
                            graph.AddConnection(point1, point2, dist, true);
                        }
                    }
                }
                return graph;
            }

            private static WoWPoint ClosestToNodes(WoWPoint point)
            {
                var closest = new WoWPoint();

                float Mindistance = float.PositiveInfinity;

                foreach (var node in _movementGraph.Nodes)
                {
                    float distance = node.Key.Distance(point);
                    if (distance < Mindistance)
                    {
                        closest = node.Key;
                        Mindistance = distance;
                    }
                }
                return closest;
            }

            public static List<WoWPoint> GetPath(WoWPoint from, WoWPoint to)
            {
                var starting = ClosestToNodes(from);
                var ending = ClosestToNodes(to);
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
                if (targetNode == this) throw new ArgumentException("Node may not connect to itself.");
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

        internal static List<WoWPoint> AllPoints = new List<WoWPoint>
        {
            new WoWPoint(1934.541, 345.6305, 91.65073),
            new WoWPoint(1935.138, 344.3627, 90.71031),
            new WoWPoint(1935.406, 342.9958, 90.52305),
            new WoWPoint(1936.287, 342.0284, 90.40526),
            new WoWPoint(1936.56, 340.6409, 90.28046),
            new WoWPoint(1936.853, 339.2363, 90.28046),
            new WoWPoint(1937.214, 337.8401, 90.28046),
            new WoWPoint(1937.529, 336.4481, 90.28046),
            new WoWPoint(1937.718, 335.033, 90.28046),
            new WoWPoint(1937.864, 333.6196, 90.28046),
            new WoWPoint(1938, 332.1949, 90.40546),
            new WoWPoint(1938.138, 330.7595, 90.40546),
            new WoWPoint(1938.268, 329.2742, 90.40546),
            new WoWPoint(1938.367, 327.8286, 90.40546),
            new WoWPoint(1939, 326.6284, 90.40546),
            new WoWPoint(1939.152, 325.3488, 90.40546),
            new WoWPoint(1938.643, 324.0077, 90.40546),
            new WoWPoint(1937.938, 322.7435, 90.40546),
            new WoWPoint(1936.926, 321.6846, 90.20951),
            new WoWPoint(1935.804, 320.7565, 89.50318),
            new WoWPoint(1934.731, 319.7964, 89.50318),
            new WoWPoint(1933.574, 318.9976, 89.50318),
            new WoWPoint(1934.153, 317.6671, 89.50318),
            new WoWPoint(1934.862, 316.364, 89.50318),
            new WoWPoint(1935.768, 315.2368, 89.50318),
            new WoWPoint(1936.782, 314.1923, 89.78174),
            new WoWPoint(1937.809, 313.1218, 90.40478),
            new WoWPoint(1938.679, 311.9375, 90.40478),
            new WoWPoint(1939.647, 310.8961, 90.40478),
            new WoWPoint(1940.283, 309.6512, 90.40478),
            new WoWPoint(1939.528, 308.3489, 90.40478),
            new WoWPoint(1938.644, 307.1501, 90.40478),
            new WoWPoint(1937.614, 306.1636, 90.40478),
            new WoWPoint(1936.41, 305.2844, 90.40478),
            new WoWPoint(1935.203, 304.4708, 90.40478),
            new WoWPoint(1933.964, 303.6405, 90.40478),
            new WoWPoint(1932.74, 302.8133, 90.40478),
            new WoWPoint(1931.511, 301.9821, 90.40478),
            new WoWPoint(1930.334, 301.1862, 90.38761),
            new WoWPoint(1929.081, 300.3267, 90.38761),
            new WoWPoint(1927.905, 299.5176, 90.38761),
            new WoWPoint(1926.688, 298.6808, 89.89787),
            new WoWPoint(1925.459, 297.8361, 88.96585),
            new WoWPoint(1924.202, 296.9715, 88.96585),
            new WoWPoint(1923.156, 296.0428, 88.96585),
            new WoWPoint(1922.526, 294.7978, 88.96585),
            new WoWPoint(1922.869, 293.309, 88.96585),
            new WoWPoint(1924.17, 292.7691, 88.96585),
            new WoWPoint(1925.041, 291.5555, 88.96585),
            new WoWPoint(1926.393, 291.1665, 88.96585),
            new WoWPoint(1927.808, 290.9275, 88.96585),
            new WoWPoint(1929.271, 290.6802, 88.96585),
            new WoWPoint(1930.741, 290.4319, 88.96585),
            new WoWPoint(1932.226, 290.1469, 88.96585),
            new WoWPoint(1933.685, 289.8384, 88.96585),
            new WoWPoint(1935.094, 289.5317, 88.96585),
            new WoWPoint(1936.59, 289.2002, 88.96585),
            new WoWPoint(1938.073, 288.871, 88.96585),
            new WoWPoint(1939.495, 288.5555, 88.96585),
            new WoWPoint(1940.978, 288.2264, 88.96585),
            new WoWPoint(1942.392, 287.9124, 88.96585),
            new WoWPoint(1943.793, 287.6015, 88.96585),
            new WoWPoint(1944.804, 287.377, 88.96585),
            new WoWPoint(1943.477, 286.7086, 88.96585),
            new WoWPoint(1942.177, 285.9071, 88.96585),
            new WoWPoint(1940.862, 286.222, 88.96585),
            new WoWPoint(1939.583, 286.8886, 88.96585),
            new WoWPoint(1938.09, 287.2899, 88.96585),
            new WoWPoint(1936.62, 287.6452, 88.96585),
            new WoWPoint(1935.142, 287.9976, 88.96585),
            new WoWPoint(1933.704, 288.2973, 88.96585),
            new WoWPoint(1932.169, 288.5811, 88.96585),
            new WoWPoint(1930.708, 288.8455, 88.96585),
            new WoWPoint(1929.185, 289.1156, 88.96585),
            new WoWPoint(1927.706, 289.226, 88.96585),
            new WoWPoint(1926.159, 289.2649, 88.96585),
            new WoWPoint(1924.732, 289.2166, 88.96585),
            new WoWPoint(1923.194, 289.1478, 88.96585),
            new WoWPoint(1921.713, 289.264, 88.96585),
            new WoWPoint(1920.342, 289.9118, 88.96585),
            new WoWPoint(1919.008, 289.7534, 88.96585),
            new WoWPoint(1917.599, 289.1839, 88.96585),
            new WoWPoint(1916.203, 289.2923, 88.96585),
            new WoWPoint(1914.872, 288.6186, 88.96585),
            new WoWPoint(1913.467, 287.907, 88.96434),
            new WoWPoint(1912.151, 287.2166, 89.00854),
            new WoWPoint(1910.849, 286.49, 89.00856),
            new WoWPoint(1909.488, 285.7112, 89.00932),
            new WoWPoint(1908.153, 284.93, 89.00966),
            new WoWPoint(1906.901, 284.187, 88.40656),
            new WoWPoint(1906.186, 283.7603, 87.74438),
            new WoWPoint(1905.417, 283.3, 87.0319),
            new WoWPoint(1904.697, 282.8666, 86.36382),
            new WoWPoint(1904.004, 282.4437, 85.71813),
            new WoWPoint(1903.308, 282.0117, 85.0668),
            new WoWPoint(1902.633, 281.5861, 84.43212),
            new WoWPoint(1901.893, 281.1062, 83.73083),
            new WoWPoint(1900.678, 280.2664, 82.57803),
            new WoWPoint(1899.979, 279.777, 81.9174),
            new WoWPoint(1899.299, 279.2949, 81.27321),
            new WoWPoint(1898.038, 278.3997, 80.07674),
            new WoWPoint(1896.857, 277.56, 78.97037),
            new WoWPoint(1896.109, 277.0287, 78.38886),
            new WoWPoint(1894.917, 276.1815, 77.46148),
            new WoWPoint(1894.201, 275.6903, 76.74723),
            new WoWPoint(1892.913, 274.8211, 76.6397),
            new WoWPoint(1891.651, 273.9885, 76.6397),
            new WoWPoint(1890.373, 273.1676, 76.6397),
            new WoWPoint(1889.395, 272.5397, 76.6397),
            new WoWPoint(1887.365, 271.5459, 76.6397),
            new WoWPoint(1885.939, 270.6685, 76.6397),
            new WoWPoint(1884.175, 269.9364, 76.6397),
            new WoWPoint(1882.47, 269.4955, 76.6397),
            new WoWPoint(1880.651, 269.4199, 76.6397),
            new WoWPoint(1878.952, 269.5559, 76.6397),
            new WoWPoint(1877.122, 269.8405, 76.6397),
            new WoWPoint(1875.456, 270.2069, 76.6397),
            new WoWPoint(1873.775, 270.6781, 76.6397),
            new WoWPoint(1872.136, 271.4067, 76.6397),
            new WoWPoint(1870.873, 272.5655, 76.6397),
            new WoWPoint(1870.274, 274.2869, 76.6397),
            new WoWPoint(1869.779, 276.041, 76.6397),
            new WoWPoint(1869.369, 277.7717, 76.6397),
            new WoWPoint(1869, 279.5267, 76.6397),
            new WoWPoint(1868.731, 281.2848, 77.48329),
            new WoWPoint(1868.508, 283.0494, 78.89693),
            new WoWPoint(1868.325, 284.7891, 80.28771),
            new WoWPoint(1868.189, 286.714, 81.64222),
            new WoWPoint(1868.119, 288.3882, 81.64882),
            new WoWPoint(1868.091, 290.1667, 81.65525),
            new WoWPoint(1868.063, 292.0775, 81.66241),
            new WoWPoint(1867.996, 293.899, 81.65234),
            new WoWPoint(1867.897, 295.7044, 81.65981),
            new WoWPoint(1867.788, 297.5976, 81.65981),
            new WoWPoint(1867.671, 299.4018, 81.65981),
            new WoWPoint(1867.542, 301.1759, 81.65981),
            new WoWPoint(1867.403, 303.0523, 82.14693),
            new WoWPoint(1867.318, 304.1663, 82.44212),
            new WoWPoint(1867.462, 305.601, 82.5976),
            new WoWPoint(1866.646, 307.1908, 82.59385),
            new WoWPoint(1864.968, 307.5127, 82.60416),
            new WoWPoint(1863.512, 306.3431, 82.62677),
            new WoWPoint(1862.838, 304.7203, 82.62998),
            new WoWPoint(1862.668, 302.8763, 82.4929),
            new WoWPoint(1862.513, 300.9716, 82.09917),
            new WoWPoint(1863.009, 299.6397, 81.66051),
            new WoWPoint(1864.153, 298.3357, 81.67035),
            new WoWPoint(1864.981, 296.6229, 81.66055),
            new WoWPoint(1865.644, 294.8657, 81.66055),
            new WoWPoint(1866.004, 293.0189, 81.63181),
            new WoWPoint(1866.345, 291.2433, 81.66109),
            new WoWPoint(1866.673, 289.5399, 81.65545),
            new WoWPoint(1867.047, 287.5912, 81.64641),
            new WoWPoint(1867.403, 285.6687, 81.05408),
            new WoWPoint(1867.729, 283.8156, 79.5627),
            new WoWPoint(1868.035, 282.0633, 78.15271),
            new WoWPoint(1868.233, 280.1647, 76.63975),
            new WoWPoint(1868.1, 278.2309, 76.63975),
            new WoWPoint(1867.736, 276.3863, 76.63975),
            new WoWPoint(1867.223, 274.7297, 76.63975),
            new WoWPoint(1866.539, 273.0252, 76.63975),
            new WoWPoint(1865.691, 271.3128, 76.63975),
            new WoWPoint(1864.766, 269.5406, 76.63975),
            new WoWPoint(1863.882, 267.8794, 76.63975),
            new WoWPoint(1863.094, 266.4003, 76.63975),
            new WoWPoint(1862.155, 264.6357, 76.63975),
            new WoWPoint(1861.269, 262.9589, 76.63975),
            new WoWPoint(1860.364, 261.2426, 76.63975),
            new WoWPoint(1859.534, 259.6694, 76.63975),
            new WoWPoint(1858.613, 257.9116, 76.63975),
            new WoWPoint(1857.846, 256.3396, 76.63975),
            new WoWPoint(1857.069, 254.5457, 76.63975),
            new WoWPoint(1856.413, 252.8137, 76.63975),
            new WoWPoint(1855.83, 250.902, 76.63975),
            new WoWPoint(1855.438, 249.1523, 76.63975),
            new WoWPoint(1855.042, 247.2676, 76.63975),
            new WoWPoint(1854.65, 245.3972, 76.63975),
            new WoWPoint(1854.252, 243.4981, 76.63975),
            new WoWPoint(1853.854, 241.599, 76.63975),
            new WoWPoint(1853.456, 239.8504, 76.63975),
            new WoWPoint(1852.906, 237.8832, 76.63975),
            new WoWPoint(1852.352, 236.131, 76.63975),
            new WoWPoint(1851.731, 234.3551, 76.63638),
            new WoWPoint(1850.929, 232.5422, 76.61283),
            new WoWPoint(1849.901, 231.0574, 76.512),
            new WoWPoint(1848.509, 229.5242, 76.33594),
            new WoWPoint(1847.095, 228.2602, 76.13227),
            new WoWPoint(1845.593, 227.1301, 75.86497),
            new WoWPoint(1844.028, 226.1127, 75.54622),
            new WoWPoint(1842.445, 225.1218, 75.19227),
            new WoWPoint(1840.84, 224.14, 74.81817),
            new WoWPoint(1839.092, 223.0829, 74.3727),
            new WoWPoint(1837.469, 222.1023, 73.98505),
            new WoWPoint(1835.852, 221.1388, 73.65218),
            new WoWPoint(1834.042, 220.1918, 73.30492),
            new WoWPoint(1832.417, 219.3995, 73.02544),
            new WoWPoint(1830.619, 218.5591, 72.7402),
            new WoWPoint(1828.777, 218.0296, 72.48987),
            new WoWPoint(1826.797, 218.1825, 72.29936),
            new WoWPoint(1825.066, 219.1, 72.14537),
            new WoWPoint(1823.713, 220.636, 72.16131),
            new WoWPoint(1822.845, 222.2212, 72.17426),
            new WoWPoint(1823.07, 221.0511, 72.17426),
            new WoWPoint(1823.801, 219.1746, 72.08892),
            new WoWPoint(1824.566, 217.4399, 72.08738),
            new WoWPoint(1825.373, 215.596, 72.08925),
            new WoWPoint(1825.929, 213.8295, 72.08636),
            new WoWPoint(1826.426, 211.9389, 72.04757),
            new WoWPoint(1826.896, 209.9051, 72.01084),
            new WoWPoint(1827.355, 208.0956, 71.96185),
            new WoWPoint(1827.885, 206.1685, 71.98461),
            new WoWPoint(1828.358, 204.4998, 72.15602),
            new WoWPoint(1827.621, 205.5628, 71.98589),
            new WoWPoint(1826.611, 207.4359, 71.96057),
            new WoWPoint(1826.39, 209.2922, 71.97974),
            new WoWPoint(1827.015, 211.2356, 72.06022),
            new WoWPoint(1829.204, 213.8666, 72.34122),
            new WoWPoint(1830.988, 215.056, 72.58929),
            new WoWPoint(1832.826, 216.0139, 72.87608),
            new WoWPoint(1834.612, 216.9123, 73.19056),
            new WoWPoint(1836.34, 217.7621, 73.50081),
            new WoWPoint(1838.2, 218.6768, 73.86983),
            new WoWPoint(1839.915, 219.5201, 74.2431),
            new WoWPoint(1841.735, 220.4153, 74.64281),
            new WoWPoint(1843.516, 221.291, 75.01785),
            new WoWPoint(1845.323, 222.1797, 75.39172),
            new WoWPoint(1847.157, 223.0814, 75.74406),
            new WoWPoint(1848.976, 223.9758, 76.0302),
            new WoWPoint(1850.704, 224.8256, 76.26397),
            new WoWPoint(1852.445, 225.6819, 76.43136),
            new WoWPoint(1854.377, 226.5788, 76.58401),
            new WoWPoint(1856.276, 227.0847, 76.63471),
            new WoWPoint(1858.289, 227.1893, 76.6384),
            new WoWPoint(1860.113, 226.8993, 76.6384),
            new WoWPoint(1862.058, 226.228, 76.6384),
            new WoWPoint(1863.947, 225.4118, 76.6384),
            new WoWPoint(1865.775, 224.3252, 76.6384),
            new WoWPoint(1867.221, 222.9497, 76.67688),
            new WoWPoint(1868.368, 221.2448, 76.72533),
            new WoWPoint(1869.227, 219.4891, 76.80381),
            new WoWPoint(1870.043, 217.658, 76.92509),
            new WoWPoint(1870.758, 215.7295, 77.06325),
            new WoWPoint(1871.237, 213.6848, 77.21829),
            new WoWPoint(1871.471, 211.5963, 77.37345),
            new WoWPoint(1871.468, 209.5987, 77.53197),
            new WoWPoint(1871.336, 207.5303, 77.67339),
            new WoWPoint(1870.888, 205.5407, 77.79637),
            new WoWPoint(1870.542, 203.6142, 77.91109),
            new WoWPoint(1871.257, 201.6723, 77.99248),
            new WoWPoint(1872.645, 200.2214, 78.05074),
            new WoWPoint(1874.335, 198.8055, 78.06548),
            new WoWPoint(1876.003, 197.4778, 78.05643),
            new WoWPoint(1877.469, 196.2988, 78.53111),
            new WoWPoint(1878.617, 195.2676, 78.93838),
            new WoWPoint(1877.384, 196.4646, 78.40937),
            new WoWPoint(1875.552, 197.5874, 78.05636),
            new WoWPoint(1873.505, 197.9102, 78.07671),
            new WoWPoint(1871.472, 197.6425, 78.09043),
            new WoWPoint(1869.492, 196.8242, 78.11261),
            new WoWPoint(1867.625, 195.7971, 78.10568),
            new WoWPoint(1865.884, 194.6999, 78.12531),
            new WoWPoint(1864.372, 193.2941, 78.163),
            new WoWPoint(1863.122, 191.4614, 78.24471),
            new WoWPoint(1862.149, 189.7151, 78.32972),
            new WoWPoint(1861.41, 187.7862, 78.41985),
            new WoWPoint(1860.934, 185.6936, 78.56492),
            new WoWPoint(1860.701, 183.5917, 78.70671),
            new WoWPoint(1860.751, 181.4921, 78.78603),
            new WoWPoint(1860.985, 179.4624, 78.87417),
            new WoWPoint(1861.25, 177.4512, 78.92635),
            new WoWPoint(1861.537, 175.2797, 79.05644),
            new WoWPoint(1861.823, 173.1082, 79.24771),
            new WoWPoint(1862.082, 171.1111, 79.47551),
            new WoWPoint(1862.353, 168.9228, 79.65809),
            new WoWPoint(1862.581, 166.7889, 79.82732),
            new WoWPoint(1862.502, 164.7071, 79.93683),
            new WoWPoint(1862.169, 162.7657, 80.01546),
            new WoWPoint(1861.648, 160.7621, 79.84775),
            new WoWPoint(1860.76, 158.8759, 79.60757),
            new WoWPoint(1859.597, 156.9338, 79.25523),
            new WoWPoint(1858.552, 155.2467, 78.94477),
            new WoWPoint(1857.6, 153.3269, 78.6735),
            new WoWPoint(1857.049, 151.3019, 78.39735),
            new WoWPoint(1856.977, 149.0847, 78.29149),
            new WoWPoint(1856.963, 147.0121, 78.29149),
            new WoWPoint(1857.067, 144.9879, 78.29149),
            new WoWPoint(1857.817, 142.9718, 78.29149),
            new WoWPoint(1859.008, 141.2407, 78.29149),
            new WoWPoint(1858.454, 142.9063, 78.29149),
            new WoWPoint(1857.559, 144.9211, 78.29149),
            new WoWPoint(1856.757, 146.8165, 78.29149),
            new WoWPoint(1856.277, 148.7881, 78.29149),
            new WoWPoint(1856.377, 151.0179, 78.37331),
            new WoWPoint(1856.891, 152.9258, 78.58342),
            new WoWPoint(1858.055, 154.8887, 78.84865),
            new WoWPoint(1859.511, 156.2964, 79.18159),
            new WoWPoint(1861.375, 157.6026, 79.58561),
            new WoWPoint(1863.16, 158.5653, 79.93066),
            new WoWPoint(1865.096, 159.3012, 80.24175),
            new WoWPoint(1867.22, 159.8311, 80.29958),
            new WoWPoint(1869.429, 160.0173, 80.39165),
            new WoWPoint(1871.595, 159.7221, 80.49672),
            new WoWPoint(1873.596, 158.9941, 80.63857),
            new WoWPoint(1875.329, 158.1898, 80.78615),
            new WoWPoint(1877.402, 157.0178, 81.01435),
            new WoWPoint(1879.238, 155.9363, 81.16399),
            new WoWPoint(1881.049, 154.8689, 81.3418),
            new WoWPoint(1882.86, 153.8016, 81.51586),
            new WoWPoint(1884.776, 152.6725, 81.68163),
            new WoWPoint(1886.688, 151.5455, 81.80525),
            new WoWPoint(1888.474, 150.4931, 81.94233),
            new WoWPoint(1890.386, 149.366, 82.04492),
            new WoWPoint(1892.159, 148.3211, 82.15762),
            new WoWPoint(1894.051, 147.1878, 82.29243),
            new WoWPoint(1895.852, 146.1039, 82.42487),
            new WoWPoint(1897.779, 144.8881, 82.57092),
            new WoWPoint(1899.44, 143.6647, 82.74178),
            new WoWPoint(1901.202, 142.243, 82.88981),
            new WoWPoint(1902.429, 141.2129, 82.96387),
            new WoWPoint(1903.699, 140.0748, 83.03761),
            new WoWPoint(1905.231, 138.657, 83.11783),
            new WoWPoint(1906.9, 137.0634, 83.18495),
            new WoWPoint(1908.476, 135.4799, 83.23169),
            new WoWPoint(1909.999, 133.9258, 83.27137),
            new WoWPoint(1911.465, 132.4195, 83.29646),
            new WoWPoint(1913.01, 130.8054, 83.31306),
            new WoWPoint(1914.525, 129.2236, 83.33146),
            new WoWPoint(1916.055, 127.6971, 83.35045),
            new WoWPoint(1917.653, 126.1998, 83.37271),
            new WoWPoint(1919.307, 124.6756, 83.39737),
            new WoWPoint(1920.861, 123.2437, 83.42635),
            new WoWPoint(1922.45, 121.7793, 83.45206),
            new WoWPoint(1924.158, 120.2053, 83.48397),
            new WoWPoint(1925.758, 118.731, 83.51707),
            new WoWPoint(1927.347, 117.2665, 83.54658),
            new WoWPoint(1929.055, 115.6926, 83.59225),
            new WoWPoint(1930.741, 114.1385, 83.63953),
            new WoWPoint(1932.31, 112.6741, 83.69673),
            new WoWPoint(1933.828, 111.2306, 83.78054),
            new WoWPoint(1935.409, 109.715, 83.88113),
            new WoWPoint(1936.99, 108.1993, 83.99892),
            new WoWPoint(1938.667, 106.5921, 84.15204),
            new WoWPoint(1936.962, 107.9206, 84.01778),
            new WoWPoint(1935.061, 109.4028, 83.8941),
            new WoWPoint(1933.357, 110.7313, 83.80161),
            new WoWPoint(1932.291, 112.6778, 83.69886),
            new WoWPoint(1931.539, 114.791, 83.61983),
            new WoWPoint(1930.494, 116.7321, 83.55584),
            new WoWPoint(1929.371, 118.6467, 83.54561),
            new WoWPoint(1927.916, 120.0246, 83.51991),
            new WoWPoint(1925.484, 119.932, 83.50246),
            new WoWPoint(1923.316, 119.4002, 83.58598),
            new WoWPoint(1921.287, 118.6083, 83.47388),
            new WoWPoint(1919.369, 117.2306, 83.47054),
            new WoWPoint(1917.829, 115.6752, 83.47482),
            new WoWPoint(1916.312, 114.0023, 83.48257),
            new WoWPoint(1914.81, 112.3474, 83.49839),
            new WoWPoint(1913.349, 110.736, 83.50317),
            new WoWPoint(1911.733, 109.0276, 83.51458),
            new WoWPoint(1910.097, 107.5714, 83.52279),
            new WoWPoint(1909.029, 106.649, 83.52497),
            new WoWPoint(1909.49, 105.7605, 83.52497),
            new WoWPoint(1910.154, 103.5195, 83.52497),
            new WoWPoint(1910.303, 101.239, 83.52497),
            new WoWPoint(1909.695, 99.15549, 83.52497),
            new WoWPoint(1908.375, 97.359, 83.52497),
            new WoWPoint(1906.435, 96.66339, 83.52497),
            new WoWPoint(1904.343, 97.78813, 83.52497),
            new WoWPoint(1903.148, 98.71612, 83.52497),
            new WoWPoint(1904.358, 100.0397, 83.52497),
            new WoWPoint(1906.458, 100.168, 83.52497),
            new WoWPoint(1908.518, 100.5196, 83.52497),
            new WoWPoint(1910.275, 101.7882, 83.52497),
            new WoWPoint(1911.153, 103.743, 83.52497),
            new WoWPoint(1911.894, 105.9593, 83.52497),
            new WoWPoint(1912.543, 108.1125, 83.51871),
            new WoWPoint(1913.127, 110.3448, 83.5079),
            new WoWPoint(1913.285, 112.6555, 83.49095),
            new WoWPoint(1911.5, 114.0876, 83.47842),
            new WoWPoint(1910.507, 115.9816, 83.46043),
            new WoWPoint(1910.14, 118.1707, 83.43066),
            new WoWPoint(1909.742, 120.5782, 83.3985),
            new WoWPoint(1909.535, 122.7215, 83.37262),
            new WoWPoint(1909.649, 124.6957, 83.35102),
            new WoWPoint(1908.947, 126.8787, 83.33102),
            new WoWPoint(1908.112, 129.1715, 83.31202),
            new WoWPoint(1907.282, 131.3251, 83.28592),
            new WoWPoint(1906.473, 133.4078, 83.25121),
            new WoWPoint(1905.573, 135.5966, 83.19274),
            new WoWPoint(1904.65, 137.6312, 83.12756),
            new WoWPoint(1903.588, 139.8775, 83.0407),
            new WoWPoint(1902.659, 141.8446, 82.95116),
            new WoWPoint(1901.653, 144.0032, 82.84799),
            new WoWPoint(1900.809, 146.0391, 82.74949),
            new WoWPoint(1899.098, 147.6208, 82.58946),
            new WoWPoint(1897.288, 149.023, 82.40775),
            new WoWPoint(1895.573, 150.7772, 82.23333),
            new WoWPoint(1894.211, 152.5482, 82.19233),
            new WoWPoint(1892.402, 154.1531, 81.78342),
            new WoWPoint(1890.304, 154.8693, 81.61021),
            new WoWPoint(1887.887, 155.4442, 81.75059),
            new WoWPoint(1885.725, 155.9507, 81.65755),
            new WoWPoint(1883.369, 156.503, 81.54059),
            new WoWPoint(1881.116, 157.0308, 81.17466),
            new WoWPoint(1878.956, 157.5423, 81.07249),
            new WoWPoint(1876.731, 158.3788, 80.86857),
            new WoWPoint(1874.67, 159.5425, 80.6509),
            new WoWPoint(1872.6, 160.8608, 80.47842),
            new WoWPoint(1870.729, 162.3089, 80.33181),
            new WoWPoint(1869.042, 163.796, 80.21246),
            new WoWPoint(1867.244, 165.467, 80.07909),
            new WoWPoint(1865.582, 167.1314, 79.93468),
            new WoWPoint(1863.967, 168.8403, 79.75288),
            new WoWPoint(1862.42, 170.5335, 79.54565),
            new WoWPoint(1861.298, 172.4832, 79.28014),
            new WoWPoint(1861.413, 174.7882, 79.08677),
            new WoWPoint(1861.462, 175.7866, 79.01937),
            new WoWPoint(1861.57, 177.9595, 78.91611),
            new WoWPoint(1861.775, 180.493, 78.83663),
            new WoWPoint(1862.257, 182.703, 78.75079),
            new WoWPoint(1863.034, 185.078, 78.5795),
            new WoWPoint(1863.853, 187.4545, 78.40856),
            new WoWPoint(1864.582, 189.5357, 78.26599),
            new WoWPoint(1865.482, 191.8186, 78.20941),
            new WoWPoint(1866.511, 193.9167, 78.14669),
            new WoWPoint(1867.664, 196.084, 78.1054),
            new WoWPoint(1869.307, 197.6233, 78.10963),
            new WoWPoint(1870.537, 199.4003, 78.07442),
            new WoWPoint(1870.536, 201.7375, 77.99052),
            new WoWPoint(1870.551, 202.8252, 77.94403),
            new WoWPoint(1870.763, 205.0487, 77.82782),
            new WoWPoint(1870.901, 206.1574, 77.75645),
            new WoWPoint(1871.183, 208.4183, 77.61398),
            new WoWPoint(1871.48, 210.796, 77.43775),
            new WoWPoint(1871.884, 213.1702, 77.28011),
            new WoWPoint(1872.565, 215.5123, 77.15193),
            new WoWPoint(1872.129, 217.8081, 76.98512),
            new WoWPoint(1871.677, 220.1204, 76.84028),
            new WoWPoint(1871.644, 221.1194, 76.78218),
            new WoWPoint(1871.844, 223.4736, 76.70782),
            new WoWPoint(1872.299, 225.7809, 76.6601),
            new WoWPoint(1872.535, 226.8125, 76.64946),
            new WoWPoint(1873.688, 228.677, 76.64234),
            new WoWPoint(1875.647, 229.8967, 76.6408),
            new WoWPoint(1877.715, 231.1912, 76.6408),
            new WoWPoint(1878.575, 231.7292, 76.6408),
            new WoWPoint(1880.541, 232.939, 76.6408),
            new WoWPoint(1882.786, 234.0682, 76.6408),
            new WoWPoint(1885.148, 234.9676, 76.6408),
            new WoWPoint(1887.341, 235.7326, 76.6408),
            new WoWPoint(1889.658, 236.0174, 76.6408),
            new WoWPoint(1892.006, 235.3115, 76.76011),
            new WoWPoint(1891.768, 236.748, 76.6396),
            new WoWPoint(1889.447, 237.6934, 76.6396),
            new WoWPoint(1887.498, 238.9184, 76.6396),
            new WoWPoint(1887.025, 239.8159, 76.6396),
            new WoWPoint(1886.161, 241.9984, 76.6396),
            new WoWPoint(1885.578, 244.3363, 76.6396),
            new WoWPoint(1885.276, 246.8166, 76.6396),
            new WoWPoint(1885.143, 249.2383, 76.6396),
            new WoWPoint(1885.052, 251.6327, 76.6396),
            new WoWPoint(1884.982, 254.0866, 76.6396),
            new WoWPoint(1884.976, 256.5556, 76.6396),
            new WoWPoint(1885.183, 259.0013, 76.6396),
            new WoWPoint(1885.64, 261.3979, 76.6396),
            new WoWPoint(1886.231, 263.7802, 76.6396),
            new WoWPoint(1886.926, 266.1031, 76.6396),
            new WoWPoint(1887.331, 267.0966, 76.6396),
            new WoWPoint(1888.376, 269.3009, 76.6396),
            new WoWPoint(1889.687, 271.3925, 76.6396),
            new WoWPoint(1891.101, 273.3445, 76.6396),
            new WoWPoint(1892.645, 275.1958, 76.6396),
            new WoWPoint(1893.41, 276.0293, 76.6396),
            new WoWPoint(1895.108, 277.6564, 78.06069),
            new WoWPoint(1895.954, 278.4084, 78.76936),
            new WoWPoint(1896.691, 278.9923, 79.43527),
            new WoWPoint(1897.571, 279.632, 80.27599),
            new WoWPoint(1898.329, 280.1632, 80.99238),
            new WoWPoint(1899.184, 280.7364, 81.78915),
            new WoWPoint(1899.996, 281.2676, 82.5406),
            new WoWPoint(1900.845, 281.8224, 83.32634),
            new WoWPoint(1901.657, 282.3532, 84.09698),
            new WoWPoint(1902.482, 282.8919, 84.87995),
            new WoWPoint(1903.405, 283.495, 85.75648),
            new WoWPoint(1904.18, 284.0016, 86.49264),
            new WoWPoint(1904.98, 284.5242, 87.25238),
            new WoWPoint(1905.78, 285.0469, 88.01212),
            new WoWPoint(1906.579, 285.5695, 88.7718),
            new WoWPoint(1908.696, 286.9526, 89.01152),
            new WoWPoint(1910.739, 288.2302, 89.00998),
            new WoWPoint(1911.838, 288.6848, 89.00998),
            new WoWPoint(1912.906, 289.0126, 88.96411),
            new WoWPoint(1913.969, 289.3033, 88.96411),
            new WoWPoint(1914.991, 289.5171, 88.96432),
            new WoWPoint(1915.992, 289.6809, 88.96432),
            new WoWPoint(1917.014, 289.8018, 88.96432),
            new WoWPoint(1918.056, 289.8496, 88.96432),
            new WoWPoint(1920.45, 289.7492, 88.96432),
            new WoWPoint(1921.505, 289.6686, 88.96432),
            new WoWPoint(1922.529, 289.5707, 88.96432),
            new WoWPoint(1923.627, 289.4643, 88.96432),
            new WoWPoint(1926.059, 289.1384, 88.96432),
            new WoWPoint(1927.113, 289.1848, 88.96432),
            new WoWPoint(1928.178, 289.5029, 88.96432),
            new WoWPoint(1928.135, 290.8955, 88.96432),
            new WoWPoint(1927.176, 291.4067, 88.96432),
            new WoWPoint(1926.223, 291.8322, 88.96432),
            new WoWPoint(1923.969, 292.8049, 88.96432),
            new WoWPoint(1923.015, 293.3793, 88.96432),
            new WoWPoint(1922.259, 294.1969, 88.96432),
            new WoWPoint(1921.733, 295.0624, 88.96432),
            new WoWPoint(1922.734, 296.9155, 88.96432),
            new WoWPoint(1923.833, 296.8239, 88.96432),
            new WoWPoint(1924.818, 297.1319, 88.96432),
            new WoWPoint(1926.8, 298.6512, 89.95766),
            new WoWPoint(1927.699, 299.2106, 90.38755),
            new WoWPoint(1928.644, 299.7777, 90.38755),
            new WoWPoint(1930.797, 300.9877, 90.38755),
            new WoWPoint(1931.784, 301.5423, 90.40378),
            new WoWPoint(1933.99, 302.9207, 90.40378),
            new WoWPoint(1934.874, 303.5027, 90.40378),
            new WoWPoint(1935.72, 304.0609, 90.40378),
            new WoWPoint(1936.58, 304.6271, 90.40378),
            new WoWPoint(1937.451, 305.2014, 90.40378),
            new WoWPoint(1938.242, 305.845, 90.40378),
            new WoWPoint(1939.114, 306.6498, 90.40378),
            new WoWPoint(1940.044, 307.4655, 90.40378),
            new WoWPoint(1940.442, 308.5452, 90.40378),
            new WoWPoint(1940.027, 309.6814, 90.40378),
            new WoWPoint(1939.499, 310.7791, 90.40378),
            new WoWPoint(1938.924, 311.8606, 90.40378),
            new WoWPoint(1938.342, 312.8906, 90.40378),
            new WoWPoint(1937.726, 313.9574, 90.17552),
            new WoWPoint(1936.91, 314.7945, 89.53106),
            new WoWPoint(1936.013, 315.6642, 89.50283),
            new WoWPoint(1935.346, 316.6744, 89.50283),
            new WoWPoint(1934.835, 317.8326, 89.50283),
            new WoWPoint(1934.466, 319.0144, 89.50283),
            new WoWPoint(1934.348, 320.2665, 89.50283),
            new WoWPoint(1935.204, 321.1181, 89.50283),
            new WoWPoint(1936.156, 321.841, 89.88734),
            new WoWPoint(1937.135, 322.5881, 90.40282),
            new WoWPoint(1938.126, 323.2005, 90.40535),
            new WoWPoint(1939.278, 323.6826, 90.40535),
            new WoWPoint(1940.153, 324.5492, 90.40535),
            new WoWPoint(1940.523, 325.6887, 90.40535),
            new WoWPoint(1940.068, 326.8188, 90.40535),
            new WoWPoint(1939.596, 327.9942, 90.40535),
            new WoWPoint(1939.12, 329.1762, 90.40535),
            new WoWPoint(1938.708, 330.3583, 90.40535),
            new WoWPoint(1938.393, 331.5566, 90.40535),
            new WoWPoint(1938.079, 332.7549, 90.3358),
            new WoWPoint(1937.76, 333.9669, 90.28055),
            new WoWPoint(1937.446, 335.1653, 90.28055),
            new WoWPoint(1937.135, 336.3501, 90.28055),
            new WoWPoint(1936.822, 337.5417, 90.28055),
            new WoWPoint(1936.5, 338.7672, 90.28055),
            new WoWPoint(1936.178, 339.9852, 90.28055),
            new WoWPoint(1935.843, 341.2144, 90.28055),
            new WoWPoint(1935.496, 342.4038, 90.40495),
            new WoWPoint(1935.077, 343.5918, 90.51674),
            new WoWPoint(1934.61, 344.7168, 91.10134),
            new WoWPoint(1934.024, 345.8391, 91.76418),
        };
    }
}