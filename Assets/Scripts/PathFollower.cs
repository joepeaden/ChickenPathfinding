using UnityEngine;
using Unity.Mathematics;

namespace ChickenPathfinding
{
    public class PathFollower : MonoBehaviour
    {
        private const float REACHED_DESTINATION_THRESHOLD = 0.01f;

        FlowFieldController flowController;
        public float speed = 5f;

        void Start()
        {
            // get rid of these static and find things
            flowController = FindObjectOfType<FlowFieldController>();
            PathFollowerController.pathFollowers.Add(this);
        }

        private void Oestroy()
        {
            PathFollowerController.pathFollowers.Remove(this);
        }

        void Update()
        {
            // if (flowController != null)
            // {
            //     // Get movement direction from flow field
            //     float2 moveVector = flowController.GetFlowDirection(transform.position);
            //     if (!HaveReachedDestination(moveVector))
            //     {
            //         // Move in the flow direction
            //         Vector3 moveDirection = new Vector3(moveVector.x, moveVector.y, 0).normalized;
            //         transform.position += speed * Time.deltaTime * moveDirection;
            //     }
            // }
        }

        public bool HaveReachedDestination(float2 moveVector)
        {
            // if the move vector returned is less than this value, we've reached our target
            // the vector is practically (0, 0)
            return math.lengthsq(moveVector) < REACHED_DESTINATION_THRESHOLD;
        }

        void OnDrawGizmos()
        {
            if (flowController != null && flowController.target != null)
            {
                // Draw line to target
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, flowController.target.position);

                // Draw current flow direction
                float2 moveVector = flowController.GetFlowDirection(transform.position);
                if (!HaveReachedDestination(moveVector))
                {
                    Gizmos.color = Color.cyan;
                    Vector3 dir = new Vector3(moveVector.x, moveVector.y, 0).normalized;
                    Gizmos.DrawRay(transform.position, dir * 2f);
                }
            }
        }
    }
}
