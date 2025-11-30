using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.Jobs;

namespace ChickenPathfinding
{
    /// <summary>
    /// Scheduler that handles jobs dependencies between FlowFieldController and PathAgentController. 
    /// Does not host the jobs themselves, only coordinates completion and dependencies.
    /// </summary>
    public class FlowFieldJobScheduler
    {
        private FlowFieldController _flowController;
        private PathAgentController _agentController;

        private JobHandle _flowGenerationJobHandle;
        private JobHandle _directionLookupJobHandle;
        private JobHandle _movementJobHandle;

        public FlowFieldJobScheduler(FlowFieldController flowController, PathAgentController agentController)
        {
            _flowController = flowController;
            _agentController = agentController;
        }

        /// <summary>
        /// Request a flow field update. Completes all associated jobs before doing so.
        /// </summary>
        public void RequestFlowFieldUpdate(int2 destination)
        {
            CompleteAllFlowFieldJobs();
            _flowGenerationJobHandle = _flowController.StartFlowFieldGeneration(destination);
        }

        /// <summary>
        /// Schedule agent direction lookup and movement if all dependencies are ready.
        /// Returns true if jobs were scheduled successfully.
        /// </summary>
        public bool TryScheduleAgentJobs()
        {
            if (!PreviousFlowFieldJobsAreFinished())
            {
                return false;
            }

            CompleteAllFlowFieldJobs();

            _agentController.UpdateAgentCurrentPositions();

            _directionLookupJobHandle = ScheduleDirectionLookup();
            _movementJobHandle = ScheduleMovement();

            return true;
        }

        /// <summary>
        /// Check if all flow field generation jobs are complete and ready for agent jobs.
        /// </summary>
        public bool PreviousFlowFieldJobsAreFinished()
        {
            return _flowGenerationJobHandle.IsCompleted &&
                   _directionLookupJobHandle.IsCompleted &&
                   _movementJobHandle.IsCompleted;
        }

        public void CompleteAllFlowFieldJobs()
        {
            _flowGenerationJobHandle.Complete();
            _directionLookupJobHandle.Complete();
            _movementJobHandle.Complete();
        }

        /// <summary>
        /// Schedules retrieval of direction that each agent should move
        /// </summary>
        private JobHandle ScheduleDirectionLookup()
        {
            var directionJob = new AssignMoveDirJob
            {
                flowField = _flowController.GetCurrentFlowField(),
                gridData = _flowController.GridData,
                currentPositions = _agentController.CurrentPositions,
                resultDirections = _agentController.ResultDirections
            };

            int arrayBatchSize = 100;
            return directionJob.Schedule(_agentController.AgentCount, arrayBatchSize, _flowGenerationJobHandle);
        }

        /// <summary>
        /// Schedules transform updates for agents
        /// </summary>
        private JobHandle ScheduleMovement()
        {
            var moveJob = new CreateTransformOffsets
            {
                resultDirections = _agentController.ResultDirections,
                moveOffsets = _agentController.MoveOffsets,
                agentSpeed = _agentController.AgentSpeed,
                deltaTime = Time.deltaTime
            };

            return moveJob.ScheduleByRef(_agentController.TransformAccessArray, _directionLookupJobHandle);
        }
    }
}
