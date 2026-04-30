using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 무기 1개의 런타임 상태.
/// baseData는 고정 정의이고,
/// loadedAmmo / equippedAttachments는 전투 중 변하는 상태다.
/// </summary>
[Serializable]
public class WeaponRuntime
{
    [SerializeField] private WeaponData baseData;

    [Header("Runtime Ammo")]
    [SerializeField] private List<AmmoModuleData> loadedAmmo = new List<AmmoModuleData>();

    [Header("Runtime Attachment")]
    [SerializeField] private List<WeaponAttachmentData> equippedAttachments = new List<WeaponAttachmentData>();

    // 기존 구조 호환 / 디버그용 캐시
    [SerializeField] private List<AttachmentType> equippedAttachmentTypes = new List<AttachmentType>();

    public WeaponData BaseData => baseData;

    public bool HasBaseData => baseData != null;
    public bool CanReload() => HasBaseData && loadedAmmo.Count < CurrentSlotCapacity;

    public bool HasLoadedAmmo => loadedAmmo.Count > 0;
    public string WeaponName => baseData != null ? baseData.weaponName : "None";
    public WeaponType WeaponType => baseData != null ? baseData.weaponType : default;

    public IReadOnlyList<AmmoModuleData> LoadedAmmo => loadedAmmo;
    public int LoadedAmmoCount => loadedAmmo != null ? loadedAmmo.Count : 0;

    public IReadOnlyList<WeaponAttachmentData> EquippedAttachments => equippedAttachments;
    public IReadOnlyList<AttachmentType> EquippedAttachmentTypes => equippedAttachmentTypes;

    public int CurrentApCost
    {
        get
        {
            if (baseData == null) return 1;
            return Mathf.Max(1, baseData.apCost + GetApCostDeltaTotal());
        }
    }

    public int CurrentSlotCapacity
    {
        get
        {
            if (baseData == null) return 1;
            return Mathf.Max(1, baseData.slotCapacity + GetSlotCapacityDeltaTotal());
        }
    }

    public float CurrentWeaponDamageMultiplier
    {
        get
        {
            if (baseData == null) return 1f;
            return Mathf.Max(0.01f, baseData.weaponDamageMultiplier + GetWeaponDamageMultiplierAddTotal());
        }
    }

    public float CurrentAimSpread
    {
        get
        {
            if (baseData == null) return 0f;
            return Mathf.Max(0f, baseData.aimSpread + GetAimSpreadAddTotal());
        }
    }

    public int CurrentProjectilesPerAttack
    {
        get
        {
            if (baseData == null) return 1;
            return Mathf.Max(1, baseData.projectilesPerAttack + GetProjectilesPerAttackDeltaTotal());
        }
    }

    public float CurrentOptimalRangeMax
    {
        get
        {
            if (baseData == null) return 0f;
            return Mathf.Max(0f, baseData.optimalRangeMax + GetOptimalRangeMaxAddTotal());
        }
    }

    public float CurrentMaxRange
    {
        get
        {
            if (baseData == null) return 0f;

            float maxRange = Mathf.Max(0f, baseData.maxRange + GetMaxRangeAddTotal());

            // 최대 사거리는 최소한 OptimalRange 이상은 되게 맞춘다.
            return Mathf.Max(maxRange, CurrentOptimalRangeMax);
        }
    }

    public float CurrentOptimalDamageMultiplier
    {
        get
        {
            if (baseData == null) return 1f;
            return Mathf.Max(0f, baseData.optimalDamageMultiplier + GetOptimalDamageMultiplierAddTotal());
        }
    }

    public float CurrentFarDamageMultiplier
    {
        get
        {
            if (baseData == null) return 1f;
            return Mathf.Max(0f, baseData.farDamageMultiplier + GetFarDamageMultiplierAddTotal());
        }
    }

    public WeaponRuntime(WeaponData data)
    {
        baseData = data;
        loadedAmmo = new List<AmmoModuleData>();
        equippedAttachments = new List<WeaponAttachmentData>();
        equippedAttachmentTypes = new List<AttachmentType>();
    }

    public WeaponRuntime()
    {
        loadedAmmo = new List<AmmoModuleData>();
        equippedAttachments = new List<WeaponAttachmentData>();
        equippedAttachmentTypes = new List<AttachmentType>();
    }

    /// <summary>
    /// 런타임 무기의 베이스 무기 정의를 바꾼다.
    /// </summary>
    public void SetBaseData(WeaponData data)
    {
        baseData = data;
        RemoveInvalidAttachmentsForCurrentWeapon();
        RefreshEquippedAttachmentTypeCache();
        ClampLoadedAmmoToCapacity();
    }

    // --------------------------------------------------
    // Ammo
    // --------------------------------------------------

    public bool CanLoadAmmo()
    {
        return HasBaseData && loadedAmmo.Count < CurrentSlotCapacity;
    }

    public bool TryLoadAmmo(AmmoModuleData ammo)
    {
        if (ammo == null)
            return false;

        if (CanLoadAmmo() == false)
            return false;

        loadedAmmo.Add(ammo);
        return true;
    }

    public bool CanFire()
    {
        return HasBaseData && loadedAmmo.Count > 0;
    }

    public AmmoModuleData PeekNextAmmo()
    {
        if (loadedAmmo == null || loadedAmmo.Count == 0)
            return null;

        // 현재 기준은 FIFO
        return loadedAmmo[0];
    }

    public bool TryConsumeNextAmmo(out AmmoModuleData usedAmmo)
    {
        if (loadedAmmo == null || loadedAmmo.Count == 0)
        {
            usedAmmo = null;
            return false;
        }
        usedAmmo = loadedAmmo[0];
        loadedAmmo.RemoveAt(0);
        return true;
    }


    public void ClearLoadedAmmo()
    {
        loadedAmmo.Clear();
    }

    // --------------------------------------------------
    // Attachment
    // --------------------------------------------------

    /// <summary>
    /// 장착 가능 여부만 검사.
    /// </summary>
    public bool CanEquipAttachment(WeaponAttachmentData attachment)
    {
        if (attachment == null)
            return false;

        if (baseData == null)
            return false;

        return attachment.IsAllowedForWeapon(baseData);
    }

    /// <summary>
    /// 같은 슬롯 타입 attachment가 이미 있으면 교체한다.
    /// </summary>
    public bool TryEquipAttachment(WeaponAttachmentData attachment)
    {

        if (attachment == null)
        {
            return false;
        }

        if (baseData == null)
        {
            return false;
        }

        if (attachment.IsAllowedForWeapon(baseData) == false)
        {
            return false;
        }

        int sameSlotIndex = GetAttachmentIndexByType(attachment.attachmentType);

        if (sameSlotIndex >= 0)
        {
            equippedAttachments[sameSlotIndex] = attachment;
        }
        else
        {
            equippedAttachments.Add(attachment);
        }

        RefreshEquippedAttachmentTypeCache();
        ClampLoadedAmmoToCapacity();

        return true;
    }

    public bool TryRemoveAttachment(AttachmentType attachmentType)
    {
        int index = GetAttachmentIndexByType(attachmentType);

        if (index < 0)
            return false;

        equippedAttachments.RemoveAt(index);
        RefreshEquippedAttachmentTypeCache();
        ClampLoadedAmmoToCapacity();
        return true;
    }

    public void ClearAttachments()
    {
        equippedAttachments.Clear();
        RefreshEquippedAttachmentTypeCache();
        ClampLoadedAmmoToCapacity();
    }

    public WeaponAttachmentData GetAttachment(AttachmentType attachmentType)
    {
        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] != null &&
                equippedAttachments[i].attachmentType == attachmentType)
            {
                return equippedAttachments[i];
            }
        }

        return null;
    }

    // --------------------------------------------------
    // Debug
    // --------------------------------------------------

    public string GetDebugSummary()
    {
        string attachmentsText = "None";

        if (equippedAttachments != null && equippedAttachments.Count > 0)
        {
            attachmentsText = string.Empty;

            for (int i = 0; i < equippedAttachments.Count; i++)
            {
                if (equippedAttachments[i] == null)
                    continue;

                if (attachmentsText.Length > 0)
                    attachmentsText += "\n";

                attachmentsText += $"- {equippedAttachments[i].GetDebugSummary()}";
            }
        }

        return
            $"[WeaponRuntime Debug]\n" +
            $"Weapon: {WeaponName} ({WeaponType})\n" +
            $"Loaded Ammo: {LoadedAmmoCount}/{CurrentSlotCapacity}\n" +
            $"AP Cost: {CurrentApCost}\n" +
            $"Damage Mul: {CurrentWeaponDamageMultiplier}\n" +
            $"Aim Spread: {CurrentAimSpread}\n" +
            $"Projectiles: {CurrentProjectilesPerAttack}\n" +
            $"Optimal Range: {CurrentOptimalRangeMax}\n" +
            $"Max Range: {CurrentMaxRange}\n" +
            $"Optimal Damage Mul: {CurrentOptimalDamageMultiplier}\n" +
            $"Far Damage Mul: {CurrentFarDamageMultiplier}\n" +
            $"Attachments:\n{attachmentsText}";
    }

    // --------------------------------------------------
    // Internal Helpers
    // --------------------------------------------------

    private void RefreshEquippedAttachmentTypeCache()
    {
        equippedAttachmentTypes.Clear();

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] == null)
                continue;

            equippedAttachmentTypes.Add(equippedAttachments[i].attachmentType);
        }
    }

    private void RemoveInvalidAttachmentsForCurrentWeapon()
    {
        if (baseData == null)
        {
            equippedAttachments.Clear();
            return;
        }

        for (int i = equippedAttachments.Count - 1; i >= 0; i--)
        {
            WeaponAttachmentData attachment = equippedAttachments[i];

            if (attachment == null || attachment.IsAllowedForWeapon(baseData) == false)
            {
                equippedAttachments.RemoveAt(i);
            }
        }
    }

    private int GetAttachmentIndexByType(AttachmentType attachmentType)
    {
        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] != null &&
                equippedAttachments[i].attachmentType == attachmentType)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// 슬롯 수가 줄었을 때 과적재 ammo를 잘라낸다.
    /// 현재 최소 구현에서는 debug 상황만 고려한다.
    /// 실제 인벤토리/전투 규칙이 들어오면 discard 반환 등으로 확장하면 된다.
    /// </summary>
    private void ClampLoadedAmmoToCapacity()
    {
        int capacity = CurrentSlotCapacity;

        while (loadedAmmo.Count > capacity)
        {
            loadedAmmo.RemoveAt(loadedAmmo.Count - 1);
            Debug.LogWarning($"[{WeaponName}] Loaded ammo exceeded capacity, so one round was removed.");
        }
    }

    private int GetApCostDeltaTotal()
    {
        int total = 0;

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] != null)
                total += equippedAttachments[i].apCostDelta;
        }

        return total;
    }

    private int GetSlotCapacityDeltaTotal()
    {
        int total = 0;

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] != null)
                total += equippedAttachments[i].slotCapacityDelta;
        }

        return total;
    }

    private float GetWeaponDamageMultiplierAddTotal()
    {
        float total = 0f;

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] != null)
                total += equippedAttachments[i].weaponDamageMultiplierAdd;
        }

        return total;
    }

    private float GetAimSpreadAddTotal()
    {
        float total = 0f;

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] != null)
                total += equippedAttachments[i].aimSpreadAdd;
        }

        return total;
    }

    private int GetProjectilesPerAttackDeltaTotal()
    {
        int total = 0;

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] != null)
                total += equippedAttachments[i].projectilesPerAttackDelta;
        }

        return total;
    }

    private float GetOptimalRangeMaxAddTotal()
    {
        float total = 0f;

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] != null)
                total += equippedAttachments[i].optimalRangeMaxAdd;
        }

        return total;
    }

    private float GetMaxRangeAddTotal()
    {
        float total = 0f;

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] != null)
                total += equippedAttachments[i].maxRangeAdd;
        }

        return total;
    }

    private float GetOptimalDamageMultiplierAddTotal()
    {
        float total = 0f;

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] != null)
                total += equippedAttachments[i].optimalDamageMultiplierAdd;
        }

        return total;
    }

    private float GetFarDamageMultiplierAddTotal()
    {
        float total = 0f;

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            if (equippedAttachments[i] != null)
                total += equippedAttachments[i].farDamageMultiplierAdd;
        }

        return total;
    }

    /// <summary>
    /// 현재 무기의 사거리 밴드를 계산한다.
    /// attachment 반영된 Current 값 기준으로 판정한다.
    /// </summary>
    public ShotRangeBand GetRangeBand(float distance)
    {
        if (!HasBaseData)
            return ShotRangeBand.OutOfRange;

        if (distance <= CurrentOptimalRangeMax)
            return ShotRangeBand.Optimal;

        if (distance <= CurrentMaxRange)
            return ShotRangeBand.Far;

        return ShotRangeBand.OutOfRange;
    }

    /// <summary>
    /// 현재 무기의 사거리 배수를 계산한다.
    /// attachment 반영된 Current 값 기준으로 반환한다.
    /// OutOfRange면 0을 반환한다.
    /// </summary>
    public float GetRangeDamageMultiplier(float distance)
    {
        ShotRangeBand band = GetRangeBand(distance);

        switch (band)
        {
            case ShotRangeBand.Optimal:
                return CurrentOptimalDamageMultiplier;

            case ShotRangeBand.Far:
                return CurrentFarDamageMultiplier;

            case ShotRangeBand.OutOfRange:
            default:
                return 0f;
        }
    }

    public bool AllowsAttachmentType(AttachmentType type)
    {
        if (baseData == null || baseData.allowedAttachmentTypes == null)
            return false;

        return baseData.allowedAttachmentTypes.Contains(type);
    }

    public WeaponAttachmentData GetAttachmentInSlot(AttachmentType type)
    {
        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            WeaponAttachmentData current = equippedAttachments[i];

            if (current == null)
                continue;

            if (current.attachmentType == type)
                return current;
        }

        return null;
    }

    /// <summary>
    /// 같은 슬롯 타입 부착물이 2개 이상 들어간 비정상 상태인지 검사.
    /// </summary>
    public bool HasDuplicateAttachmentInSameSlot(AttachmentType attachmentType)
    {
        int count = 0;

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            WeaponAttachmentData current = equippedAttachments[i];

            if (current == null)
                continue;

            if (current.attachmentType == attachmentType)
            {
                count++;

                if (count > 1)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 같은 슬롯 타입 attachment가 이미 있으면 교체한다.
    /// 교체된 기존 attachment는 replacedAttachment로 반환한다.
    /// </summary>
    public bool TryEquipAttachment(
        WeaponAttachmentData attachment,
        out WeaponAttachmentData replacedAttachment,
        out string reason)
    {
        replacedAttachment = null;
        reason = string.Empty;

        if (attachment == null)
        {
            reason = "Attachment is null.";
            return false;
        }

        if (baseData == null)
        {
            reason = "Weapon baseData is null.";
            return false;
        }

        if (attachment.IsAllowedForWeapon(baseData) == false)
        {
            reason = $"[{attachment.attachmentName}] cannot be equipped on [{WeaponName}].";
            return false;
        }

        if (HasDuplicateAttachmentInSameSlot(attachment.attachmentType))
        {
            Debug.LogError($"[{WeaponName}] Duplicate attachment state detected in slot [{attachment.attachmentType}].");
            reason = "Duplicate attachment state detected.";
            return false;
        }

        int sameSlotIndex = GetAttachmentIndexByType(attachment.attachmentType);


        if (sameSlotIndex >= 0)
        {
            replacedAttachment = equippedAttachments[sameSlotIndex];
            equippedAttachments[sameSlotIndex] = attachment;
        }
        else
        {
            equippedAttachments.Add(attachment);
        }



        RefreshEquippedAttachmentTypeCache();
        ClampLoadedAmmoToCapacity();

        reason = $"Equipped: {attachment.attachmentName}";
        return true;
    }

    public bool TryRemoveAttachment(AttachmentType attachmentType, out WeaponAttachmentData removedAttachment)
    {
        removedAttachment = null;

        int index = GetAttachmentIndexByType(attachmentType);

        if (index < 0)
            return false;

        removedAttachment = equippedAttachments[index];
        equippedAttachments.RemoveAt(index);

        RefreshEquippedAttachmentTypeCache();
        ClampLoadedAmmoToCapacity();
        return true;
    }

    public void HandleLoadedAmmoOverflow(AmmoDeckRuntime ammoDeckRuntime)
    {
        if (ammoDeckRuntime == null)
            return;

        int capacity = CurrentSlotCapacity;

        while (loadedAmmo.Count > capacity)
        {
            int lastIndex = loadedAmmo.Count - 1;
            AmmoModuleData overflowAmmo = loadedAmmo[lastIndex];
            loadedAmmo.RemoveAt(lastIndex);

            ammoDeckRuntime.Discard(overflowAmmo);
        }
    }
}