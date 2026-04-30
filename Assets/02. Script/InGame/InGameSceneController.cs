using UnityEngine;

public class InGameSceneController : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] private RewardFlowController rewardFlowController;

    private void Start()
    {
        TryOpenPendingReward();
    }

    private void TryOpenPendingReward()
    {
        if (RunGameManager.Instance == null)
        {
            Debug.LogWarning("[InGameSceneController] RunGameManager.Instance is missing.");
            return;
        }

        if (!RunGameManager.Instance.HasPendingCombatResult)
        {
            Debug.Log("[InGameSceneController] No pending combat result.");
            return;
        }

        PendingCombatResult result = RunGameManager.Instance.ConsumePendingCombatResult();

        if (result == null)
            return;

        if (!result.wasVictory)
            return;

        if (!result.shouldShowReward)
            return;

        if (rewardFlowController == null)
        {
            Debug.LogWarning("[InGameSceneController] RewardFlowController is missing.");
            return;
        }

        Debug.Log("[InGameSceneController] Open reward panel from pending combat result.");

        rewardFlowController.OpenReward();
    }
}