using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardChoiceCardUI : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button backgroundButton;

    [Header("Reward Display")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Gold Bonus Display")]
    [SerializeField] private GameObject goldBadgeRoot;
    [SerializeField] private TMP_Text goldAmountText;

    [Header("Fallback Icon")]
    [SerializeField] private Sprite fallbackRewardIcon;

    private RewardCandidate currentCandidate;
    private RewardPanelUI ownerPanel;

    public void Bind(RewardCandidate candidate, RewardPanelUI owner)
    {
        currentCandidate = candidate;
        ownerPanel = owner;

        if (candidate == null)
        {
            Clear();
            return;
        }

        if (nameText != null)
            nameText.text = candidate.GetDisplayName();

        if (typeText != null)
            typeText.text = candidate.rewardType.ToString();

        if (descriptionText != null)
            descriptionText.text = candidate.GetDescription();

        RefreshIcon(candidate);
        RefreshGoldBonus(candidate.goldAmount);

        if (backgroundButton != null)
        {
            backgroundButton.onClick.RemoveAllListeners();
            backgroundButton.onClick.AddListener(OnClickCard);
        }

        gameObject.SetActive(true);
    }

    private void RefreshIcon(RewardCandidate candidate)
    {
        if (iconImage == null)
            return;

        Sprite icon = candidate.GetIcon();

        if (icon == null)
            icon = fallbackRewardIcon;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    private void RefreshGoldBonus(int goldAmount)
    {
        bool hasGold = goldAmount > 0;

        if (goldBadgeRoot != null)
            goldBadgeRoot.SetActive(hasGold);

        if (goldAmountText != null)
            goldAmountText.text = $"x{goldAmount}";
    }

    private void OnClickCard()
    {
        if (ownerPanel == null || currentCandidate == null)
            return;

        ownerPanel.OnRewardCardClicked(currentCandidate);
    }

    public void SetInteractable(bool interactable)
    {
        if (backgroundButton != null)
            backgroundButton.interactable = interactable;
    }

    public void Clear()
    {
        currentCandidate = null;
        ownerPanel = null;

        if (nameText != null)
            nameText.text = "";

        if (typeText != null)
            typeText.text = "";

        if (descriptionText != null)
            descriptionText.text = "";

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (goldBadgeRoot != null)
            goldBadgeRoot.SetActive(false);

        if (backgroundButton != null)
            backgroundButton.onClick.RemoveAllListeners();

        gameObject.SetActive(false);
    }
}