using UnityEngine;

public class InGameMapManager : MonoBehaviour
{
    [Header("Map UI")]
    [SerializeField] private RunMapOverlayUI runMapOverlayUI;

    [Header("Shop")]
    [SerializeField] private ShopFlowController shopFlowController;

    [Header("Rest")]
    [SerializeField] private float restHealRatio = 0.3f;
    [SerializeField] private RestEffectUI restEffectUI;

    [Header("Reward")]
    [SerializeField] private RewardFlowController rewardFlowController;

    private MapNodeData openedRewardNode;

    private RunData runData;

    // 현재 열려 있는 Shop 노드를 기억한다.
    // Shop UI가 닫힐 때 이 노드를 cleared 처리하기 위함이다.
    private MapNodeData openedShopNode;

    private void Start()
    {
        if (RunGameManager.Instance == null || !RunGameManager.Instance.HasActiveRun)
        {
            Debug.LogError("[InGameMapManager] No active run.");
            return;
        }

        runData = RunGameManager.Instance.CurrentRunData;

        HandlePendingCombatResult();

        if (runMapOverlayUI != null)
        {
            runMapOverlayUI.BindRunData(runData);
            runMapOverlayUI.OnNodeClicked = TryEnterNode;
            runMapOverlayUI.Rebuild();
        }

        if (shopFlowController != null)
            shopFlowController.OnShopClosed += HandleShopClosed;

        if (rewardFlowController != null)
            rewardFlowController.OnRewardCompleted += HandleRewardCompleted;

        if (TopRunDataUI.Instance != null)
            TopRunDataUI.Instance.Refresh();
    }

    private void OnDestroy()
    {
        if (shopFlowController != null)
            shopFlowController.OnShopClosed -= HandleShopClosed;

        if (rewardFlowController != null)
            rewardFlowController.OnRewardCompleted -= HandleRewardCompleted;
    }

    public void TryEnterNode(string nodeId)
    {
        if (runData == null || runData.mapData == null)
            return;

        MapNodeData currentNode = FindNode(runData.mapData.currentNodeId);
        MapNodeData targetNode = FindNode(nodeId);

        if (currentNode == null || targetNode == null)
            return;

        // 현재 노드에서 이어진 노드만 들어갈 수 있다.
        if (!currentNode.nextNodeIds.Contains(nodeId))
            return;

        // 이미 방문한 노드는 다시 들어가지 않는다.
        if (targetNode.isVisited)
            return;

        runData.mapData.currentNodeId = targetNode.nodeId;
        targetNode.isVisited = true;

        ProcessNode(targetNode);
    }

    private void ProcessNode(MapNodeData node)
    {
        if (node == null)
            return;

        RoomType effectiveType = node.roomType;

        if (effectiveType == RoomType.Random)
            effectiveType = ResolveRandomNode(node);

        switch (effectiveType)
        {
            case RoomType.Start:
                node.isCleared = true;
                runMapOverlayUI?.Rebuild();
                break;

            case RoomType.Combat:
            case RoomType.Boss:
                GameSceneManager.Instance.LoadSceneAsyncByName("CombatSc");
                break;

            case RoomType.Reward:
                OpenRewardNode(node);
                break;

            case RoomType.Shop:
                OpenShop(node);
                break;

            case RoomType.Rest:
                ApplyRest(node);
                break;
        }
    }

    private void OpenShop(MapNodeData node)
    {
        if (node == null)
            return;

        if (shopFlowController == null)
        {
            Debug.LogWarning("[InGameMapManager] ShopFlowController is missing.");

            // 상점 UI가 없으면 진행이 막히니까 일단 클리어 처리한다.
            // 나중에 원하면 return으로 바꿔도 된다.
            node.isCleared = true;
            runMapOverlayUI?.Rebuild();
            return;
        }

        // Shop UI가 닫힐 때 cleared 처리할 노드를 기억한다.
        openedShopNode = node;

        shopFlowController.OpenShop();

        // Shop이 열려 있는 동안 지도 상태를 갱신하고 싶으면 Rebuild.
        // 단, ShopPanel이 위에 떠 있으므로 클릭은 UI가 막아주는 구조가 좋다.
        runMapOverlayUI?.Rebuild();

        Debug.Log($"[InGameMapManager] Shop node opened: {node.nodeId}");
    }

    private void HandleShopClosed()
    {
        if (openedShopNode == null)
            return;

        openedShopNode.isCleared = true;

        Debug.Log($"[InGameMapManager] Shop node cleared: {openedShopNode.nodeId}");

        openedShopNode = null;

        runMapOverlayUI?.Rebuild();

        if (TopRunDataUI.Instance != null)
            TopRunDataUI.Instance.Refresh();
    }

    private void ApplyReward(MapNodeData node)
    {
        node.isCleared = true;
        Debug.Log($"[InGameMapManager] Reward node cleared: {node.nodeId}");
    }

    private void ApplyRest(MapNodeData node)
    {
        if (node == null)
            return;

        if (runData == null)
            return;

        node.isCleared = true;

        int beforeHp = runData.currentHp;

        int healAmount = Mathf.CeilToInt(runData.maxHp * restHealRatio);

        runData.currentHp = Mathf.Min(
            runData.currentHp + healAmount,
            runData.maxHp
        );

        int actualHealedAmount = runData.currentHp - beforeHp;

        Debug.Log(
            $"[InGameMapManager] Rest node cleared: {node.nodeId} | " +
            $"Heal = {actualHealedAmount} | HP = {runData.currentHp}/{runData.maxHp}"
        );

        if (restEffectUI != null)
            restEffectUI.Play();

        if (TopRunDataUI.Instance != null)
            TopRunDataUI.Instance.Refresh();

        runMapOverlayUI?.Rebuild();
    }

    private void HandlePendingCombatResult()
    {
        if (RunGameManager.Instance.TryConsumePendingCombatResult(out PendingCombatResult result))
        {
            if (result != null && result.wasVictory)
            {
                MapNodeData clearedNode = FindNode(result.clearedNodeId);

                if (clearedNode != null)
                    clearedNode.isCleared = true;
            }
        }
    }

    private RoomType ResolveRandomNode(MapNodeData node)
    {
        if (node.hasResolvedRandomType)
            return node.resolvedRandomType;

        RoomType[] pool =
        {
            RoomType.Combat,
            RoomType.Reward,
            RoomType.Shop
        };

        RoomType selected = pool[Random.Range(0, pool.Length)];

        node.resolvedRandomType = selected;
        node.hasResolvedRandomType = true;

        return selected;
    }

    private MapNodeData FindNode(string nodeId)
    {
        if (runData == null || runData.mapData == null)
            return null;

        for (int i = 0; i < runData.mapData.allNodes.Count; i++)
        {
            if (runData.mapData.allNodes[i].nodeId == nodeId)
                return runData.mapData.allNodes[i];
        }

        return null;
    }
    private void OpenRewardNode(MapNodeData node)
    {
        if (node == null)
            return;

        if (rewardFlowController == null)
        {
            Debug.LogWarning("[InGameMapManager] RewardFlowController is missing.");

            // 막히지 않게 임시 클리어
            node.isCleared = true;
            runMapOverlayUI?.Rebuild();
            return;
        }

        openedRewardNode = node;

        rewardFlowController.OpenReward();

        runMapOverlayUI?.Rebuild();

        Debug.Log($"[InGameMapManager] Reward node opened: {node.nodeId}");
    }

    private void HandleRewardCompleted()
    {
        if (openedRewardNode == null)
            return;

        openedRewardNode.isCleared = true;

        Debug.Log($"[InGameMapManager] Reward node cleared: {openedRewardNode.nodeId}");

        openedRewardNode = null;

        runMapOverlayUI?.Rebuild();

        if (TopRunDataUI.Instance != null)
            TopRunDataUI.Instance.Refresh();
    }
}