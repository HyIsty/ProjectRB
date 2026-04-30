using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 총기 Inspect 패널 우측의 부착물 슬롯 1칸 UI.
/// 
/// 현재 역할:
/// - 이 슬롯이 어떤 부착물 타입 슬롯인지 표시
/// - 장착 여부 표시
/// - 허용 안 되는 슬롯은 아예 숨김 처리 가능
/// 
/// 나중에 확장:
/// - 드롭 타겟
/// - hover highlight
/// - 실제 부착물 아이콘 표시
/// </summary>
public class InventoryAttachmentSlotUI : MonoBehaviour
{
    [Header("Slot Identity")]
    [SerializeField] private AttachmentType slotType;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI slotNameText;
    [SerializeField] private TextMeshProUGUI stateText;

    [Header("Optional Visual")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite equippedSlotSprite;

    /// <summary>
    /// 이 슬롯이 담당하는 부착물 타입
    /// </summary>
    public AttachmentType SlotType => slotType;

    /// <summary>
    /// 슬롯 활성 상태 표시
    /// 
    /// allowed = false 면 이 슬롯은 현재 총기에서 지원 안 하므로 숨긴다.
    /// allowed = true 면 표시하고 장착 여부를 표시한다.
    /// </summary>
    public void SetData(bool allowed, bool isEquipped)
    {
        // 이 총기에 없는 슬롯이면 아예 숨김
        gameObject.SetActive(allowed);

        if (!allowed)
            return;

        if (slotNameText != null)
            slotNameText.text = GetKoreanSlotName(slotType);

        if (stateText != null)
            stateText.text = isEquipped ? "장착됨" : "비어 있음";

        if (iconImage != null)
        {
            iconImage.sprite = isEquipped ? equippedSlotSprite : emptySlotSprite;
            iconImage.enabled = iconImage.sprite != null;
        }
    }

    /// <summary>
    /// 표시용 한글 이름 변환
    /// </summary>
    private string GetKoreanSlotName(AttachmentType type)
    {
        switch (type)
        {
            case AttachmentType.Muzzle:
                return "총구";
            case AttachmentType.Magazine:
                return "탄창";
            case AttachmentType.Grip:
                return "손잡이";
            case AttachmentType.Scope:
                return "스코프";
            case AttachmentType.Stock:
                return "개머리판";
            default:
                return type.ToString();
        }
    }
}