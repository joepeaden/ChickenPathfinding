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

        public void MoveByOffset(Vector3 offset)
        {
            transform.position += offset;
        }
    }
}
