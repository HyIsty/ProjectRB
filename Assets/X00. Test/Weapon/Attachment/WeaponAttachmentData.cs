using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 하나의 부착물 데이터.
/// 현재 최소 구현에서는 "스탯 수정자"만 다룬다.
/// 특수 발동형 효과는 나중에 별도 구조로 확장한다.
/// </summary>
[Serializable]
public class WeaponAttachmentData
{
    [Header("Identity")]
    public string attachmentId = "attachment_id";
    public string attachmentName = "New Attachment";
    public AttachmentType attachmentType = AttachmentType.Muzzle;
    public string attachmentDescription = string.Empty;

    [Header("Equip Restriction")]
    [Tooltip("비워두면 모든 WeaponType 허용. 값이 있으면 해당 타입 무기에만 장착 가능.")]
    public List<WeaponType> allowedWeaponTypes = new List<WeaponType>();

    // -----------------------------
    // 1) Weapon Stat Attachment
    // -----------------------------
    [Header("Weapon Stat Modifiers")]
    public int apCostDelta = 0;
    public int slotCapacityDelta = 0;
    public float weaponDamageMultiplierAdd = 0f;
    public float aimSpreadAdd = 0f;
    public int projectilesPerAttackDelta = 0;
    public float optimalRangeMaxAdd = 0f;
    public float maxRangeAdd = 0f;
    public float optimalDamageMultiplierAdd = 0f;
    public float farDamageMultiplierAdd = 0f;

    // -----------------------------
    // 2) Ammo Conditional Attachment
    // -----------------------------
    [Header("Ammo Conditional Rule")]
    [Tooltip("비어 있으면 조건 없음. 값이 있으면 해당 ammoId일 때만 아래 효과 적용.")]
    public string requiredAmmoId = "";

    [Header("Ammo Conditional Modifiers")]
    public int conditionalProjectileBaseDamageAdd = 0;
    public float conditionalWeaponDamageMultiplierAdd = 0f;
    public float conditionalOptimalRangeMaxAdd = 0f;
    public float conditionalMaxRangeAdd = 0f;
    public float conditionalOptimalDamageMultiplierAdd = 0f;
    public float conditionalFarDamageMultiplierAdd = 0f;

    // -----------------------------
    // 3) Final Damage Attachment
    // -----------------------------
    [Header("Final Damage Modifiers")]
    public float finalDamageFlatAdd = 0f;
    public float finalDamageMultiplierAdd = 0f;

    [Header("Sprite")]
    public Sprite attachmentSprite;
    [Header("Shop")]
    public int shopPrice = 100;
    /// <summary>
    /// 해당 무기에 장착 가능한지 검사한다.
    /// </summary>
    public bool IsAllowedForWeapon(WeaponData weaponData)
    {
        if (weaponData == null)
            return false;

        // 1) 무기 자체가 이 슬롯 타입을 지원하는지 검사
        if (weaponData.allowedAttachmentTypes != null &&
            weaponData.allowedAttachmentTypes.Length > 0 &&
            weaponData.allowedAttachmentTypes.Contains(attachmentType) == false)
        {
            return false;
        }

        // 2) 부착물 자체가 특정 무기 타입만 허용하는지 검사
        if (allowedWeaponTypes != null &&
            allowedWeaponTypes.Count > 0 &&
            allowedWeaponTypes.Contains(weaponData.weaponType) == false)
        {
            return false;
        }

        return true;
    }

    public bool MatchesAmmo(AmmoModuleData usedRound)
    {
        if (string.IsNullOrWhiteSpace(requiredAmmoId))
            return false;

        if (usedRound == null)
            return false;

        return usedRound.id == requiredAmmoId;
    }

    /// <summary>
    /// 디버그 출력용 짧은 요약 문자열.
    /// </summary>
    public string GetDebugSummary()
    {
        return $"{attachmentName} ({attachmentType}) | " +
               $"AP {apCostDelta:+#;-#;0}, " +
               $"Slot {slotCapacityDelta:+#;-#;0}, " +
               $"DmgMul {weaponDamageMultiplierAdd:+0.##;-0.##;0}, " +
               $"Spread {aimSpreadAdd:+0.##;-0.##;0}, " +
               $"Proj {projectilesPerAttackDelta:+#;-#;0}, " +
               $"OptRange {optimalRangeMaxAdd:+0.##;-0.##;0}, " +
               $"MaxRange {maxRangeAdd:+0.##;-0.##;0}, " +
               $"OptDmg {optimalDamageMultiplierAdd:+0.##;-0.##;0}, " +
               $"FarDmg {farDamageMultiplierAdd:+0.##;-0.##;0}";
    }
}