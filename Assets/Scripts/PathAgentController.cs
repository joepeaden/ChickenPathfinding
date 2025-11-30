using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine.Jobs;

namespace ChickenPathfinding
{
    /// <summary>
    /// Controls the pathfinding of agents utilizing the flow field.
    /// </summary>
    public class PathAgentController : MonoBehaviour
    {
        private const int MAX_ENEMY_COUNT = 3000;

        // Properties for scheduler access
        public NativeArray<float3> CurrentPositions => _currentPositions;
        public NativeArray<float2> ResultDirections => _resultDirections;
        public NativeArray<float3> MoveOffsets => _moveOffsets;
        public float AgentSpeed => agentSpeed;
        public int AgentCount => _pathAgents.Count;
        public TransformAccessArray TransformAccessArray => _transformAccessArray;

        [SerializeField] private MyGrid _grid;
        [SerializeField] private Transform target;
        [SerializeField] private float agentSpeed;
        [SerializeField] private AgentSpawnedEvent _agentSpawnedEvent;

        private FlowFieldController _flowController;
        private FlowFieldJobScheduler _jobScheduler;
        private int2 _lastTargetGridPos = new int2(-1, -1);
        private NativeArray<float3> _currentPositions;
        private NativeArray<float3> _moveOffsets;
        private NativeArray<float2> _resultDirections;
        private List<PathAgent> _pathAgents = new();
        private TransformAccessArray _transformAccessArray;

        private void Awake()
        {
            _flowController = new (_grid);
            _jobScheduler = new FlowFieldJobScheduler(_flowController, this);

            _currentPositions = new (MAX_ENEMY_COUNT, Allocator.Persistent);
            _resultDirections = new (MAX_ENEMY_COUNT, Allocator.Persistent);
            _moveOffsets = new (MAX_ENEMY_COUNT, Allocator.Persistent);
            _transformAccessArray = new (MAX_ENEMY_COUNT);

            _agentSpawnedEvent.AddListener(HandleAgentSpawned);
        }

        private void Update()
        {
            RegenFlowIfNeeded();

            _jobScheduler.TryScheduleAgentJobs();
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

        /// <summary>
        /// Update current positions collection with world positions
        /// </summary>
        public void UpdateAgentCurrentPositions()
        {
            for (int i = 0; i < AgentCount; i++)
            {
                _currentPositions[i] = _transformAccessArray[i].position;
            }
        }

        private void RegenFlowIfNeeded()
        {
            int2 targetGridPos = _flowController.GetGridPositionFromWorld(target.transform.position);

            if (targetGridPos.x != _lastTargetGridPos.x || targetGridPos.y != _lastTargetGridPos.y)
            {
                _jobScheduler.RequestFlowFieldUpdate(targetGridPos);
                _lastTargetGridPos = targetGridPos;
            }
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
