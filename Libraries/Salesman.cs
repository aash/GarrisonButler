using System.Collections.Generic;
using System.Linq;

namespace GarrisonButler.Libraries
{
    public class Salesman
    {
        private class Node
        {
            public int Value { get; set; }
            public Node[] ChildNodes { get; set; }
            public bool Selected { get; set; }
        }

        #region Member Variables

        private readonly double[,] _adjacencyMatrix;
        private readonly IEnumerable<int> _vertices;

        #endregion

        #region Constructor

        public Salesman(IEnumerable<int> vertices, double[,] matrix)
        {
            _vertices = vertices;
            _adjacencyMatrix = matrix;
        }

        #endregion

        #region Public Methods

        public IEnumerable<int> Solve(out double cost)
        {
            var startVertex = _vertices.First();//source node is assumed to be the first
            var set = new HashSet<int>(_vertices);
            set.Remove(startVertex);

            var root = new Node();
            cost = GetMinimumCostRoute(startVertex, set, root);
            return TraverseTree(root, startVertex);
        }

        #endregion

        #region Private Methods

        private double GetMinimumCostRoute(int startVertex, HashSet<int> set, Node root)
        {
            if (!set.Any())
            {
                //source node is assumed to be the first
                root.ChildNodes = new Node[1] { new Node { Value = _vertices.First(), Selected = true } };
                return _adjacencyMatrix[startVertex, 0];
            }

            double totalCost = double.MaxValue;
            int i = 0;
            int selectedIdx = i;
            root.ChildNodes = new Node[set.Count()];

            foreach (var destination in set)
            {
                root.ChildNodes[i] = new Node { Value = destination };

                double costOfVistingCurrentNode = _adjacencyMatrix[startVertex, destination];

                var newSet = new HashSet<int>(set);
                newSet.Remove(destination);
                double costOfVisitingOtherNodes = GetMinimumCostRoute(destination, newSet, root.ChildNodes[i]);
                double currentCost = costOfVistingCurrentNode + costOfVisitingOtherNodes;

                if (totalCost > currentCost)
                {
                    totalCost = currentCost;
                    selectedIdx = i;
                }

                i++;
            }

            root.ChildNodes[selectedIdx].Selected = true;

            return totalCost;

        }

        private IEnumerable<int> TraverseTree(Node root, int startint)
        {
            var q = new Queue<int>();
            q.Enqueue(startint);
            TraverseTreeUtil(root, q);
            return q;
        }

        private void TraverseTreeUtil(Node root, Queue<int> vertices)
        {
            if (root.ChildNodes == null)
            {
                return;
            }

            foreach (var child in root.ChildNodes)
            {
                if (child.Selected)
                {
                    vertices.Enqueue(child.Value);
                    TraverseTreeUtil(child, vertices);
                }
            }
        }

        #endregion
    }
}