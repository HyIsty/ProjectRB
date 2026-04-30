using System;
using UnityEngine;

/// <summary>
/// 총기의 "고정 데이터"를 담는 일반 직렬화 클래스.
/// ScriptableObject가 아니라, 다른 MonoBehaviour 안에 필드로 넣어서 사용한다.
/// </summary>
[Serializable]
public class WeaponData
{
    [Header("Basic Info")]
    public string weaponName = "New Weapon";
    public WeaponType weaponType = WeaponType.Pistol;

    [Header("Combat Cost / Capacity")]
    [Min(0)]
    public int apCost = 1;

    [Min(1)]
    public int slotCapacity = 1;

    [Header("Weapon Stat")]
    [Tooltip("총기 자체의 데미지 배율. 1.0 = 기본, 0.7 = 약함, 1.5 = 강함")]
    [Min(0f)]
    public float weaponDamageMultiplier = 1f;

    [Tooltip("조준 오차 각도(도). 값이 클수록 덜 정확하다.")]
    [Min(0f)]
    public float aimSpread = 0f;

    [Header("Attack Pattern")]
    [Tooltip("한 슬롯을 소비해 1회 공격할 때 나가는 탄환/펠릿 수")]
    [Min(1)]
    public int projectilesPerAttack = 1;

    [Header("Range Bands")]
    [Tooltip("거리 <= optimalRangeMax 이면 적정 거리")]
    [Min(0)]
    public int optimalRangeMax = 3;

    [Tooltip("거리 <= maxRange 이면 먼 거리, 초과하면 사거리 밖")]
    [Min(0)]
    public int maxRange = 8;

    [Header("Range Damage Multipliers")]
    [Tooltip("적정 거리 데미지 배율")]
    [Min(0f)]
    public float optimalDamageMultiplier = 1f;

    [Tooltip("먼 거리 데미지 배율")]
    [Min(0f)]
    public float farDamageMultiplier = 0.8f;

    [Header("Attachment")]
    [Tooltip("나중에 장착 가능한 부착물 타입들")]
    public AttachmentType[] allowedAttachmentTypes;

    [Header("Sprite")]
    public Sprite weaponSprite;

    [Header("Shop")]
    public int shopPrice = 150;

    /// <summary>
    /// 현재 거리가 최대 사거리 밖인지 확인한다.
    /// </summary>
    public bool IsOutOfRange(float distance)
    {
        return distance > maxRange;
    }

    /// <summary>
    /// 현재 거리에 따라 데미지 배율을 반환한다.
    /// 적정 / 멂 / 사거리 밖 3구간만 사용한다.
    /// </summary>
    public float GetRangeDamageMultiplier(float distance)
    {
        if (distance <= optimalRangeMax)
            return optimalDamageMultiplier;

        if (distance <= maxRange)
            return farDamageMultiplier;

        return 0f;
    }

    /// <summary>
    /// 데이터가 이상하게 들어갔는지 확인하고 보정한다.
    /// MonoBehaviour 쪽 OnValidate에서 호출해서 사용한다.
    /// </summary>
    public void ClampAndValidate()
    {
        if (apCost < 0)
            apCost = 0;

        if (slotCapacity < 1)
            slotCapacity = 1;

        if (weaponDamageMultiplier < 0f)
            weaponDamageMultiplier = 0f;

        if (aimSpread < 0f)
            aimSpread = 0f;

        if (optimalRangeMax < 0)
            optimalRangeMax = 0;

        if (maxRange < optimalRangeMax)
            maxRange = optimalRangeMax;

        if (optimalDamageMultiplier < 0f)
            optimalDamageMultiplier = 0f;

        if (farDamageMultiplier < 0f)
            farDamageMultiplier = 0f;

        // 권총/저격총은 1회 공격당 1발 고정으로 보정
        if (weaponType == WeaponType.Pistol || weaponType == WeaponType.Sniper)
        {
            projectilesPerAttack = 1;
        }
        else
        {
            if (projectilesPerAttack < 1)
                projectilesPerAttack = 1;
        }

        if (allowedAttachmentTypes == null)
            allowedAttachmentTypes = Array.Empty<AttachmentType>();
    }
}