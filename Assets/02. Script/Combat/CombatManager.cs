using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combat 씬 진입 시 RunData를 읽어 전투를 시작하는 매니저.
/// 현재 방향:
/// - BoardManager는 방 생성 / 스폰 좌표 / 보드 규칙 제공
/// - CombatManager는 플레이어/적 생성, 런타임 적용, 씬 시스템 바인딩 담당
/// - Enemy는 스폰 직후 EnemyAIController.BindRuntime()으로 씬 참조를 주입
/// </summary>
public class CombatManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private CombatHUDUI combatHUDUI;
    [SerializeField] private InventoryUIController inventoryUIController;
    [SerializeField] private MoveRangeHighlighter moveRangeHighlighter;

    [Header("Room Templates")]
    [SerializeField] private List<RoomTemplateData> roomTemplates = new List<RoomTemplateData>();
    [SerializeField] private bool useRandomRoom = true;
    [SerializeField] private int fixedRoomIndex = 0;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform enemySpawnRoot;
    [SerializeField] private List<GameObject> possibleEnemyPrefabs = new List<GameObject>();

    [Header("Debug")]
    [SerializeField] private GameObject spawnedPlayer;
    [SerializeField] private List<GameObject> spawnedEnemies = new List<GameObject>();

    // 전투 중 살아있는 적 AI 추적용
    private readonly HashSet<EnemyAIController> aliveEnemyAIs = new HashSet<EnemyAIController>();
    private bool isCombatEnded;

    public bool IsCombatEnded => isCombatEnded;

    private void Start()
    {
        BeginCombat();
    }

    private void BeginCombat()
    {
        if (RunGameManager.Instance == null || !RunGameManager.Instance.HasActiveRun)
        {
            Debug.LogError("[CombatManager] No active run data.");
            return;
        }

        if (boardManager == null)
        {
            Debug.LogError("[CombatManager] BoardManager is missing.");
            return;
        }

        RoomTemplateData selectedRoom = GetSelectedRoomTemplate();
        if (selectedRoom == null)
        {
            Debug.LogError("[CombatManager] Selected room is null.");
            return;
        }

        CombatSpawnResult spawnResult = boardManager.BuildRoom(selectedRoom);
        if (spawnResult == null)
        {
            Debug.LogError("[CombatManager] BuildRoom failed.");
            return;
        }

        isCombatEnded = false;

        // 1. 플레이어 생성
        spawnedPlayer = SpawnPlayer(spawnResult.playerSpawnGrid);
        if (spawnedPlayer == null)
        {
            Debug.LogError("[CombatManager] Player spawn failed.");
            return;
        }

        // 2. RunData -> 플레이어 런타임 적용
        ApplyRunDataToPlayer(spawnedPlayer, RunGameManager.Instance.CurrentRunData);

        // 3. 적 생성 + 바인딩 + 등록
        SpawnEnemies(spawnResult.enemySpawnGrids);

        // 4. 씬 시스템 바인딩
        BindSceneSystems();

        // 5. 플레이어 턴 시작
        if (turnManager != null)
            turnManager.StartPlayerTurn();
    }

    // ---------------------------------------------------------------------
    // Spawn
    // ---------------------------------------------------------------------

    private GameObject SpawnPlayer(Vector2Int playerGrid)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[CombatManager] Player prefab is missing.");
            return null;
        }

        Vector3 worldPos = boardManager.GridToWorld(playerGrid);
        GameObject player = Instantiate(playerPrefab, worldPos, Quaternion.identity);

        GridUnit gridUnit = player.GetComponent<GridUnit>();
        if (gridUnit == null)
        {
            Debug.LogError("[CombatManager] Player prefab has no GridUnit.");
            return player;
        }

        bool registered = boardManager.RegisterSpawnedUnit(gridUnit, playerGrid, OccupantType.Player);
        if (!registered)
            Debug.LogWarning("[CombatManager] Failed to register player on board.");

        return player;
    }

    private void SpawnEnemies(List<Vector2Int> enemySpawnGrids)
    {
        if (possibleEnemyPrefabs == null || possibleEnemyPrefabs.Count == 0)
        {
            Debug.LogWarning("[CombatManager] No enemy prefabs assigned.");
            return;
        }

        Transform playerTransform = spawnedPlayer != null ? spawnedPlayer.transform : null;
        GridUnit playerGridUnit = spawnedPlayer != null ? spawnedPlayer.GetComponent<GridUnit>() : null;
        UnitHealthController playerHealth = spawnedPlayer != null ? spawnedPlayer.GetComponent<UnitHealthController>() : null;

        for (int i = 0; i < enemySpawnGrids.Count; i++)
        {
            Vector2Int gridPos = enemySpawnGrids[i];
            GameObject enemyPrefab = possibleEnemyPrefabs[UnityEngine.Random.Range(0, possibleEnemyPrefabs.Count)];

            Vector3 worldPos = boardManager.GridToWorld(gridPos);
            GameObject enemy = Instantiate(enemyPrefab, worldPos, Quaternion.identity, enemySpawnRoot);

            GridUnit gridUnit = enemy.GetComponent<GridUnit>();
            if (gridUnit != null)
            {
                bool registered = boardManager.RegisterSpawnedUnit(gridUnit, gridPos, OccupantType.Enemy);
                if (!registered)
                    Debug.LogWarning($"[CombatManager] Failed to register enemy at {gridPos}");
            }

            // 여기부터 추가
            EnemyAIController enemyAI = enemy.GetComponent<EnemyAIController>();
            EnemyShooter enemyShooter = enemy.GetComponent<EnemyShooter>();

            if (enemyAI != null)
            {
                enemyAI.BindRuntime(
                    boardManager,
                    this,
                    playerTransform,
                    playerGridUnit,
                    playerHealth
                );

                EnemyGunAimController enemyAim = enemyAI.GetComponentInChildren<EnemyGunAimController>();

                if (enemyAim != null)
                    enemyAim.BindTarget(spawnedPlayer.transform);

                enemyAI.OnEnemyDied += HandleEnemyDied;
                aliveEnemyAIs.Add(enemyAI);
            }
            else
            {
                Debug.LogWarning($"[CombatManager] Enemy has no EnemyAIController. Name = {enemy.name}");
            }

            // EnemyShooter는 인스펙터에 enemyWeaponData / enemyAmmoData 넣어뒀다는 가정
            if (enemyShooter != null)
            {
                enemyShooter.TryReload();
            }

            spawnedEnemies.Add(enemy);
        }
    }

    private void BindEnemyRuntime(
        GameObject enemyObject,
        Transform playerTransform,
        GridUnit playerGridUnit,
        UnitHealthController playerHealth)
    {
        if (enemyObject == null)
            return;

        EnemyAIController enemyAI = enemyObject.GetComponent<EnemyAIController>();
        EnemyShooter enemyShooter = enemyObject.GetComponent<EnemyShooter>();
        UnitHealthController enemyHealth = enemyObject.GetComponent<UnitHealthController>();

        if (enemyAI == null)
        {
            Debug.LogWarning($"[CombatManager] Enemy has no EnemyAIController. Name = {enemyObject.name}");
            return;
        }

        // EnemyShooter는 적 프리팹 인스펙터에
        // enemyWeaponData / enemyAmmoData가 세팅되어 있다고 가정.
        // 따라서 CombatManager는 굳이 여기서 WeaponRuntime을 직접 넣지 않고,
        // TryReload만 호출해서 시작 장전시킨다.
        if (enemyShooter != null)
        {
            enemyShooter.TryReload();
        }
        else
        {
            Debug.LogWarning($"[CombatManager] Enemy has no EnemyShooter. Name = {enemyObject.name}");
        }

        enemyAI.BindRuntime(
            boardManager,
            this,
            playerTransform,
            playerGridUnit,
            playerHealth
        );

        RegisterEnemy(enemyAI, enemyHealth);
    }

    // ---------------------------------------------------------------------
    // Player Runtime Reconstruction
    // ---------------------------------------------------------------------

    private void ApplyRunDataToPlayer(GameObject player, RunData runData)
    {
        if (player == null || runData == null)
            return;

        UnitHealthController health = player.GetComponent<UnitHealthController>();
        if (health != null)
            health.Initialize(runData.maxHp, runData.currentHp);

        PlayerWeaponController weaponController = player.GetComponent<PlayerWeaponController>();
        InventoryRuntime inventoryRuntime = player.GetComponent<InventoryRuntime>();
        AmmoDeckRuntime deckRuntime = player.GetComponentInChildren<AmmoDeckRuntime>(true);

        if (weaponController != null)
            ApplyRunWeaponsToPlayer(weaponController, runData);

        if (deckRuntime != null)
            ApplyRunDeckToRuntime(deckRuntime, weaponController, runData);

        if (inventoryRuntime != null && runData.inventory != null)
            inventoryRuntime.SetInventoryRuntime(runData.inventory.spareAttachments);
    }

    /// <summary>
    /// RunData의 장착 무기 2개를 전투용 무기 런타임으로 변환해서 플레이어에 넣는다.
    /// </summary>
    private void ApplyRunWeaponsToPlayer(PlayerWeaponController weaponController, RunData runData)
    {
        WeaponRuntime slot0Runtime = CreateWeaponRuntimeFromLoadout(runData.equippedWeapons[0]);
        WeaponRuntime slot1Runtime = CreateWeaponRuntimeFromLoadout(runData.equippedWeapons[1]);

        weaponController.SetWeaponRuntime(0, slot0Runtime);
        weaponController.SetWeaponRuntime(1, slot1Runtime);

        int selectedIndex = Mathf.Clamp(runData.currentWeaponSlotIndex, 0, 1);

        if (!weaponController.HasWeaponInSlot(selectedIndex))
        {
            if (weaponController.HasWeaponInSlot(0))
                selectedIndex = 0;
            else if (weaponController.HasWeaponInSlot(1))
                selectedIndex = 1;
        }

        weaponController.SetCurrentWeaponIndex(selectedIndex);
    }

    private void ApplyRunDeckToRuntime(AmmoDeckRuntime deckRuntime, PlayerWeaponController weaponController, RunData runData)
    {
        if (deckRuntime == null || runData == null)
            return;

        // loaded ammo는 RunData에 저장하지 않으므로 전투 시작 시 비움
        if (weaponController != null)
            weaponController.ClearLoadedAmmoAll();

        deckRuntime.SetDeckFromRun(runData.ammoDeck, true);
    }

    private WeaponRuntime CreateWeaponRuntimeFromLoadout(WeaponLoadoutData loadout)
    {
        // WeaponRuntime 객체 자체는 항상 만든다.
        // baseData가 없으면 빈 슬롯 런타임.
        WeaponRuntime runtime = new WeaponRuntime();

        if (loadout == null || loadout.weaponData == null || !loadout.hasWeapon)
            return runtime;

        runtime.SetBaseData(loadout.weaponData);

        if (loadout.equippedAttachments != null)
        {
            for (int i = 0; i < loadout.equippedAttachments.Count; i++)
            {
                WeaponAttachmentData attachment = loadout.equippedAttachments[i];
                if (attachment == null)
                    continue;

                runtime.TryEquipAttachment(attachment);
            }
        }

        return runtime;
    }

    // ---------------------------------------------------------------------
    // Scene System Binding
    // ---------------------------------------------------------------------

    private void BindSceneSystems()
    {
        if (spawnedPlayer == null)
            return;

        GridUnit playerGridUnit = spawnedPlayer.GetComponent<GridUnit>();
        PlayerWeaponController weaponController = spawnedPlayer.GetComponent<PlayerWeaponController>();
        InventoryRuntime inventoryRuntime = spawnedPlayer.GetComponent<InventoryRuntime>();
        AmmoDeckRuntime deckRuntime = spawnedPlayer.GetComponentInChildren<AmmoDeckRuntime>(true);

        // InputManager
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.BindInventory(inventoryUIController);
            PlayerInputManager.Instance.BindTurnManager(turnManager);
            PlayerInputManager.Instance.BindPreviewSystems(moveRangeHighlighter);
            PlayerInputManager.Instance.BindPlayer(spawnedPlayer);
            PlayerInputManager.Instance.SetSceneMode(PlayerInputManager.InputSceneMode.Combat);
        }

        // HUD
        if (combatHUDUI != null && playerGridUnit != null)
            combatHUDUI.BindPlayer(playerGridUnit);

        // Inventory
        if (inventoryUIController != null)
        {
            inventoryUIController.BindCombatContext(
                weaponController,
                inventoryRuntime,
                deckRuntime
            );
        }

        // Move Preview
        if (moveRangeHighlighter != null && playerGridUnit != null)
            moveRangeHighlighter.BindPlayer(playerGridUnit);
    }

    // ---------------------------------------------------------------------
    // Enemy Registration / Notifications
    // ---------------------------------------------------------------------

    private void RegisterEnemy(EnemyAIController enemyAI, UnitHealthController enemyHealth)
    {
        if (enemyAI == null)
            return;

        aliveEnemyAIs.Add(enemyAI);
        enemyAI.OnEnemyDied += HandleEnemyDied;
    }

    private void HandleEnemyDied(EnemyAIController enemyAI)
    {
        if (enemyAI == null)
            return;

        if (aliveEnemyAIs.Contains(enemyAI))
            aliveEnemyAIs.Remove(enemyAI);

        enemyAI.OnEnemyDied -= HandleEnemyDied;

        Debug.Log($"[CombatManager] Enemy removed. Remaining = {aliveEnemyAIs.Count}");

        if (!isCombatEnded && aliveEnemyAIs.Count == 0)
        {
            HandleVictory();
        }
    }

    /// <summary>
    /// PlayerShooter에서 플레이어가 발사했을 때 호출해라.
    /// 인지 범위 안 총성 -> 적대 규칙용.
    /// </summary>
    public void NotifyPlayerGunshot(Vector3 shotWorldPosition)
    {
        foreach (EnemyAIController enemyAI in aliveEnemyAIs)
        {
            if (enemyAI == null || enemyAI.IsDead)
                continue;

            enemyAI.NotifyPlayerGunshot(shotWorldPosition);
        }
    }

    /// <summary>
    /// 플레이어 탄에 특정 적이 맞았을 때 호출.
    /// 어디에 있든 피격 -> 적대 규칙용.
    /// </summary>
    public void NotifyEnemyDamagedByPlayer(EnemyAIController enemyAI)
    {
        if (enemyAI == null || enemyAI.IsDead)
            return;

        enemyAI.NotifyDamagedByPlayer();
    }

    /// <summary>
    /// 나중에 Enemy Turn Runner나 TurnManager 쪽에서 쓰기 쉽게 스냅샷 반환.
    /// </summary>
    public List<EnemyAIController> GetAliveEnemySnapshot()
    {
        List<EnemyAIController> snapshot = new List<EnemyAIController>();

        foreach (EnemyAIController enemyAI in aliveEnemyAIs)
        {
            if (enemyAI == null || enemyAI.IsDead)
                continue;

            snapshot.Add(enemyAI);
        }

        return snapshot;
    }

    // ---------------------------------------------------------------------
    // Room Selection
    // ---------------------------------------------------------------------

    private RoomTemplateData GetSelectedRoomTemplate()
    {
        if (roomTemplates == null || roomTemplates.Count == 0)
        {
            Debug.LogError("[CombatManager] No RoomTemplateData assigned.");
            return null;
        }

        if (useRandomRoom)
        {
            int randomIndex = Random.Range(0, roomTemplates.Count);
            return roomTemplates[randomIndex];
        }

        fixedRoomIndex = Mathf.Clamp(fixedRoomIndex, 0, roomTemplates.Count - 1);
        return roomTemplates[fixedRoomIndex];
    }
    public bool CheckAndHandleCombatEnd()
    {
        if (isCombatEnded)
            return true;

        bool playerDead = IsPlayerDead();
        bool noEnemiesLeft = !HasAliveEnemies();

        if (playerDead)
        {
            HandleDefeat();
            return true;
        }

        if (noEnemiesLeft)
        {
            HandleVictory();
            return true;
        }

        return false;
    }

    public bool HasAliveEnemies()
    {
        foreach (EnemyAIController enemyAI in aliveEnemyAIs)
        {
            if (enemyAI != null && !enemyAI.IsDead)
                return true;
        }

        return false;
    }

    public bool IsPlayerDead()
    {
        if (spawnedPlayer == null)
            return true;

        UnitHealthController playerHealth = spawnedPlayer.GetComponent<UnitHealthController>();
        if (playerHealth == null)
            return true;

        return playerHealth.CurrentHP <= 0;
    }

    private void HandleVictory()
    {
        if (isCombatEnded)
            return;

        isCombatEnded = true;

        if (turnManager != null)
            turnManager.SetCombatEnded(true);

        RunGameManager runManager = RunGameManager.Instance;

        if (runManager == null || !runManager.HasActiveRun)
        {
            Debug.LogError("[CombatManager] No active run during victory handling.");
            return;
        }

        RunData runData = runManager.CurrentRunData;

        if (runData == null)
        {
            Debug.LogError("[CombatManager] RunData is null during victory handling.");
            return;
        }

        // 플레이어 HP 같은 전투 결과를 RunData에 반영
        ApplyCombatResultToRunData(runData);

        if (IsCurrentNodeBoss(runData))
        {
            HandleFinalVictory(runManager);
            return;
        }

        HandleNormalCombatVictory(runManager, runData);
    }

    private bool IsCurrentNodeBoss(RunData runData)
    {
        if (runData == null || runData.mapData == null)
            return false;

        string currentNodeId = runData.mapData.currentNodeId;

        if (string.IsNullOrEmpty(currentNodeId))
            return false;

        if (runData.mapData.allNodes == null)
            return false;

        for (int i = 0; i < runData.mapData.allNodes.Count; i++)
        {
            MapNodeData node = runData.mapData.allNodes[i];

            if (node == null)
                continue;

            if (node.nodeId != currentNodeId)
                continue;

            return node.roomType == RoomType.Boss;
        }

        return false;
    }

    private void HandleNormalCombatVictory(RunGameManager runManager, RunData runData)
    {
        string clearedNodeId = "";

        if (runData.mapData != null)
            clearedNodeId = runData.mapData.currentNodeId;

        PendingCombatResult result = new PendingCombatResult(
            wasVictory: true,
            shouldShowReward: true,
            clearedNodeId: clearedNodeId
        );

        runManager.SetPendingCombatResult(result);

        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("[CombatManager] GameSceneManager is missing.");
            return;
        }

        GameSceneManager.Instance.LoadSceneAsyncByName("InGameSc");
    }

    private void HandleFinalVictory(RunGameManager runManager)
    {
        if (runManager != null && runManager.HasActiveRun)
            runManager.EndRun();

        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("[CombatManager] GameSceneManager is missing.");
            return;
        }

        GameSceneManager.Instance.LoadSceneAsyncByName("VictorySc");
    }

    private void HandleDefeat()
    {
        if (isCombatEnded)
            return;

        isCombatEnded = true;
        Debug.Log("[CombatManager] Defeat!");

        if (turnManager != null)
            turnManager.SetCombatEnded(false);

        if (RunGameManager.Instance != null && RunGameManager.Instance.HasActiveRun)
            RunGameManager.Instance.EndRun();

        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("[CombatManager] GameSceneManager is missing.");
            return;
        }

        GameSceneManager.Instance.LoadSceneAsyncByName("DefeatSc");
    }

    private void ApplyCombatResultToRunData(RunData runData)
    {
        if (runData == null)
            return;

        if (spawnedPlayer == null)
        {
            Debug.LogWarning("[CombatManager] ApplyCombatResultToRunData failed: spawnedPlayer is null.");
            return;
        }

        UnitHealthController health = spawnedPlayer.GetComponent<UnitHealthController>();
        PlayerWeaponController weaponController = spawnedPlayer.GetComponent<PlayerWeaponController>();

        if (health != null)
        {
            runData.currentHp = health.CurrentHP;
            runData.maxHp = health.MaxHP;
        }

        if (weaponController != null)
        {
            runData.currentWeaponSlotIndex = weaponController.CurrentWeaponIndex;
        }

        Debug.Log(
            $"[CombatManager] ApplyCombatResultToRunData -> " +
            $"HP {runData.currentHp}/{runData.maxHp}, " +
            $"WeaponSlot {runData.currentWeaponSlotIndex}"
        );
    }
}