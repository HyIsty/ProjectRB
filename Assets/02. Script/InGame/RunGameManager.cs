using System.Collections.Generic;
using UnityEngine;

public class RunGameManager : MonoBehaviour
{
    public static RunGameManager Instance { get; private set; }

    [Header("Runtime Debug View")]
    [SerializeField] private RunData currentRunData;

    public RunData CurrentRunData => currentRunData;
    public bool HasActiveRun => currentRunData != null;

    [Header("Pending Result")]
    [SerializeField] private PendingCombatResult pendingCombatResult;

    public bool HasPendingCombatResult
    {
        get { return pendingCombatResult != null; }
    }

    public PendingCombatResult PendingCombatResult
    {
        get { return pendingCombatResult; }
    }

    [Header("Starter Run Settings")]
    [SerializeField] private int starterMaxHp = 10;
    [SerializeField] private int starterCurrentHp = 10;
    [SerializeField] private int starterGold = 0;
    [SerializeField] private int starterRemoveAmmoPrice = 75;

    [Header("Starter Weapons")]
    [SerializeField] private WeaponData starterWeaponSlot0;
    [SerializeField] private WeaponData starterWeaponSlot1;

    [SerializeField] private bool useStarterWeaponSlot0 = true;
    [SerializeField] private bool useStarterWeaponSlot1 = false;

    [Header("Starter Ammo Deck")]
    [SerializeField] private List<AmmoModuleData> starterAmmoDeck = new List<AmmoModuleData>();

    [Header("Starter Inventory")]
    [SerializeField] private List<WeaponAttachmentData> starterSpareAttachments = new List<WeaponAttachmentData>();

    [Header("Map Generation")]
    [Tooltip("Start와 Boss 포함 총 depth 수")]
    [SerializeField] private int totalMapDepth = 15;

    [SerializeField] private int minNodesPerDepth = 2;
    [SerializeField] private int maxNodesPerDepth = 4;

    [Tooltip("맵 UI 배치용 간격")]
    [SerializeField] private float nodeXSpacing = 180f;
    [SerializeField] private float nodeYSpacing = 120f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartNewRunFromTitle()
    {
        RunData newRunData = CreateNewRunData();
        StartNewRun(newRunData);
    }

    public void StartNewRun(RunData newRunData)
    {
        if (newRunData == null)
        {
            Debug.LogError("[RunGameManager] StartNewRun failed: newRunData is null.");
            return;
        }

        currentRunData = newRunData;

        // 새 런 시작 시 이전 전투 결과가 남아 있으면 안 된다.
        ClearPendingCombatResult();

        Debug.Log("[RunGameManager] New run started.");
    }

    public void EndRun()
    {
        currentRunData = null;

        // 런 종료 시 pending result도 제거한다.
        ClearPendingCombatResult();

        Debug.Log("[RunGameManager] Run ended.");
    }

    public void SetRunData(RunData updatedRunData)
    {
        if (updatedRunData == null)
        {
            Debug.LogError("[RunGameManager] SetRunData failed: updatedRunData is null.");
            return;
        }

        currentRunData = updatedRunData;
    }

    private RunData CreateNewRunData()
    {
        RunData runData = new RunData();

        runData.maxHp = starterMaxHp;
        runData.currentHp = starterCurrentHp;
        runData.gold = starterGold;
        runData.removeAmmoPrice = starterRemoveAmmoPrice;

        EnsureWeaponSlots(runData);

        runData.equippedWeapons[0] = CreateStarterWeaponLoadout(useStarterWeaponSlot0, starterWeaponSlot0);
        runData.equippedWeapons[1] = CreateStarterWeaponLoadout(useStarterWeaponSlot1, starterWeaponSlot1);

        if (!runData.equippedWeapons[0].hasWeapon && runData.equippedWeapons[1].hasWeapon)
            runData.currentWeaponSlotIndex = 1;
        else
            runData.currentWeaponSlotIndex = 0;

        runData.ammoDeck = new List<AmmoModuleData>();

        if (starterAmmoDeck != null)
            runData.ammoDeck.AddRange(starterAmmoDeck);

        runData.inventory = new InventoryData();

        if (starterSpareAttachments != null)
            runData.inventory.spareAttachments.AddRange(starterSpareAttachments);

        // 맵 절차 생성
        runData.mapData = RunMapGenerator.GenerateMap(
            totalMapDepth,
            minNodesPerDepth,
            maxNodesPerDepth,
            nodeXSpacing,
            nodeYSpacing
        );

        return runData;
    }

    private void EnsureWeaponSlots(RunData runData)
    {
        if (runData == null)
            return;

        if (runData.equippedWeapons == null || runData.equippedWeapons.Length < 2)
            runData.equippedWeapons = new WeaponLoadoutData[2];

        for (int i = 0; i < runData.equippedWeapons.Length; i++)
        {
            if (runData.equippedWeapons[i] == null)
                runData.equippedWeapons[i] = new WeaponLoadoutData();
        }
    }

    private WeaponLoadoutData CreateStarterWeaponLoadout(bool useThisSlot, WeaponData weaponData)
    {
        return new WeaponLoadoutData
        {
            hasWeapon = useThisSlot && weaponData != null,
            weaponData = weaponData,
            equippedAttachments = new List<WeaponAttachmentData>()
        };
    }

    public bool HasWeaponInRunSlot(int slotIndex)
    {
        if (currentRunData == null)
            return false;

        if (currentRunData.equippedWeapons == null)
            return false;

        if (slotIndex < 0 || slotIndex >= currentRunData.equippedWeapons.Length)
            return false;

        WeaponLoadoutData loadout = currentRunData.equippedWeapons[slotIndex];

        return loadout != null
            && loadout.hasWeapon
            && loadout.weaponData != null;
    }

    public void SetPendingCombatResult(PendingCombatResult result)
    {
        pendingCombatResult = result;

        if (pendingCombatResult != null)
        {
            Debug.Log(
                $"[RunGameManager] Pending combat result set. " +
                $"Victory={pendingCombatResult.wasVictory}, " +
                $"Reward={pendingCombatResult.shouldShowReward}, " +
                $"Node={pendingCombatResult.clearedNodeId}"
            );
        }
    }

    public bool TryConsumePendingCombatResult(out PendingCombatResult result)
    {
        if (pendingCombatResult == null)
        {
            result = null;
            return false;
        }

        result = pendingCombatResult;

        // 한 번 소비한 결과는 다시 쓰면 안 된다.
        pendingCombatResult = null;

        Debug.Log("[RunGameManager] Pending combat result consumed.");

        return true;
    }

    public PendingCombatResult ConsumePendingCombatResult()
    {
        if (pendingCombatResult == null)
        {
            Debug.Log("[RunGameManager] No pending combat result to consume.");
            return null;
        }

        PendingCombatResult result = pendingCombatResult;

        // 한 번 소비한 결과는 다시 쓰면 안 된다.
        pendingCombatResult = null;

        Debug.Log("[RunGameManager] Pending combat result consumed.");

        return result;
    }

    public void ClearPendingCombatResult()
    {
        pendingCombatResult = null;
    }
}