using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using System.Linq;
using Unity.Burst;

namespace ChickenPathfinding
{
    /// <summary>
    /// Handles generating the flow field, and allows use of the flow field through GetFlowDirections
    /// when necessary.
    /// </summary>
    public class FlowFieldController
    {
        private MyGrid _grid;
        private FlowField flowField;
        private JobHandle _flowGenerationJobHandle;

        public FlowFieldController(MyGrid grid)
        {
            _grid = grid;
            flowField = new FlowField();
        }

        /// <summary>
        /// "Kickoff" because it doesn't happen immediately necesesarily, it's a Jobs implementation. 
        /// </summary>
        /// <param name="destination"></param>
        public void KickOffRegenFlowField(int2 destination)
        {
            _flowGenerationJobHandle = flowField.KickOffGenerationJobs(_grid.GridDataReadonly, destination);
        }

        public int2 GetGridPositionFromWorld(Vector3 position)
        {
            return new int2(
                Mathf.RoundToInt(position.x / _grid.NodeSize + _grid.Width / 2f),
                Mathf.RoundToInt(position.y / _grid.NodeSize + _grid.Height / 2f)
            );
        }

        // Provide access to flow field for agents
        public JobHandle ScheduleGetFlowDirections(NativeArray<float3> currentPositions, NativeArray<float2> resultDirections)
        {
            AssignMoveDirJob assignMoveJob = new AssignMoveDirJob()
            {
                flowField = flowField.GetCopyOfFlowField(),
                gridData = _grid.GridDataReadonly,
                currentPositions = currentPositions,
                resultDirections = resultDirections
            };

            int arrayBatchSize = 100;
            return assignMoveJob.Schedule(currentPositions.Count(), arrayBatchSize, _flowGenerationJobHandle);
        }

        // this is horrendous. Passing back and forth like this. I guess this is the pain that comes with learning something new
        public void DisposeCopiedFlowField()
        {
            flowField.DisposeCopiedFlowField();
        }

        /// <summary>
        /// Dispose anything that needs disposing
        /// </summary>
        public void Cleanup()
        {
            flowField?.Dispose();
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
