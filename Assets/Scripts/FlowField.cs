using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;

namespace ChickenPathfinding
{
    [BurstCompile]
    struct CostFieldJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Node>.ReadOnly nodes;
        [ReadOnly] public int2 goalPosition;
        public NativeArray<byte> costField;
        public int width;

        public void Execute(int index)
        {
            Node node = nodes[index];
            int x = node.position.x;
            int y = node.position.y;
            int flatIndex = x + y * width;

            if (!node.walkable)
            {
                costField[flatIndex] = 0;
            }
            else if (node.position.x == goalPosition.x && node.position.y == goalPosition.y)
            {
                costField[flatIndex] = 255;
            }
            else
            {
                costField[flatIndex] = 1;
            }
        }
    }

    [BurstCompile]
    struct IntegrationFieldJob : IJob
    {
        public NativeArray<byte> costField;
        public NativeArray<ushort> integrationField;
        [ReadOnly] public int2 goalPosition;
        [ReadOnly] public int width;
        [ReadOnly] public int height;

        public void Execute()
        {
            // Initialize integration field
            for (int i = 0; i < integrationField.Length; i++)
            {
                integrationField[i] = costField[i] == 0 ? (ushort)65535 : (ushort)65534;
            }

            // Set goal
            int goalIndex = goalPosition.x + goalPosition.y * width;
            integrationField[goalIndex] = 0;

            // Dijkstra's algorithm
            NativeQueue<int2> openSet = new NativeQueue<int2>(Allocator.Temp);
            openSet.Enqueue(goalPosition);

            while (!openSet.IsEmpty())
            {
                int2 currentPos = openSet.Dequeue();
                int currentIndex = currentPos.x + currentPos.y * width;
                ushort currentCost = integrationField[currentIndex];

                // Check neighbors
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int2 neighborPos = currentPos + new int2(dx, dy);
                        if (neighborPos.x < 0 || neighborPos.x >= width || neighborPos.y < 0 || neighborPos.y >= height) continue;

                        int nIndex = neighborPos.x + neighborPos.y * width;
                        byte nCostValue = costField[nIndex];

                        if (nCostValue == 0) continue;

                        ushort moveCost = (ushort)((dx != 0 && dy != 0) ? 14 : 10);
                        ushort newCost = (ushort)(currentCost + moveCost);

                        if (newCost < integrationField[nIndex])
                        {
                            integrationField[nIndex] = newCost;
                            openSet.Enqueue(neighborPos);
                        }
                    }
                }
            }

            openSet.Dispose();
        }
    }

    [BurstCompile]
    struct FlowFieldJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<ushort> integrationField;
        public NativeArray<float2> flowField;
        [ReadOnly] public int width;
        [ReadOnly] public int height;

        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;
            int2 pos = new int2(x, y);
            flowField[index] = CalculateDirection(pos);
        }

        private float2 CalculateDirection(int2 pos)
        {
            int index = pos.x + pos.y * width;
            ushort currentCost = integrationField[index];

            if (currentCost >= 65534) return float2.zero;

            float2 bestDirection = float2.zero;
            ushort bestNeighborCost = currentCost;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int2 neighbor = pos + new int2(dx, dy);
                    if (neighbor.x < 0 || neighbor.x >= width || neighbor.y < 0 || neighbor.y >= height) continue;

                    int nIndex = neighbor.x + neighbor.y * width;
                    ushort nCost = integrationField[nIndex];

                    if (nCost < bestNeighborCost)
                    {
                        bestNeighborCost = nCost;
                        bestDirection = math.normalize(new float2(dx, dy));
                    }
                }
            }

            return bestDirection;
        }
    }

    public class FlowField
    {
        private NativeArray<byte> _costField;
        private NativeArray<ushort> _integrationField;

        public NativeArray<float2> GeneratedFlowField => _generatedFlowField;
        private NativeArray<float2> _generatedFlowField;

        private int _width;
        private int _height;

        public FlowField()
        {
            // Arrays will be initialized in Generate
        }

        public void Generate(GridData gridData, int2 goalPosition)
        {
            // Initialize arrays if needed
            if (!_costField.IsCreated || _costField.Length != gridData.width * gridData.height)
            {
                if (_costField.IsCreated) _costField.Dispose();
                if (_integrationField.IsCreated) _integrationField.Dispose();
                if (_generatedFlowField.IsCreated) _generatedFlowField.Dispose();

                _costField = new NativeArray<byte>(gridData.width * gridData.height, Allocator.Persistent);
                _integrationField = new NativeArray<ushort>(gridData.width * gridData.height, Allocator.Persistent);
                _generatedFlowField = new NativeArray<float2>(gridData.width * gridData.height, Allocator.Persistent);
                _width = gridData.width;
                _height = gridData.height;
            }

            // Step 1: Initialize cost field
            var costJob = new CostFieldJob
            {
                nodes = gridData.nodes,
                goalPosition = goalPosition,
                costField = _costField,
                width = _width
            };
            JobHandle costHandle = costJob.Schedule(gridData.nodes.Length, 64);

            // Step 2: Calculate integration field (Dijkstra-like)
            var integrationJob = new IntegrationFieldJob
            {
                costField = _costField,
                integrationField = _integrationField,
                goalPosition = goalPosition,
                width = _width,
                height = _height
            };
            JobHandle integrationHandle = integrationJob.Schedule(costHandle);

            // Step 3: Generate flow field
            var flowJob = new FlowFieldJob
            {
                integrationField = _integrationField,
                flowField = _generatedFlowField,
                width = _width,
                height = _height
            };
            JobHandle flowHandle = flowJob.Schedule(_generatedFlowField.Length, 64, integrationHandle);

            flowHandle.Complete();
        }

        public float2 GetDirection(int2 pos)
        {
            if (!IsValidPosition(pos)) return float2.zero;
            int index = pos.x + pos.y * _width;
            return _generatedFlowField[index];
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
            int x = (int)math.round(worldPos.x / gridData.nodeSize + gridData.width / 2f);
            int y = (int)math.round(worldPos.y / gridData.nodeSize + gridData.height / 2f);
            return new int2(x, y);
        }

        public void Dispose()
        {
            if (_costField.IsCreated)
            {
                _costField.Dispose();
            }
            if (_integrationField.IsCreated)
            {
                _integrationField.Dispose();
            }
            if (_generatedFlowField.IsCreated)
            {
                _generatedFlowField.Dispose();
            }
        }
    }
}
