using System.Collections.Generic;
using UnityEngine;

public class TurnGridBoardExample : MonoBehaviour
{
    [Header("Board Size")]
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 6;
    [SerializeField] private float cellSize = 1.5f;

    [Header("Blocked Cells")]
    [SerializeField] private List<Vector2Int> blockedCells = new List<Vector2Int>();

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    public bool IsInsideBoard(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < width &&
               gridPos.y >= 0 && gridPos.y < height;
    }

    public bool IsBlocked(Vector2Int gridPos)
    {
        return blockedCells.Contains(gridPos);
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return transform.position + new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0f);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - transform.position;

        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.y / cellSize);

        return new Vector2Int(x, y);
    }

    public int GetManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = GridToWorld(gridPos);

                Gizmos.DrawWireCube(worldPos, Vector3.one * (cellSize * 0.95f));
            }
        }

        Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        for (int i = 0; i < blockedCells.Count; i++)
        {
            if (!IsInsideBoard(blockedCells[i]))
                continue;

            Vector3 worldPos = GridToWorld(blockedCells[i]);
            Gizmos.DrawCube(worldPos, Vector3.one * (cellSize * 0.7f));
        }
    }
}