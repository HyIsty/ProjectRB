using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// effectId -> handler 매핑을 담당하는 실행 진입점.
/// giant switch로 불리는 것을 피하고, Dictionary 기반으로 확장한다.
/// </summary>
public class CardActionManager : MonoBehaviour
{
    public static CardActionManager Instance { get; private set; }

    private Dictionary<string, Action<CardActionContext>> handlers;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        handlers = new Dictionary<string, Action<CardActionContext>>(StringComparer.OrdinalIgnoreCase);
        RegisterDefaultHandlers();
    }

    private void RegisterDefaultHandlers()
    {
        RegisterHandler(AmmoEffectIds.None, HandleNone);

        RegisterHandler(AmmoEffectIds.StunTarget, HandleStunTarget);
        RegisterHandler(AmmoEffectIds.RootTarget, HandleRootTarget);
        RegisterHandler(AmmoEffectIds.BlockShootTarget, HandleBlockShootTarget);

        RegisterHandler(AmmoEffectIds.HealSelfOnHit, HandleHealSelfOnHit);

        // 아래 둘은 아직 이번 1차 구현에서 미연결
        // RegisterHandler(AmmoEffectIds.BurnTarget, HandleBurnTarget);
        // RegisterHandler(AmmoEffectIds.DrawSelfOnKill, HandleDrawSelfOnKill);
    }

    public void RegisterHandler(string effectId, Action<CardActionContext> handler)
    {
        if (string.IsNullOrWhiteSpace(effectId))
        {
            Debug.LogWarning("CardActionManager: effectId is null or empty.");
            return;
        }

        if (handler == null)
        {
            Debug.LogWarning($"CardActionManager: handler is null for effectId [{effectId}]");
            return;
        }

        handlers[effectId.Trim()] = handler;
    }

    public void ExecuteAmmoEffect(CardActionContext context)
    {
        if (context == null)
        {
            Debug.LogWarning("CardActionManager: context is null.");
            return;
        }

        if (context.usedRound == null)
        {
            // 탄 데이터가 없으면 effect도 실행하지 않는다.
            return;
        }

        string effectId = context.usedRound.GetSafeEffectId();

        if (!handlers.TryGetValue(effectId, out Action<CardActionContext> handler))
        {
            Debug.LogWarning($"CardActionManager: no handler found for effectId [{effectId}]");
            return;
        }

        handler.Invoke(context);
    }

    private void HandleNone(CardActionContext context)
    {
        // 아무 효과 없음
    }

    private void HandleStunTarget(CardActionContext context)
    {
        if (!context.didHit) return;

        UnitStatusController targetStatus = context.GetTargetStatus();
        if (targetStatus == null) return;

        int duration = Mathf.Max(1, context.usedRound.effectDuration);
        targetStatus.ApplyStun(duration);
    }

    private void HandleRootTarget(CardActionContext context)
    {
        if (!context.didHit) return;

        UnitStatusController targetStatus = context.GetTargetStatus();
        if (targetStatus == null) return;

        int duration = Mathf.Max(1, context.usedRound.effectDuration);
        targetStatus.ApplyRoot(duration);
    }

    private void HandleBlockShootTarget(CardActionContext context)
    {
        if (!context.didHit) return;

        UnitStatusController targetStatus = context.GetTargetStatus();
        if (targetStatus == null) return;

        int duration = Mathf.Max(1, context.usedRound.effectDuration);
        targetStatus.ApplyShootBlock(duration);
    }

    private void HandleHealSelfOnHit(CardActionContext context)
    {
        if (!context.didHit) return;

        UnitHealthController attackerHealth = context.GetAttackerHealth();
        if (attackerHealth == null) return;

        int healAmount = Mathf.Max(0, context.usedRound.effectPower);
        if (healAmount <= 0) return;

        attackerHealth.Heal(healAmount);
    }
}