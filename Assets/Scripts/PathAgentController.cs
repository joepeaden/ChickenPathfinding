using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;
using System.Linq;

namespace ChickenPathfinding
{
    /// <summary>
    /// Controls the pathfinding of agents utilizing the flow field.
    /// </summary>
    public class PathAgentController : MonoBehaviour
    {
        private const int MAX_ENEMY_COUNT = 3000;

        [SerializeField] private MyGrid _grid;
        [SerializeField] private Transform target;
        [SerializeField] private float agentSpeed;
        [SerializeField] private AgentSpawnedEvent _agentSpawnedEvent;

        private FlowFieldController _flowController;
        private int2 lastTargetGridPos = new int2(-1, -1);
        private NativeArray<float3> currentPositions;
        private NativeArray<float3> moveOffsets;
        private NativeArray<float2> resultDirections;
        private List<PathAgent> pathAgents = new();

        private void Awake()
        {
            _flowController = new (_grid);
            currentPositions = new NativeArray<float3>(MAX_ENEMY_COUNT, Allocator.Persistent);
            resultDirections = new NativeArray<float2>(MAX_ENEMY_COUNT, Allocator.Persistent);
            moveOffsets = new NativeArray<float3>(MAX_ENEMY_COUNT, Allocator.Persistent);

            _agentSpawnedEvent.AddListener(HandleAgentSpawned);
        }

        private void Update()
        {
            RegenFlowIfNeeded();
            
            HandlePathfinding();
        }

        private void OnDestroy()
        {
            DisposePersistentCollections();
            _flowController.Cleanup();
            _agentSpawnedEvent.RemoveListener(HandleAgentSpawned);
        }

        private void HandleAgentSpawned(PathAgent agent)
        {
            pathAgents.Add(agent);
        }

        private void RegenFlowIfNeeded()
        {
            int2 targetGridPos = _flowController.GetGridPositionFromWorld(target.transform.position);

            if (targetGridPos.x != lastTargetGridPos.x || targetGridPos.y != lastTargetGridPos.y)
            {
                _flowController.RegenFlowField(targetGridPos);
                lastTargetGridPos = targetGridPos;
            }
        }

        private void HandlePathfinding()
        {
            // what happens if there aren't enough current positions? catch error.
            for (int i = 0; i < pathAgents.Count; i++)
            {
                currentPositions[i] = pathAgents[i].transform.position;
            }

            _flowController.GetFlowDirections(currentPositions, resultDirections);

            CreateTransformOffsets assignMoveJob = new ()
            {
                resultDirections = resultDirections,
                moveOffsets = moveOffsets,
                agentSpeed = agentSpeed,
                deltaTime = Time.deltaTime
            };

            JobHandle jh = assignMoveJob.Schedule(currentPositions.Count(), 100);
            
            // do we really have to force complete? Same as with in FlowController.GetFlowDirections.
            jh.Complete();

            // this also needs to be a job
            for (int i = 0; i < pathAgents.Count; i++)
            {
                if (!IsZeroMoveDir(moveOffsets[i]))
                {
                    pathAgents[i].MoveByOffset(moveOffsets[i]);
                }
            }
        }

        private bool IsZeroMoveDir(float3 moveDir)
        {
            return moveDir.x == 0 && moveDir.y == 0 && moveDir.z == 0;
        }

        private void DisposePersistentCollections()
        {
            if (currentPositions.IsCreated) { currentPositions.Dispose(); }
            if (moveOffsets.IsCreated) { moveOffsets.Dispose(); }
            if (resultDirections.IsCreated) { resultDirections.Dispose(); }
        }
    }

     [BurstCompile]
    public struct CreateTransformOffsets : IJobParallelFor
    {
        private const float REACHED_DESTINATION_THRESHOLD = 0.01f;
        [ReadOnly] public NativeArray<float2> resultDirections;
        [ReadOnly] public float agentSpeed;
        [ReadOnly] public float deltaTime;
        public NativeArray<float3> moveOffsets;

        public void Execute(int index)
        {
            float2 moveVector = resultDirections[index]; 

            if (!HaveReachedDestination(moveVector))
            {
                // Move in the flow direction
                Vector3 moveDirection = new Vector3(moveVector.x, moveVector.y, 0).normalized;
                moveOffsets[index] = agentSpeed * deltaTime * moveDirection;
            }
            else
            {
                moveOffsets[index] = float3.zero;
            }
        }

        private bool HaveReachedDestination(float2 moveVector)
        {
            // if the move vector returned is less than this value, we've reached our target
            // the vector is practically (0, 0)
            return math.lengthsq(moveVector) < REACHED_DESTINATION_THRESHOLD;
        }
    }
}
