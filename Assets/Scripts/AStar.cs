using UnityEngine;
using System.Collections.Generic;
using Unity.Profiling;

namespace ChickenPathfinding
{
    public static class AStar
    {
        private static readonly ProfilerMarker pathfindMarker = new ProfilerMarker("ChickenPathfinding.FindPath");

        public static List<Vector3> FindPath(Grid grid, Vector3 startPos, Vector3 targetPos)
        {
            using (pathfindMarker.Auto())
            {
                Node startNode = grid.GetNode(grid.GetGridPosition(startPos));
                Node targetNode = grid.GetNode(grid.GetGridPosition(targetPos));

                if (startNode == null || targetNode == null || !startNode.walkable || !targetNode.walkable)
                {
                    return null; // No valid path
                }

                // Initialize start node costs
                startNode.gCost = 0;
                startNode.hCost = GetDistance(startNode, targetNode);

                PriorityQueue<Node> openSet = new PriorityQueue<Node>();
                HashSet<Node> openSetHash = new HashSet<Node>();  // O(1) contains checking
                HashSet<Node> closedSet = new HashSet<Node>();
                openSet.Enqueue(startNode);
                openSetHash.Add(startNode);

                while (openSet.Count > 0)
                {
                    Node currentNode = openSet.Dequeue();
                    closedSet.Add(currentNode);

                    if (currentNode == targetNode)
                    {
                        return RetracePath(startNode, targetNode, grid);
                    }

                    foreach (Node neighbor in grid.GetNeighbors(currentNode))
                    {
                        if (closedSet.Contains(neighbor))
                            continue;

                        float newCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                        if (newCostToNeighbor < neighbor.gCost || !openSetHash.Contains(neighbor))
                        {
                            neighbor.gCost = newCostToNeighbor;
                            neighbor.hCost = GetDistance(neighbor, targetNode);
                            neighbor.parent = currentNode;

                            if (!openSetHash.Contains(neighbor))
                            {
                                openSetHash.Add(neighbor);
                                openSet.Enqueue(neighbor);
                            }
                        }
                    }
                }

                return null; // No path found
            }
        }
        static List<Vector3> RetracePath(Node startNode, Node endNode, Grid grid)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            path.Reverse();

            List<Vector3> waypoints = new List<Vector3>();
            foreach (Node node in path)
            {
                waypoints.Add(grid.GetWorldPosition(node.position));
            }
            return waypoints;
        }

        static float GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.position.x - nodeB.position.x);
            int dstY = Mathf.Abs(nodeA.position.y - nodeB.position.y);

            return dstX + dstY; // Manhattan distance
        }
    }
}
