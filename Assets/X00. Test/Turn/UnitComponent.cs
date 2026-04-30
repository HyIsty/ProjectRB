using UnityEngine;

public enum UnitTeam
{
    Player,
    Enemy
}

public enum MainActionType
{
    None,
    Move,
    BasicAttack,
    SkillAttack,
    Defend,
    UseItem,
    EndTurn
}

[DisallowMultipleComponent]
public class UnitComponent : MonoBehaviour
{
    [Header("기본 정보")]
    [SerializeField] private string unitName = "Unit";
    [SerializeField] private UnitTeam team = UnitTeam.Player;

    [Header("능력치")]
    [SerializeField] private int hp = 10;
    [SerializeField] private int mp = 5;

    [Tooltip("매 턴 우선순위 계산에 사용되는 값. 높을수록 더 빨리 행동 기회를 얻는다.")]
    [SerializeField] private float turnSpeed = 10f;

    [Header("행동 포인트")]
    [Tooltip("턴 시작 시 회복되는 기본 행동 포인트")]
    [SerializeField] private int baseActionPointPerTurn = 5;

    [Tooltip("최대 행동 포인트")]
    [SerializeField] private int maxActionPoint = 20;

    [Tooltip("현재 행동 포인트")]
    [SerializeField] private int currentActionPoint = 0;

    [Header("이동")]
    [Tooltip("초당 이동 속도")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("턴 게이지")]
    [Tooltip("TurnSystemManager가 누적 관리하는 값. Inspector 확인용")]
    [SerializeField] private float turnGauge = 0f;

    public string UnitName => unitName;
    public UnitTeam Team => team;
    public int HP => hp;
    public int MP => mp;
    public float TurnSpeed => turnSpeed;
    public int CurrentActionPoint => currentActionPoint;
    public int BaseActionPointPerTurn => baseActionPointPerTurn;
    public int MaxActionPoint => maxActionPoint;
    public float MoveSpeed => moveSpeed;
    public float TurnGauge
    {
        get => turnGauge;
        set => turnGauge = value;
    }

    public bool IsDead => hp <= 0;

    public void RefillActionPoint()
    {
        currentActionPoint = Mathf.Clamp(baseActionPointPerTurn, 0, maxActionPoint);
    }

    public bool TrySpendActionPoint(int cost)
    {
        if (cost < 1)
        {
            Debug.LogWarning($"{unitName} - 행동 비용은 최소 1 이상이어야 합니다.");
            return false;
        }

        if (currentActionPoint < cost)
        {
            Debug.Log($"{unitName} - 행동 포인트 부족. 필요:{cost}, 현재:{currentActionPoint}");
            return false;
        }

        currentActionPoint -= cost;
        return true;
    }

    public void RestoreMP(int amount)
    {
        if (amount <= 0) return;
        mp += amount;
    }

    public bool TrySpendMP(int amount)
    {
        if (amount < 0) return false;
        if (mp < amount) return false;

        mp -= amount;
        return true;
    }

    public void TakeDamage(int damage)
    {
        if (damage < 0) return;

        hp -= damage;
        if (hp < 0)
            hp = 0;

        Debug.Log($"{unitName} 이(가) {damage} 피해를 입었습니다. 남은 HP: {hp}");
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        hp += amount;
    }

    public float GetDistanceTo(Vector3 targetPosition)
    {
        return Vector3.Distance(transform.position, targetPosition);
    }
}
