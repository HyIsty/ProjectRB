using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combat 씬이 끝난 후, InGame 씬이 1회 소비할 임시 결과 데이터.
/// RunData(장기 데이터)와 분리한다.
/// </summary>

[Serializable]
public class PendingCombatResult
{
    // 전투에서 이겼는지
    public bool wasVictory;

    // InGame으로 돌아왔을 때 보상 UI를 열어야 하는지
    public bool shouldShowReward;

    // 나중에 어떤 노드에서 온 결과인지 추적하고 싶을 때 사용
    public string clearedNodeId;

    public PendingCombatResult(bool wasVictory, bool shouldShowReward, string clearedNodeId = "")
    {
        this.wasVictory = wasVictory;
        this.shouldShowReward = shouldShowReward;
        this.clearedNodeId = clearedNodeId;
    }
}

[System.Serializable]
public class RoomData
{
    public Vector2Int gridPosition;
    public RoomType roomType;
    public List<Vector2Int> connectedRooms = new List<Vector2Int>();

    public RoomData(Vector2Int gridPosition, RoomType roomType)
    {
        this.gridPosition = gridPosition;
        this.roomType = roomType;
    }
}

[System.Serializable]
public class MetaProgressData
{
    public int totalGold = 0;
    public int highestFloor = 0;
    public List<string> unlockedRewards = new List<string>();
}

public enum DeckBuildCardTypeExample
{
    Attack,
    Block,
    Draw,
    Energy,
    Utility
}


/// <summary>
/// 바닥 자체의 성질.
/// 지금은 Floor / Blocked만 두지만,
/// 나중에 물, 독지대, 파괴불가 벽 같은 확장이 쉬워진다.
/// </summary>
public enum TileBaseType
{
    Floor,
    Blocked
}

/// <summary>
/// 엄폐물 / 장애물 정보.
/// 지금은 Obstacle만 사용한다.
/// </summary>
public enum CoverType
{
    None,
    Obstacle
}

/// <summary>
/// 시작 위치 표시용 마커.
/// 전투 시작 후에는 이 정보를 직접 참조하지 않아도 된다.
/// 지금은 디버그/초기 배치용 의미가 크다.
/// </summary>
public enum SpawnMarkerType
{
    None,
    PlayerCandidate,
    EnemyCandidate
}

/// <summary>
/// 현재 그 칸에 누가 서 있는지.
/// </summary>
public enum OccupantType
{
    None,
    Player,
    Enemy
}

/// <summary>
/// 런타임에서 사용하는 실제 타일 데이터.
/// "맵 정보"와 "현재 점유 정보"를 분리하기 위해 만든다.
/// </summary>
[System.Serializable]
public class TileData
{
    public Vector2Int gridPos;
    public TileBaseType baseType;
    public CoverType coverType;
    public SpawnMarkerType spawnMarker;
    public OccupantType occupantType;
    public GameObject occupantObject;

    public TileData(Vector2Int gridPos)
    {
        this.gridPos = gridPos;
        baseType = TileBaseType.Floor;
        coverType = CoverType.None;
        spawnMarker = SpawnMarkerType.None;
        occupantType = OccupantType.None;
        occupantObject = null;
    }
}

public enum ShotRangeBand
{
    Optimal,    // 적정 거리
    Far,        // 멂
    OutOfRange  // 사거리 밖
}
/// <summary>
/// 런타임에서 실제로 사용하는 탄환 카드 데이터.
/// 한 장, 한 장의 "실제 카드 인스턴스"라고 생각하면 된다.
/// </summary>
[Serializable]
public class AmmoModuleData
{
    [Header("Runtime Card Data")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public int damage;
    public List<string> glossaryList = new List<string>();
    public Sprite sprite;

    [Header("Shop")]
    public int shopPrice = 50;

    [Header("Effect Data")]
    [Tooltip("예: none, stun_target, root_target, heal_self_on_hit")]
    public string effectId = "none";

    [Tooltip("효과의 수치값. 예: 회복량, 추가 효과량 등")]
    public int effectPower = 0;

    [Tooltip("효과 지속 턴 수. 예: stun 1턴, root 2턴")]
    public int effectDuration = 0;

    /// <summary>
    /// 코드에서 직접 런타임 탄 데이터를 만들 때 사용하는 생성자.
    /// 기존 4개 인자 호출을 깨지 않기 위해 effect 관련 값은 기본값을 둔다.
    /// </summary>
    public AmmoModuleData(
        string id,
        string displayName,
        string description,
        int damage,
        List<string> glossaryList,
        string effectId = "none",
        int effectPower = 0,
        int effectDuration = 0)
    {
        // 값이 비어 있으면 최소한의 안전값을 넣어 준다.
        this.id = string.IsNullOrWhiteSpace(id) ? "" : id;
        this.displayName = string.IsNullOrWhiteSpace(displayName) ? "Unnamed Round" : displayName;
        this.description = description;
        this.damage = damage;
        this.glossaryList = glossaryList;

        this.effectId = string.IsNullOrWhiteSpace(effectId) ? "none" : effectId.Trim();
        this.effectPower = effectPower;
        this.effectDuration = effectDuration;
    }

    /// <summary>
    /// 인스펙터용 입력 데이터(AmmoModuleEntry)로부터
    /// 실제 런타임 카드 데이터를 복사 생성한다.
    /// </summary>
    public AmmoModuleData(AmmoModuleEntry entry)
    {
        id = string.IsNullOrWhiteSpace(entry.id) ? "" : entry.id;
        displayName = string.IsNullOrWhiteSpace(entry.displayName) ? "Unnamed Round" : entry.displayName;
        description = entry.description;
        damage = entry.damage;
        glossaryList = entry.glossaryList;

        effectId = string.IsNullOrWhiteSpace(entry.effectId) ? "none" : entry.effectId.Trim();
        effectPower = entry.effectPower;
        effectDuration = entry.effectDuration;
    }

    /// <summary>
    /// effectId가 비어 있으면 안전하게 none으로 처리한다.
    /// CardActionManager에서 이 값을 기준으로 핸들러를 찾는다.
    /// </summary>
    public string GetSafeEffectId()
    {
        return string.IsNullOrWhiteSpace(effectId) ? "none" : effectId.Trim();
    }

    public override string ToString()
    {
        return $"{displayName} (+{damage} DMG, Effect: {GetSafeEffectId()}, Power: {effectPower}, Duration: {effectDuration})";
    }
}

/// <summary>
/// ScriptableObject를 쓰지 않기 때문에,
/// 스타팅 덱을 인스펙터에 직접 적어 넣기 위한 입력용 데이터.
/// "이 탄종을 몇 장 스타팅 덱에 넣을지"까지 포함한다.
/// </summary>
[Serializable]
public class AmmoModuleEntry
{
    [Header("Basic Data")]
    public string id;
    public string displayName;
    [TextArea] public string description;
    public int damage;
    public List<string> glossaryList = new List<string>(); 

    [Header("Effect Data")]
    public string effectId = "none";
    public int effectPower = 0;
    public int effectDuration = 0;
}

public static class AmmoEffectIds
{
    public const string None = "none";

    public const string StunTarget = "stun_target";
    public const string RootTarget = "root_target";
    public const string BlockShootTarget = "block_shoot_target";

    public const string HealSelfOnHit = "heal_self_on_hit";

    // 아래 둘은 설계 예시로는 존재하지만,
    // 현재 타이밍/연동이 아직 확정되지 않았으므로 이번 1차 구현에서는 미사용.
    public const string BurnTarget = "burn_target";
    public const string DrawSelfOnKill = "draw_self_on_kill";
}

/// <summary>
/// 사격 결과 이후 effect 실행에 필요한 정보를 한 번에 전달하는 컨텍스트.
/// target-only 구조가 아니라 attacker / target / result 전체를 담는다.
/// </summary>
[System.Serializable]
public class CardActionContext
{
    public GameObject attacker;
    public GameObject target;

    public AmmoModuleData usedRound;

    public int finalDamage;
    public bool didHit;
    public bool didKill;
    public ShotRangeBand rangeBand;

    public CardActionContext(
        GameObject attacker,
        GameObject target,
        AmmoModuleData usedRound,
        int finalDamage,
        bool didHit,
        bool didKill,
        ShotRangeBand rangeBand)
    {
        this.attacker = attacker;
        this.target = target;
        this.usedRound = usedRound;
        this.finalDamage = finalDamage;
        this.didHit = didHit;
        this.didKill = didKill;
        this.rangeBand = rangeBand;
    }

    public UnitStatusController GetAttackerStatus()
    {
        if (attacker == null) return null;
        return attacker.GetComponent<UnitStatusController>();
    }

    public UnitStatusController GetTargetStatus()
    {
        if (target == null) return null;
        return target.GetComponent<UnitStatusController>();
    }

    public UnitHealthController GetAttackerHealth()
    {
        if (attacker == null) return null;
        return attacker.GetComponent<UnitHealthController>();
    }

    public UnitHealthController GetTargetHealth()
    {
        if (target == null) return null;
        return target.GetComponent<UnitHealthController>();
    }
}

/// <summary>
/// 총기 타입 - 권총 / 소총 / 샷건 / 저격총
/// </summary>
public enum WeaponType
{
    Pistol,
    Rifle,
    Shotgun,
    Sniper
}

public enum AttachmentType
{
    Muzzle = 0,
    Magazine = 1,
    Stock = 2,
    Grip = 3,
    Scope = 4
}
public enum AttachmentEffectLayer
{
    WeaponStat,         // 상시 무기 스탯
    AmmoConditional,    // 특정 탄일 때만 발사 1회 수정
    FinalDamage         // 최종 데미지 계산 후 마지막 적용
}
/// <summary>
/// glossary 한 항목의 데이터.
/// 예:
/// key = "Stun"
/// title = "Stun"
/// description = "This unit cannot move, shoot, or act."
/// </summary>
[System.Serializable]
public class EffectGlossaryEntry
{
    [Header("Identity")]
    public string key;
    public string title;

    [Header("Tooltip Text")]
    [TextArea]
    public string description;
}
/// <summary>
/// BoardManager가 전투 맵 생성 후 CombatManager에 넘겨줄 스폰 결과 데이터.
/// </summary>
[Serializable]
public class CombatSpawnResult
{
    public Vector2Int playerSpawnGrid;
    public List<Vector2Int> enemySpawnGrids = new List<Vector2Int>();
}

public enum EnemyState
{
    Idle,
    Hostile,
    CanShoot,
    Dead
}

public enum EnemyActionType
{
    None,
    PatrolMove,
    HostileMove,
    CoverMove,
    Shoot,
    Reload
}
public class Constants
{

}
