using UnityEngine;

public class Grid : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public float nodeSize = 1f;
    public LayerMask obstacleLayer;

    private Node[,] grid;

    void Start()
    {
        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new Node[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                grid[x, y] = new Node(pos);

                // Check for obstacles
                Vector3 worldPos = GetWorldPosition(pos);
                if (Physics2D.OverlapCircle(worldPos, nodeSize / 2f, obstacleLayer))
                {
                    grid[x, y].walkable = false;
                }
            }
        }
    }

    public Node GetNode(Vector2Int gridPos)
    {
        if (gridPos.x >= 0 && gridPos.x < width && gridPos.y >= 0 && gridPos.y < height)
        {
            return grid[gridPos.x, gridPos.y];
        }
        return null;
    }

    public Vector2Int GetGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt((worldPosition.x - transform.position.x) / nodeSize + width / 2f);
        int y = Mathf.RoundToInt((worldPosition.y - transform.position.y) / nodeSize + height / 2f);
        return new Vector2Int(x, y);
    }

    public Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        float offsetX = (float)width / 2f - gridPos.x;
        float offsetY = (float)height / 2f - gridPos.y;
        return transform.position + new Vector3(-offsetX * nodeSize, -offsetY * nodeSize, 0f);
    }

    public Node[] GetNeighbors(Node node)
    {
        System.Collections.Generic.List<Node> neighbors = new System.Collections.Generic.List<Node>();

        foreach (Vector2Int offset in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            Vector2Int neighborPos = node.position + offset;
            Node neighbor = GetNode(neighborPos);
            if (neighbor != null && neighbor.walkable)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors.ToArray();
    }

    void OnDrawGizmos()
    {
        if (grid == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = grid[x, y];
                Vector3 worldPos = GetWorldPosition(node.position);
                Gizmos.color = node.walkable ? Color.white : Color.red;
                Gizmos.DrawWireCube(worldPos, Vector3.one * nodeSize);
            }
        }
    }
}
