using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AmmoTooltip 옆에 자동으로 붙는 glossary 설명 패널.
/// - 기본은 AmmoTooltip 오른쪽
/// - 오른쪽 공간이 부족하면 왼쪽으로 뒤집기
/// - 마지막에 canvas 안으로 clamp
/// </summary>
public class GlossaryTooltipUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform root;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private EffectGlossaryDatabase glossaryDatabase;

    [Header("Position")]
    [SerializeField] private Vector2 anchoredOffset = new Vector2(8f, 0f);

    private bool isShowing = false;
    private RectTransform currentAnchor;

    private void Reset()
    {
        root = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    private void Awake()
    {
        HideImmediate();
    }

    private void LateUpdate()
    {
        if (!isShowing || currentAnchor == null)
            return;

        Reposition();
    }

    /// <summary>
    /// AmmoTooltipUI가 움직였을 때 외부에서 강제로 다시 위치를 맞출 때 사용.
    /// </summary>
    public void RefreshPosition()
    {
        if (!isShowing || currentAnchor == null)
            return;

        Reposition();
    }

    public void ShowForKeys(IReadOnlyList<string> glossaryKeys, RectTransform anchor)
    {
        if (glossaryKeys == null || glossaryKeys.Count == 0)
        {
            Hide();
            return;
        }

        if (glossaryDatabase == null)
        {
            Debug.LogWarning("[GlossaryTooltipUI] GlossaryDatabase reference is missing.");
            Hide();
            return;
        }

        StringBuilder sb = new StringBuilder();
        int foundCount = 0;
        string firstTitle = string.Empty;

        for (int i = 0; i < glossaryKeys.Count; i++)
        {
            string key = glossaryKeys[i];
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (glossaryDatabase.TryGetEntry(key, out EffectGlossaryEntry entry) == false)
                continue;

            if (foundCount == 0)
                firstTitle = entry.title;

            if (foundCount > 0)
                sb.Append("\n\n");

            if (glossaryKeys.Count > 1)
            {
                sb.Append(entry.title);
                sb.Append("\n");
            }

            sb.Append(entry.description);
            foundCount++;
        }

        if (foundCount == 0)
        {
            Hide();
            return;
        }

        if (nameText != null)
            nameText.text = foundCount == 1 ? firstTitle : "Effects";

        if (descriptionText != null)
            descriptionText.text = sb.ToString();

        currentAnchor = anchor;
        isShowing = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            gameObject.SetActive(true);
        }

        Reposition();
    }

    public void Hide()
    {
        isShowing = false;
        currentAnchor = null;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void HideImmediate()
    {
        isShowing = false;
        currentAnchor = null;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// 오른쪽에 둘지, 왼쪽으로 뒤집을지 판단하고 최종 위치를 잡는다.
    /// </summary>
    private void Reposition()
    {
        if (root == null || rootCanvas == null || currentAnchor == null)
            return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);

        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        // anchor의 월드 코너
        Vector3[] corners = new Vector3[4];
        currentAnchor.GetWorldCorners(corners);

        // 오른쪽 중앙 / 왼쪽 중앙
        Vector3 rightCenterWorld = (corners[2] + corners[3]) * 0.5f;
        Vector3 leftCenterWorld = (corners[0] + corners[1]) * 0.5f;

        // canvas local point로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(uiCamera, rightCenterWorld),
            uiCamera,
            out Vector2 rightLocal);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            RectTransformUtility.WorldToScreenPoint(uiCamera, leftCenterWorld),
            uiCamera,
            out Vector2 leftLocal);

        // 현재 glossary tooltip 크기
        float tooltipWidth = root.rect.width;

        // 기본은 오른쪽
        Vector2 desiredPos = rightLocal + anchoredOffset;

        // pivot이 (0, 0.5)라고 가정하면, 오른쪽에 뒀을 때 오른쪽 화면 밖 나가는지 검사
        float canvasRight = canvasRect.rect.xMax;
        float wouldRightEdge = desiredPos.x + tooltipWidth;

        if (wouldRightEdge > canvasRight)
        {
            // 오른쪽이 막히면 왼쪽으로 뒤집기
            desiredPos.x = leftLocal.x - anchoredOffset.x - tooltipWidth;
            desiredPos.y = leftLocal.y + anchoredOffset.y;
        }

        root.anchoredPosition = desiredPos;

        // 마지막 안전 clamp
        ClampInsideCanvas(root, canvasRect);
    }

    private void ClampInsideCanvas(RectTransform target, RectTransform canvasRect)
    {
        Vector2 pos = target.anchoredPosition;
        Rect canvas = canvasRect.rect;
        Rect rect = target.rect;
        Vector2 pivot = target.pivot;

        float minX = canvas.xMin + rect.width * pivot.x;
        float maxX = canvas.xMax - rect.width * (1f - pivot.x);

        float minY = canvas.yMin + rect.height * pivot.y;
        float maxY = canvas.yMax - rect.height * (1f - pivot.y);

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        target.anchoredPosition = pos;
    }
}