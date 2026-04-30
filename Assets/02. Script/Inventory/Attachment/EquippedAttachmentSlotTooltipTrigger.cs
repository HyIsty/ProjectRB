using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 무기 inspect popup의 장착 슬롯 hover용 input script.
/// 현재 슬롯에 장착된 attachment가 있을 때만 tooltip을 띄운다.
/// </summary>
public class EquippedAttachmentSlotTooltipTrigger : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerMoveHandler
{
    [Header("Dependencies")]
    [SerializeField] private InventoryUIController inventoryUIController;

    // 현재 이 슬롯에 바인딩된 부착물
    private WeaponAttachmentData boundAttachment;

    // 현재 무기에서 지원되는 슬롯인지
    private bool isSupportedSlot;

    private void Awake()
    {
        if (inventoryUIController == null)
            inventoryUIController = FindFirstObjectByType<InventoryUIController>();
    }

    /// <summary>
    /// inspect popup refresh 시 호출해서
    /// 이 슬롯의 현재 상태를 바인딩한다.
    /// </summary>
    public void Bind(
        WeaponAttachmentData attachment,
        bool supportedSlot,
        InventoryUIController controller = null)
    {
        boundAttachment = attachment;
        isSupportedSlot = supportedSlot;

        if (controller != null)
            inventoryUIController = controller;

        // 지원 안 하거나 attachment가 없으면 툴팁 꺼둔다.
        if (CanShowTooltip() == false)
            HideTooltip();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CanShowTooltip() == false)
            return;

        inventoryUIController.ShowAttachmentTooltip(boundAttachment, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (CanShowTooltip() == false)
            return;

        inventoryUIController.UpdateAttachmentTooltipPosition(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private bool CanShowTooltip()
    {
        if (inventoryUIController == null)
            return false;

        if (isSupportedSlot == false)
            return false;

        if (boundAttachment == null)
            return false;

        return true;
    }

    private void HideTooltip()
    {
        if (inventoryUIController != null)
            inventoryUIController.HideAttachmentTooltip();
    }
}