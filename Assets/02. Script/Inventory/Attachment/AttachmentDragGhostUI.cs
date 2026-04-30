using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 드래그 중 마우스를 따라다니는 고스트 아이콘 UI.
/// Screen Space - Overlay / Camera Canvas 둘 다 대응 가능하도록
/// 화면 좌표를 Canvas 로컬 좌표로 변환해서 이동한다.
/// </summary>
public class AttachmentDragGhostUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform rootRectTransform;
    [SerializeField] private RectTransform canvasRectTransform;
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Follow Offset")]
    [SerializeField] private Vector2 screenOffset = new Vector2(24f, -24f);

    private Camera uiCamera;

    private void Awake()
    {
        // Inspector에서 안 넣었으면 자동 탐색
        if (rootRectTransform == null)
            rootRectTransform = GetComponent<RectTransform>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (canvasRectTransform == null && rootCanvas != null)
            canvasRectTransform = rootCanvas.GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        // UI 고스트는 포인터 입력을 막으면 안 된다.
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = rootCanvas.worldCamera;

        Hide();
    }

    /// <summary>
    /// 드래그 시작 시 아이콘 표시.
    /// </summary>
    public void Show(Sprite sprite, Vector2 screenPosition)
    {
        if (iconImage != null)
            iconImage.sprite = sprite;

        gameObject.SetActive(true);
        UpdatePosition(screenPosition);
    }

    /// <summary>
    /// 드래그 중 마우스 위치를 따라 이동.
    /// </summary>
    public void UpdatePosition(Vector2 screenPosition)
    {
        if (rootRectTransform == null || canvasRectTransform == null)
            return;

        Vector2 localPoint;
        Vector2 targetScreenPoint = screenPosition + screenOffset;

        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            targetScreenPoint,
            uiCamera,
            out localPoint
        );

        if (!success)
            return;

        rootRectTransform.anchoredPosition = localPoint;
    }

    /// <summary>
    /// 드래그 종료 시 숨김.
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}