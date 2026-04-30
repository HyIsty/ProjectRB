using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 HUD.
/// 현재 이 버전의 핵심 역할:
/// - 좌하단 HP / AP 표시
/// - 우하단 턴 종료 버튼
/// - 우하단 현재 선택 무기의 loaded ammo queue 표시
///
/// 주의:
/// - 현재 무기 탄 UI는 Tab_Deck에서 쓰던 DeckAmmoRowItemUI 프리팹을 재사용한다.
/// - draw/discard/weapon1/weapon2 전체를 관리하는 InventoryDeckTabUI는 재사용하지 않는다.
/// </summary>
public class CombatHUDUI : MonoBehaviour
{
    [Header("Left Bottom")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text apText;

    [Header("Right Bottom")]
    [SerializeField] private Button endTurnButton;
    [SerializeField] private TMP_Text turnStateText;
    [SerializeField] private TMP_Text currentWeaponNameText;
    [SerializeField] private TMP_Text emptyAmmoText;

    [Header("Current Weapon Queue")]
    [SerializeField] private Transform currentWeaponQueueContent;

    [Tooltip("Tab_Deck에서 사용 중인 DeckAmmoRowItem 프리팹을 그대로 넣는다.")]
    [SerializeField] private DeckAmmoRowItemUI ammoRowPrefab;

    private TurnManager turnManager;
    private UnitHealthController healthController;
    private PlayerWeaponController weaponController;

    // 현재 우하단 큐에 생성한 row들
    private readonly List<DeckAmmoRowItemUI> spawnedQueueRows = new();

    // 불필요한 rebuild를 줄이기 위한 최소 캐시
    private int lastWeaponIndex = int.MinValue;
    private int lastLoadedAmmoCount = int.MinValue;

    private void OnEnable()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveListener(OnClickEndTurn);
            endTurnButton.onClick.AddListener(OnClickEndTurn);
        }

        turnManager = TurnManager.Instance;
    }

    private void OnDisable()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveListener(OnClickEndTurn);
        }
    }

    /// <summary>
    /// runtime-spawned player를 HUD에 바인딩한다.
    /// scene-side HUD가 spawned player를 읽는 현재 구조에 맞춘 함수.
    /// </summary>
    public void BindPlayer(GridUnit playerRoot)
    {
        healthController = playerRoot != null ? playerRoot.GetComponent<UnitHealthController>() : null;
        weaponController = playerRoot != null ? playerRoot.GetComponent<PlayerWeaponController>() : null;

        // 캐시 초기화
        lastWeaponIndex = int.MinValue;
        lastLoadedAmmoCount = int.MinValue;

        RefreshAll();
    }

    private void Update()
    {
        RefreshHealthUI();
        RefreshActionPointUI();
        RefreshTurnStateUI();
        RefreshCurrentWeaponQueueIfNeeded();
    }

    public void RefreshAll()
    {
        RefreshHealthUI();
        RefreshActionPointUI();
        RefreshTurnStateUI();
        RebuildCurrentWeaponQueue();
    }

    private void OnClickEndTurn()
    {
        if (turnManager == null)
            return;

        turnManager.RequestEndPlayerTurn();
    }

    private void RefreshHealthUI()
    {
        if (healthController == null)
            return;

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;
            hpSlider.maxValue = Mathf.Max(1, healthController.MaxHP);
            hpSlider.value = Mathf.Clamp(healthController.CurrentHP, 0, healthController.MaxHP);
        }

        if (hpText != null)
        {
            hpText.text = $"HP : {healthController.CurrentHP} / {healthController.MaxHP}";
        }
    }

    private void RefreshActionPointUI()
    {
        if (turnManager == null || apText == null)
            return;

        apText.text = $"{turnManager.CurrentPlayerAP} / {turnManager.MaxPlayerAP}";
    }

    private void RefreshTurnStateUI()
    {
        if (turnManager == null)
            return;

        if (turnStateText != null)
        {
            turnStateText.text = turnManager.CurrentState switch
            {
                TurnManager.CombatTurnState.PlayerTurn => "Player Turn",
                TurnManager.CombatTurnState.EnemyTurn => "Enemy Turn",
                TurnManager.CombatTurnState.Busy => "Busy",
                _ => "None"
            };
        }

        if (endTurnButton != null)
        {
            endTurnButton.interactable = turnManager.IsPlayerTurn;
        }
    }

    /// <summary>
    /// 현재 선택 무기 인덱스 또는 loaded ammo 개수가 바뀌었을 때만 rebuild.
    /// 무기 전환(1/2), 리로드, 사격 후 queue 갱신용.
    /// </summary>
    private void RefreshCurrentWeaponQueueIfNeeded()
    {
        if (weaponController == null)
            return;

        WeaponRuntime currentWeapon = weaponController.GetCurrentWeaponRuntime();
        int currentWeaponIndex = weaponController.CurrentWeaponIndex;
        int loadedAmmoCount = currentWeapon != null ? currentWeapon.LoadedAmmoCount : 0;

        if (currentWeaponIndex != lastWeaponIndex || loadedAmmoCount != lastLoadedAmmoCount)
        {
            RebuildCurrentWeaponQueue();
        }
    }

    /// <summary>
    /// 현재 선택 무기의 loaded ammo queue를 우하단 HUD에 다시 그린다.
    /// Tab_Deck의 weapon queue 규칙과 동일하게 #1, #2, #3... 순서로 표시한다.
    /// </summary>
    private void RebuildCurrentWeaponQueue()
    {
        ClearCurrentWeaponQueue();

        if (weaponController == null)
        {
            SetEmptyAmmoState(true);
            UpdateCurrentWeaponName(null);
            return;
        }

        WeaponRuntime currentWeapon = weaponController.GetCurrentWeaponRuntime();
        lastWeaponIndex = weaponController.CurrentWeaponIndex;
        lastLoadedAmmoCount = currentWeapon != null ? currentWeapon.LoadedAmmoCount : 0;

        UpdateCurrentWeaponName(currentWeapon);

        if (currentWeapon == null || currentWeapon.LoadedAmmoCount <= 0)
        {
            SetEmptyAmmoState(true);
            return;
        }

        SetEmptyAmmoState(false);

        IReadOnlyList<AmmoModuleData> loadedAmmoList = currentWeapon.LoadedAmmo;
        for (int i = 0; i < loadedAmmoList.Count; i++)
        {
            DeckAmmoRowItemUI row = Instantiate(ammoRowPrefab, currentWeaponQueueContent);

            spawnedQueueRows.Add(row);
            row.BindQueueRow(i + 1, loadedAmmoList[i]);
        }
    }

    private void ClearCurrentWeaponQueue()
    {
        for (int i = 0; i < spawnedQueueRows.Count; i++)
        {
            if (spawnedQueueRows[i] != null)
            {
                Destroy(spawnedQueueRows[i].gameObject);
            }
        }

        spawnedQueueRows.Clear();
    }

    private void UpdateCurrentWeaponName(WeaponRuntime currentWeapon)
    {
        if (currentWeaponNameText == null)
            return;

        currentWeaponNameText.text = currentWeapon != null
            ? $"{currentWeapon.WeaponName}"
            : "Weapon : None";
    }

    private void SetEmptyAmmoState(bool isEmpty)
    {
        if (emptyAmmoText != null)
        {
            emptyAmmoText.gameObject.SetActive(isEmpty);
        }
    }
}