using UnityEngine;

/// <summary>
/// 상태이상 자체를 저장하는 공용 컴포넌트.
/// 이 스크립트는 직접 플레이어 입력이나 적 AI를 제어하지 않는다.
/// 대신, 다른 스크립트가 CanMove / CanShoot / CanAct를 읽도록 한다.
/// </summary>
public class UnitStatusController : MonoBehaviour
{
    [Header("Turn-Based Status Counters")]
    [SerializeField] private int stunTurnsRemaining;
    [SerializeField] private int rootTurnsRemaining;
    [SerializeField] private int shootBlockTurnsRemaining;
    [SerializeField] private int actBlockTurnsRemaining;

    [Header("Readonly Debug")]
    [SerializeField] private bool canMove = true;
    [SerializeField] private bool canShoot = true;
    [SerializeField] private bool canAct = true;

    /// <summary>
    /// 이동 가능 여부.
    /// stun / root / act block 영향을 반영한다.
    /// </summary>
    public bool CanMove => canMove;

    /// <summary>
    /// 사격 가능 여부.
    /// stun / shoot block / act block 영향을 반영한다.
    /// </summary>
    public bool CanShoot => canShoot;

    /// <summary>
    /// 행동 가능 여부.
    /// 적 AI가 이 값을 보고 턴 스킵 여부를 결정하게 만든다.
    /// </summary>
    public bool CanAct => canAct;

    private void Awake()
    {
        RefreshCapabilityFlags();
    }

    /// <summary>
    /// 기절. 이동/사격/행동 전부 막는 대표 상태.
    /// </summary>
    public void ApplyStun(int turns)
    {
        if (turns <= 0) return;

        stunTurnsRemaining = Mathf.Max(stunTurnsRemaining, turns);
        RefreshCapabilityFlags();
    }

    /// <summary>
    /// 속박. 이동만 막는다.
    /// </summary>
    public void ApplyRoot(int turns)
    {
        if (turns <= 0) return;

        rootTurnsRemaining = Mathf.Max(rootTurnsRemaining, turns);
        RefreshCapabilityFlags();
    }

    /// <summary>
    /// 사격 봉쇄.
    /// </summary>
    public void ApplyShootBlock(int turns)
    {
        if (turns <= 0) return;

        shootBlockTurnsRemaining = Mathf.Max(shootBlockTurnsRemaining, turns);
        RefreshCapabilityFlags();
    }

    /// <summary>
    /// 일반 행동 봉쇄.
    /// 필요할 때 확장용으로 둔다.
    /// </summary>
    public void ApplyActBlock(int turns)
    {
        if (turns <= 0) return;

        actBlockTurnsRemaining = Mathf.Max(actBlockTurnsRemaining, turns);
        RefreshCapabilityFlags();
    }

    /// <summary>
    /// 상태 전체 제거.
    /// </summary>
    public void ClearAllStatus()
    {
        stunTurnsRemaining = 0;
        rootTurnsRemaining = 0;
        shootBlockTurnsRemaining = 0;
        actBlockTurnsRemaining = 0;

        RefreshCapabilityFlags();
    }

    /// <summary>
    /// 턴이 한 번 지나갔을 때 duration 감소.
    /// 정확히 "턴 시작"에 깎을지 "턴 종료"에 깎을지는 아직 설계 미확정이므로,
    /// 나중에 TurnManager가 원하는 타이밍에 이 함수를 호출하면 된다.
    /// </summary>
    public void AdvanceTurnDurations()
    {
        if (stunTurnsRemaining > 0) stunTurnsRemaining--;
        if (rootTurnsRemaining > 0) rootTurnsRemaining--;
        if (shootBlockTurnsRemaining > 0) shootBlockTurnsRemaining--;
        if (actBlockTurnsRemaining > 0) actBlockTurnsRemaining--;

        RefreshCapabilityFlags();
    }

    private void RefreshCapabilityFlags()
    {
        bool hasStun = stunTurnsRemaining > 0;
        bool hasRoot = rootTurnsRemaining > 0;
        bool hasShootBlock = shootBlockTurnsRemaining > 0;
        bool hasActBlock = actBlockTurnsRemaining > 0;

        canAct = !hasStun && !hasActBlock;
        canMove = !hasStun && !hasRoot && !hasActBlock;
        canShoot = !hasStun && !hasShootBlock && !hasActBlock;
    }

    // 디버그 확인용 getter들
    public int GetStunTurnsRemaining() => stunTurnsRemaining;
    public int GetRootTurnsRemaining() => rootTurnsRemaining;
    public int GetShootBlockTurnsRemaining() => shootBlockTurnsRemaining;
    public int GetActBlockTurnsRemaining() => actBlockTurnsRemaining;
}