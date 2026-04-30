using TMPro;
using UnityEngine;

/// <summary>
/// Attachment tooltip 표시 전용 UI.
/// 마우스 오른쪽 아래에 Tooltip의 왼쪽 위 모서리가 붙어서 따라다닌다.
/// </summary>
public class AttachmentTooltipUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private RectTransform tooltipRoot;
    [SerializeField] private Canvas parentCanvas;

    [Header("Texts")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Follow")]
    [SerializeField] private Vector2 screenOffset = new Vector2(20f, -20f);

    private void Awake()
    {
        if (tooltipRoot == null)
            tooltipRoot = transform as RectTransform;

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        if (tooltipRoot != null)
        {
            // Tooltip의 왼쪽 위 모서리가 기준점이 되게 한다.
            // 즉, 마우스 오른쪽 아래에 tooltip이 붙는다.
            tooltipRoot.pivot = new Vector2(0f, 1f);

            // ScreenPointToLocalPointInRectangle 결과는 Canvas 중심 기준 local 좌표라서
            // tooltip anchor도 중앙 고정으로 두는 게 위치 튐이 적다.
            tooltipRoot.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
        }

        Hide();
    }

    public void Show(WeaponAttachmentData data, Vector2 screenPosition)
    {
        if (data == null)
            return;

        if (nameText != null)
            nameText.text = data.attachmentName;

        if (typeText != null)
            typeText.text = data.attachmentType.ToString();

        if (descriptionText != null)
        {
            descriptionText.text = string.IsNullOrWhiteSpace(data.attachmentDescription)
                ? "No description"
                : data.attachmentDescription;
        }

        gameObject.SetActive(true);
        Follow(screenPosition);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Follow(Vector2 screenPosition)
    {
        if (tooltipRoot == null || parentCanvas == null)
            return;

        RectTransform canvasRect = parentCanvas.transform as RectTransform;

        if (canvasRect == null)
            return;

        Camera uiCamera = null;

        if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = parentCanvas.worldCamera;

        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            uiCamera,
            out Vector2 localPoint
        );

        if (!converted)
            return;

        // Pivot이 왼쪽 위라서,
        // anchoredPosition은 tooltip의 왼쪽 위 위치가 된다.
        tooltipRoot.anchoredPosition = localPoint + screenOffset;
    }
}