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
    }
}
