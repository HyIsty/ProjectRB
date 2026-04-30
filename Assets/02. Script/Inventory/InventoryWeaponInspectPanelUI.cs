using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 총기 상세 패널(A 패널) 전체를 관리하는 스크립트.
///
/// 이제 두 가지 데이터 경로를 모두 지원한다.
/// 1. CombatScene : WeaponRuntime 기반
/// 2. InGameScene : WeaponLoadoutData 기반
///
/// 공통 역할:
/// - 무기 이름/이미지 표시
/// - 왼쪽 무기 스탯 표시
/// - 오른쪽 부착물 슬롯 5칸 표시
/// - 장착된 부착물 아이콘/요약/tooltip 표시
///
/// 중요한 규칙:
/// - tooltip은 InGame / Combat 둘 다 허용
/// - drag/drop 편집은 InGame에서만 허용
/// - 실제 장착/교체/해제 처리의 최종 판단은 InventoryUIController가 맡는다.
/// </summary>
public class InventoryWeaponInspectPanelUI : MonoBehaviour
{
    [Serializable]
    public class AttachmentSlotView
    {
        public AttachmentType slotType;
        public GameObject root;

        public TextMeshProUGUI slotNameText;
        public Image backgroundFrameImage;
        public Image attachmentIconImage;
        public TextMeshProUGUI attachmentEffectText;
    }

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI weaponNameHeaderText;
    [SerializeField] private Image weaponImage;
    [SerializeField] private Sprite fallbackWeaponSprite;

    [Header("Left Stats (One TMP Per Line)")]
    [SerializeField] private TextMeshProUGUI weaponTypeText;
    [SerializeField] private TextMeshProUGUI apCostText;
    [SerializeField] private TextMeshProUGUI slotCapacityText;
    [SerializeField] private TextMeshProUGUI spreadText;
    [SerializeField] private TextMeshProUGUI maxRangeText;
    [SerializeField] private TextMeshProUGUI effectiveRangeText;
    [SerializeField] private TextMeshProUGUI falloffText;

    [Header("Right Attachment Slots")]
    [SerializeField] private AttachmentSlotView[] attachmentSlots = new AttachmentSlotView[5];
    [SerializeField] private InventoryUIController inventoryUIController;
    [SerializeField] private EquippedAttachmentSlotTooltipTrigger[] slotTooltipTriggers = new EquippedAttachmentSlotTooltipTrigger[5];
    [SerializeField] private EquippedAttachmentSlotDragSource[] slotDragSources = new EquippedAttachmentSlotDragSource[5];

    [Header("Rich Text Colors")]
    [SerializeField] private string positiveHex = "#6CFF8A";
    [SerializeField] private string negativeHex = "#FF6B6B";

    private void Awake()
    {
        if (inventoryUIController == null)
            inventoryUIController = FindFirstObjectByType<InventoryUIController>();
    }

    /// <summary>
    /// 기존 Combat 전용 호출 호환용.
    /// 아직 다른 스크립트에서 Refresh(runtime)를 직접 부르고 있어도 버티게 둔다.
    /// </summary>
    public void Refresh(WeaponRuntime runtime)
    {
        bool canEdit = inventoryUIController != null && inventoryUIController.CanProcessAttachmentEdit();

        RefreshFromCombatWeapon(
            runtime,
            selectedWeaponIndex: -1,
            canEdit: canEdit,
            tooltipUI: null,
            controller: inventoryUIController
        );
    }

    /// <summary>
    /// CombatScene용.
    /// WeaponRuntime을 받아서 패널 전체를 갱신한다.
    /// </summary>
    public void RefreshFromCombatWeapon(
        WeaponRuntime runtimeWeapon,
        int selectedWeaponIndex,
        bool canEdit,
        AttachmentTooltipUI tooltipUI,
        InventoryUIController controller)
    {
        if (controller != null)
            inventoryUIController = controller;

        if (runtimeWeapon == null || !runtimeWeapon.HasBaseData)
        {
            ClearAll();
            return;
        }

        RefreshHeaderFromRuntime(runtimeWeapon);
        RefreshLeftStatsFromRuntime(runtimeWeapon);
        RefreshAttachmentSlotsFromRuntime(runtimeWeapon, canEdit);
    }

    /// <summary>
    /// InGameScene용.
    /// RunData 안의 WeaponLoadoutData를 받아서 패널 전체를 갱신한다.
    /// </summary>
    public void RefreshFromRunDataWeapon(
        WeaponLoadoutData runWeapon,
        int selectedWeaponIndex,
        bool canEdit,
        AttachmentTooltipUI tooltipUI,
        InventoryUIController controller)
    {
        if (controller != null)
            inventoryUIController = controller;

        if (runWeapon == null || runWeapon.weaponData == null)
        {
            ClearAll();
            return;
        }

        RefreshHeaderFromRunLoadout(runWeapon);
        RefreshLeftStatsFromRunLoadout(runWeapon);
        RefreshAttachmentSlotsFromRunLoadout(runWeapon, canEdit);
    }

    private void RefreshHeaderFromRuntime(WeaponRuntime runtime)
    {
        if (weaponNameHeaderText != null)
            weaponNameHeaderText.text = runtime.WeaponName;

        if (weaponImage == null)
            return;

        Sprite sprite = null;

        if (runtime.BaseData != null)
            sprite = runtime.BaseData.weaponSprite;

        weaponImage.sprite = sprite != null ? sprite : fallbackWeaponSprite;
        weaponImage.enabled = weaponImage.sprite != null;
    }

    private void RefreshHeaderFromRunLoadout(WeaponLoadoutData runWeapon)
    {
        if (weaponNameHeaderText != null)
            weaponNameHeaderText.text = ReadString(runWeapon.weaponData, "weaponName", "displayName", "WeaponName");

        if (weaponImage == null)
            return;

        Sprite sprite = runWeapon.weaponData != null ? runWeapon.weaponData.weaponSprite : null;
        weaponImage.sprite = sprite != null ? sprite : fallbackWeaponSprite;
        weaponImage.enabled = weaponImage.sprite != null;
    }

    private void RefreshLeftStatsFromRuntime(WeaponRuntime runtime)
    {
        object baseData = TryReadMemberValue(runtime, "baseData", "BaseData");
        if (baseData == null)
        {
            ClearLeftStatsOnly();
            return;
        }

        SetSimpleLine(weaponTypeText, $"총기 타입 : {runtime.WeaponType}");

        float baseApCost = ReadFloat(baseData, "apCost", "ApCost");
        float currentApCost = ReadFloat(runtime, "CurrentApCost");
        SetDeltaLine(apCostText, "행동력 코스트", baseApCost.ToString("0"), currentApCost - baseApCost, true, "0");

        float baseSlotCapacity = ReadFloat(baseData, "slotCapacity", "SlotCapacity");
        float currentSlotCapacity = ReadFloat(runtime, "CurrentSlotCapacity");
        SetDeltaLine(slotCapacityText, "슬롯 수", baseSlotCapacity.ToString("0"), currentSlotCapacity - baseSlotCapacity, false, "0");

        float baseSpread = ReadFloat(baseData, "aimSpread", "AimSpread");
        float currentSpread = ReadFloat(runtime, "CurrentAimSpread");
        SetDeltaLine(spreadText, "퍼짐 정도", ConvertSpreadToLabel(baseSpread), currentSpread - baseSpread, true, "0.##");

        float baseMaxRange = ReadFloat(baseData, "maxRange", "MaxRange");
        float currentMaxRange = ReadFloat(runtime, "CurrentMaxRange");
        SetDeltaLine(maxRangeText, "최대 사거리", baseMaxRange.ToString("0.#"), currentMaxRange - baseMaxRange, false, "0.#");

        float baseOptimalRange = ReadFloat(baseData, "optimalRangeMax", "OptimalRangeMax");
        float currentOptimalRange = ReadFloat(runtime, "CurrentOptimalRangeMax");
        SetDeltaLine(effectiveRangeText, "적절 사거리", ConvertEffectiveRangeToLabel(baseOptimalRange), currentOptimalRange - baseOptimalRange, false, "0.#");

        float baseFarDamageMultiplier = ReadFloat(baseData, "farDamageMultiplier", "FarDamageMultiplier");
        float currentFarDamageMultiplier = ReadFloat(runtime, "CurrentFarDamageMultiplier");
        SetDeltaLine(falloffText, "거리별 감소", ConvertFalloffToLabel(baseFarDamageMultiplier), currentFarDamageMultiplier - baseFarDamageMultiplier, false, "0.##");
    }

    private void RefreshLeftStatsFromRunLoadout(WeaponLoadoutData runWeapon)
    {
        object baseData = runWeapon.weaponData;
        if (baseData == null)
        {
            ClearLeftStatsOnly();
            return;
        }

        string weaponTypeString = ReadString(baseData, "weaponType", "WeaponType");
        SetSimpleLine(weaponTypeText, $"총기 타입 : {weaponTypeString}");

        List<WeaponAttachmentData> attachments = runWeapon.equippedAttachments ?? new List<WeaponAttachmentData>();

        float baseApCost = ReadFloat(baseData, "apCost", "ApCost");
        float currentApCost = baseApCost + SumAttachmentFloat(attachments, "apCostDelta", "apCostAdd", "apCostModifier");
        SetDeltaLine(apCostText, "행동력 코스트", baseApCost.ToString("0"), currentApCost - baseApCost, true, "0");

        float baseSlotCapacity = ReadFloat(baseData, "slotCapacity", "SlotCapacity");
        float currentSlotCapacity = baseSlotCapacity + SumAttachmentFloat(attachments, "slotCapacityDelta", "slotCapacityAdd", "slotCapacityModifier");
        SetDeltaLine(slotCapacityText, "슬롯 수", baseSlotCapacity.ToString("0"), currentSlotCapacity - baseSlotCapacity, false, "0");

        float baseSpread = ReadFloat(baseData, "aimSpread", "AimSpread");
        float currentSpread = baseSpread + SumAttachmentFloat(attachments, "aimSpreadAdd", "spreadAdd", "aimSpreadDelta");
        SetDeltaLine(spreadText, "퍼짐 정도", ConvertSpreadToLabel(baseSpread), currentSpread - baseSpread, true, "0.##");

        float baseMaxRange = ReadFloat(baseData, "maxRange", "MaxRange");
        float currentMaxRange = baseMaxRange + SumAttachmentFloat(attachments, "maxRangeAdd", "rangeAdd", "maxRangeDelta");
        SetDeltaLine(maxRangeText, "최대 사거리", baseMaxRange.ToString("0.#"), currentMaxRange - baseMaxRange, false, "0.#");

        float baseOptimalRange = ReadFloat(baseData, "optimalRangeMax", "OptimalRangeMax");
        float currentOptimalRange = baseOptimalRange + SumAttachmentFloat(attachments, "optimalRangeAdd", "effectiveRangeAdd", "optimalRangeDelta");
        SetDeltaLine(effectiveRangeText, "적절 사거리", ConvertEffectiveRangeToLabel(baseOptimalRange), currentOptimalRange - baseOptimalRange, false, "0.#");

        float baseFarDamageMultiplier = ReadFloat(baseData, "farDamageMultiplier", "FarDamageMultiplier");
        float currentFarDamageMultiplier = baseFarDamageMultiplier + SumAttachmentFloat(attachments, "farDamageMultiplierAdd", "farMultiplierAdd", "falloffAdd");
        SetDeltaLine(falloffText, "거리별 감소", ConvertFalloffToLabel(baseFarDamageMultiplier), currentFarDamageMultiplier - baseFarDamageMultiplier, false, "0.##");
    }

    private void RefreshAttachmentSlotsFromRuntime(WeaponRuntime runtime, bool canEdit)
    {
        for (int i = 0; i < attachmentSlots.Length; i++)
        {
            AttachmentSlotView slotView = attachmentSlots[i];
            if (slotView == null || slotView.root == null)
                continue;

            slotView.root.SetActive(true);

            bool isAllowed = runtime.AllowsAttachmentType(slotView.slotType);

            if (slotView.slotNameText != null)
                slotView.slotNameText.text = GetKoreanSlotName(slotView.slotType);

            ApplySlotVisible(slotView, isAllowed);

            if (!isAllowed)
            {
                ClearSlotVisual(slotView);
                BindSlotDragSource(i, null, false);
                BindSlotTooltipTrigger(i, null, false);
                continue;
            }

            WeaponAttachmentData equippedAttachment = runtime.GetAttachmentInSlot(slotView.slotType);
            bool hasAttachment = equippedAttachment != null;

            ApplyEquippedAttachmentVisual(slotView, equippedAttachment);
            BindSlotDragSource(i, equippedAttachment, canEdit && hasAttachment);
            BindSlotTooltipTrigger(i, equippedAttachment, hasAttachment);
        }
    }

    private void RefreshAttachmentSlotsFromRunLoadout(WeaponLoadoutData runWeapon, bool canEdit)
    {
        WeaponData weaponData = runWeapon.weaponData;
        List<WeaponAttachmentData> attachments = runWeapon.equippedAttachments ?? new List<WeaponAttachmentData>();

        for (int i = 0; i < attachmentSlots.Length; i++)
        {
            AttachmentSlotView slotView = attachmentSlots[i];
            if (slotView == null || slotView.root == null)
                continue;

            slotView.root.SetActive(true);

            bool isAllowed = AllowsAttachmentType(weaponData, slotView.slotType);

            if (slotView.slotNameText != null)
                slotView.slotNameText.text = GetKoreanSlotName(slotView.slotType);

            ApplySlotVisible(slotView, isAllowed);

            if (!isAllowed)
            {
                ClearSlotVisual(slotView);
                BindSlotDragSource(i, null, false);
                BindSlotTooltipTrigger(i, null, false);
                continue;
            }

            WeaponAttachmentData equippedAttachment = GetAttachmentInSlot(attachments, slotView.slotType);
            bool hasAttachment = equippedAttachment != null;

            ApplyEquippedAttachmentVisual(slotView, equippedAttachment);
            BindSlotDragSource(i, equippedAttachment, canEdit && hasAttachment);
            BindSlotTooltipTrigger(i, equippedAttachment, hasAttachment);
        }
    }

    private void BindSlotTooltipTrigger(int index, WeaponAttachmentData equippedAttachment, bool isSupportedAndOccupied)
    {
        if (slotTooltipTriggers == null || index < 0 || index >= slotTooltipTriggers.Length)
            return;

        if (slotTooltipTriggers[index] == null)
            return;

        slotTooltipTriggers[index].Bind(equippedAttachment, isSupportedAndOccupied, inventoryUIController);
    }

    private void BindSlotDragSource(int index, WeaponAttachmentData equippedAttachment, bool canDrag)
    {
        if (slotDragSources == null || index < 0 || index >= slotDragSources.Length)
            return;

        if (slotDragSources[index] == null)
            return;

        slotDragSources[index].Bind(equippedAttachment, canDrag);
    }

    private void ApplyEquippedAttachmentVisual(AttachmentSlotView slotView, WeaponAttachmentData equippedAttachment)
    {
        bool hasAttachment = equippedAttachment != null;

        if (slotView.attachmentIconImage != null)
        {
            slotView.attachmentIconImage.enabled = hasAttachment;
            slotView.attachmentIconImage.sprite = hasAttachment ? GetAttachmentSprite(equippedAttachment) : null;
        }

        if (slotView.attachmentEffectText != null)
        {
            slotView.attachmentEffectText.text = hasAttachment
                ? GetShortDescription(ReadString(equippedAttachment, "attachmentDescription", "description"))
                : string.Empty;
        }
    }

    private void ClearSlotVisual(AttachmentSlotView slotView)
    {
        if (slotView.attachmentIconImage != null)
        {
            slotView.attachmentIconImage.sprite = null;
            slotView.attachmentIconImage.enabled = false;
        }

        if (slotView.attachmentEffectText != null)
            slotView.attachmentEffectText.text = string.Empty;
    }

    private Sprite GetAttachmentSprite(WeaponAttachmentData attachment)
    {
        if (attachment == null)
            return null;

        object spriteValue = TryReadMemberValue(attachment, "attachmentSprite", "attachmentSpirte");
        return spriteValue as Sprite;
    }

    private bool AllowsAttachmentType(WeaponData weaponData, AttachmentType slotType)
    {
        if (weaponData == null)
            return false;

        object value = TryReadMemberValue(weaponData, "allowedAttachmentTypes", "AllowedAttachmentTypes");
        if (value == null)
            return false;

        if (value is IEnumerable<AttachmentType> typedEnumerable)
        {
            foreach (AttachmentType type in typedEnumerable)
            {
                if (type == slotType)
                    return true;
            }
        }
        else if (value is System.Collections.IEnumerable enumerable)
        {
            foreach (object item in enumerable)
            {
                if (item is AttachmentType attachmentType && attachmentType == slotType)
                    return true;
            }
        }

        return false;
    }

    private WeaponAttachmentData GetAttachmentInSlot(List<WeaponAttachmentData> attachments, AttachmentType slotType)
    {
        if (attachments == null)
            return null;

        for (int i = 0; i < attachments.Count; i++)
        {
            WeaponAttachmentData attachment = attachments[i];
            if (attachment == null)
                continue;

            object typeValue = TryReadMemberValue(attachment, "attachmentType", "AttachmentType");
            if (typeValue is AttachmentType attachmentType && attachmentType == slotType)
                return attachment;
        }

        return null;
    }

    private float SumAttachmentFloat(List<WeaponAttachmentData> attachments, params string[] memberNames)
    {
        if (attachments == null || attachments.Count == 0)
            return 0f;

        float sum = 0f;

        for (int i = 0; i < attachments.Count; i++)
        {
            WeaponAttachmentData attachment = attachments[i];
            if (attachment == null)
                continue;

            sum += ReadFloat(attachment, memberNames);
        }

        return sum;
    }

    private string GetShortDescription(string text, int maxLength = 20)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }

    private void SetSimpleLine(TextMeshProUGUI text, string fullText)
    {
        if (text == null)
            return;

        text.text = fullText;
    }

    private void SetDeltaLine(
        TextMeshProUGUI text,
        string label,
        string baseValue,
        float deltaValue,
        bool lowerIsBetter,
        string deltaFormat = "0")
    {
        if (text == null)
            return;

        if (Mathf.Approximately(deltaValue, 0f))
        {
            text.text = $"{label} : {baseValue}";
            return;
        }

        bool isPositive = lowerIsBetter ? deltaValue < 0f : deltaValue > 0f;
        string colorHex = isPositive ? positiveHex : negativeHex;
        string sign = deltaValue > 0f ? "+" : "";

        text.text = $"{label} : {baseValue} <color={colorHex}>{sign}{deltaValue.ToString(deltaFormat)}</color>";
    }

    private float ReadFloat(object target, params string[] memberNames)
    {
        object value = TryReadMemberValue(target, memberNames);
        if (value == null)
            return 0f;

        try
        {
            return Convert.ToSingle(value);
        }
        catch
        {
            return 0f;
        }
    }

    private string ReadString(object target, params string[] memberNames)
    {
        object value = TryReadMemberValue(target, memberNames);
        if (value == null)
            return string.Empty;

        return value.ToString();
    }

    private object TryReadMemberValue(object target, params string[] memberNames)
    {
        if (target == null)
            return null;

        Type targetType = target.GetType();

        for (int i = 0; i < memberNames.Length; i++)
        {
            string memberName = memberNames[i];

            FieldInfo field = targetType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                return field.GetValue(target);

            PropertyInfo property = targetType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return property.GetValue(target);
        }

        return null;
    }

    private string ConvertSpreadToLabel(float spread)
    {
        if (spread <= 1f) return "낮음";
        if (spread <= 3f) return "중간";
        return "높음";
    }

    private string ConvertEffectiveRangeToLabel(float optimalRange)
    {
        if (optimalRange <= 3f) return "짧음";
        if (optimalRange <= 6f) return "중간";
        return "긺";
    }

    private string ConvertFalloffToLabel(float farMultiplier)
    {
        if (farMultiplier > 1f) return "증가";
        if (farMultiplier >= 0.8f) return "옅음";
        if (farMultiplier >= 0.5f) return "중간";
        return "심함";
    }

    private string GetKoreanSlotName(AttachmentType type)
    {
        switch (type)
        {
            case AttachmentType.Muzzle: return "총구";
            case AttachmentType.Magazine: return "탄창";
            case AttachmentType.Grip: return "손잡이";
            case AttachmentType.Scope: return "스코프";
            case AttachmentType.Stock: return "개머리판";
            default: return type.ToString();
        }
    }

    private void ClearAll()
    {
        if (weaponNameHeaderText != null)
            weaponNameHeaderText.text = string.Empty;

        if (weaponImage != null)
        {
            weaponImage.sprite = fallbackWeaponSprite;
            weaponImage.enabled = weaponImage.sprite != null;
        }

        ClearLeftStatsOnly();

        for (int i = 0; i < attachmentSlots.Length; i++)
        {
            if (attachmentSlots[i] != null && attachmentSlots[i].root != null)
                attachmentSlots[i].root.SetActive(false);

            BindSlotDragSource(i, null, false);
            BindSlotTooltipTrigger(i, null, false);
        }
    }

    private void ClearLeftStatsOnly()
    {
        ClearText(weaponTypeText);
        ClearText(apCostText);
        ClearText(slotCapacityText);
        ClearText(spreadText);
        ClearText(maxRangeText);
        ClearText(effectiveRangeText);
        ClearText(falloffText);
    }

    private void ClearText(TextMeshProUGUI text)
    {
        if (text == null)
            return;

        text.text = string.Empty;
    }

    private void ApplySlotVisible(AttachmentSlotView slotView, bool isAllowed)
    {
        Color visibleFrameColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        Color visibleTextColor = new Color(1f, 1f, 1f, 1f);
        Color hiddenColor = new Color(1f, 1f, 1f, 0f);

        Color targetTextColor = isAllowed ? visibleTextColor : hiddenColor;
        Color targetFrameColor = isAllowed ? visibleFrameColor : hiddenColor;

        if (slotView.slotNameText != null)
            slotView.slotNameText.color = targetTextColor;

        if (slotView.attachmentEffectText != null)
            slotView.attachmentEffectText.color = targetTextColor;

        if (slotView.backgroundFrameImage != null)
            slotView.backgroundFrameImage.color = targetFrameColor;
    }
}