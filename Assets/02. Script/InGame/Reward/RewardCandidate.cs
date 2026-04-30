using System;
using UnityEngine;

public enum RewardType
{
    Weapon,
    Ammo,
    Attachment
}

[Serializable]
public class RewardCandidate
{
    [Header("Reward Type")]
    public RewardType rewardType;

    [Header("Reward Data")]
    public WeaponData weaponData;
    public AmmoModuleData ammoData;
    public WeaponAttachmentData attachmentData;

    public int goldAmount;

    public string GetDisplayName()
    {
        switch (rewardType)
        {
            case RewardType.Weapon:
                return weaponData != null ? weaponData.weaponName : "Missing Weapon";

            case RewardType.Ammo:
                return ammoData != null ? ammoData.displayName : "Missing Ammo";

            case RewardType.Attachment:
                return attachmentData != null ? attachmentData.attachmentName : "Missing Attachment";

            default:
                return "Unknown Reward";
        }
    }

    public string GetDescription()
    {
        switch (rewardType)
        {
            case RewardType.Weapon:
                if (weaponData == null)
                    return "No weapon data.";

                return
                    $"Type : {weaponData.weaponType}\n" +
                    $"AP Cost : {weaponData.apCost}\n" +
                    $"Slots : {weaponData.slotCapacity}\n" +
                    $"Max Range : {weaponData.maxRange}";

            case RewardType.Ammo:
                if (ammoData == null)
                    return "No ammo data.";

                return
                    $"Damage : {ammoData.damage}\n" +
                    $"{ammoData.description}";

            case RewardType.Attachment:
                if (attachmentData == null)
                    return "No attachment data.";

                return
                    $"Slot : {attachmentData.attachmentType}\n" +
                    $"{attachmentData.attachmentDescription}";

            default:
                return "";
        }
    }

    public Sprite GetIcon()
    {
        switch (rewardType)
        {
            case RewardType.Weapon:
                return weaponData != null ? weaponData.weaponSprite : null;

            case RewardType.Ammo:
                return ammoData != null ? ammoData.sprite : null;

            case RewardType.Attachment:
                // 네 현재 코드에서 attachmentSpirte 오타 필드를 쓰고 있으면 이 줄을 attachmentSpirte로 바꿔라.
                return attachmentData != null ? attachmentData.attachmentSprite : null;

            default:
                return null;
        }
    }
}