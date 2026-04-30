using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridUnit))]
[RequireComponent(typeof(UnitStatusController))]
[RequireComponent(typeof(UnitHealthController))]
[RequireComponent(typeof(EnemyShooter))]
public class EnemyAIController : MonoBehaviour
{
    [Serializable]
    public class EnemyPersonality
    {
        [Header("Temper")]
        [Range(0f, 1f)] public float aggression = 0.55f;
        [Range(0f, 1f)] public float caution = 0.65f;

        [Tooltip("상위 후보가 아니라 2~3등 후보를 고를 확률 성향")]
        [Range(0f, 1f)] public float impulse = 0.15f;

        [Header("Finish Off")]
        [Tooltip("플레이어 HP 비율이 이 값 이하이면 엄폐보다 마무리 압박을 더 강하게 본다")]
        [Range(0f, 1f)] public float finishOffThreshold = 0.35f;

        [Tooltip("플레이어 HP 비율이 이 값 이하이면 거의 킬 압박을 최우선으로 본다")]
        [Range(0f, 1f)] public float criticalFinishOffThreshold = 0.20f;
    }

    private struct ScoredEnemyAction
    {
        public EnemyActionType actionType;
        public float score;
        public Vector2Int targetGrid;

        public ScoredEnemyAction(EnemyActionType actionType, float score, Vector2Int targetGrid)
        {
            this.actionType = actionType;
            this.score = score;
            this.targetGrid = targetGrid;
        }
    }

    [Header("Scene Refs (Bind at runtime)")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private CombatManager combatManager;

    [Header("Player Refs (Bind at runtime)")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GridUnit playerGridUnit;
    [SerializeField] private UnitHealthController playerHealth;

    [Header("Detection")]
    [SerializeField] private float awarenessRangeTiles = 6f;
    [SerializeField] private int turnsToReturnIdle = 2;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float obstacleProbeRadius = 0.15f;

    [Header("Turn / Tempo")]
    [SerializeField] private int maxAP = 3;
    [SerializeField] private float actionDelay = 0.15f;
    [SerializeField] private int safetyLoopCount = 12;

    [Header("Behavior")]
    [SerializeField] private EnemyPersonality personality = new EnemyPersonality();

    [Header("Patrol")]
    [SerializeField] private List<Vector2Int> patrolRoute = new List<Vector2Int>();

    [Header("Debug")]
    [SerializeField] private EnemyState currentState = EnemyState.Idle;
    [SerializeField] private int currentAP;
    [SerializeField] private int patrolIndex;
    [SerializeField] private int turnsSinceLostPlayer;
    [SerializeField] private Vector2Int lastKnownPlayerGrid;
    [SerializeField] private bool heardPlayerGunshotThisCycle;
    [SerializeField] private bool wasDamagedByPlayerThisCycle;
    [SerializeField] private bool didShootLastAction;
    [SerializeField] private int shotsFiredThisTurn;

    private GridUnit gridUnit;
    private UnitStatusController statusController;
    private UnitHealthController healthController;
    private EnemyShooter enemyShooter;

    private bool countedLostPlayerThisTurn;
    private bool isBound;

    /// <summary>
    /// CombatManager가 이 이벤트를 구독해서
    /// 적 HashSet 제거 + 마지막 적 사망 시 즉시 승리 판정을 처리한다.
    /// </summary>
    public event Action<EnemyAIController> OnEnemyDied;

    public EnemyState CurrentState => currentState;
    public bool IsDead => currentState == EnemyState.Dead;
    public int CurrentAP => currentAP;
    public bool IsBound => isBound;

    private Vector2Int MyGridPos
    {
        get
        {
            // 네 GridUnit 프로퍼티명이 다르면 여기만 수정
            return gridUnit.CurrentGridPos;
        }
    }

    private void Awake()
    {
        // 프리팹 내부 자기 컴포넌트만 Awake에서 캐싱
        gridUnit = GetComponent<GridUnit>();
        statusController = GetComponent<UnitStatusController>();
        healthController = GetComponent<UnitHealthController>();
        enemyShooter = GetComponent<EnemyShooter>();

        // 핵심:
        // UnitHealthController가 죽음을 확정했을 때,
        // EnemyAIController가 그 신호를 받아 CombatManager용 적 사망 이벤트로 변환한다.
        if (healthController != null)
            healthController.OnDied += HandleHealthDied;
    }

    private void Start()
    {
        if (!isBound)
        {
            Debug.LogWarning($"[{name}] EnemyAIController is not bound. CombatManager에서 BindRuntime 호출 확인해라.");
        }
    }

    private void OnDestroy()
    {
        // 구독 해제
        if (healthController != null)
            healthController.OnDied -= HandleHealthDied;
    }

    /// <summary>
    /// CombatManager가 적 생성 직후 호출하는 바인딩 함수
    /// </summary>
    public void BindRuntime(
        BoardManager boardManager,
        CombatManager combatManager,
        Transform playerTransform,
        GridUnit playerGridUnit,
        UnitHealthController playerHealth)
    {
        this.boardManager = boardManager;
        this.combatManager = combatManager;
        this.playerTransform = playerTransform;
        this.playerGridUnit = playerGridUnit;
        this.playerHealth = playerHealth;

        if (playerGridUnit != null)
            lastKnownPlayerGrid = playerGridUnit.CurrentGridPos;

        isBound = true;
    }

    /// <summary>
    /// 플레이어가 총을 쐈을 때 외부에서 호출.
    /// "인지 범위 안에서 플레이어가 사격 -> 적대(벽 있어도)" 규칙용.
    /// </summary>
    public void NotifyPlayerGunshot(Vector3 shotWorldPosition)
    {
        if (IsDead)
            return;

        float dist = Vector2.Distance(transform.position, shotWorldPosition);
        if (dist <= awarenessRangeTiles)
        {
            heardPlayerGunshotThisCycle = true;
        }
    }

    /// <summary>
    /// 플레이어 탄에 적중했을 때 외부에서 호출.
    /// "서로 어디 있든 피격 -> 적대" 규칙용.
    /// </summary>
    public void NotifyDamagedByPlayer()
    {
        if (IsDead)
            return;

        wasDamagedByPlayerThisCycle = true;
    }

    /// <summary>
    /// UnitHealthController 쪽에서 사망이 확정됐을 때 받는 콜백.
    /// 여기서 CombatManager가 듣는 적 사망 이벤트를 발사한다.
    /// </summary>
    private void HandleHealthDied(UnitHealthController diedHealth)
    {
        if (diedHealth != healthController)
            return;

        NotifyDied();
    }

    /// <summary>
    /// 적 사망 처리.
    /// 중요:
    /// 실제 Destroy(gameObject)는 UnitHealthController.Die()에서 이미 수행하므로
    /// 여기서는 중복 파괴하지 않는다.
    /// </summary>
    public void NotifyDied()
    {
        if (currentState == EnemyState.Dead)
            return;

        currentState = EnemyState.Dead;

        // CombatManager가 이 이벤트를 받아 aliveEnemyAIs에서 제거하고,
        // 남은 적이 0이면 즉시 승리 처리하게 한다.
        OnEnemyDied?.Invoke(this);
    }

    /// <summary>
    /// CombatManager / EnemyTurnRunner에서 enemy phase에 호출
    /// </summary>
    public IEnumerator ExecuteTurn()
    {
        if (IsDead || !isBound)
            yield break;

        currentAP = maxAP;
        shotsFiredThisTurn = 0;
        didShootLastAction = false;
        countedLostPlayerThisTurn = false;

        int safety = safetyLoopCount;

        while (currentAP > 0 && safety > 0)
        {
            safety--;

            if (IsDead)
                yield break;

            if (statusController != null && !statusController.CanAct)
                break;

            UpdatePerceptionAndState();

            if (IsDead)
                yield break;

            bool acted = TryExecuteBestAction();
            if (!acted)
                break;

            yield return new WaitForSeconds(actionDelay);
        }

        // 적 턴 끝나면 일회성 자극 플래그 초기화
        heardPlayerGunshotThisCycle = false;
        wasDamagedByPlayerThisCycle = false;
        didShootLastAction = false;
    }

    private void UpdatePerceptionAndState()
    {
        if (IsDead)
            return;

        if (playerTransform == null || playerGridUnit == null)
            return;

        bool inAwareness = IsPlayerInAwarenessRange();
        bool hasVisualDetection = inAwareness && HasLineOfSightFromWorld(transform.position, playerTransform.position);

        // 총성 / 피격은 벽 무시 적대화
        bool forcedAggro = heardPlayerGunshotThisCycle || wasDamagedByPlayerThisCycle;

        if (forcedAggro || hasVisualDetection)
        {
            turnsSinceLostPlayer = 0;
            countedLostPlayerThisTurn = false;
            lastKnownPlayerGrid = playerGridUnit.CurrentGridPos;

            bool canThreaten = enemyShooter != null
                               && enemyShooter.HasUsableWeapon
                               && enemyShooter.CanThreatenTargetPosition(playerTransform);

            currentState = canThreaten ? EnemyState.CanShoot : EnemyState.Hostile;
            return;
        }

        // 플레이어 상실 처리
        if (currentState == EnemyState.Hostile || currentState == EnemyState.CanShoot)
        {
            if (!countedLostPlayerThisTurn)
            {
                turnsSinceLostPlayer++;
                countedLostPlayerThisTurn = true;
            }

            if (turnsSinceLostPlayer >= turnsToReturnIdle)
            {
                currentState = EnemyState.Idle;
            }
            else
            {
                currentState = EnemyState.Hostile;
            }
        }
        else if (currentState != EnemyState.Dead)
        {
            currentState = EnemyState.Idle;
        }

        Debug.Log($"[{name}] State={currentState} | AP={currentAP}");
    }

    private bool TryExecuteBestAction()
    {
        List<ScoredEnemyAction> actions = BuildActionCandidates();
        if (actions.Count == 0)
            return false;

        ScoredEnemyAction chosen = ChooseAction(actions);
        return ExecuteAction(chosen);
    }

    private List<ScoredEnemyAction> BuildActionCandidates()
    {
        List<ScoredEnemyAction> actions = new List<ScoredEnemyAction>();

        if (currentState == EnemyState.Dead)
            return actions;

        switch (currentState)
        {
            case EnemyState.Idle:
                {
                    ScoredEnemyAction patrol = FindBestPatrolAction();
                    if (patrol.actionType != EnemyActionType.None)
                        actions.Add(patrol);
                    break;
                }

            case EnemyState.Hostile:
                {
                    if (CanReloadNow())
                    {
                        float reloadScore = 16f;
                        if (didShootLastAction)
                            reloadScore -= 4f;

                        actions.Add(new ScoredEnemyAction(EnemyActionType.Reload, reloadScore, MyGridPos));
                    }

                    ScoredEnemyAction hostileMove = FindBestHostileMoveAction();
                    if (hostileMove.actionType != EnemyActionType.None)
                        actions.Add(hostileMove);

                    break;
                }

            case EnemyState.CanShoot:
                {
                    // 플레이어를 위협할 수 있는 위치인데 탄이 있으면 사격 후보를 넣는다.
                    if (CanShootNow())
                    {
                        float shootScore = EvaluateShootScore();
                        actions.Add(new ScoredEnemyAction(EnemyActionType.Shoot, shootScore, MyGridPos));
                    }
                    // 플레이어를 위협할 수 있는 위치인데 탄이 없으면,
                    // 사격 후보 대신 장전 후보를 넣는다.
                    else if (CanReloadAsAttackPreparation())
                    {
                        float reloadScore = EvaluateReloadAsAttackPreparationScore();
                        actions.Add(new ScoredEnemyAction(EnemyActionType.Reload, reloadScore, MyGridPos));
                    }

                    ScoredEnemyAction coverMove = FindBestPostShotCoverMoveAction();
                    if (coverMove.actionType != EnemyActionType.None)
                        actions.Add(coverMove);

                    ScoredEnemyAction hostileMove = FindBestHostileMoveAction();
                    if (hostileMove.actionType != EnemyActionType.None)
                        actions.Add(hostileMove);

                    break;
                }
        }

        return actions;
    }

    private bool ExecuteAction(ScoredEnemyAction action)
    {
        switch (action.actionType)
        {
            case EnemyActionType.PatrolMove:
            case EnemyActionType.HostileMove:
            case EnemyActionType.CoverMove:
                {
                    if (currentAP < 1)
                        return false;

                    if (statusController != null && !statusController.CanMove)
                        return false;

                    if (boardManager == null)
                        return false;

                    // 네 BoardManager 메서드명이 다르면 여기만 수정
                    bool moved = boardManager.MoveUnit(gridUnit, action.targetGrid);
                    if (!moved)
                        return false;

                    currentAP -= 1;
                    if (SoundManager.Instance != null)
                        SoundManager.Instance.PlayUnitMove();
                    didShootLastAction = false;
                    return true;
                }

            case EnemyActionType.Reload:
                {
                    if (currentAP < 1)
                        return false;

                    if (enemyShooter == null)
                        return false;

                    bool reloaded = enemyShooter.TryReload();
                    if (!reloaded)
                        return false;

                    currentAP -= 1;
                    didShootLastAction = false;
                    return true;
                }

            case EnemyActionType.Shoot:
                {
                    if (!CanShootNow())
                        return false;

                    if (enemyShooter == null)
                        return false;

                    bool fired = enemyShooter.TryShootTarget(playerTransform);
                    if (!fired)
                        return false;

                    currentAP -= enemyShooter.CurrentShootApCost;
                    didShootLastAction = true;
                    shotsFiredThisTurn++;
                    return true;
                }
        }

        return false;
    }

    private bool CanShootNow()
    {
        if (enemyShooter == null || playerTransform == null)
            return false;

        if (statusController != null && !statusController.CanShoot)
            return false;

        if (currentAP < enemyShooter.CurrentShootApCost)
            return false;

        return enemyShooter.CanShootTarget(playerTransform);
    }

    private bool CanReloadNow()
    {
        if (enemyShooter == null)
            return false;

        if (currentAP < 1)
            return false;

        return enemyShooter.NeedsReload;
    }

    private float EvaluateShootScore()
    {
        float playerHpRatio = GetPlayerHpRatio();
        float finishPressure = 1f - playerHpRatio;
        bool isFinishOffMode = playerHpRatio <= personality.finishOffThreshold;
        bool isCriticalFinishOff = playerHpRatio <= personality.criticalFinishOffThreshold;

        float score = 40f;

        // 공격성 반영
        score += personality.aggression * 18f;

        // 플레이어가 약할수록 바로 쏘고 싶어짐
        score += finishPressure * 32f;

        // 방금 쐈으면 연사보다 엄폐/재각을 조금 더 보게 함
        if (didShootLastAction)
            score -= 14f;

        // 근데 마무리 각이면 연사 패널티를 무시하고 다시 올림
        if (isFinishOffMode)
            score += 18f;

        if (isCriticalFinishOff)
            score += 25f;

        // 같은 턴에 이미 여러 번 쐈으면 살짝 피로도
        if (shotsFiredThisTurn >= 1)
            score -= shotsFiredThisTurn * 4f;

        return score;
    }

    private ScoredEnemyAction FindBestHostileMoveAction()
    {
        if (boardManager == null)
            return default;

        if (currentAP < 1)
            return default;

        if (statusController != null && !statusController.CanMove)
            return default;

        List<Vector2Int> candidates = GetAdjacentCardinalPositions(MyGridPos);

        float bestScore = float.MinValue;
        Vector2Int bestGrid = MyGridPos;
        bool found = false;

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2Int candidate = candidates[i];

            if (!boardManager.CanEnterTile(candidate))
                continue;

            float score = EvaluateHostileTileScore(candidate);

            if (!found || score > bestScore)
            {
                found = true;
                bestScore = score;
                bestGrid = candidate;
            }
        }

        if (!found)
            return default;

        return new ScoredEnemyAction(EnemyActionType.HostileMove, bestScore, bestGrid);
    }

    private ScoredEnemyAction FindBestPostShotCoverMoveAction()
    {
        if (boardManager == null)
            return default;

        if (currentAP < 1)
            return default;

        if (statusController != null && !statusController.CanMove)
            return default;

        List<Vector2Int> candidates = GetAdjacentCardinalPositions(MyGridPos);

        float bestScore = float.MinValue;
        Vector2Int bestGrid = MyGridPos;
        bool found = false;

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2Int candidate = candidates[i];

            if (!boardManager.CanEnterTile(candidate))
                continue;

            float score = EvaluatePostShotCoverTileScore(candidate);

            if (!found || score > bestScore)
            {
                found = true;
                bestScore = score;
                bestGrid = candidate;
            }
        }

        if (!found)
            return default;

        return new ScoredEnemyAction(EnemyActionType.CoverMove, bestScore, bestGrid);
    }

    private ScoredEnemyAction FindBestPatrolAction()
    {
        if (patrolRoute == null || patrolRoute.Count == 0)
            return default;

        if (boardManager == null)
            return default;

        if (currentAP < 1)
            return default;

        if (statusController != null && !statusController.CanMove)
            return default;

        Vector2Int current = MyGridPos;
        Vector2Int patrolTarget = patrolRoute[patrolIndex];

        if (current == patrolTarget)
        {
            patrolIndex = (patrolIndex + 1) % patrolRoute.Count;
            patrolTarget = patrolRoute[patrolIndex];
        }

        List<Vector2Int> candidates = GetAdjacentCardinalPositions(current);

        float bestScore = float.MinValue;
        Vector2Int bestGrid = current;
        bool found = false;

        int currentManhattan = GetManhattanDistance(current, patrolTarget);

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2Int candidate = candidates[i];

            if (!boardManager.CanEnterTile(candidate))
                continue;

            int nextManhattan = GetManhattanDistance(candidate, patrolTarget);

            float score = 10f;
            score += (currentManhattan - nextManhattan) * 5f;

            // 순찰 중에도 벽 옆을 살짝 선호하면 덜 멍청해 보임
            score += EvaluateCoverScore(candidate) * 0.25f;

            if (!found || score > bestScore)
            {
                found = true;
                bestScore = score;
                bestGrid = candidate;
            }
        }

        if (!found)
            return default;

        return new ScoredEnemyAction(EnemyActionType.PatrolMove, bestScore, bestGrid);
    }

    private float EvaluateHostileTileScore(Vector2Int candidate)
    {
        Vector2Int current = MyGridPos;
        Vector2Int target = lastKnownPlayerGrid;

        float distBefore = Vector2.Distance((Vector2)current, (Vector2)target);
        float distAfter = Vector2.Distance((Vector2)candidate, (Vector2)target);

        Vector3 candidateWorld = boardManager.GridToWorld(candidate);
        Vector3 playerWorld = playerTransform != null ? playerTransform.position : candidateWorld;

        bool hasLos = HasLineOfSightFromWorld(candidateWorld, playerWorld);
        bool inShootRange = enemyShooter != null && enemyShooter.IsTargetInShootRangeFromWorld(candidateWorld, playerWorld);

        float playerHpRatio = GetPlayerHpRatio();
        float finishPressure = 1f - playerHpRatio;
        bool isFinishOffMode = playerHpRatio <= personality.finishOffThreshold;
        bool isCriticalFinishOff = playerHpRatio <= personality.criticalFinishOffThreshold;

        float score = 0f;

        // 플레이어 쪽으로 다가가는 가치
        score += (distBefore - distAfter) * 8f;

        // LOS 열리면 가점
        if (hasLos)
            score += 18f;

        // 사거리 안으로 들어오면 가점
        if (inShootRange)
            score += 24f;

        // 엄폐 점수
        float coverScore = EvaluateCoverScore(candidate) * personality.caution * GetDynamicCoverWeightMultiplier();
        score += coverScore;

        // 플레이어 피가 낮으면 마무리 압박 증가
        score += finishPressure * personality.aggression * 24f;

        if (isFinishOffMode)
            score += 12f;

        if (isCriticalFinishOff)
            score += 20f;

        // LOS도 있고 엄폐도 없으면 노출 패널티
        if (hasLos && EvaluateCoverScore(candidate) <= 0.01f)
            score -= 10f;

        // 방금 쐈다면 무작정 앞으로만 가지 않게 약간 억제
        if (didShootLastAction)
            score -= 6f;

        return score;
    }

    private float EvaluatePostShotCoverTileScore(Vector2Int candidate)
    {
        Vector3 candidateWorld = boardManager.GridToWorld(candidate);
        Vector3 playerWorld = playerTransform != null ? playerTransform.position : candidateWorld;

        bool hasLos = HasLineOfSightFromWorld(candidateWorld, playerWorld);
        bool inShootRange = enemyShooter != null && enemyShooter.IsTargetInShootRangeFromWorld(candidateWorld, playerWorld);

        float playerHpRatio = GetPlayerHpRatio();
        bool isFinishOffMode = playerHpRatio <= personality.finishOffThreshold;
        bool isCriticalFinishOff = playerHpRatio <= personality.criticalFinishOffThreshold;

        float score = 0f;

        // 방금 쐈다면 다음 행동으로 엄폐를 꽤 선호
        if (didShootLastAction)
            score += 18f;

        // 엄폐 그 자체
        score += EvaluateCoverScore(candidate) * personality.caution * 1.2f * GetDynamicCoverWeightMultiplier();

        // 아예 너무 멀어져서 다음 턴 사격 각도 다 버리는 건 감점
        if (!inShootRange)
            score -= 8f;

        // LOS 완전 상실도 약간 감점
        if (!hasLos)
            score -= 3f;

        // 플레이어 빈사면 엄폐보다 마무리 우선
        if (isFinishOffMode)
            score -= 12f;

        if (isCriticalFinishOff)
            score -= 20f;

        return score;
    }

    private float EvaluateCoverScore(Vector2Int candidate)
    {
        if (playerGridUnit == null || boardManager == null)
            return 0f;

        Vector2 toPlayer = (Vector2)(playerGridUnit.CurrentGridPos - candidate);
        if (toPlayer.sqrMagnitude <= 0.001f)
            return 0f;

        toPlayer.Normalize();

        float score = 0f;

        // 후보 칸 기준 상하좌우 인접 장애물 검사
        Vector2Int[] dirs =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        for (int i = 0; i < dirs.Length; i++)
        {
            Vector2Int neighbor = candidate + dirs[i];
            if (!IsObstacleAtGrid(neighbor))
                continue;

            Vector2 neighborDir = dirs[i];
            neighborDir.Normalize();

            // 플레이어 방향 쪽에 장애물이 있으면 더 좋은 엄폐
            float dot = Vector2.Dot(neighborDir, toPlayer);

            if (dot > 0.4f)
                score += 14f; // 플레이어 방향 엄폐
            else
                score += 4f;  // 그냥 옆 장애물
        }

        return score;
    }

    private float GetDynamicCoverWeightMultiplier()
    {
        float playerHpRatio = GetPlayerHpRatio();

        if (playerHpRatio <= personality.criticalFinishOffThreshold)
            return 0.25f;

        if (playerHpRatio <= personality.finishOffThreshold)
            return 0.50f;

        return 1f;
    }

    private float GetPlayerHpRatio()
    {
        if (playerHealth == null)
            return 1f;

        if (playerHealth.MaxHP <= 0)
            return 1f;

        return Mathf.Clamp01((float)playerHealth.CurrentHP / playerHealth.MaxHP);
    }

    private bool IsPlayerInAwarenessRange()
    {
        if (playerGridUnit == null)
            return false;

        // 인지 거리 계산은 prototype-safe하게 직선 거리 기준
        float dist = Vector2.Distance((Vector2)MyGridPos, (Vector2)playerGridUnit.CurrentGridPos);
        return dist <= awarenessRangeTiles;
    }

    private bool HasLineOfSightFromWorld(Vector3 fromWorld, Vector3 toWorld)
    {
        RaycastHit2D hit = Physics2D.Linecast(fromWorld, toWorld, obstacleMask);
        return hit.collider == null;
    }

    private bool IsObstacleAtGrid(Vector2Int gridPos)
    {
        if (boardManager == null)
            return false;

        Vector3 world = boardManager.GridToWorld(gridPos);
        Collider2D hit = Physics2D.OverlapCircle(world, obstacleProbeRadius, obstacleMask);
        return hit != null;
    }

    private List<Vector2Int> GetAdjacentCardinalPositions(Vector2Int origin)
    {
        return new List<Vector2Int>(4)
        {
            origin + Vector2Int.up,
            origin + Vector2Int.down,
            origin + Vector2Int.left,
            origin + Vector2Int.right
        };
    }

    private int GetManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private ScoredEnemyAction ChooseAction(List<ScoredEnemyAction> actions)
    {
        actions.Sort((a, b) => b.score.CompareTo(a.score));

        if (actions.Count == 1)
            return actions[0];

        // 1등과 2등 차이가 크면 그냥 1등
        float decisiveGap = 22f;
        if (actions[0].score - actions[1].score >= decisiveGap)
            return actions[0];

        float roll = UnityEngine.Random.value;

        // impulse가 높을수록 2~3등 행동을 섞음
        if (actions.Count >= 3 && roll < personality.impulse * 0.20f)
            return actions[2];

        if (actions.Count >= 2 && roll < personality.impulse * 0.70f)
            return actions[1];

        return actions[0];
    }

    private bool CanConsiderShootAction()
    {
        if (enemyShooter == null || playerTransform == null)
            return false;

        if (statusController != null && !statusController.CanShoot)
            return false;

        // 탄이 있으면 실제 사격 가능 여부
        if (!enemyShooter.NeedsReload)
            return CanShootNow();

        // 탄이 없으면:
        // "장전 가능"만으로는 부족하다.
        // 플레이어가 지금 사격 가능한 위치에 있어야 한다.
        if (!CanReloadNow())
            return false;

        return enemyShooter.CanThreatenTargetPosition(playerTransform);
    }

    private float EvaluateReloadAsShootScore()
    {
        float score = 0f;

        // 플레이어가 지금 사격 가능한 위치에 있으니까,
        // 장전은 의미 있는 행동이다.
        score += 28f;

        // AP가 장전 후에도 남으면 다음 행동으로 쏠 가능성이 있으니 가산.
        if (currentAP >= 2)
            score += 10f;

        // 이미 이번 턴에 쐈으면 장전 우선도를 조금 낮춘다.
        if (didShootLastAction)
            score -= 6f;

        // 너무 압도적으로 높게 주지 않는다.
        // 그래야 엄폐 이동이 정말 더 좋으면 이동을 선택할 수 있다.
        return score;
    }

    private bool CanReloadAsAttackPreparation()
    {
        if (enemyShooter == null || playerTransform == null)
            return false;

        if (statusController != null && !statusController.CanShoot)
            return false;

        if (!CanReloadNow())
            return false;

        // 핵심:
        // 그냥 장전 가능한 게 아니라,
        // 현재 위치에서 플레이어를 위협할 수 있는 상황일 때만
        // "공격 준비용 장전" 후보를 넣는다.
        return enemyShooter.CanThreatenTargetPosition(playerTransform);
    }

    private float EvaluateReloadAsAttackPreparationScore()
    {
        float score = 42f;

        // 공격성이 높으면 탄 비었을 때 장전하고 다시 압박하려는 성향 증가
        score += personality.aggression * 10f;

        // AP가 2 이상이면 장전 후에도 이번 턴에 뭔가 더 할 수 있으므로 가치 증가
        if (currentAP >= 2)
            score += 6f;

        // 방금 쐈다면 바로 장전하는 건 조금 낮춤
        if (didShootLastAction)
            score -= 6f;

        // 플레이어 체력이 낮으면 마무리 준비 가치 증가
        float playerHpRatio = GetPlayerHpRatio();
        if (playerHpRatio <= personality.finishOffThreshold)
            score += 8f;

        if (playerHpRatio <= personality.criticalFinishOffThreshold)
            score += 12f;

        return score;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, awarenessRangeTiles);
    }
#endif
}