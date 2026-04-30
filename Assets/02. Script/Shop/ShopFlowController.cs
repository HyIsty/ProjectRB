using System.Collections.Generic;
using UnityEngine;
using System;

public class ShopFlowController : MonoBehaviour
{
    [Header("Shop References")]
    [SerializeField] private ShopFactory shopFactory;
    [SerializeField] private ShopPanelUI shopPanelUI;
    [SerializeField] private WeaponReplacePopupUI weaponReplacePopupUI;
    [SerializeField] private RemoveAmmoPopupUI removeAmmoPopupUI;

    [Header("Remove Ammo Service")]
    [SerializeField] private int removeAmmoPriceIncrease = 25;

    private ShopItemCardUI pendingWeaponCard;
    private ShopItemCandidate pendingWeaponItem;

    private bool hasUsedRemoveAmmoService;

    public bool HasUsedRemoveAmmoService => hasUsedRemoveAmmoService;

    public event Action OnShopClosed;

    public void OpenShop()
    {
        hasUsedRemoveAmmoService = false;

        if (shopFactory == null)
        {
            Debug.LogWarning("[ShopFlowController] ShopFactory is missing.");
            return;
        }

        if (shopPanelUI == null)
        {
            Debug.LogWarning("[ShopFlowController] ShopPanelUI is missing.");
            return;
        }

        ShopStock stock = shopFactory.GenerateShopStock();

        shopPanelUI.Show(stock, this);

        if (weaponReplacePopupUI != null)
            weaponReplacePopupUI.Hide();

        if (removeAmmoPopupUI != null)
            removeAmmoPopupUI.Hide();
    }

    public void CloseShop()
    {
        ClearPendingWeaponPurchase();

        if (weaponReplacePopupUI != null)
            weaponReplacePopupUI.Hide();

        if (removeAmmoPopupUI != null)
            removeAmmoPopupUI.Hide();

        if (shopPanelUI != null)
            shopPanelUI.Hide();

        OnShopClosed?.Invoke();
    }

    public void TryBuyItem(ShopItemCardUI card)
    {
        if (card == null || card.CurrentItem == null)
            return;

        if (card.IsSoldOut)
            return;

        RunData runData = GetRunData();

        if (runData == null)
            return;

        ShopItemCandidate item = card.CurrentItem;

        if (!CanAfford(runData, item.price))
        {
            Debug.Log("[Shop] Not enough gold.");
            return;
        }

        switch (item.itemType)
        {
            case RewardType.Weapon:
                TryBuyWeapon(card, item, runData);
                break;

            case RewardType.Ammo:
                BuyAmmo(card, item, runData);
                break;

            case RewardType.Attachment:
                BuyAttachment(card, item, runData);
                break;
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayShopBuy();
    }

    private void BuyAmmo(ShopItemCardUI card, ShopItemCandidate item, RunData runData)
    {
        if (item.ammoData == null)
            return;

        SpendGold(runData, item.price);

        if (runData.ammoDeck == null)
            runData.ammoDeck = new List<AmmoModuleData>();

        runData.ammoDeck.Add(item.ammoData);

        card.SetSoldOut();

        RefreshShopUI();

        Debug.Log($"[Shop] Bought ammo: {item.ammoData.displayName}");
        if (TopRunDataUI.Instance != null)
            TopRunDataUI.Instance.Refresh();
    }

    private void BuyAttachment(ShopItemCardUI card, ShopItemCandidate item, RunData runData)
    {
        if (item.attachmentData == null)
            return;

        SpendGold(runData, item.price);

        if (runData.inventory == null)
            runData.inventory = new InventoryData();

        if (runData.inventory.spareAttachments == null)
            runData.inventory.spareAttachments = new List<WeaponAttachmentData>();

        runData.inventory.spareAttachments.Add(item.attachmentData);

        card.SetSoldOut();

        RefreshShopUI();

        Debug.Log($"[Shop] Bought attachment: {item.attachmentData.attachmentName}");
        if (TopRunDataUI.Instance != null)
            TopRunDataUI.Instance.Refresh();
    }

    private void TryBuyWeapon(ShopItemCardUI card, ShopItemCandidate item, RunData runData)
    {
        if (item.weaponData == null)
            return;

        int emptySlotIndex = FindEmptyWeaponSlot(runData);

        if (emptySlotIndex >= 0)
        {
            SpendGold(runData, item.price);
            SetWeaponToSlot(runData, emptySlotIndex, item.weaponData);

            card.SetSoldOut();

            RefreshShopUI();

            Debug.Log($"[Shop] Bought weapon into empty slot: {item.weaponData.weaponName}");
            if (TopRunDataUI.Instance != null)
                TopRunDataUI.Instance.Refresh();
            return;
        }

        pendingWeaponCard = card;
        pendingWeaponItem = item;

        if (weaponReplacePopupUI == null)
        {
            Debug.LogWarning("[ShopFlowController] WeaponReplacePopupUI is missing.");
            ClearPendingWeaponPurchase();
            return;
        }

        weaponReplacePopupUI.Show(
            runData,
            OnWeaponReplaceConfirmed,
            OnWeaponReplaceCanceled
        );
    }

    private void OnWeaponReplaceConfirmed(int slotIndex)
    {
        if (pendingWeaponCard == null || pendingWeaponItem == null)
            return;

        RunData runData = GetRunData();

        if (runData == null)
            return;

        if (!CanAfford(runData, pendingWeaponItem.price))
        {
            Debug.Log("[Shop] Not enough gold when confirming weapon replacement.");
            ClearPendingWeaponPurchase();
            RefreshShopUI();
            return;
        }

        SpendGold(runData, pendingWeaponItem.price);
        SetWeaponToSlot(runData, slotIndex, pendingWeaponItem.weaponData);

        pendingWeaponCard.SetSoldOut();

        if (weaponReplacePopupUI != null)
            weaponReplacePopupUI.Hide();

        Debug.Log($"[Shop] Replaced weapon slot {slotIndex} with {pendingWeaponItem.weaponData.weaponName}");

        ClearPendingWeaponPurchase();
        if (TopRunDataUI.Instance != null)
            TopRunDataUI.Instance.Refresh();
        RefreshShopUI();
    }

    private void OnWeaponReplaceCanceled()
    {
        ClearPendingWeaponPurchase();

        if (weaponReplacePopupUI != null)
            weaponReplacePopupUI.Hide();

        // 구매 취소이므로 골드 차감 없음.
        // ShopPanel은 유지.
    }

    public void TryOpenRemoveAmmoService()
    {
        RunData runData = GetRunData();

        if (runData == null)
            return;

        if (hasUsedRemoveAmmoService)
        {
            Debug.Log("[Shop] Remove ammo service is already used in this shop.");
            return;
        }

        if (removeAmmoPopupUI == null)
        {
            Debug.LogWarning("[ShopFlowController] RemoveAmmoPopupUI is missing.");
            return;
        }

        if (runData.gold < runData.removeAmmoPrice)
        {
            Debug.Log("[Shop] Not enough gold for ammo removal.");
            return;
        }

        if (runData.ammoDeck == null || runData.ammoDeck.Count == 0)
        {
            Debug.Log("[Shop] Ammo deck is empty.");
            return;
        }

        removeAmmoPopupUI.Show(
            runData.ammoDeck,
            OnAmmoRemoveConfirmed,
            OnAmmoRemoveCanceled
        );
    }

    private void OnAmmoRemoveConfirmed(AmmoModuleData ammoToRemove)
    {
        if (ammoToRemove == null)
            return;

        if (hasUsedRemoveAmmoService)
            return;

        RunData runData = GetRunData();

        if (runData == null)
            return;

        if (runData.gold < runData.removeAmmoPrice)
        {
            Debug.Log("[Shop] Not enough gold for ammo removal confirm.");
            RefreshShopUI();
            return;
        }

        if (runData.ammoDeck == null)
            return;

        bool removed = runData.ammoDeck.Remove(ammoToRemove);

        if (!removed)
        {
            Debug.LogWarning("[Shop] Failed to remove selected ammo.");
            return;
        }

        SpendGold(runData, runData.removeAmmoPrice);

        runData.removeAmmoPrice += removeAmmoPriceIncrease;

        hasUsedRemoveAmmoService = true;

        if (removeAmmoPopupUI != null)
            removeAmmoPopupUI.Hide();

        RefreshShopUI();

        Debug.Log($"[Shop] Removed ammo: {ammoToRemove.displayName}");
    }

    private void OnAmmoRemoveCanceled()
    {
        if (removeAmmoPopupUI != null)
            removeAmmoPopupUI.Hide();

        // 취소는 골드 차감 없음.
    }

    private bool CanAfford(RunData runData, int price)
    {
        if (runData == null)
            return false;

        return runData.gold >= price;
    }

    private void SpendGold(RunData runData, int price)
    {
        if (runData == null)
            return;

        int safePrice = Mathf.Max(0, price);
        runData.gold = Mathf.Max(0, runData.gold - safePrice);
    }

    private int FindEmptyWeaponSlot(RunData runData)
    {
        if (runData == null || runData.equippedWeapons == null)
            return -1;

        for (int i = 0; i < runData.equippedWeapons.Length; i++)
        {
            WeaponLoadoutData slot = runData.equippedWeapons[i];

            if (slot == null)
                return i;

            if (!slot.hasWeapon)
                return i;

            if (slot.weaponData == null)
                return i;
        }

        return -1;
    }

    private void SetWeaponToSlot(RunData runData, int slotIndex, WeaponData weaponData)
    {
        if (runData == null || weaponData == null)
            return;

        if (runData.equippedWeapons == null || runData.equippedWeapons.Length < 2)
            runData.equippedWeapons = new WeaponLoadoutData[2];

        if (slotIndex < 0 || slotIndex >= runData.equippedWeapons.Length)
            return;

        if (runData.equippedWeapons[slotIndex] == null)
            runData.equippedWeapons[slotIndex] = new WeaponLoadoutData();

        WeaponLoadoutData slot = runData.equippedWeapons[slotIndex];

        slot.hasWeapon = true;
        slot.weaponData = weaponData;

        if (slot.equippedAttachments == null)
            slot.equippedAttachments = new List<WeaponAttachmentData>();
        else
            slot.equippedAttachments.Clear();

        runData.currentWeaponSlotIndex = slotIndex;
    }

    private void ClearPendingWeaponPurchase()
    {
        pendingWeaponCard = null;
        pendingWeaponItem = null;
    }

    private void RefreshShopUI()
    {
        if (shopPanelUI != null)
            shopPanelUI.RefreshAfterChange();

        if (TopRunDataUI.Instance != null)
            TopRunDataUI.Instance.Refresh();
    }

    private RunData GetRunData()
    {
        if (RunGameManager.Instance == null || !RunGameManager.Instance.HasActiveRun)
        {
            Debug.LogWarning("[ShopFlowController] No active run.");
            return null;
        }

        return RunGameManager.Instance.CurrentRunData;
    }
}