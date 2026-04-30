using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Header")]
    [SerializeField] private Button closeButton;

    [Header("Top Random Cards")]
    [SerializeField] private List<ShopItemCardUI> topRandomCards = new List<ShopItemCardUI>();

    [Header("Bottom Ammo Cards")]
    [SerializeField] private List<ShopItemCardUI> bottomAmmoCards = new List<ShopItemCardUI>();

    [Header("Bottom Weapon Cards")]
    [SerializeField] private List<ShopItemCardUI> bottomWeaponCards = new List<ShopItemCardUI>();

    [Header("Remove Ammo Service")]
    [SerializeField] private Button removeAmmoButton;
    [SerializeField] private TMP_Text removeAmmoPriceText;
    [SerializeField] private GameObject removeAmmoSoldOutRoot;

    private ShopFlowController ownerFlow;
    [Header("Tooltip")]
    [SerializeField] private ShopTooltipController tooltipController;

    public void Show(ShopStock stock, ShopFlowController owner)
    {
        ownerFlow = owner;

        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        BindCards(topRandomCards, stock != null ? stock.topRandomItems : null);
        BindCards(bottomAmmoCards, stock != null ? stock.bottomAmmoItems : null);
        BindCards(bottomWeaponCards, stock != null ? stock.bottomWeaponItems : null);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }

        if (removeAmmoButton != null)
        {
            removeAmmoButton.onClick.RemoveAllListeners();
            removeAmmoButton.onClick.AddListener(OnRemoveAmmoClicked);
        }

        RefreshAfterChange();
    }

    public void Hide()
    {
        if (tooltipController != null)
            tooltipController.Hide();

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);

        ownerFlow = null;
    }

    private void BindCards(List<ShopItemCardUI> cards, List<ShopItemCandidate> items)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            ShopItemCardUI card = cards[i];

            if (card == null)
                continue;

            if (items != null && i < items.Count)
                card.Bind(items[i], this);
            else
                card.Clear();
        }
    }

    public void OnItemClicked(ShopItemCardUI card)
    {
        if (ownerFlow == null)
            return;

        if (card == null)
            return;

        ownerFlow.TryBuyItem(card);
    }

    public void RefreshAfterChange()
    {
        RefreshCardAffordableStates();
        RefreshRemoveAmmoService();
    }

    private void RefreshCardAffordableStates()
    {
        RunData runData = GetRunData();

        if (runData == null)
            return;

        RefreshCardListAffordable(topRandomCards, runData.gold);
        RefreshCardListAffordable(bottomAmmoCards, runData.gold);
        RefreshCardListAffordable(bottomWeaponCards, runData.gold);
    }

    private void RefreshCardListAffordable(List<ShopItemCardUI> cards, int currentGold)
    {
        for (int i = 0; i < cards.Count; i++)
        {
            ShopItemCardUI card = cards[i];

            if (card == null)
                continue;

            if (card.CurrentItem == null)
                continue;

            bool affordable = currentGold >= card.CurrentItem.price;
            card.SetAffordable(affordable);
        }
    }

    private void RefreshRemoveAmmoService()
    {
        RunData runData = GetRunData();

        if (runData == null)
        {
            SetRemoveAmmoButtonState(false, "- G", false);
            return;
        }

        bool isSoldOut = ownerFlow != null && ownerFlow.HasUsedRemoveAmmoService;

        bool hasAmmo = runData.ammoDeck != null && runData.ammoDeck.Count > 0;
        bool canAfford = runData.gold >= runData.removeAmmoPrice;
        bool canUse = !isSoldOut && hasAmmo && canAfford;

        SetRemoveAmmoButtonState(
            canUse,
            runData.removeAmmoPrice + " G",
            isSoldOut
        );
    }

    private void SetRemoveAmmoButtonState(bool interactable, string priceText, bool soldOut)
    {
        if (removeAmmoButton != null)
            removeAmmoButton.interactable = interactable;

        if (removeAmmoPriceText != null)
            removeAmmoPriceText.text = priceText;

        if (removeAmmoSoldOutRoot != null)
            removeAmmoSoldOutRoot.SetActive(soldOut);
    }

    private void OnRemoveAmmoClicked()
    {
        if (ownerFlow == null)
            return;

        ownerFlow.TryOpenRemoveAmmoService();
    }

    private void OnCloseClicked()
    {
        if (ownerFlow != null)
            ownerFlow.CloseShop();
        else
            Hide();
    }

    private RunData GetRunData()
    {
        if (RunGameManager.Instance == null)
            return null;

        if (!RunGameManager.Instance.HasActiveRun)
            return null;

        return RunGameManager.Instance.CurrentRunData;
    }

    public void OnItemHoverEnter(ShopItemCardUI card, Vector2 screenPosition)
    {
        if (tooltipController == null)
            return;

        if (card == null || card.CurrentItem == null)
            return;

        tooltipController.Show(card.CurrentItem, screenPosition);
    }

    public void OnItemHoverMove(Vector2 screenPosition)
    {
        if (tooltipController == null)
            return;

        tooltipController.Move(screenPosition);
    }

    public void OnItemHoverExit(ShopItemCardUI card)
    {
        if (tooltipController == null)
            return;

        tooltipController.Hide();
    }
}