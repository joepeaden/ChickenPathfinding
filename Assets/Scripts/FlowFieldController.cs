using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

namespace ChickenPathfinding
{
    /// <summary>
    /// Communication interface for use of the flow field.
    /// </summary>
    public class FlowFieldController
    {
        public GridData GridData => _grid.GridDataReadonly;
        private MyGrid _grid;
        private FlowField _flowField;

        public FlowFieldController(MyGrid grid)
        {
            _grid = grid;
            _flowField = new FlowField();
        }

        /// <summary>
        /// Start flow field generation and return the job handle.
        /// </summary>
        public JobHandle StartFlowFieldGeneration(int2 destination)
        {
            return _flowField.KickOffGenerationJobs(GridData, destination);
        }

        /// <summary>
        /// Get the current flow field for reading.
        /// </summary>
        public NativeArray<float2> GetCurrentFlowField()
        {
            return _flowField.GetCurrentFlowField();
        }

        public int2 GetGridPositionFromWorld(Vector3 position)
        {
            return new int2(
                Mathf.RoundToInt(position.x / _grid.NodeSize + _grid.Width / 2f),
                Mathf.RoundToInt(position.y / _grid.NodeSize + _grid.Height / 2f)
            );
        }

        /// <summary>
        /// Dispose anything that needs disposing
        /// </summary>
        public void Cleanup()
        {
            _flowField?.Dispose();
        }
    }

    [BurstCompile]
    public struct AssignMoveDirJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float2> flowField;
        [ReadOnly] public GridData gridData;
        [ReadOnly] public NativeArray<float3> currentPositions;
        public NativeArray<float2> resultDirections;

        public void Execute(int index)
        {
            int2 gridPos = WorldToGrid(currentPositions[index], gridData);
            resultDirections[index] = GetDirection(gridPos);
        }

        private bool IsValidPosition(int2 pos)
        {
            return pos.x >= 0 && pos.x < gridData.width && pos.y >= 0 && pos.y < gridData.height;
        }

        public float2 GetDirection(int2 pos)
        {
            if (!IsValidPosition(pos)) return float2.zero;
            int theIndex = pos.x + pos.y * gridData.width;
            return flowField[theIndex];
        }

        private int2 WorldToGrid(float3 worldPos, GridData gridData)
        {
            int x = (int)math.floor(worldPos.x / gridData.nodeSize + gridData.width / 2f);
            int y = (int)math.floor(worldPos.y / gridData.nodeSize + gridData.height / 2f);
            return new int2(x, y);
        }
    }
}
