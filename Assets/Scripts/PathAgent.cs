using UnityEngine;
using Unity.Mathematics;

namespace ChickenPathfinding
{
    public class PathAgent : MonoBehaviour
    {
        private const float REACHED_DESTINATION_THRESHOLD = 0.01f;
        [SerializeField] private AgentSpawnedEvent _agentSpawnedEvent;

        void Start()
        {
            _agentSpawnedEvent.Invoke(this);
        }

        public bool HaveReachedDestination(float2 moveVector)
        {
            // if the move vector returned is less than this value, we've reached our target
            // the vector is practically (0, 0)
            return math.lengthsq(moveVector) < REACHED_DESTINATION_THRESHOLD;
        }
    }
}
