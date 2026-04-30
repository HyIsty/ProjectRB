using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RemoveAmmoRowItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Display")]
    [SerializeField] private TMP_Text leftBadgeText;
    [SerializeField] private TMP_Text ammoNameText;
    [SerializeField] private TMP_Text damageText;

    [Header("Optional Visual")]
    [SerializeField] private Image backgroundImage;

    private AmmoModuleData boundAmmo;
    private RemoveAmmoPopupUI ownerPopup;

    public void Bind(AmmoModuleData ammo, int count, RemoveAmmoPopupUI owner)
    {
        boundAmmo = ammo;
        ownerPopup = owner;

        if (leftBadgeText != null)
            leftBadgeText.text = "x" + count;

        if (ammoNameText != null)
            ammoNameText.text = ammo != null ? ammo.displayName : "Unknown Ammo";

        if (damageText != null)
            damageText.text = ammo != null ? ammo.damage.ToString() : "-";

        // 클릭을 받으려면 Image의 Raycast Target이 켜져 있어야 한다.
        if (backgroundImage != null)
            backgroundImage.raycastTarget = true;

        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (ownerPopup == null)
            return;

        if (boundAmmo == null)
            return;

        ownerPopup.SelectAmmo(boundAmmo);
    }
}