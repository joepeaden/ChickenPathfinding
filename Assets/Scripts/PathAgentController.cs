using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;

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
        private NativeArray<float2> resultDirections;
        private List<PathAgent> pathAgents = new();

        private void Awake()
        {
            _flowController = new (_grid);
            currentPositions = new NativeArray<float3>(MAX_ENEMY_COUNT, Allocator.Persistent);
            resultDirections = new NativeArray<float2>(MAX_ENEMY_COUNT, Allocator.Persistent);

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

            // this also needs to be a job
            for (int i = 0; i < pathAgents.Count; i++)
            {
                PathAgent follower = pathAgents[i];
                // Get movement direction from flow field
                float2 moveVector = resultDirections[i]; 

                if (!follower.HaveReachedDestination(moveVector))
                {
                    // Move in the flow direction
                    Vector3 moveDirection = new Vector3(moveVector.x, moveVector.y, 0).normalized;
                    follower.transform.position += agentSpeed * Time.deltaTime * moveDirection;
                }
            }
        }

        private void DisposePersistentCollections()
        {
            if (currentPositions.IsCreated) { currentPositions.Dispose(); }
            if (resultDirections.IsCreated) { resultDirections.Dispose(); }
        }
    }
}
