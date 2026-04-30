using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopItemCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Button")]
    [SerializeField] private Button backgroundButton;

    [Header("Display")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text priceText;

    [Header("Sold Out")]
    [SerializeField] private GameObject soldOutRoot;

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackIcon;

    private ShopItemCandidate currentItem;
    private ShopPanelUI ownerPanel;
    private bool isSoldOut;

    public ShopItemCandidate CurrentItem => currentItem;
    public bool IsSoldOut => isSoldOut;

    public void Bind(ShopItemCandidate item, ShopPanelUI owner)
    {
        currentItem = item;
        ownerPanel = owner;
        isSoldOut = false;

        if (item == null)
        {
            Clear();
            return;
        }

        if (nameText != null)
            nameText.text = item.GetDisplayName();

        if (typeText != null)
            typeText.text = item.itemType.ToString();

        if (priceText != null)
            priceText.text = item.price + " G";

        RefreshIcon(item);

        if (soldOutRoot != null)
            soldOutRoot.SetActive(false);

        if (backgroundButton != null)
        {
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.onClick.AddListener(OnClickBuy);
            backgroundButton.interactable = true;
        }

        gameObject.SetActive(true);
    }

    public void SetSoldOut()
    {
        isSoldOut = true;

        if (soldOutRoot != null)
            soldOutRoot.SetActive(true);

        if (backgroundButton != null)
            backgroundButton.interactable = false;

        // 팔린 카드 위에 마우스가 올라가 있으면 툴팁을 꺼준다.
        if (ownerPanel != null)
            ownerPanel.OnItemHoverExit(this);
    }

    public void SetAffordable(bool affordable)
    {
        if (backgroundButton == null)
            return;

        if (isSoldOut)
        {
            backgroundButton.interactable = false;
            return;
        }

        backgroundButton.interactable = affordable;
    }

    private void RefreshIcon(ShopItemCandidate item)
    {
        if (iconImage == null)
            return;

        Sprite icon = item.GetIcon();

        if (icon == null)
            icon = fallbackIcon;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    private void OnClickBuy()
    {
        if (ownerPanel == null)
            return;

        if (currentItem == null)
            return;

        if (isSoldOut)
            return;

        ownerPanel.OnItemClicked(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ownerPanel == null)
            return;

        if (currentItem == null)
            return;

        if (isSoldOut)
            return;

        ownerPanel.OnItemHoverEnter(this, eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (ownerPanel == null)
            return;

        ownerPanel.OnItemHoverMove(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ownerPanel == null)
            return;

        ownerPanel.OnItemHoverExit(this);
    }

    public void Clear()
    {
        currentItem = null;
        ownerPanel = null;
        isSoldOut = false;

        if (nameText != null)
            nameText.text = "";

        if (typeText != null)
            typeText.text = "";

        if (priceText != null)
            priceText.text = "";

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (soldOutRoot != null)
            soldOutRoot.SetActive(false);

        if (backgroundButton != null)
        {
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.interactable = false;
        }

        gameObject.SetActive(false);
    }
}