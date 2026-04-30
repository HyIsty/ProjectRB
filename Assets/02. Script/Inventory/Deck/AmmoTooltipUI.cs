using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 탄환 메인 툴팁 UI.
/// 
/// 역할:
/// 1. 탄환 이름 표시
/// 2. 탄환 기본 데미지 표시
/// 3. 부착물 등으로 인한 데미지 변화량(delta) 표시
/// 4. 특수 효과 설명 표시
/// 5. 필요하면 GlossaryTooltipUI도 같이 띄움
/// 6. 마우스를 따라가되, 화면(canvas) 밖으로 나가지 않게 clamp
/// 
/// 주의:
/// - 이 툴팁 자체는 raycast를 막지 않는 것이 좋다.
/// - GlossaryTooltipUI는 sibling(형제) 오브젝트여야 한다.
///   AmmoTooltip의 자식이면 같이 말려다녀서 핀트가 틀어진다.
/// </summary>
public class AmmoTooltipUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform root;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Text")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private TMP_Text effectDescriptionText;

    [Header("Linked Tooltip")]
    [SerializeField] private GlossaryTooltipUI glossaryTooltipUI;

    [Header("Follow")]
    [SerializeField] private Vector2 screenOffset = new Vector2(20f, -20f);

    [Header("Damage Delta Colors")]
    [SerializeField] private string positiveColorHex = "#4CAF50";
    [SerializeField] private string negativeColorHex = "#F44336";

    /// <summary>
    /// 현재 툴팁이 표시 중인지 여부.
    /// </summary>
    private bool isShowing = false;

    /// <summary>
    /// 다른 시스템이 AmmoTooltip 위치를 anchor로 쓰고 싶을 때 접근용.
    /// 예: GlossaryTooltip이 AmmoTooltip 오른쪽에 붙을 때.
    /// </summary>
    public RectTransform RootRectTransform => root;

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
        if (!isShowing)
            return;

        // 매 프레임 마우스를 따라가되, 화면 안으로 clamp
        FollowMouseClamped();

        // 메인 툴팁이 움직였으면 glossary 쪽도 같이 위치 보정
        if (glossaryTooltipUI != null)
        {
            glossaryTooltipUI.RefreshPosition();
        }
    }

    /// <summary>
    /// 탄환 데이터를 받아 메인 툴팁을 표시한다.
    /// </summary>
    /// <param name="ammoData">표시할 탄환 데이터</param>
    /// <param name="previewDamageDelta">
    /// 기본 데미지 옆에 붙일 변화량.
    /// 예:
    /// +2면 초록색,
    /// -1이면 빨간색,
    /// 0이면 표시 안 함
    /// </param>
    public void ShowForAmmo(AmmoModuleData ammoData, int previewDamageDelta)
    {
        if (ammoData == null)
            return;

        // 1. 이름 표시
        if (nameText != null)
        {
            nameText.text = GetAmmoDisplayName(ammoData);
        }

        // 2. 데미지 표시
        if (damageText != null)
        {
            damageText.text = BuildDamageText(GetAmmoBaseDamage(ammoData), previewDamageDelta);
        }

        // 4. 특수 효과 설명 표시
        if (effectDescriptionText != null)
        {
            effectDescriptionText.text = GetEffectDescription(ammoData);
        }

        // 5. 메인 툴팁 먼저 표시
        ShowInternal();

        // 6. glossaryKeys가 있으면 glossary tooltip도 같이 띄움
        if (glossaryTooltipUI != null)
        {
            if (ammoData.glossaryList != null && ammoData.glossaryList.Count > 0)
            {
                glossaryTooltipUI.ShowForKeys(ammoData.glossaryList, root);
            }
            else
            {
                glossaryTooltipUI.Hide();
            }
        }
    }

    /// <summary>
    /// 메인 툴팁과 glossary 툴팁을 함께 숨긴다.
    /// </summary>
    public void Hide()
    {
        isShowing = false;

        if (glossaryTooltipUI != null)
        {
            glossaryTooltipUI.Hide();
        }

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

    /// <summary>
    /// 시작 시점 강제 초기화용.
    /// </summary>
    public void HideImmediate()
    {
        isShowing = false;

        if (glossaryTooltipUI != null)
        {
            glossaryTooltipUI.Hide();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // 오브젝트는 활성화 상태로 두고 alpha만 0으로 처리해도 된다.
        // 이렇게 하면 참조와 레이아웃이 더 안정적이다.
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 메인 툴팁 내부 표시 처리.
    /// </summary>
    private void ShowInternal()
    {
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

        // 처음 표시 순간에도 바로 위치 정리
        FollowMouseClamped();
    }

    /// <summary>
    /// 마우스 기준으로 위치를 잡고,
    /// 그 뒤 canvas 안으로 clamp 해서 화면 밖으로 안 나가게 한다.
    /// </summary>
    private void FollowMouseClamped()
    {
        if (root == null || rootCanvas == null)
            return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        // 텍스트가 길어져 크기가 변했을 수 있으므로 레이아웃 강제 갱신
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(root);

        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        // 1. 우선 마우스 근처 원하는 위치에 배치
        Vector2 screenPoint = (Vector2)Input.mousePosition + screenOffset;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            uiCamera,
            out Vector2 localPoint))
        {
            root.anchoredPosition = localPoint;
        }

        // 2. 최종적으로 canvas 내부에 clamp
        ClampInsideCanvas(root, canvasRect);
    }

    /// <summary>
    /// target RectTransform이 canvas 밖으로 나가지 않도록 위치를 보정한다.
    /// pivot을 고려해서 안전하게 clamp 한다.
    /// </summary>
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

    /// <summary>
    /// 데미지 문자열 생성.
    /// 기본 데미지 + 변화량(delta)을 한 줄로 만든다.
    /// </summary>
    private string BuildDamageText(int baseDamage, int previewDamageDelta)
    {
        if (previewDamageDelta > 0)
        {
            return $"Damage : {baseDamage} <color={positiveColorHex}>+{previewDamageDelta}</color>";
        }

        if (previewDamageDelta < 0)
        {
            return $"Damage : {baseDamage} <color={negativeColorHex}>{previewDamageDelta}</color>";
        }

        return $"Damage : {baseDamage}";
    }

    /// <summary>
    /// 탄환 표시 이름을 가져온다.
    /// ammoName이 있으면 그걸 우선 사용하고,
    /// 없으면 id를 fallback으로 쓴다.
    /// </summary>
    private string GetAmmoDisplayName(AmmoModuleData ammoData)
    {
        if (ammoData == null)
            return "-";

        // 네 프로젝트 필드명이 다르면 여기만 수정하면 된다.
        if (string.IsNullOrWhiteSpace(ammoData.displayName) == false)
            return ammoData.displayName;

        if (string.IsNullOrWhiteSpace(ammoData.id) == false)
            return ammoData.id;

        return "Unknown Ammo";
    }

    /// <summary>
    /// 탄환 기본 데미지를 반환한다.
    /// </summary>
    private int GetAmmoBaseDamage(AmmoModuleData ammoData)
    {
        if (ammoData == null)
            return 0;

        return ammoData.damage;
    }

    /// <summary>
    /// 탄환의 특수 효과 설명 문자열을 가져온다.
    /// 비어 있으면 기본 문구를 반환한다.
    /// </summary>
    private string GetEffectDescription(AmmoModuleData ammoData)
    {
        if (ammoData == null)
            return "No description.";

        if (string.IsNullOrWhiteSpace(ammoData.description))
            return "No special effect.";

        return ammoData.description;
    }
}