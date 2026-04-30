using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RewardPanelUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Cards")]
    [SerializeField] private List<RewardChoiceCardUI> rewardCards = new List<RewardChoiceCardUI>();

    [Header("Buttons")]
    [SerializeField] private Button skipButton;

    [Header("Input Guard")]
    [SerializeField] private float openInputLockSeconds = 0.15f;

    private RewardFlowController ownerFlow;
    private bool canSelectReward;

    public void Show(List<RewardCandidate> candidates, RewardFlowController owner)
    {
        ownerFlow = owner;

        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        BindCards(candidates);

        StartCoroutine(InputLockRoutine());
    }

    public void Hide()
    {
        canSelectReward = false;

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void BindCards(List<RewardCandidate> candidates)
    {
        for (int i = 0; i < rewardCards.Count; i++)
        {
            RewardChoiceCardUI card = rewardCards[i];

            if (card == null)
                continue;

            if (candidates != null && i < candidates.Count)
            {
                card.Bind(candidates[i], this);
            }
            else
            {
                card.Clear();
            }

            card.SetInteractable(false);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipClicked);
            skipButton.interactable = false;
        }
    }

    private IEnumerator InputLockRoutine()
    {
        canSelectReward = false;

        yield return new WaitForSecondsRealtime(openInputLockSeconds);

        canSelectReward = true;

        for (int i = 0; i < rewardCards.Count; i++)
        {
            if (rewardCards[i] != null)
                rewardCards[i].SetInteractable(true);
        }

        if (skipButton != null)
            skipButton.interactable = true;
    }

    public void OnRewardCardClicked(RewardCandidate candidate)
    {
        if (!canSelectReward)
            return;

        if (ownerFlow == null)
            return;

        ownerFlow.SelectReward(candidate);
    }

    private void OnSkipClicked()
    {
        if (!canSelectReward)
            return;

        if (ownerFlow == null)
            return;

        ownerFlow.SkipReward();
    }
}