using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace ChickenPathfinding
{
    public class MyGrid : MonoBehaviour
    {
        // this way, nobody outside of here can modify. We also have a nice
        // readonly package we can expose too in _gridDataReadonly
        public GridData GridDataReadonly => _gridDataReadonly;
        public NativeArray<Node>.ReadOnly Nodes => _gridDataReadonly.nodes;
        public int Width => _gridDataReadonly.width;
        public int Height => _gridDataReadonly.height;
        public float NodeSize => _gridDataReadonly.nodeSize;
        
        [SerializeField] private int height = 10;
        [SerializeField] private int width = 10;
        [SerializeField] private float nodeSize = 1f;

        public LayerMask obstacleLayer;

        private GridData _gridDataReadonly;
        private NativeArray<Node> grid;

        private void Awake()
        {
            CreateGrid();
        }

        public void CreateGrid()
        {
            DisposeGrid();
            grid = new NativeArray<Node>(width * height, Allocator.Persistent);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int index = GetIndex(x, y);
                    int2 pos = new int2(x, y);
                    grid[index] = new Node
                    {
                        position = pos,
                        walkable = true
                    };

                    // Check for obstacles
                    Vector3 worldPos = GetWorldPosition(pos);
                    if (Physics2D.OverlapCircle(worldPos, nodeSize / 2f, obstacleLayer))
                    {
                        Node node = grid[index];
                        node.walkable = false;
                        grid[index] = node;
                    }
                }
            }

            _gridDataReadonly = new GridData
            {
                width = width,
                height = height,
                nodes = grid.AsReadOnly(),
                nodeSize = nodeSize
            };
        }

        private int GetIndex(int x, int y)
        {
            return y * width + x;
        }

        private void DisposeGrid()
        {
            if (grid.IsCreated) { grid.Dispose(); }
        }

        private void OnDrawGizmos()
        {
            if (!grid.IsCreated) return;

            for (int i = 0; i < grid.Length; i++)
            {
                Node node = grid[i];
                Vector3 worldPos = GetWorldPosition(node.position);
                Gizmos.color = node.walkable ? Color.white : Color.red;
                Gizmos.DrawWireCube(worldPos, Vector3.one * nodeSize);
            }
        }

        // Helper method for visualization
        private Vector3 GetWorldPosition(int2 gridPos)
        {
            float offsetX = width / 2f - gridPos.x;
            float offsetY = height / 2f - gridPos.y;
            return new Vector3(-offsetX * nodeSize, -offsetY * nodeSize, 0f);
        }

        private void OnDestroy()
        {
            DisposeGrid();
        }
    }
    
    public struct GridData
    {
        public NativeArray<Node>.ReadOnly nodes;
        public int width;
        public int height;
        public float nodeSize;
    }
}
