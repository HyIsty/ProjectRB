using UnityEngine;

public class ShopTooltipController : MonoBehaviour
{
    [Header("Existing Tooltips")]
    [SerializeField] private AmmoTooltipUI ammoTooltipUI;
    [SerializeField] private AttachmentTooltipUI attachmentTooltipUI;

    [Header("New Tooltip")]
    [SerializeField] private WeaponTooltipUI weaponTooltipUI;

    [Header("Position")]
    [SerializeField] private Vector2 screenOffset = new Vector2(20f, -20f);

    private RewardType currentType;

    public void Show(ShopItemCandidate item, Vector2 screenPosition)
    {
        if (item == null)
            return;

        Hide();

        currentType = item.itemType;

        switch (item.itemType)
        {
            case RewardType.Ammo:
                ShowAmmo(item.ammoData, screenPosition);
                break;

            case RewardType.Attachment:
                ShowAttachment(item.attachmentData, screenPosition);
                break;

            case RewardType.Weapon:
                ShowWeapon(item.weaponData, screenPosition);
                break;
        }
    }

    public void Move(Vector2 screenPosition)
    {
        Vector2 targetPosition = screenPosition + screenOffset;

        switch (currentType)
        {
            case RewardType.Ammo:
                // 네 기존 AmmoTooltipUI가 자체적으로 마우스 추적하면 이 줄은 없어도 됨.
                if (ammoTooltipUI != null)
                    ammoTooltipUI.transform.position = targetPosition;
                break;
            case RewardType.Attachment:
                if (attachmentTooltipUI != null)
                    attachmentTooltipUI.Follow(screenPosition);
                break;

            case RewardType.Weapon:
                if (weaponTooltipUI != null)
                    weaponTooltipUI.SetScreenPosition(targetPosition);
                break;
        }
    }

    public void Hide()
    {
        if (ammoTooltipUI != null)
            ammoTooltipUI.Hide();

        if (attachmentTooltipUI != null)
            attachmentTooltipUI.Hide();

        if (weaponTooltipUI != null)
            weaponTooltipUI.Hide();
    }

    private void ShowAmmo(AmmoModuleData ammoData, Vector2 screenPosition)
    {
        if (ammoData == null || ammoTooltipUI == null)
            return;

        // 여기 함수명은 네 기존 AmmoTooltipUI에 맞춰야 한다.
        // 만약 Show(ammoData, int previewDelta) 구조면 아래처럼.
        ammoTooltipUI.ShowForAmmo(ammoData, 0);

        ammoTooltipUI.transform.position = screenPosition + screenOffset;
    }

    private void ShowAttachment(WeaponAttachmentData attachmentData, Vector2 screenPosition)
    {
        if (attachmentData == null || attachmentTooltipUI == null)
            return;

        attachmentTooltipUI.Show(attachmentData, screenPosition);
    }

    private void ShowWeapon(WeaponData weaponData, Vector2 screenPosition)
    {
        if (weaponData == null || weaponTooltipUI == null)
            return;

        weaponTooltipUI.Show(weaponData);
        weaponTooltipUI.SetScreenPosition(screenPosition + screenOffset);
    }
}