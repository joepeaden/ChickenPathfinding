using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

namespace ChickenPathfinding
{
    public class FlowField
    {
        private byte[,] costField; // Cost values (0 = obstacle, 1 = walkable, 255 = goal)
        private ushort[,] integrationField; // Accumulated cost from goal
        private float2[,] flowField; // Direction vectors

        private int _width;
        private int _height;

        public FlowField()
        {
            // Arrays will be initialized in Generate
        }

        public void Generate(GridData gridData, int2 goalPosition)
        {
            // Initialize arrays if needed
            if (costField == null || costField.GetLength(0) != gridData.width || costField.GetLength(1) != gridData.height)
            {
                costField = new byte[gridData.width, gridData.height];
                integrationField = new ushort[gridData.width, gridData.height];
                flowField = new float2[gridData.width, gridData.height];
                _width = gridData.width;
                _height = gridData.height;
            }

            // Step 1: Initialize cost field
            InitializeCostField(gridData, goalPosition);

            // Step 2: Calculate integration field (Dijkstra-like)
            CalculateIntegrationField(goalPosition);

            // Step 3: Generate flow field
            GenerateFlowField();
        }

        public float2 GetDirection(int2 pos)
        {
            if (!IsValidPosition(pos)) return float2.zero;
            return flowField[pos.x, pos.y];
        }

        public float2 GetDirection(float3 worldPos, GridData gridData)
        {
            int2 gridPos = WorldToGrid(worldPos, gridData);
            return GetDirection(gridPos);
        }

        private bool IsValidPosition(int2 pos)
        {
            return pos.x >= 0 && pos.x < _width && pos.y >= 0 && pos.y < _height;
        }

        private int2 WorldToGrid(float3 worldPos, GridData gridData)
        {
            int x = Mathf.RoundToInt(worldPos.x / gridData.nodeSize + gridData.width / 2f);
            int y = Mathf.RoundToInt(worldPos.y / gridData.nodeSize + gridData.height / 2f);
            return new int2(x, y);
        }

        private void InitializeCostField(GridData gridData, int2 goalPosition)
        {
            for (int i = 0; i < gridData.nodes.Length; i++)
            {
                Node node = gridData.nodes[i];
                int x = node.position.x;
                int y = node.position.y;

                if (!node.walkable)
                {
                    costField[x, y] = 0; // Obstacle
                }
                else if (node.position.x == goalPosition.x && node.position.y == goalPosition.y)
                {
                    costField[x, y] = 255; // Goal
                }
                else
                {
                    costField[x, y] = 1; // Walkable
                }
            }
        }

        private void CalculateIntegrationField(int2 goalPosition)
        {
            // Initialize integration field
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    integrationField[x, y] = costField[x, y] == 0 ? (ushort)65535 : (ushort)65534;
                }
            }

            // Set goal
            integrationField[goalPosition.x, goalPosition.y] = 0;

            // Dijkstra's algorithm - process cells in order of increasing cost
            Queue<int2> openSet = new Queue<int2>();
            openSet.Enqueue(goalPosition);

            while (openSet.Count > 0)
            {
                int2 currentPos = openSet.Dequeue();
                ushort currentCost = integrationField[currentPos.x, currentPos.y];

                // Check all 8 neighbors
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int2 neighborPos = currentPos + new int2(dx, dy);
                        if (!IsValidPosition(neighborPos)) continue;

                        byte neighborCostValue = costField[neighborPos.x, neighborPos.y];

                        if (neighborCostValue == 0) continue; // Obstacle

                        // Calculate new cost (diagonal moves cost more)
                        ushort moveCost = (ushort)((dx != 0 && dy != 0) ? 14 : 10);
                        ushort newCost = (ushort)(currentCost + moveCost);

                        if (newCost < integrationField[neighborPos.x, neighborPos.y])
                        {
                            integrationField[neighborPos.x, neighborPos.y] = newCost;
                            openSet.Enqueue(neighborPos);
                        }
                    }
                }
            }
        }

        private void GenerateFlowField()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    int2 pos = new int2(x, y);
                    flowField[x, y] = CalculateDirection(pos);
                }
            }
        }

        private float2 CalculateDirection(int2 pos)
        {
            if (!IsValidPosition(pos)) return float2.zero;

            ushort currentCost = integrationField[pos.x, pos.y];

            if (currentCost >= 65534) return float2.zero; // No valid path

            float2 bestDirection = float2.zero;
            ushort bestNeighborCost = currentCost;

            // Check all 8 neighbors to find lowest cost
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int2 neighborPos = pos + new int2(dx, dy);
                    if (!IsValidPosition(neighborPos)) continue;

                    ushort neighborCost = integrationField[neighborPos.x, neighborPos.y];

                    if (neighborCost < bestNeighborCost)
                    {
                        bestNeighborCost = neighborCost;
                        bestDirection = math.normalize(new float2(dx, dy));
                    }
                }
            }

            return bestDirection;
        }
    }
}
