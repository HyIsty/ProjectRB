using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 장착 슬롯을 drag source로 만들어주는 얇은 입력 스크립트.
/// 실제 장착/해제 처리 로직은 controller가 한다.
/// </summary>
public class EquippedAttachmentSlotDragSource : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Dependencies")]
    [SerializeField] private InventoryUIController inventoryUIController;
    [SerializeField] private AttachmentDragGhostUI dragGhostUI;

    private WeaponAttachmentData boundAttachment;
    private bool isSupportedSlot;

    private void Awake()
    {
        if (inventoryUIController == null)
            inventoryUIController = FindFirstObjectByType<InventoryUIController>();

        if (dragGhostUI == null)
            dragGhostUI = FindFirstObjectByType<AttachmentDragGhostUI>();
    }

    public void Bind(WeaponAttachmentData attachment, bool supportedSlot)
    {
        boundAttachment = attachment;
        isSupportedSlot = supportedSlot;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag())
            return;

        if (inventoryUIController != null)
            inventoryUIController.HideAttachmentTooltip();

        AttachmentDragState.BeginDrag(boundAttachment, AttachmentDragOrigin.Equipped);

        if (dragGhostUI != null)
            dragGhostUI.Show(boundAttachment.attachmentSprite, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhostUI == null)
            return;

        if (!AttachmentDragState.IsDragging)
            return;

        dragGhostUI.UpdatePosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragGhostUI != null)
            dragGhostUI.Hide();

        AttachmentDragState.EndDrag();
    }

    private bool CanStartDrag()
    {
        if (!isSupportedSlot)
            return false;

        if (boundAttachment == null)
            return false;

        return true;
    }
}