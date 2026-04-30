using System;
using UnityEngine;

public class UnitHealthController : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private int maxHP = 30;
    [SerializeField] private int currentHP;

    [Header("Ref")]
    [SerializeField] private GridUnit unit;

    [Header("Death Option")]
    [Tooltip("죽었을 때 GameObject를 Destroy할지 여부")]
    [SerializeField] private bool destroyOnDeath = true;

    public event Action HealthChanged;
    public event Action<UnitHealthController> OnDied;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0;

    private void Awake()
    {
        currentHP = maxHP;

        if (unit == null)
            unit = GetComponent<GridUnit>();
    }

    public void Initialize(int maxHp, int currentHp)
    {
        maxHP = Mathf.Max(1, maxHp);
        currentHP = Mathf.Clamp(currentHp, 0, maxHP);
        RaiseHealthChanged();
    }

    public void TakeDamage(int damage, ShotRangeBand rangeBand)
    {
        if (IsDead || damage <= 0)
            return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);

        RaiseHealthChanged();

        Debug.Log($"{name} took {damage} damage. RangeBand = {rangeBand}, CurrentHP = {currentHP}");

        if (currentHP <= 0)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayUnitDeath();
            Die();
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayUnitHit();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
            return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0);

        RaiseHealthChanged();

        Debug.Log($"{name} took {damage} damage. CurrentHP = {currentHP}");

        if (currentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        currentHP += amount;
        currentHP = Mathf.Min(currentHP, maxHP);

        RaiseHealthChanged();

        Debug.Log($"{name} healed {amount}. CurrentHP = {currentHP}");
    }

    public void SetCurrentHP(int value)
    {
        currentHP = Mathf.Clamp(value, 0, maxHP);
        RaiseHealthChanged();
    }

    private void Die()
    {
        Debug.Log($"{name} died.");

        OnDied?.Invoke(this);

        if (unit != null && unit.BoardManager != null)
            unit.BoardManager.RemoveUnit(unit);

        if (destroyOnDeath)
            Destroy(gameObject);
    }

    private void RaiseHealthChanged()
    {
        HealthChanged?.Invoke();
    }
}