using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tab_Deck에서 사용하는 탄환 한 줄 UI.
/// 
/// 역할:
/// - 왼쪽 배지 텍스트 표시 (예: x3, NEXT, #2)
/// - 탄환 이름 표시
/// - 탄환 기본 데미지 표시
/// - 마우스 호버 시 AmmoTooltipUI 호출
/// 
/// 중요:
/// - 이 스크립트는 "표시 + hover 입력"만 담당한다.
/// - draw/discard 집계, queue 순서 판단, previewDamageDelta 계산은
///   InventoryDeckTabUI 쪽에서 해서 넘겨준다.
/// </summary>
public class DeckAmmoRowItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text leftBadgeText;
    [SerializeField] private TMP_Text ammoNameText;
    [SerializeField] private TMP_Text damageText;

    [Header("Hover")]
    [SerializeField] private AmmoTooltipUI ammoTooltipUI;

    // 현재 row가 표시 중인 탄환 데이터
    private AmmoModuleData currentAmmoData;

    // 툴팁에 넘길 preview damage delta
    private int currentPreviewDamageDelta;

    // 현재 row가 유효한 데이터인지
    private bool isBound = false;

    private void Reset()
    {
        backgroundImage = GetComponent<Image>();
    }

    /// <summary>
    /// row UI를 초기화한다.
    /// </summary>
    /// <param name="ammoData">표시할 탄환 데이터</param>
    /// <param name="leftBadge">왼쪽 배지 텍스트 (예: x3, NEXT, #2)</param>
    /// <param name="previewDamageDelta">툴팁에 표시할 데미지 변화량</param>
    /// <param name="tooltipUI">호버 시 띄울 AmmoTooltipUI</param>
    public void Initialize(
        AmmoModuleData ammoData,
        string leftBadge,
        int previewDamageDelta,
        AmmoTooltipUI tooltipUI)
    {
        currentAmmoData = ammoData;
        currentPreviewDamageDelta = previewDamageDelta;
        ammoTooltipUI = tooltipUI;
        isBound = ammoData != null;

        RefreshTexts(leftBadge);
    }

    /// <summary>
    /// 현재 바인딩된 데이터 기준으로 텍스트를 갱신한다.
    /// </summary>
    private void RefreshTexts(string leftBadge)
    {
        // 왼쪽 배지
        if (leftBadgeText != null)
        {
            leftBadgeText.text = string.IsNullOrWhiteSpace(leftBadge) ? "-" : leftBadge;
        }

        // 탄환 이름
        if (ammoNameText != null)
        {
            ammoNameText.text = GetAmmoDisplayName(currentAmmoData);
        }

        // 기본 데미지
        if (damageText != null)
        {
            damageText.text = GetAmmoBaseDamage(currentAmmoData).ToString();
        }
    }

    /// <summary>
    /// 마우스가 row 위에 올라오면 메인 탄환 툴팁을 띄운다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isBound == false)
            return;

        if (ammoTooltipUI == null)
            return;

        ammoTooltipUI.ShowForAmmo(currentAmmoData, currentPreviewDamageDelta);
    }

    /// <summary>
    /// 마우스를 떼면 툴팁을 숨긴다.
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (ammoTooltipUI == null)
            return;

        ammoTooltipUI.Hide();
    }

    /// <summary>
    /// 현재 바인딩 해제.
    /// 나중에 row 재사용(pooling)할 때도 쓰기 좋다.
    /// </summary>
    public void Clear()
    {
        currentAmmoData = null;
        currentPreviewDamageDelta = 0;
        isBound = false;

        if (leftBadgeText != null)
            leftBadgeText.text = "-";

        if (ammoNameText != null)
            ammoNameText.text = "-";

        if (damageText != null)
            damageText.text = "-";
    }

    /// <summary>
    /// 탄환 표시 이름을 가져온다.
    /// ammoName 우선, 없으면 id fallback.
    /// </summary>
    private string GetAmmoDisplayName(AmmoModuleData ammoData)
    {
        if (ammoData == null)
            return "-";

        if (string.IsNullOrWhiteSpace(ammoData.displayName) == false)
            return ammoData.displayName;

        if (string.IsNullOrWhiteSpace(ammoData.id) == false)
            return ammoData.id;

        return "Unknown Ammo";
    }

    /// <summary>
    /// 탄환 기본 데미지를 가져온다.
    /// </summary>
    private int GetAmmoBaseDamage(AmmoModuleData ammoData)
    {
        if (ammoData == null)
            return 0;

        return ammoData.damage;
    }

    /// <summary>
    /// HUD / DeckTab 공용으로 사용할 수 있는 queue row 바인드.
    /// order는 #1, #2, #3... 순서 표시용.
    /// </summary>
    public void BindQueueRow(int order, AmmoModuleData ammoData, AmmoTooltipUI tooltipOverride = null)
    {
        currentAmmoData = ammoData;

        // HUD에서도 기존 tooltip을 그대로 쓰고 싶으면 override 허용
        if (tooltipOverride != null)
        {
            ammoTooltipUI = tooltipOverride;
        }

        if (leftBadgeText != null)
        {
            leftBadgeText.text = $"#{order}";
        }

        if (ammoNameText != null)
        {
            ammoNameText.text = ammoData != null ? ammoData.displayName : "None";
        }

        if (damageText != null)
        {
            damageText.text = ammoData != null ? ammoData.damage.ToString() : "-";
        }
    }
}