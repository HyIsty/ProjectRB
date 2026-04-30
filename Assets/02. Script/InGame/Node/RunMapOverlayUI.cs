using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 런 전체 공용 맵 UI.
/// - InGame: 항상 하단 도킹 + 클릭 가능
/// - Shop: Tab 오버레이 + 클릭 가능
/// - Combat: Tab 오버레이 + 클릭 불가
/// - Title/Victory/Defeat: 숨김
/// </summary>
public class RunMapOverlayUI : MonoBehaviour
{
    [Header("UI Roots")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform viewportRect;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform mapContentRoot;
    [SerializeField] private RectTransform lineRoot;
    [SerializeField] private RectTransform nodeRoot;

    [Header("Prefabs")]
    [SerializeField] private MapNodeButtonUI nodeButtonPrefab;
    [SerializeField] private Image linePrefab;

    [Header("Layout - Overlay")]
    [SerializeField] private Vector2 overlayAnchorMin = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 overlayAnchorMax = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 overlayPivot = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 overlaySizeDelta = new Vector2(1400f, 700f);
    [SerializeField] private Vector2 overlayAnchoredPos = new Vector2(0f, 0f);

    [Header("Focus")]
    [SerializeField] private float focusDuration = 0.2f;

    [Header("Lines")]
    [SerializeField] private float normalLineThickness = 4f;
    [SerializeField] private float highlightedLineThickness = 8f;
    [SerializeField] private Color normalLineColor = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color highlightedLineColor = new Color(1f, 1f, 1f, 0.95f);

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private RunData runData;
    private Coroutine focusRoutine;
    private RunMapViewMode currentMode = RunMapViewMode.Hidden;

    private readonly List<MapNodeButtonUI> spawnedButtons = new List<MapNodeButtonUI>();
    private readonly List<Image> spawnedLines = new List<Image>();

    /// <summary>
    /// InGame/Shop 쪽에서 노드 클릭 시 실행할 외부 핸들러.
    /// Combat read-only에서는 내부에서 자동 차단한다.
    /// </summary>
    public Action<string> OnNodeClicked;

    public RunMapViewMode CurrentMode => currentMode;

    private void Awake()
    {
        if (scrollRect != null)
            scrollRect.inertia = false;

        ApplyVisible(false);
    }

    public void BindRunData(RunData data)
    {
        runData = data;
    }

    public void SetMode(RunMapViewMode mode)
    {
        currentMode = mode;
        switch (currentMode)
        {
            case RunMapViewMode.Hidden:
                ApplyVisible(false);
                break;

            case RunMapViewMode.InGamePersistentClickable:
                ApplyVisible(true);
                ApplyOverlayLayout();
                PushToBottomLayer();
                Rebuild();
                break;

            case RunMapViewMode.OverlayClickable:
            case RunMapViewMode.OverlayReadOnly:
                ApplyVisible(true);
                ApplyOverlayLayout();
                BringToTopLayer();
                Rebuild();
                break;
        }

        if (debugLog)
            Debug.Log($"[RunMapOverlayUI] Mode -> {currentMode}");
    }

    public void Rebuild()
    {
        ClearButtons();
        ClearLines();

        if (runData == null || runData.mapData == null)
            return;

        MapNodeData currentNode = FindNode(runData.mapData.currentNodeId);
        if (currentNode == null)
            return;

        for (int i = 0; i < runData.mapData.allNodes.Count; i++)
        {
            MapNodeData node = runData.mapData.allNodes[i];

            MapNodeButtonUI buttonUI = Instantiate(nodeButtonPrefab, nodeRoot);
            RectTransform rect = buttonUI.GetComponent<RectTransform>();
            rect.anchoredPosition = node.uiPosition;

            bool interactable = IsNodeClickableInCurrentMode() && IsNodeInteractable(currentNode, node);
            MapNodeButtonUI.NodeVisualState visualState = GetNodeVisualState(currentNode, node);

            buttonUI.Bind(this, node, interactable, visualState);
            spawnedButtons.Add(buttonUI);
        }

        DrawAllConnections();
        DrawAvailableConnections(currentNode);
        FocusOnCurrentNode(true);
    }

    public void TryNodeClick(string nodeId)
    {
        if (!IsNodeClickableInCurrentMode())
            return;

        OnNodeClicked?.Invoke(nodeId);
    }

    public void FocusOnCurrentNode(bool instant)
    {
        if (runData == null || runData.mapData == null)
            return;

        MapNodeData currentNode = FindNode(runData.mapData.currentNodeId);
        if (currentNode == null)
            return;

        FocusOnNodeX(currentNode, instant);
    }

    private bool IsNodeClickableInCurrentMode()
    {
        return currentMode == RunMapViewMode.InGamePersistentClickable
            || currentMode == RunMapViewMode.OverlayClickable;
    }

    private void ApplyVisible(bool visible)
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = visible ? 1f : 0f;
            rootCanvasGroup.interactable = visible;
            rootCanvasGroup.blocksRaycasts = visible;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }

    private void PushToBottomLayer()
    {
        if (panelRoot != null)
            panelRoot.SetAsFirstSibling();
    }

    private void BringToTopLayer()
    {
        if (panelRoot != null)
            panelRoot.SetAsLastSibling();
    }

    private void ApplyOverlayLayout()
    {
        if (panelRoot == null)
            return;

        panelRoot.anchorMin = overlayAnchorMin;
        panelRoot.anchorMax = overlayAnchorMax;
        panelRoot.pivot = overlayPivot;
        panelRoot.sizeDelta = overlaySizeDelta;
        panelRoot.anchoredPosition = overlayAnchoredPos;

        // 오버레이는 위로 띄움
        panelRoot.SetAsLastSibling();
    }

    private MapNodeButtonUI.NodeVisualState GetNodeVisualState(MapNodeData currentNode, MapNodeData targetNode)
    {
        if (currentNode == null || targetNode == null)
            return MapNodeButtonUI.NodeVisualState.Locked;

        if (currentNode.nodeId == targetNode.nodeId)
            return MapNodeButtonUI.NodeVisualState.Current;

        if (targetNode.isCleared)
            return MapNodeButtonUI.NodeVisualState.Cleared;

        if (IsNodeInteractable(currentNode, targetNode))
            return MapNodeButtonUI.NodeVisualState.Reachable;

        return MapNodeButtonUI.NodeVisualState.Locked;
    }

    private bool IsNodeInteractable(MapNodeData currentNode, MapNodeData targetNode)
    {
        if (currentNode == null || targetNode == null)
            return false;

        if (currentNode.nodeId == targetNode.nodeId)
            return false;

        if (targetNode.isVisited)
            return false;

        return currentNode.nextNodeIds.Contains(targetNode.nodeId);
    }

    private void DrawAllConnections()
    {
        if (runData == null || runData.mapData == null || linePrefab == null)
            return;

        for (int i = 0; i < runData.mapData.allNodes.Count; i++)
        {
            MapNodeData fromNode = runData.mapData.allNodes[i];
            if (fromNode == null || fromNode.nextNodeIds == null)
                continue;

            for (int j = 0; j < fromNode.nextNodeIds.Count; j++)
            {
                MapNodeData toNode = FindNode(fromNode.nextNodeIds[j]);
                if (toNode == null)
                    continue;

                CreateLine(fromNode.uiPosition, toNode.uiPosition, false);
            }
        }
    }

    private void DrawAvailableConnections(MapNodeData currentNode)
    {
        if (currentNode == null || linePrefab == null)
            return;

        for (int i = 0; i < currentNode.nextNodeIds.Count; i++)
        {
            MapNodeData nextNode = FindNode(currentNode.nextNodeIds[i]);
            if (nextNode == null)
                continue;

            if (nextNode.isVisited)
                continue;

            CreateLine(currentNode.uiPosition, nextNode.uiPosition, true);
        }
    }

    private void CreateLine(Vector2 from, Vector2 to, bool highlight)
    {
        Image line = Instantiate(linePrefab, lineRoot);
        RectTransform rect = line.rectTransform;

        Vector2 dir = to - from;
        float length = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rect.anchoredPosition = from;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);

        Vector2 size = rect.sizeDelta;
        size.x = length;
        size.y = highlight ? highlightedLineThickness : normalLineThickness;
        rect.sizeDelta = size;

        line.color = highlight ? highlightedLineColor : normalLineColor;

        spawnedLines.Add(line);
    }

    private void FocusOnNodeX(MapNodeData node, bool instant)
    {
        if (node == null || mapContentRoot == null)
            return;

        Vector2 targetPos = new Vector2(
            -node.uiPosition.x,
            mapContentRoot.anchoredPosition.y
        );

        if (focusRoutine != null)
            StopCoroutine(focusRoutine);

        if (instant)
        {
            mapContentRoot.anchoredPosition = targetPos;
        }
        else
        {
            focusRoutine = StartCoroutine(FocusRoutine(targetPos));
        }
    }

    private IEnumerator FocusRoutine(Vector2 targetPos)
    {
        Vector2 start = mapContentRoot.anchoredPosition;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, focusDuration);
            mapContentRoot.anchoredPosition = Vector2.Lerp(start, targetPos, t);
            yield return null;
        }

        mapContentRoot.anchoredPosition = targetPos;
        focusRoutine = null;
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

    private void ClearButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
                Destroy(spawnedButtons[i].gameObject);
        }

        spawnedButtons.Clear();
    }

    private void ClearLines()
    {
        for (int i = 0; i < spawnedLines.Count; i++)
        {
            if (spawnedLines[i] != null)
                Destroy(spawnedLines[i].gameObject);
        }

        spawnedLines.Clear();
    }
}