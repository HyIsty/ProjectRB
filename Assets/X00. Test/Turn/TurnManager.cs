using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 턴 흐름의 scene-side owner.
/// 현재 목적:
/// - player turn 시작 / 종료
/// - player AP 관리
/// - enemy phase 순차 실행
/// - 전투 종료 판정 호출
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    public enum CombatTurnState
    {
        None,
        PlayerTurn,
        EnemyTurn,
        Busy,
        Victory,
        Defeat
    }

    [Header("Scene Refs")]
    [SerializeField] private CombatManager combatManager;

    [Header("Player AP")]
    [SerializeField] private int maxPlayerAP = 3;

    [Header("Flow Timing")]
    [SerializeField] private float beforeEnemyPhaseDelay = 0.1f;
    [SerializeField] private float betweenEnemyDelay = 0.12f;
    [SerializeField] private float afterEnemyPhaseDelay = 0.1f;

    [Header("Debug")]
    [SerializeField] private CombatTurnState currentState = CombatTurnState.None;
    [SerializeField] private int currentPlayerAP;
    [SerializeField] private bool isResolvingPhase;

    public event Action<CombatTurnState> OnTurnStateChanged;
    public event Action<int, int> OnPlayerAPChanged;

    public CombatTurnState CurrentState => currentState;
    public int CurrentPlayerAP => currentPlayerAP;
    public int MaxPlayerAP => maxPlayerAP;

    public bool IsPlayerTurn => currentState == CombatTurnState.PlayerTurn;
    public bool IsEnemyTurn => currentState == CombatTurnState.EnemyTurn;
    public bool IsBusy => currentState == CombatTurnState.Busy || isResolvingPhase;

    private void Awake()
    {
        if (combatManager == null)
            combatManager = FindFirstObjectByType<CombatManager>();

        Instance = this;
    }

    /// <summary>
    /// Combat 시작 시 호출.
    /// 네 current CombatManager는 BeginCombat 마지막에 이걸 이미 부르고 있다.
    /// </summary>
    public void StartPlayerTurn()
    {
        if (combatManager == null)
        {
            Debug.LogError("[TurnManager] CombatManager is missing.");
            return;
        }

        if (combatManager.IsCombatEnded)
            return;

        isResolvingPhase = false;
        SetState(CombatTurnState.PlayerTurn);

        currentPlayerAP = maxPlayerAP;
        RaisePlayerAPChanged();
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayTurnStart();

        
        Debug.Log($"[TurnManager] Player Turn Start | AP = {currentPlayerAP}/{maxPlayerAP}");
    }

    /// <summary>
    /// PlayerInputManager EndTurn 입력, HUD EndTurn 버튼에서 이걸 호출하면 된다.
    /// </summary>
    public bool RequestEndPlayerTurn()
    {
        if (!IsPlayerTurn)
        {
            Debug.Log("[TurnManager] Cannot end player turn. It is not player turn.");
            return false;
        }

        if (isResolvingPhase)
            return false;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayTurnEnd();

        StartCoroutine(RunEnemyPhaseRoutine());
        return true;
    }

    /// <summary>
    /// 플레이어 액션들이 AP 소모할 때 호출.
    /// movement / shoot / reload가 여길 타면 된다.
    /// </summary>
    public bool TrySpendPlayerAP(int amount)
    {
        if (!IsPlayerTurn)
        {
            Debug.Log("[TurnManager] AP spend failed. Not player turn.");
            return false;
        }

        if (isResolvingPhase)
        {
            Debug.Log("[TurnManager] AP spend failed. Phase is resolving.");
            return false;
        }

        if (amount <= 0)
            return true;

        if (currentPlayerAP < amount)
        {
            Debug.Log($"[TurnManager] Not enough AP. Need {amount}, Current {currentPlayerAP}");
            return false;
        }

        currentPlayerAP -= amount;
        RaisePlayerAPChanged();

        Debug.Log($"[TurnManager] Spend AP = {amount} | Left = {currentPlayerAP}/{maxPlayerAP}");
        return true;
    }

    public bool HasEnoughPlayerAP(int amount)
    {
        if (currentPlayerAP >= amount)
            return true;
        return false;
    }

    /// <summary>
    /// 필요하면 디버그용으로 즉시 AP 리필.
    /// </summary>
    public void RefillPlayerAP()
    {
        currentPlayerAP = maxPlayerAP;
        RaisePlayerAPChanged();
    }

    /// <summary>
    /// 전투 종료를 TurnManager 상태에도 반영.
    /// CombatManager에서 승패 확정 시 호출.
    /// </summary>
    public void SetCombatEnded(bool isVictory)
    {
        isResolvingPhase = false;
        SetState(isVictory ? CombatTurnState.Victory : CombatTurnState.Defeat);
    }

    private IEnumerator RunEnemyPhaseRoutine()
    {
        if (combatManager == null)
            yield break;

        isResolvingPhase = true;

        // 플레이어 턴 종료 직후 잠깐 Busy
        SetState(CombatTurnState.Busy);
        yield return new WaitForSeconds(beforeEnemyPhaseDelay);

        // 전투 끝났는지 먼저 체크
        if (combatManager.CheckAndHandleCombatEnd())
        {
            isResolvingPhase = false;
            yield break;
        }

        SetState(CombatTurnState.EnemyTurn);

        List<EnemyAIController> snapshot = combatManager.GetAliveEnemySnapshot();

        for (int i = 0; i < snapshot.Count; i++)
        {
            EnemyAIController enemyAI = snapshot[i];

            if (enemyAI == null || enemyAI.IsDead)
                continue;

            // 적 하나 행동 전에 전투 종료 체크
            if (combatManager.CheckAndHandleCombatEnd())
            {
                isResolvingPhase = false;
                yield break;
            }

            yield return StartCoroutine(enemyAI.ExecuteTurn());

            // 적 하나 행동 후에도 다시 체크
            if (combatManager.CheckAndHandleCombatEnd())
            {
                isResolvingPhase = false;
                yield break;
            }

            yield return new WaitForSeconds(betweenEnemyDelay);
        }

        yield return new WaitForSeconds(afterEnemyPhaseDelay);

        // 적 턴 끝났는데도 전투 안 끝났으면 플레이어 턴으로 복귀
        if (!combatManager.CheckAndHandleCombatEnd())
        {
            StartPlayerTurn();
        }
        else
        {
            isResolvingPhase = false;
        }
    }

    private void SetState(CombatTurnState newState)
    {
        currentState = newState;
        OnTurnStateChanged?.Invoke(currentState);
        Debug.Log($"[TurnManager] State -> {currentState}");
    }

    private void RaisePlayerAPChanged()
    {
        OnPlayerAPChanged?.Invoke(currentPlayerAP, maxPlayerAP);
    }
}