using UnityEngine;

namespace ChickenPathfinding
{
    public class PathAgent : MonoBehaviour
    {
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
