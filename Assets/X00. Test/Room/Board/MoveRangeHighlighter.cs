using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이동 대기 모드에서 플레이어 주변의 이동 가능 칸을 표시하는 관리자.
///
/// 중요:
/// - 이 스크립트는 더 이상 직접 입력을 읽지 않는다.
/// - 포인터 좌표는 PlayerInputManager가 전달한다.
/// - 실제 이동 실행은 하지 않고, 표시/hover만 담당한다.
///
/// 현재 최소 구현:
/// - 상하좌우 1칸 검사
/// - CanEnterTile 가능한 칸만 표시
/// - 전달받은 포인터 위치 기준으로 hover 강조
/// </summary>
public class MoveRangeHighlighter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private GridUnit playerUnit;
    [SerializeField] private Camera mainCamera;

    [Header("Colors")]
    [SerializeField] private Color availableColor = new Color(0.2f, 0.8f, 1f, 0.28f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.2f, 0.45f);

    [Header("Runtime Highlight Settings")]
    [SerializeField] private int sortingOrder = 100;
    [SerializeField] private string highlighterObjectNamePrefix = "MoveOption_";

    /// <summary>
    /// 현재 표시 중인 후보 칸들.
    /// key = gridPos
    /// value = 하이라이터 인스턴스
    /// </summary>
    private readonly Dictionary<Vector2Int, TileClickHighlighter> activeHighlighters = new Dictionary<Vector2Int, TileClickHighlighter>();

    /// <summary>
    /// 현재 hover 중인 이동 후보 칸.
    /// 후보 칸이 아니면 null.
    /// </summary>
    private Vector2Int? currentHoveredGridPos = null;

    public bool IsShowing { get; private set; }

    private static readonly Vector2Int[] CardinalDirections =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    public void BindPlayer(GridUnit unit)
    {
        if(unit != null)
        {
            playerUnit = unit;
        }
    }

    /// <summary>
    /// 플레이어 현재 위치 기준으로 이동 가능 칸을 표시한다.
    /// </summary>
    public void ShowMoveOptions()
    {
        if (boardManager == null || playerUnit == null)
            return;

        HideAll();

        Vector2Int origin = playerUnit.CurrentGridPos;

        for (int i = 0; i < CardinalDirections.Length; i++)
        {
            Vector2Int target = origin + CardinalDirections[i];

            if (!boardManager.CanEnterTile(target))
                continue;

            TileClickHighlighter highlighter = TileClickHighlighter.CreateRuntimeInstance(
                $"{highlighterObjectNamePrefix}{target.x}_{target.y}",
                sortingOrder
            );

            highlighter.transform.SetParent(transform, worldPositionStays: true);
            highlighter.Show(boardManager, target, availableColor);

            activeHighlighters.Add(target, highlighter);
        }

        IsShowing = true;
        currentHoveredGridPos = null;
    }

    /// <summary>
    /// 모든 하이라이트를 숨기고 정리한다.
    /// </summary>
    public void HideAll()
    {
        foreach (var pair in activeHighlighters)
        {
            if (pair.Value != null)
                Destroy(pair.Value.gameObject);
        }

        activeHighlighters.Clear();
        currentHoveredGridPos = null;
        IsShowing = false;
    }

    /// <summary>
    /// 현재 표시 중인 후보 칸인지 여부.
    /// </summary>
    public bool IsMoveOption(Vector2Int gridPos)
    {
        return activeHighlighters.ContainsKey(gridPos);
    }

    /// <summary>
    /// 현재 hover 중인 이동 후보 칸 반환.
    /// </summary>
    public Vector2Int? GetHoveredMoveOption()
    {
        return currentHoveredGridPos;
    }

    /// <summary>
    /// 외부에서 전달받은 "포인터 스크린 좌표" 기준으로 hover를 갱신한다.
    /// 
    /// 이 함수는 PlayerInputManager가 MovePreview 상태일 때 매 프레임 호출하면 된다.
    /// </summary>
    public void TickHoverFromScreenPosition(Vector2 screenPosition)
    {
        if (!IsShowing)
            return;

        if (boardManager == null || mainCamera == null)
            return;

        Vector3 screenPos = new Vector3(
            screenPosition.x,
            screenPosition.y,
            -mainCamera.transform.position.z
        );

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        Vector2Int hoveredGrid = boardManager.WorldToGrid(worldPos);

        Vector2Int? nextHovered = null;

        if (activeHighlighters.ContainsKey(hoveredGrid))
            nextHovered = hoveredGrid;

        if (currentHoveredGridPos == nextHovered)
            return;

        // 이전 hover 색 원복
        if (currentHoveredGridPos.HasValue &&
            activeHighlighters.TryGetValue(currentHoveredGridPos.Value, out TileClickHighlighter prevHighlighter) &&
            prevHighlighter != null)
        {
            prevHighlighter.SetColor(availableColor);
        }

        currentHoveredGridPos = nextHovered;

        // 새 hover 적용
        if (currentHoveredGridPos.HasValue &&
            activeHighlighters.TryGetValue(currentHoveredGridPos.Value, out TileClickHighlighter newHighlighter) &&
            newHighlighter != null)
        {
            newHighlighter.SetColor(hoverColor);
        }
    }

    /// <summary>
    /// 현재 포인터 스크린 좌표가 이동 후보 칸 위에 있다면 그 좌표를 반환한다.
    /// 
    /// 반환값:
    /// - true  : 이동 후보 칸 위에 있음
    /// - false : 이동 후보 칸 아님
    /// </summary>
    public bool TryGetMoveOptionFromScreenPosition(Vector2 screenPosition, out Vector2Int targetGridPos)
    {
        targetGridPos = default;

        if (!IsShowing)
            return false;

        if (boardManager == null || mainCamera == null)
            return false;

        Vector3 screenPos = new Vector3(
            screenPosition.x,
            screenPosition.y,
            -mainCamera.transform.position.z
        );

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        Vector2Int gridPos = boardManager.WorldToGrid(worldPos);

        if (!activeHighlighters.ContainsKey(gridPos))
            return false;

        targetGridPos = gridPos;
        return true;
    }
}