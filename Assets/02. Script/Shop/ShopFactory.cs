using System.Collections.Generic;
using UnityEngine;

public class ShopFactory : MonoBehaviour
{
    [SerializeField] private ItemPoolDatabase itemPoolDatabase;
    [Header("Stock Counts")]
    [SerializeField] private int topRandomCount = 5;
    [SerializeField] private int bottomAmmoCount = 3;
    [SerializeField] private int bottomWeaponCount = 2;

    [Header("Duplicate Rule")]
    [SerializeField] private bool allowDuplicateItems = false;

    [Header("Price Variance")]
    [SerializeField] private int priceVariance = 0;

    public ShopStock GenerateShopStock()
    {
        ShopStock stock = new ShopStock();

        stock.topRandomItems = GenerateTopRandomItems(topRandomCount);
        stock.bottomAmmoItems = GenerateFixedAmmoItems(bottomAmmoCount);
        stock.bottomWeaponItems = GenerateFixedWeaponItems(bottomWeaponCount);

        return stock;
    }

    private List<ShopItemCandidate> GenerateTopRandomItems(int count)
    {
        List<ShopItemCandidate> result = new List<ShopItemCandidate>();

        for (int i = 0; i < count; i++)
        {
            ShopItemCandidate item = CreateRandomItem(result);

            if (item != null)
                result.Add(item);
        }

        return result;
    }

    private List<ShopItemCandidate> GenerateFixedAmmoItems(int count)
    {
        List<ShopItemCandidate> result = new List<ShopItemCandidate>();

        for (int i = 0; i < count; i++)
        {
            ShopItemCandidate item = CreateAmmoItem();

            if (item == null)
                continue;

            if (!allowDuplicateItems && IsDuplicateItem(item, result))
            {
                i--;
                continue;
            }

            result.Add(item);
        }

        return result;
    }

    private List<ShopItemCandidate> GenerateFixedWeaponItems(int count)
    {
        List<ShopItemCandidate> result = new List<ShopItemCandidate>();

        for (int i = 0; i < count; i++)
        {
            ShopItemCandidate item = CreateWeaponItem();

            if (item == null)
                continue;

            if (!allowDuplicateItems && IsDuplicateItem(item, result))
            {
                i--;
                continue;
            }

            result.Add(item);
        }

        return result;
    }

    private ShopItemCandidate CreateRandomItem(List<ShopItemCandidate> existingItems)
    {
        List<RewardType> possibleTypes = GetAvailableTypes();

        if (possibleTypes.Count == 0)
        {
            Debug.LogWarning("[ShopFactory] No shop item pools available.");
            return null;
        }

        for (int i = 0; i < 20; i++)
        {
            RewardType type = possibleTypes[Random.Range(0, possibleTypes.Count)];
            ShopItemCandidate item = CreateItemByType(type);

            if (item == null)
                continue;

            if (allowDuplicateItems || !IsDuplicateItem(item, existingItems))
                return item;
        }

        RewardType fallbackType = possibleTypes[Random.Range(0, possibleTypes.Count)];
        return CreateItemByType(fallbackType);
    }

    private List<RewardType> GetAvailableTypes()
    {
        List<RewardType> types = new List<RewardType>();

        if (itemPoolDatabase == null)
            return types;

        if (itemPoolDatabase.HasWeapons())
            types.Add(RewardType.Weapon);

        if (itemPoolDatabase.HasAmmo())
            types.Add(RewardType.Ammo);

        if (itemPoolDatabase.HasAttachments())
            types.Add(RewardType.Attachment);


        return types;
    }

    private ShopItemCandidate CreateItemByType(RewardType type)
    {
        switch (type)
        {
            case RewardType.Weapon:
                return CreateWeaponItem();

            case RewardType.Ammo:
                return CreateAmmoItem();

            case RewardType.Attachment:
                return CreateAttachmentItem();

            default:
                return null;
        }
    }

    private ShopItemCandidate CreateWeaponItem()
    {
        if (itemPoolDatabase == null || !itemPoolDatabase.HasWeapons())
            return null;

        WeaponData weapon = itemPoolDatabase.GetRandomWeapon();

        ShopItemCandidate item = new ShopItemCandidate();
        item.itemType = RewardType.Weapon;
        item.weaponData = weapon;
        item.price = ApplyPriceVariance(GetWeaponPrice(weapon));

        return item;
    }

    private ShopItemCandidate CreateAmmoItem()
    {
        if (itemPoolDatabase == null || !itemPoolDatabase.HasAmmo())
            return null;

        AmmoModuleData ammo = itemPoolDatabase.GetRandomAmmo();

        ShopItemCandidate item = new ShopItemCandidate();
        item.itemType = RewardType.Ammo;
        item.ammoData = ammo;
        item.price = ApplyPriceVariance(GetAmmoPrice(ammo));

        return item;
    }

    private ShopItemCandidate CreateAttachmentItem()
    {
        if (itemPoolDatabase == null || !itemPoolDatabase.HasAttachments())
            return null;

        WeaponAttachmentData attachment = itemPoolDatabase.GetRandomAttachment();

        ShopItemCandidate item = new ShopItemCandidate();
        item.itemType = RewardType.Attachment;
        item.attachmentData = attachment;
        item.price = ApplyPriceVariance(GetAttachmentPrice(attachment));

        return item;
    }

    private int GetWeaponPrice(WeaponData weapon)
    {
        if (weapon == null)
            return 0;

        return Mathf.Max(1, weapon.shopPrice);
    }

    private int GetAmmoPrice(AmmoModuleData ammo)
    {
        if (ammo == null)
            return 0;

        return Mathf.Max(1, ammo.shopPrice);
    }

    private int GetAttachmentPrice(WeaponAttachmentData attachment)
    {
        if (attachment == null)
            return 0;

        return Mathf.Max(1, attachment.shopPrice);
    }

    private int ApplyPriceVariance(int basePrice)
    {
        if (priceVariance <= 0)
            return Mathf.Max(1, basePrice);

        int rolled = basePrice + Random.Range(-priceVariance, priceVariance + 1);
        return Mathf.Max(1, rolled);
    }

    private bool IsDuplicateItem(ShopItemCandidate item, List<ShopItemCandidate> existingItems)
    {
        if (item == null || existingItems == null)
            return false;

        for (int i = 0; i < existingItems.Count; i++)
        {
            ShopItemCandidate existing = existingItems[i];

            if (existing == null)
                continue;

            if (existing.itemType != item.itemType)
                continue;

            switch (item.itemType)
            {
                case RewardType.Weapon:
                    if (existing.weaponData == item.weaponData)
                        return true;
                    break;

                case RewardType.Ammo:
                    if (existing.ammoData == item.ammoData)
                        return true;
                    break;

                case RewardType.Attachment:
                    if (existing.attachmentData == item.attachmentData)
                        return true;
                    break;
            }
        }

        return false;
    }
}