using UnityEngine;
using UnityEngine.Events;

namespace ChickenPathfinding
{
    [CreateAssetMenu(fileName = "AgentSpawnedEvent", menuName = "Events/AgentSpawnedEvent")]
    public class AgentSpawnedEvent : ScriptableObject
    {
        private UnityEvent<PathAgent> OnAgentSpawned = new();

        public void Invoke(PathAgent agent)
        {
            OnAgentSpawned.Invoke(agent);
        }

        public void AddListener(UnityAction<PathAgent> listener)
        {
            OnAgentSpawned.AddListener(listener);
        }

        public void RemoveListener(UnityAction<PathAgent> listener)
        {
            OnAgentSpawned.RemoveListener(listener);
        }
    }
}