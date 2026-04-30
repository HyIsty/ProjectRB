using UnityEngine;

/// <summary>
/// 타일 1칸을 표시하는 단일 하이라이터.
/// 
/// 역할:
/// - 특정 gridPos 위치에 표시
/// - 색 변경
/// - 표시 / 숨김
///
/// 중요:
/// 이 스크립트는 "계산"을 하지 않는다.
/// 이동 가능 칸 계산은 MoveRangeHighlighter 같은 상위 관리자가 담당한다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class TileClickHighlighter : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    // 런타임 생성용 공용 흰 사각형 스프라이트 캐시
    private static Sprite cachedSquareSprite;

    /// <summary>
    /// 현재 이 하이라이터가 표시 중인 그리드 좌표.
    /// </summary>
    public Vector2Int CurrentGridPos { get; private set; }

    /// <summary>
    /// 현재 표시 중인지 여부.
    /// </summary>
    public bool IsVisible => spriteRenderer != null && spriteRenderer.enabled;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        Hide();
    }

    /// <summary>
    /// 외부에서 SpriteRenderer를 직접 주입할 때 사용.
    /// </summary>
    public void SetSpriteRenderer(SpriteRenderer targetRenderer)
    {
        spriteRenderer = targetRenderer;
    }

    /// <summary>
    /// 특정 타일 위치에 하이라이터를 표시한다.
    /// </summary>
    public void Show(BoardManager boardManager, Vector2Int gridPos, Color color)
    {
        if (boardManager == null || spriteRenderer == null)
            return;

        CurrentGridPos = gridPos;

        // 보드 타일 위치로 이동
        transform.position = boardManager.GridToWorld(gridPos);

        // cellSize에 맞춰 크기 조절
        transform.localScale = new Vector3(boardManager.CellSize, boardManager.CellSize, 1f);

        // 색 적용
        spriteRenderer.color = color;
        spriteRenderer.enabled = true;
    }

    /// <summary>
    /// 색상만 갱신한다.
    /// </summary>
    public void SetColor(Color color)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = color;
    }

    /// <summary>
    /// 즉시 숨긴다.
    /// </summary>
    public void Hide()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    /// <summary>
    /// 프리팹 없이 런타임에 하이라이트 오브젝트를 자동 생성한다.
    /// </summary>
    public static TileClickHighlighter CreateRuntimeInstance(string objectName = "Runtime_TileHighlighter", int sortingOrder = 100)
    {
        GameObject go = new GameObject(objectName);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateSquareSprite();
        sr.sortingOrder = sortingOrder;

        TileClickHighlighter highlighter = go.AddComponent<TileClickHighlighter>();
        highlighter.SetSpriteRenderer(sr);
        highlighter.Hide();

        return highlighter;
    }

    /// <summary>
    /// 1x1 월드 유닛 크기로 쓰기 좋은 흰 사각형 스프라이트를 만든다.
    /// Texture2D.whiteTexture를 기반으로 생성한다.
    /// </summary>
    private static Sprite GetOrCreateSquareSprite()
    {
        if (cachedSquareSprite != null)
            return cachedSquareSprite;

        Texture2D texture = Texture2D.whiteTexture;

        // pixelsPerUnit을 texture.width로 주면 가로 크기가 1 world unit이 된다.
        cachedSquareSprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            texture.width
        );

        return cachedSquareSprite;
    }
}