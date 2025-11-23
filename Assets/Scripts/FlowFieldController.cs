using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using System.Linq;

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

        public FlowFieldController(MyGrid grid)
        {
            _grid = grid;
            flowField = new FlowField();
        }

        public void RegenFlowField(int2 destination)
        {
            flowField.Generate(_grid.GridDataReadonly, destination);
        }

        public int2 GetGridPositionFromWorld(Vector3 position)
        {
            return new int2(
                Mathf.RoundToInt(position.x / _grid.NodeSize + _grid.Width / 2f),
                Mathf.RoundToInt(position.y / _grid.NodeSize + _grid.Height / 2f)
            );
        }

        // Provide access to flow field for agents
        public void GetFlowDirections(NativeArray<float3> currentPositions, NativeArray<float2> resultDirections)
        {
            AssignMoveDirJob assignMoveJob = new AssignMoveDirJob()
            {
                flowField = flowField.GeneratedFlowField,
                gridData = _grid.GridDataReadonly,
                currentPositions = currentPositions,
                resultDirections = resultDirections
            };

            JobHandle jh = assignMoveJob.Schedule(currentPositions.Count(), 100);
            
            // I could see if there's a way to complete this without forcing it to be this frame - do we 
            // really need to update positions every frame? If not then we'd have to make it not call a 
            // new job until the last job was done.
            jh.Complete();
        }

        /// <summary>
        /// Dispose anything that needs disposing
        /// </summary>
        public void Cleanup()
        {
            flowField?.Dispose();
        }
    }

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
