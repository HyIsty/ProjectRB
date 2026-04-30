using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShopStock
{
    public List<ShopItemCandidate> topRandomItems = new List<ShopItemCandidate>();
    public List<ShopItemCandidate> bottomAmmoItems = new List<ShopItemCandidate>();
    public List<ShopItemCandidate> bottomWeaponItems = new List<ShopItemCandidate>();
}


[System.Serializable]
public class ShopItemCandidate
{
    [Header("Item Type")]
    public RewardType itemType;

    [Header("Item Data")]
    public WeaponData weaponData;
    public AmmoModuleData ammoData;
    public WeaponAttachmentData attachmentData;

    [Header("Shop Price")]
    public int price;

    public string GetDisplayName()
    {
        switch (itemType)
        {
            case RewardType.Weapon:
                return weaponData != null ? weaponData.weaponName : "Unknown Weapon";

            case RewardType.Ammo:
                return ammoData != null ? ammoData.displayName : "Unknown Ammo";

            case RewardType.Attachment:
                return attachmentData != null ? attachmentData.attachmentName : "Unknown Attachment";

            default:
                return "Unknown Item";
        }
    }

    public string GetDescription()
    {
        switch (itemType)
        {
            case RewardType.Weapon:
                return "Buy this weapon.";

            case RewardType.Ammo:
                return ammoData != null ? ammoData.description : "Buy this ammo module.";

            case RewardType.Attachment:
                return attachmentData != null ? attachmentData.attachmentDescription : "Buy this attachment.";

            default:
                return "";
        }
    }

    public Sprite GetIcon()
    {
        switch (itemType)
        {
            case RewardType.Weapon:
                return weaponData != null ? weaponData.weaponSprite : null;

            case RewardType.Ammo:
                return ammoData != null ? ammoData.sprite : null;

            case RewardType.Attachment:
                // 네 프로젝트 현재 필드명이 attachmentSpirte면 그대로 쓴다.
                return attachmentData != null ? attachmentData.attachmentSprite : null;

            default:
                return null;
        }
    }
}