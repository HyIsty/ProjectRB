using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tab_Deck 전체를 갱신하는 메인 UI 컨트롤러.
///
/// 역할:
/// - Combat 모드:
///   - Draw pile / Discard pile 집계형 표시
///   - Weapon 1 / Weapon 2 loaded ammo 순서형 표시
///
/// - InGame 모드:
///   - RunData.ammoDeck 전체를 Draw pile 영역에 집계형 표시
///   - Weapon 1 / Weapon 2 / Discard 영역은 내용만 비움
///
/// 중요:
/// - Draw / Discard = 같은 ammo id끼리 묶어서 xN 표시
/// - Queue 1 / Queue 2 = 순서 유지, #1, #2, #3...
/// - InGame에서는 패널을 숨기지 않고, 나머지 3영역만 비운다.
/// </summary>
public class InventoryDeckTabUI : MonoBehaviour
{
    private enum DeckDataMode
    {
        None,
        CombatRuntime,
        RunData
    }

    [Header("Optional Default References (Combat)")]
    [SerializeField] private AmmoDeckRuntime ammoDeckRuntime;
    [SerializeField] private PlayerWeaponController playerWeaponController;

    [Header("Shared UI References")]
    [SerializeField] private AmmoTooltipUI ammoTooltipUI;
    [SerializeField] private DeckAmmoRowItemUI rowPrefab;

    [Header("Contents")]
    [SerializeField] private Transform drawPileContent;
    [SerializeField] private Transform weapon1QueueContent;
    [SerializeField] private Transform weapon2QueueContent;
    [SerializeField] private Transform discardPileContent;

    private readonly List<DeckAmmoRowItemUI> spawnedDrawRows = new List<DeckAmmoRowItemUI>();
    private readonly List<DeckAmmoRowItemUI> spawnedWeapon1Rows = new List<DeckAmmoRowItemUI>();
    private readonly List<DeckAmmoRowItemUI> spawnedWeapon2Rows = new List<DeckAmmoRowItemUI>();
    private readonly List<DeckAmmoRowItemUI> spawnedDiscardRows = new List<DeckAmmoRowItemUI>();

    private Coroutine refreshRoutine;

    // 현재 어떤 데이터 모드로 그릴지 기억해 둔다.
    private DeckDataMode currentMode = DeckDataMode.None;

    // InGame 바인딩용
    private RunData boundRunData;

    // Combat 바인딩용
    private AmmoDeckRuntime boundAmmoDeckRuntime;
    private PlayerWeaponController boundPlayerWeaponController;

    private void Start()
    {
        // 기존 Combat 전용 씬에서 inspector를 안 꽂아둔 경우를 위한 fallback
        if (ammoDeckRuntime == null)
            ammoDeckRuntime = FindFirstObjectByType<AmmoDeckRuntime>();

        if (playerWeaponController == null)
            playerWeaponController = FindFirstObjectByType<PlayerWeaponController>();

        // 기본 참조가 있으면 Combat 모드 기본 바인딩으로 잡아둔다.
        if (ammoDeckRuntime != null || playerWeaponController != null)
        {
            boundAmmoDeckRuntime = ammoDeckRuntime;
            boundPlayerWeaponController = playerWeaponController;
            currentMode = DeckDataMode.CombatRuntime;
        }
    }

    private void OnEnable()
    {
        Refresh();
        RefreshNextFrame();
    }

    /// <summary>
    /// InGame 씬용 바인딩.
    /// RunData를 받아 Draw pile 영역만 채우도록 한다.
    /// </summary>
    public void RefreshFromRunData(RunData runData)
    {
        boundRunData = runData;

        // Combat 바인딩 해제
        boundAmmoDeckRuntime = null;
        boundPlayerWeaponController = null;

        currentMode = DeckDataMode.RunData;
        Refresh();
        RefreshNextFrame();
    }

    /// <summary>
    /// Combat 씬용 바인딩.
    /// AmmoDeckRuntime + PlayerWeaponController를 받아 기존 4영역 전부 갱신한다.
    /// </summary>
    public void RefreshFromCombatRuntime(
        AmmoDeckRuntime runtimeDeck,
        PlayerWeaponController weaponController)
    {
        boundRunData = null;

        boundAmmoDeckRuntime = runtimeDeck;
        boundPlayerWeaponController = weaponController;

        currentMode = DeckDataMode.CombatRuntime;
        Refresh();
        RefreshNextFrame();
    }

    /// <summary>
    /// 현재 바인딩된 모드 기준으로 Tab_Deck 전체 새로고침.
    /// </summary>
    public void Refresh()
    {
        switch (currentMode)
        {
            case DeckDataMode.RunData:
                RefreshRunDataMode();
                break;

            case DeckDataMode.CombatRuntime:
                RefreshCombatMode();
                break;

            default:
                // 아직 아무 것도 바인딩되지 않았다면 안전하게 비워둔다.
                ClearAllSections();
                break;
        }
    }

    public void RefreshNextFrame()
    {
        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);

        refreshRoutine = StartCoroutine(CoRefreshNextFrame());
    }

    private IEnumerator CoRefreshNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();
        Refresh();
        refreshRoutine = null;
    }

    /// <summary>
    /// Combat 씬 기준 갱신.
    /// 기존 동작 유지.
    /// </summary>
    private void RefreshCombatMode()
    {
        RefreshDrawPileFromCombatRuntime();
        RefreshWeaponQueueFromCombatRuntime(0, weapon1QueueContent, spawnedWeapon1Rows);
        RefreshWeaponQueueFromCombatRuntime(1, weapon2QueueContent, spawnedWeapon2Rows);
        RefreshDiscardPileFromCombatRuntime();
    }

    /// <summary>
    /// InGame 씬 기준 갱신.
    /// Draw pile만 RunData.ammoDeck으로 채우고, 나머지 3섹션은 비운다.
    /// </summary>
    private void RefreshRunDataMode()
    {
        IReadOnlyList<AmmoModuleData> ammoDeck = boundRunData != null
            ? boundRunData.ammoDeck
            : null;

        List<AmmoStackViewData> grouped = BuildGroupedAmmoList(ammoDeck);
        RebuildGroupedSection(grouped, drawPileContent, spawnedDrawRows);

        // InGame에서는 나머지 섹션은 내용만 비운다.
        ClearRows(spawnedWeapon1Rows);
        ClearRows(spawnedWeapon2Rows);
        ClearRows(spawnedDiscardRows);
    }

    private void RefreshDrawPileFromCombatRuntime()
    {
        IReadOnlyList<AmmoModuleData> drawPile = boundAmmoDeckRuntime != null
            ? boundAmmoDeckRuntime.GetDrawPileDataSnapshot()
            : null;

        List<AmmoStackViewData> grouped = BuildGroupedAmmoList(drawPile);
        RebuildGroupedSection(grouped, drawPileContent, spawnedDrawRows);
    }

    private void RefreshDiscardPileFromCombatRuntime()
    {
        IReadOnlyList<AmmoModuleData> discardPile = boundAmmoDeckRuntime != null
            ? boundAmmoDeckRuntime.GetDiscardPileDataSnapshot()
            : null;

        List<AmmoStackViewData> grouped = BuildGroupedAmmoList(discardPile);
        RebuildGroupedSection(grouped, discardPileContent, spawnedDiscardRows);
    }

    private void RefreshWeaponQueueFromCombatRuntime(
        int weaponIndex,
        Transform contentRoot,
        List<DeckAmmoRowItemUI> spawnedRows)
    {
        WeaponRuntime weaponRuntime = boundPlayerWeaponController != null
            ? boundPlayerWeaponController.GetWeaponRuntimeByIndex(weaponIndex)
            : null;

        IReadOnlyList<AmmoModuleData> loadedAmmo = weaponRuntime != null
            ? weaponRuntime.LoadedAmmo
            : null;

        RebuildQueueSection(loadedAmmo, contentRoot, spawnedRows);
    }

    /// <summary>
    /// Draw/Discard처럼 같은 ammo를 묶어서 xN으로 보여주는 섹션 생성.
    /// </summary>
    private void RebuildGroupedSection(
        List<AmmoStackViewData> groupedData,
        Transform contentRoot,
        List<DeckAmmoRowItemUI> spawnedRows)
    {
        ClearRows(spawnedRows);

        if (groupedData == null || contentRoot == null || rowPrefab == null)
            return;

        for (int i = 0; i < groupedData.Count; i++)
        {
            AmmoStackViewData data = groupedData[i];

            DeckAmmoRowItemUI row = Instantiate(rowPrefab, contentRoot);
            spawnedRows.Add(row);

            string badgeText = $"x{data.count}";
            int previewDamageDelta = 0;

            row.Initialize(data.representativeAmmo, badgeText, previewDamageDelta, ammoTooltipUI);
        }
    }

    /// <summary>
    /// Weapon loaded queue처럼 순서를 유지해서 #1, #2, #3 으로 보여주는 섹션 생성.
    /// </summary>
    private void RebuildQueueSection(
        IReadOnlyList<AmmoModuleData> queueData,
        Transform contentRoot,
        List<DeckAmmoRowItemUI> spawnedRows)
    {
        ClearRows(spawnedRows);

        if (queueData == null || contentRoot == null || rowPrefab == null)
            return;

        for (int i = 0; i < queueData.Count; i++)
        {
            AmmoModuleData ammo = queueData[i];

            DeckAmmoRowItemUI row = Instantiate(rowPrefab, contentRoot);
            spawnedRows.Add(row);

            string badgeText = $"#{i + 1}";
            int previewDamageDelta = 0;

            row.Initialize(ammo, badgeText, previewDamageDelta, ammoTooltipUI);
        }
    }

    /// <summary>
    /// 같은 ammo id끼리 묶어서 count를 계산한다.
    /// 표시 순서는 "처음 등장한 순서"를 유지한다.
    /// </summary>
    private List<AmmoStackViewData> BuildGroupedAmmoList(IReadOnlyList<AmmoModuleData> ammoList)
    {
        List<AmmoStackViewData> result = new List<AmmoStackViewData>();

        if (ammoList == null || ammoList.Count == 0)
            return result;

        Dictionary<string, AmmoStackViewData> map = new Dictionary<string, AmmoStackViewData>();

        for (int i = 0; i < ammoList.Count; i++)
        {
            AmmoModuleData ammo = ammoList[i];
            if (ammo == null)
                continue;

            string key = GetAmmoGroupKey(ammo);

            if (map.TryGetValue(key, out AmmoStackViewData existing))
            {
                existing.count++;
            }
            else
            {
                AmmoStackViewData data = new AmmoStackViewData
                {
                    representativeAmmo = ammo,
                    count = 1
                };

                map.Add(key, data);
                result.Add(data);
            }
        }

        return result;
    }

    private string GetAmmoGroupKey(AmmoModuleData ammo)
    {
        if (ammo == null)
            return "NULL";

        if (!string.IsNullOrWhiteSpace(ammo.id))
            return ammo.id;

        if (!string.IsNullOrWhiteSpace(ammo.displayName))
            return ammo.displayName;

        return ammo.GetHashCode().ToString();
    }

    private void ClearAllSections()
    {
        ClearRows(spawnedDrawRows);
        ClearRows(spawnedWeapon1Rows);
        ClearRows(spawnedWeapon2Rows);
        ClearRows(spawnedDiscardRows);
    }

    private void ClearRows(List<DeckAmmoRowItemUI> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i] != null)
                Destroy(rows[i].gameObject);
        }

        rows.Clear();
    }

    /// <summary>
    /// Draw/Discard 집계용 임시 view data.
    /// </summary>
    private class AmmoStackViewData
    {
        public AmmoModuleData representativeAmmo;
        public int count;
    }
}