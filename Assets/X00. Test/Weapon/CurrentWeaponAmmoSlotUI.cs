using TMPro;
using UnityEngine;

/// <summary>
/// 현재 무기에 장전된 탄환 1칸을 표시하는 UI.
/// AmmoModuleData에 스프라이트 필드가 아직 확정되어 있지 않아서,
/// 현재 최소 구현은 텍스트 기반 슬롯으로 간다.
/// </summary>
public class CurrentWeaponAmmoSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text orderText;
    [SerializeField] private TMP_Text ammoNameText;
    [SerializeField] private TMP_Text ammoDamageText;

    public void Bind(int order, AmmoModuleData ammoData)
    {
        if (orderText != null)
            orderText.text = $"#{order}";

        if (ammoNameText != null)
            ammoNameText.text = ammoData != null ? ammoData.displayName : "None";

        if (ammoDamageText != null)
            ammoDamageText.text = ammoData != null ? ammoData.damage.ToString() : "-";
    }
}