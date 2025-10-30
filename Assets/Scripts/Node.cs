using UnityEngine;

public class Node
{
    public Vector2Int position;
    public bool walkable = true;
    public float gCost;
    public float hCost;
    public float fCost => gCost + hCost;
    public Node parent;

    public Node(Vector2Int pos)
    {
        position = pos;
    }
}
