using UnityEngine;
using Unity.Mathematics;

namespace ChickenPathfinding
{
    public class FlowFieldController : MonoBehaviour
    {
        // privatize this!!!!!
        [SerializeField] public MyGrid _grid;
        public Transform target;
        private FlowField flowField;
        private int2 lastTargetGridPos = new int2(-1, -1);

        void Start()
        {
            if (_grid == null)
            {
                Debug.LogError("[FlowFieldController] Grid reference is missing.");
                throw new System.Exception("FlowFieldController cannot start: Grid is null.");
            }

            flowField = new FlowField();
        }

        void Update()
        {
            if (target != null && _grid.Nodes.IsCreated)
            {
                int2 targetGridPos = new int2(
                    Mathf.RoundToInt(target.position.x / _grid.NodeSize + _grid.Width / 2f),
                    Mathf.RoundToInt(target.position.y / _grid.NodeSize + _grid.Height / 2f)
                );

                if (targetGridPos.x != lastTargetGridPos.x || targetGridPos.y != lastTargetGridPos.y)
                {
                    flowField.Generate(_grid.GridDataReadonly, targetGridPos);
                    lastTargetGridPos = targetGridPos;
                }
            }
        }

        // Provide access to flow field for agents
        public float2 GetFlowDirection(float3 worldPosition)
        {
            return flowField.GetDirection(worldPosition, _grid.GridDataReadonly);
        }

        public float2[,] GetFlowField()
        {
            return flowField.flowField;
        }

        void OnDrawGizmos()
        {
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(target.position, 0.5f);
            }

            // Draw flow field directions
            if (flowField != null && _grid != null && _grid.Nodes.IsCreated)
            {
                for (int i = 0; i < _grid.Nodes.Length; i++)
                {
                    var node = _grid.Nodes[i];
                    if (!node.walkable) continue;

                    Vector3 worldPos = new Vector3(
                        (node.position.x - _grid.Width / 2f) * _grid.NodeSize,
                        (node.position.y - _grid.Height / 2f) * _grid.NodeSize,
                        0
                    );

                    float2 direction = flowField.GetDirection(node.position);
                    if (math.lengthsq(direction) > 0.01f)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawRay(worldPos, new Vector3(direction.x, direction.y, 0) * _grid.NodeSize * 0.5f);
                    }
                }
            }
        }
    }
}
