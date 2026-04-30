using UnityEngine;

/// <summary>
/// EnemyAIController가 적 사격 컴포넌트와 통신하기 위한 최소 인터페이스.
/// 네 기존 PlayerShooter / WeaponRuntime / AmmoDeckRuntime 구조에 맞춰
/// 적 전용 Shooter가 이 인터페이스를 구현하면 된다.
/// </summary>
public interface IEnemyShooter
{
    /// <summary>실제 사용할 무기가 있는가</summary>
    bool HasUsableWeapon { get; }

    /// <summary>현재 장전된 탄이 있는가</summary>
    bool HasLoadedAmmo { get; }

    /// <summary>탄이 비었거나 재장전 가치가 있는가</summary>
    bool NeedsReload { get; }

    /// <summary>현재 발사 AP 비용</summary>
    int CurrentShootApCost { get; }

    /// <summary>
    /// "위치상" 사격 가능한 각인지 판단.
    /// AP / 장전 여부와 무관하게 range + LOS 기준으로만 판단하는 용도.
    /// </summary>
    bool CanThreatenTargetPosition(Transform target);

    /// <summary>
    /// 지금 실제로 쏠 수 있는가.
    /// 보통 range + LOS + loaded ammo 기준.
    /// </summary>
    bool CanShootTarget(Transform target);

    /// <summary>실제 사격 실행</summary>
    bool TryShootTarget(Transform target);

    /// <summary>실제 장전 실행</summary>
    bool TryReload();

    /// <summary>
    /// "이 월드 위치에서" targetWorld를 사격 사거리 안에 둘 수 있는지 평가.
    /// 이동 점수 계산용.
    /// </summary>
    bool IsTargetInShootRangeFromWorld(Vector3 fromWorld, Vector3 targetWorld);
}