using UnityEngine;

public enum TurnOwner
{
    None,
    Player,
    Enemy
}

public enum PhaseType
{
    None,
    Move,
    Attack
}

public enum AttackType
{
    None,
    BasicAttack,
    SkillAttack,
    UseItem
}
public interface IBattleUnit
{
    string UnitName { get; }
    int CurrentHP { get; }
    int MaxHP { get; }
    bool IsDead { get; }

    void OnTurnStart();
    void OnTurnEnd();

    void OnPhaseStart(PhaseType phaseType);
    void OnPhaseEnd(PhaseType phaseType);

    void Move();
    void BasicAttack(IBattleUnit target);
    void SkillAttack(IBattleUnit target);
    void UseItem(IBattleUnit target);

    void TakeDamage(int damage);
}
public class BattleUnit : MonoBehaviour, IBattleUnit
{
    [Header("기본 정보")]
    [SerializeField] private string unitName = "Unit";
    [SerializeField] private int maxHP = 30;
    [SerializeField] private int currentHP = 30;

    [Header("공격력")]
    [SerializeField] private int basicAttackDamage = 5;
    [SerializeField] private int skillAttackDamage = 10;

    public string UnitName => unitName;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsDead => currentHP <= 0;

    public void OnTurnStart()
    {
        Debug.Log($"[{UnitName}] 턴 시작");
    }

    public void OnTurnEnd()
    {
        Debug.Log($"[{UnitName}] 턴 종료");
    }

    public void OnPhaseStart(PhaseType phaseType)
    {
        Debug.Log($"[{UnitName}] Phase 시작: {phaseType}");
    }

    public void OnPhaseEnd(PhaseType phaseType)
    {
        Debug.Log($"[{UnitName}] Phase 종료: {phaseType}");
    }

    public void Move()
    {
        Debug.Log($"[{UnitName}] 이동 실행");
        // 실제 프로젝트에서는 여기서 타일 이동, AP 감소, 위치 변경 등을 처리
    }

    public void BasicAttack(IBattleUnit target)
    {
        if (target == null || target.IsDead) return;

        Debug.Log($"[{UnitName}] 기본공격 → [{target.UnitName}] / 피해 {basicAttackDamage}");
        target.TakeDamage(basicAttackDamage);
    }

    public void SkillAttack(IBattleUnit target)
    {
        if (target == null || target.IsDead) return;

        Debug.Log($"[{UnitName}] 스킬공격 → [{target.UnitName}] / 피해 {skillAttackDamage}");
        target.TakeDamage(skillAttackDamage);
    }

    public void UseItem(IBattleUnit target)
    {
        if (target == null) return;

        int healAmount = 7;
        Debug.Log($"[{UnitName}] 아이템 사용 → [{target.UnitName}] / 회복 {healAmount}");

        // 여기서는 예시로 자기 자신 혹은 대상 회복으로 처리
        if (target is BattleUnit battleUnit)
        {
            battleUnit.Heal(healAmount);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);

        Debug.Log($"[{UnitName}] 피해 받음: {damage} / 현재 HP: {currentHP}/{maxHP}");

        if (IsDead)
        {
            Debug.Log($"[{UnitName}] 사망");
        }
    }

    public void Heal(int amount)
    {
        currentHP += amount;
        currentHP = Mathf.Min(currentHP, maxHP);

        Debug.Log($"[{UnitName}] 회복: {amount} / 현재 HP: {currentHP}/{maxHP}");
    }
}
