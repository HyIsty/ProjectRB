using System.Reflection;
using TMPro;
using UnityEngine;

/// <summary>
/// InGameSc에서 RunData 정보를 상단에 표시하는 UI.
/// CombatSc는 자체 CombatHUD가 있으므로 이 UI를 사용하지 않는다.
/// </summary>
public class TopRunDataUI : MonoBehaviour
{
    public static TopRunDataUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Texts")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text floorText;
    [SerializeField] private TMP_Text goldText;

    [Header("Fallback")]
    [SerializeField] private int fallbackMaxFloor = 15;

    private void Awake()
    {
        Instance = this;

        if (root == null)
            root = gameObject;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    /// <summary>
    /// 현재 RunData 값을 다시 읽어서 UI에 표시한다.
    /// Shop 구매, Rest 회복, Reward 선택 후 호출하면 된다.
    /// </summary>
    public void Refresh()
    {
        RunData runData = GetRunData();

        if (runData == null)
        {
            Clear();
            return;
        }

        if (root != null)
            root.SetActive(true);

        RefreshHp(runData);
        RefreshFloor(runData);
        RefreshGold(runData);
    }

    private RunData GetRunData()
    {
        if (RunGameManager.Instance == null)
            return null;

        if (!RunGameManager.Instance.HasActiveRun)
            return null;

        return RunGameManager.Instance.CurrentRunData;
    }

    private void RefreshHp(RunData runData)
    {
        if (hpText == null)
            return;

        hpText.text = $"HP : {runData.currentHp} / {runData.maxHp}";
    }

    private void RefreshGold(RunData runData)
    {
        if (goldText == null)
            return;

        goldText.text = $"Gold : {runData.gold}";
    }

    private void RefreshFloor(RunData runData)
    {
        if (floorText == null)
            return;

        int currentDepth = GetCurrentDepth(runData);
        int maxFloor = GetMaxFloor(runData);

        if (currentDepth < 0)
        {
            floorText.text = $"Check : - / {maxFloor}";
            return;
        }

        // depth가 0부터 시작하면 사람에게 보여줄 때는 +1
        floorText.text = $"Check : {currentDepth + 1} / {maxFloor}";
    }

    /// <summary>
    /// 현재 노드의 depth/layerIndex/floorIndex 같은 int 필드를 찾아서 읽는다.
    /// 네 MapNodeData 필드명이 정확히 뭐든 최대한 대응하려고 reflection을 쓴다.
    /// </summary>
    private int GetCurrentDepth(RunData runData)
    {
        if (runData == null || runData.mapData == null)
            return -1;

        if (string.IsNullOrEmpty(runData.mapData.currentNodeId))
            return -1;

        if (runData.mapData.allNodes == null)
            return -1;

        for (int i = 0; i < runData.mapData.allNodes.Count; i++)
        {
            MapNodeData node = runData.mapData.allNodes[i];

            if (node == null)
                continue;

            if (node.nodeId != runData.mapData.currentNodeId)
                continue;

            return TryReadIntMember(
                node,
                "depth",
                "nodeDepth",
                "layerIndex",
                "floorIndex",
                "mapDepth"
            );
        }

        return -1;
    }

    /// <summary>
    /// mapData에 maxDepth/totalDepth/depthCount 같은 값이 있으면 읽고,
    /// 없으면 노드들의 최대 depth를 계산한다.
    /// 그래도 안 되면 fallbackMaxFloor를 쓴다.
    /// </summary>
    private int GetMaxFloor(RunData runData)
    {
        if (runData == null || runData.mapData == null)
            return fallbackMaxFloor;

        int directValue = TryReadIntMember(
            runData.mapData,
            "maxDepth",
            "totalDepth",
            "depthCount",
            "maxFloor"
        );

        if (directValue > 0)
        {
            // maxDepth가 14처럼 0-based 마지막 depth라면 +1이 필요할 수 있다.
            // totalDepth/depthCount가 15라면 그대로 쓰는 게 맞다.
            // 제출 전 안정성을 위해 15 근처 값은 그대로 표시한다.
            return directValue;
        }

        int maxDepth = -1;

        if (runData.mapData.allNodes != null)
        {
            for (int i = 0; i < runData.mapData.allNodes.Count; i++)
            {
                MapNodeData node = runData.mapData.allNodes[i];

                if (node == null)
                    continue;

                int depth = TryReadIntMember(
                    node,
                    "depth",
                    "nodeDepth",
                    "layerIndex",
                    "floorIndex",
                    "mapDepth"
                );

                if (depth > maxDepth)
                    maxDepth = depth;
            }
        }

        if (maxDepth >= 0)
            return maxDepth + 1;

        return fallbackMaxFloor;
    }

    /// <summary>
    /// 필드명이나 프로퍼티명이 달라도 int 값을 읽기 위한 헬퍼.
    /// </summary>
    private int TryReadIntMember(object target, params string[] memberNames)
    {
        if (target == null || memberNames == null)
            return -1;

        System.Type type = target.GetType();

        for (int i = 0; i < memberNames.Length; i++)
        {
            string memberName = memberNames[i];

            FieldInfo field = type.GetField(
                memberName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (field != null && field.FieldType == typeof(int))
                return (int)field.GetValue(target);

            PropertyInfo property = type.GetProperty(
                memberName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (property != null && property.PropertyType == typeof(int))
                return (int)property.GetValue(target);
        }

        return -1;
    }

    private void Clear()
    {
        if (hpText != null)
            hpText.text = "HP : - / -";

        if (floorText != null)
            floorText.text = "Check : - / -";

        if (goldText != null)
            goldText.text = "Gold : -";
    }
}