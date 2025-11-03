using UnityEngine;

namespace ChickenPathfinding
{
    public class Node : System.IComparable<Node>
    {
        public Vector2Int position;
        public bool walkable = true;
        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost;
        public Node parent;

        public Node(Vector2Int pos)
        {
            position = pos;
        }

        // Compare nodes by fCost for priority queue ordering
        public int CompareTo(Node other)
        {
            if (other == null) return 1;

            int compare = fCost.CompareTo(other.fCost);
            if (compare == 0)
            {
                // If fCosts are equal, compare hCost (greedy tie-breaking)
                compare = hCost.CompareTo(other.hCost);
            }
            return compare;
        }
    }
}