using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class WeaponTooltipUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject tooltipRoot;

    [Header("Text")]
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private TMP_Text weaponTypeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text attachmentSlotText;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        Hide();
    }

    public void Show(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            Hide();
            return;
        }

        if (tooltipRoot != null)
            tooltipRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        if (weaponNameText != null)
            weaponNameText.text = weaponData.weaponName;

        if (weaponTypeText != null)
            weaponTypeText.text = weaponData.weaponType.ToString();

        if (descriptionText != null)
            descriptionText.text = BuildDescriptionText(weaponData);

        if (attachmentSlotText != null)
            attachmentSlotText.text = BuildAttachmentSlotText(weaponData);
    }

    public void Hide()
    {
        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void SetScreenPosition(Vector2 screenPosition)
    {
        if (rectTransform != null)
            rectTransform.position = screenPosition;
        else
            transform.position = screenPosition;
    }

    private string BuildDescriptionText(WeaponData weaponData)
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("AP Cost : " + weaponData.apCost);
        builder.AppendLine("Attack Slots : " + weaponData.slotCapacity);
        builder.AppendLine("Damage Multiplier : x" + weaponData.weaponDamageMultiplier);
        builder.AppendLine("Spread : " + weaponData.aimSpread);
        builder.AppendLine("Projectiles : " + weaponData.projectilesPerAttack);
        builder.AppendLine("Optimal Range : " + weaponData.optimalRangeMax);
        builder.AppendLine("Max Range : " + weaponData.maxRange);
        builder.AppendLine("Optimal Damage : x" + weaponData.optimalDamageMultiplier);
        builder.AppendLine("Far Damage : x" + weaponData.farDamageMultiplier);

        return builder.ToString();
    }

    private string BuildAttachmentSlotText(WeaponData weaponData)
    {
        if (weaponData.allowedAttachmentTypes == null ||
            weaponData.allowedAttachmentTypes.Length == 0)
        {
            return "Attachment Slots : None";
        }

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < weaponData.allowedAttachmentTypes.Length; i++)
        {
            builder.Append(weaponData.allowedAttachmentTypes[i]);

            if (i < weaponData.allowedAttachmentTypes.Length - 1)
                builder.Append(", ");
        }

        return builder.ToString();
    }
}