using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 장착된 부착물을 Attachments 탭 영역으로 드롭하면 해제하는 target.
/// inventory side로 되돌리는 최소 안전 버전.
/// </summary>
public class UnequipAttachmentDropTarget : MonoBehaviour,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Dependencies")]
    [SerializeField] private InventoryUIController inventoryUIController;

    [Header("Optional Highlight")]
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color validDragColor = new Color(0.5f, 1f, 0.5f, 1f);

    private void Awake()
    {
        if (inventoryUIController == null)
            inventoryUIController = FindFirstObjectByType<InventoryUIController>();

        ApplyNormalColor();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanAcceptCurrentDrag())
            return;

        ApplyValidColor();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyNormalColor();
    }

    public void OnDrop(PointerEventData eventData)
    {
        ApplyNormalColor();

        // Combat에서는 편집 금지
        // 선택 무기 없으면 해제 대상 없음
        if (!inventoryUIController.CanProcessAttachmentEdit())
            return;

        if (!CanAcceptCurrentDrag())
            return;

        WeaponAttachmentData draggedAttachment = AttachmentDragState.CurrentAttachment;
        if (draggedAttachment == null)
            return;

        inventoryUIController.TryUnequipAttachmentFromSelectedWeapon(draggedAttachment.attachmentType);
    }

    private bool CanAcceptCurrentDrag()
    {
        if (inventoryUIController == null)
            return false;

        if (!AttachmentDragState.IsDragging)
            return false;

        if (AttachmentDragState.CurrentOrigin != AttachmentDragOrigin.Equipped)
            return false;

        if (AttachmentDragState.CurrentAttachment == null)
            return false;

        return true;
    }

    private void ApplyValidColor()
    {
        if (highlightImage != null)
            highlightImage.color = validDragColor;
    }

    private void ApplyNormalColor()
    {
        if (highlightImage != null)
            highlightImage.color = normalColor;
    }
}