using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attachments 탭의 잉여 부착물 카드 1개.
/// 현재 역할:
/// - 아이콘 표시
/// - 이름 표시
/// - 타입 표시
/// - hover tooltip 표시
/// - drag source 역할
/// </summary>
public class AttachmentInventoryItemUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerMoveHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;

    [Header("Dependencies")]
    [SerializeField] private InventoryUIController ownerController;
    [SerializeField] private AttachmentDragGhostUI dragGhostUI;

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackSprite;

    private WeaponAttachmentData boundAttachment;

    private void Awake()
    {
        if (ownerController == null)
            ownerController = FindFirstObjectByType<InventoryUIController>();

        if (dragGhostUI == null)
            dragGhostUI = FindFirstObjectByType<AttachmentDragGhostUI>();
    }

    /// <summary>
    /// 리스트에서 아이템 생성 후 데이터 연결
    /// </summary>
    public void Bind(WeaponAttachmentData attachment, InventoryUIController controller)
    {
        boundAttachment = attachment;

        if (controller != null)
        {
            ownerController = controller;
            dragGhostUI = controller.AttachmentDragGhostUI;
        }

        RefreshView();
    }

    private void RefreshView()
    {
        if (boundAttachment == null)
        {
            if (nameText != null)
                nameText.text = "None";

            if (typeText != null)
                typeText.text = "-";

            if (iconImage != null)
            {
                iconImage.sprite = fallbackSprite;
                iconImage.enabled = fallbackSprite != null;
            }

            return;
        }

        if (nameText != null)
            nameText.text = boundAttachment.attachmentName;

        if (typeText != null)
            typeText.text = boundAttachment.attachmentType.ToString();

        if (iconImage != null)
        {
            // 현재 실제 필드명은 attachmentSpirte
            Sprite displaySprite = boundAttachment.attachmentSprite != null
                ? boundAttachment.attachmentSprite
                : fallbackSprite;

            iconImage.sprite = displaySprite;
            iconImage.enabled = displaySprite != null;
        }
    }

    // -----------------------------
    // Hover Tooltip
    // -----------------------------

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (boundAttachment == null || ownerController == null)
            return;

        if (AttachmentDragState.IsDragging)
            return;

        ownerController.ShowAttachmentTooltip(boundAttachment, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (ownerController == null)
            return;

        if (AttachmentDragState.IsDragging)
            return;

        ownerController.UpdateAttachmentTooltipPosition(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ownerController == null)
            return;

        ownerController.HideAttachmentTooltip();
    }

    // -----------------------------
    // Drag
    // -----------------------------
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (boundAttachment == null)
            return;

        if (ownerController == null)
            return;

        // Combat에서는 편집 금지
        if (!ownerController.CanProcessAttachmentEdit())
            return;

        ownerController.HideAttachmentTooltip();

        AttachmentDragState.BeginDrag(boundAttachment, AttachmentDragOrigin.Inventory);

        if (dragGhostUI != null)
        {
            // 네 필드명이 attachmentSpirte면 그걸로 바꿔라.
            dragGhostUI.Show(boundAttachment.attachmentSprite, eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhostUI != null)
            dragGhostUI.Hide();

        AttachmentDragState.EndDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhostUI == null)
            return;


        if (!AttachmentDragState.IsDragging)
            return;

        if (dragGhostUI != null)
            dragGhostUI.UpdatePosition(eventData.position);
    }

    private void OnDisable()
    {
        if (ownerController != null)
            ownerController.HideAttachmentTooltip();

        if (AttachmentDragState.IsDragging)
            AttachmentDragState.EndDrag();

        if (dragGhostUI != null)
            dragGhostUI.Hide();
    }
}