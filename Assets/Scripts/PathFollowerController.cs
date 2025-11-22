using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace ChickenPathfinding
{
    public class PathFollowerController : MonoBehaviour
    {
        private const float REACHED_DESTINATION_THRESHOLD = 0.01f;
        private const int MAX_ENEMY_COUNT = 3000;

        FlowFieldController flowController;
        public float speed = 5f;

        // obviously this needs to not be static. Probably add during spawn
        public static List<PathFollower> pathFollowers = new();

        private NativeArray<float3> currentPositions;
        public NativeArray<float2> resultDirections;

        void Start()
        {
            flowController = FindObjectOfType<FlowFieldController>();
            currentPositions = new NativeArray<float3>(MAX_ENEMY_COUNT, Allocator.Persistent);
            resultDirections = new NativeArray<float2>(MAX_ENEMY_COUNT, Allocator.Persistent);
        }

        void Update()
        {
            if (flowController != null)
            {
                // what happens if there aren't enough current positions? catch error.
                for (int i = 0; i < pathFollowers.Count; i++)
                {
                    currentPositions[i] = pathFollowers[i].transform.position;
                }

                AssignMoveDirJob assignMoveJob = new AssignMoveDirJob()
                {
                    flowField = flowController.GetFlowField(),
                    gridData = flowController._grid.GridDataReadonly,
                    currentPositions = currentPositions,
                    resultDirections = resultDirections
                };

                JobHandle jh = assignMoveJob.Schedule(MAX_ENEMY_COUNT, 100);
                
                // I could see if there's a way to complete this without forcing it to be this frame - do we 
                // really need to update positions every frame? If not then we'd have to make it not call a 
                // new job until the last job was done.
                jh.Complete();

                // this also needs to be a job
                for (int i = 0; i < pathFollowers.Count; i++)
                {
                    PathFollower f = pathFollowers[i];
                    // Get movement direction from flow field
                    // float2 moveVector = flowController.GetFlowDirection(transform.position);
                    float2 moveVector = resultDirections[i]; 

                    if (!f.HaveReachedDestination(moveVector))
                    {
                        // Move in the flow direction
                        Vector3 moveDirection = new Vector3(moveVector.x, moveVector.y, 0).normalized;
                        f.transform.position += speed * Time.deltaTime * moveDirection;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            DisposePersistentCollections();
        }

        private void DisposePersistentCollections()
        {
            if (currentPositions.IsCreated)
            {
                currentPositions.Dispose();
            }
            
            if (resultDirections.IsCreated)
            {
                resultDirections.Dispose();
            }
        }

        private bool HaveReachedDestination(float2 moveVector)
        {
            // if the move vector returned is less than this value, we've reached our target
            // the vector is practically (0, 0)
            return math.lengthsq(moveVector) < REACHED_DESTINATION_THRESHOLD;
        }

        // void OnDrawGizmos()
        // {
        //     if (flowController != null && flowController.target != null)
        //     {
        //         // Draw line to target
        //         Gizmos.color = Color.green;
        //         Gizmos.DrawLine(transform.position, flowController.target.position);

        //         // Draw current flow direction
        //         float2 moveVector = flowController.GetFlowDirection(transform.position);
        //         if (!HaveReachedDestination(moveVector))
        //         {
        //             Gizmos.color = Color.cyan;
        //             Vector3 dir = new Vector3(moveVector.x, moveVector.y, 0).normalized;
        //             Gizmos.DrawRay(transform.position, dir * 2f);
        //         }
        //     }
        // }
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
