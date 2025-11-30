using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Jobs;
using Unity.VisualScripting;

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
        private int2 _lastTargetGridPos = new int2(-1, -1);
        private NativeArray<float3> _currentPositions;
        private NativeArray<float3> _moveOffsets;
        private NativeArray<float2> _resultDirections;
        private List<PathAgent> _pathAgents = new();
        private TransformAccessArray _transformAccessArray;
        private JobHandle _flowDirectionJobHandle;
        private JobHandle _assignMoveJobHandle;

        private void Awake()
        {
            _flowController = new (_grid);
            _currentPositions = new (MAX_ENEMY_COUNT, Allocator.Persistent);
            _resultDirections = new (MAX_ENEMY_COUNT, Allocator.Persistent);
            _moveOffsets = new (MAX_ENEMY_COUNT, Allocator.Persistent);
            _transformAccessArray = new (MAX_ENEMY_COUNT);

            _agentSpawnedEvent.AddListener(HandleAgentSpawned);
        }

        private void Update()
        {
            RegenFlowIfNeeded();

            if (_flowDirectionJobHandle.IsCompleted && _assignMoveJobHandle.IsCompleted)
            {
                _flowDirectionJobHandle.Complete();
                _flowController.DisposeCopiedFlowField();
                _assignMoveJobHandle.Complete();
            }
        }

        private void LateUpdate()
        {
            ScheduleFindMoveDirection();
        }

        private void OnDestroy()
        {
            DisposePersistentCollections();
            _flowController.Cleanup();
            _agentSpawnedEvent.RemoveListener(HandleAgentSpawned);
        }

        private void HandleAgentSpawned(PathAgent agent)
        {
            _pathAgents.Add(agent);
         
            _transformAccessArray.Add(agent.transform);
        }

        private void RegenFlowIfNeeded()
        {
            int2 targetGridPos = _flowController.GetGridPositionFromWorld(target.transform.position);

            if (targetGridPos.x != _lastTargetGridPos.x || targetGridPos.y != _lastTargetGridPos.y)
            {
                _flowController.KickOffRegenFlowField(targetGridPos);
                _lastTargetGridPos = targetGridPos;
            }
        }

        private void ScheduleFindMoveDirection()
        {
            // what happens if there aren't enough current positions? catch error.
            for (int i = 0; i < _pathAgents.Count; i++)
            {
                _currentPositions[i] = _pathAgents[i].transform.position;
            }

            _flowDirectionJobHandle = _flowController.ScheduleGetFlowDirections(_currentPositions, _resultDirections);
            _assignMoveJobHandle = ScheduleAssignMove();
        }

        private JobHandle ScheduleAssignMove()
        {
            CreateTransformOffsets assignMoveJob = new ()
            {
                resultDirections = _resultDirections,
                moveOffsets = _moveOffsets,
                agentSpeed = agentSpeed,
                deltaTime = Time.deltaTime
            };

            return assignMoveJob.ScheduleByRef(_transformAccessArray, _flowDirectionJobHandle);
        }

        private void DisposePersistentCollections()
        {
            if (_currentPositions.IsCreated) { _currentPositions.Dispose(); }
            if (_moveOffsets.IsCreated) { _moveOffsets.Dispose(); }
            if (_resultDirections.IsCreated) { _resultDirections.Dispose(); }
            if (_transformAccessArray.isCreated) { _transformAccessArray.Dispose(); }
        }
    }

     [BurstCompile]
    public struct CreateTransformOffsets : IJobParallelForTransform
    {
        private const float REACHED_DESTINATION_THRESHOLD = 0.01f;
        [ReadOnly] public NativeArray<float2> resultDirections;
        [ReadOnly] public float agentSpeed;
        [ReadOnly] public float deltaTime;
        public NativeArray<float3> moveOffsets;

        public void Execute(int index, TransformAccess transform)
        {
            float2 moveVector = resultDirections[index]; 

            if (!HaveReachedDestination(moveVector))
            {
                // Move in the flow direction
                Vector3 moveDirection = new Vector3(moveVector.x, moveVector.y, 0).normalized;
                transform.position += agentSpeed * deltaTime * moveDirection;
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
