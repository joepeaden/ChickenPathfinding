using UnityEngine;
using UnityEngine.Events;

namespace ChickenPathfinding
{
    /// <summary>
    /// All this does is tell listeners that an agent was spawned and gives the reference.
    /// </summary>
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