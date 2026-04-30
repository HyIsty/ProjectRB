using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WeaponInspectPopupDropTarget : MonoBehaviour,
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
    [SerializeField] private Color invalidDragColor = new Color(1f, 0.5f, 0.5f, 1f);

    private void Awake()
    {
        if (inventoryUIController == null)
            inventoryUIController = FindFirstObjectByType<InventoryUIController>();

        ApplyNormalColor();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!AttachmentDragState.IsDragging)
            return;

        bool canDrop = CanAcceptCurrentDrag();
        ApplyHoverColor(canDrop);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyNormalColor();
    }

    public void OnDrop(PointerEventData eventData)
    {
        ApplyNormalColor();

        if (!AttachmentDragState.IsDragging)
            return;

        if (inventoryUIController == null)
            return;

        if (!inventoryUIController.CanProcessAttachmentEdit())
            return;

        WeaponAttachmentData draggedAttachment = AttachmentDragState.CurrentAttachment;
        if (draggedAttachment == null)
            return;

        if (!CanAcceptCurrentDrag())
        {
            Debug.LogWarning(
                $"[WeaponInspectPopupDropTarget] Drop rejected: [{draggedAttachment.attachmentName}]",
                this
            );
            return;
        }

        inventoryUIController.TryEquipAttachmentByPopupDrop(draggedAttachment);
    }

    private bool CanAcceptCurrentDrag()
    {
        if (inventoryUIController == null)
            return false;

        if (!inventoryUIController.CanProcessAttachmentEdit())
            return false;

        WeaponAttachmentData draggedAttachment = AttachmentDragState.CurrentAttachment;
        if (draggedAttachment == null)
            return false;

        // 실제 selected weapon 검사 / slot type 허용 검사 / 교체 판정은
        // InventoryUIController 쪽에서 scene mode에 맞게 처리한다.
        return true;
    }

    private void ApplyHoverColor(bool valid)
    {
        if (highlightImage == null)
            return;

        highlightImage.color = valid ? validDragColor : invalidDragColor;
    }

    private void ApplyNormalColor()
    {
        if (highlightImage == null)
            return;

        highlightImage.color = normalColor;
    }
}