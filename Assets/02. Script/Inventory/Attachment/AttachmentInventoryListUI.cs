using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tab_Attachments 안의 잉여 부착물 목록 전체를 관리한다.
///
/// 이제 두 가지 데이터 경로를 지원한다.
/// 1. InGameScene : RunData.inventory.spareAttachments
/// 2. CombatScene : InventoryRuntime.UnequippedAttachments
///
/// 역할:
/// - 현재 데이터 소스에서 잉여 부착물 목록 읽기
/// - item prefab 생성/삭제
/// - 각 item에 데이터 바인딩
///
/// 중요한 규칙:
/// - scene mode 판단은 여기서 하지 않는다.
/// - 부모 InventoryUIController가 어떤 Refresh 경로를 호출할지 결정한다.
/// - drag 허용 여부 최종 판단은 item / controller 쪽 gate를 따른다.
/// </summary>
public class AttachmentInventoryListUI : MonoBehaviour
{
    private enum AttachmentListDataMode
    {
        None,
        CombatRuntime,
        RunData
    }

    [Header("Optional Default References (Combat)")]
    [SerializeField] private InventoryRuntime inventoryRuntime;
    [SerializeField] private InventoryUIController inventoryUIController;

    [Header("UI References")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private AttachmentInventoryItemUI itemPrefab;

    [Header("Debug")]
    [SerializeField] private bool refreshOnEnable = true;

    // 현재 생성된 item 인스턴스 캐시
    private readonly List<AttachmentInventoryItemUI> spawnedItems = new List<AttachmentInventoryItemUI>();

    // 현재 어떤 데이터 모드로 그리는지
    private AttachmentListDataMode currentMode = AttachmentListDataMode.None;

    // InGame 바인딩용
    private RunData boundRunData;
    private int boundSelectedWeaponIndex = 0;
    private bool boundCanEdit = false;

    // Combat 바인딩용
    private InventoryRuntime boundInventoryRuntime;
    private PlayerWeaponController boundPlayerWeaponController;

    private void Start()
    {
        if (inventoryUIController == null)
            inventoryUIController = FindFirstObjectByType<InventoryUIController>();
    }

    private void OnEnable()
    {
        if (refreshOnEnable)
            Refresh();
        if (inventoryRuntime == null)
            inventoryRuntime = FindFirstObjectByType<InventoryRuntime>();
    }

    /// <summary>
    /// InGameScene용.
    /// RunData에서 spareAttachments를 읽어 리스트를 구성한다.
    /// selectedWeaponIndex / canEdit는 지금 단계에서는 직접 필터링에 쓰지 않지만,
    /// 이후 지원 무기 강조나 drag 허용 처리 확장에 쓰기 위해 같이 받아 둔다.
    /// </summary>
    public void RefreshFromRunData(
        RunData runData,
        int selectedWeaponIndex,
        bool canEdit,
        AttachmentTooltipUI tooltipUI,
        InventoryUIController controller)
    {
        boundRunData = runData;
        boundSelectedWeaponIndex = selectedWeaponIndex;
        boundCanEdit = canEdit;

        if (controller != null)
            inventoryUIController = controller;

        // Combat 바인딩 해제
        boundInventoryRuntime = null;
        boundPlayerWeaponController = null;

        currentMode = AttachmentListDataMode.RunData;
        Refresh();
    }

    /// <summary>
    /// CombatScene용.
    /// InventoryRuntime에서 UnequippedAttachments를 읽어 리스트를 구성한다.
    /// Combat에서는 canEdit=false가 들어와 drag/drop이 막히는 흐름을 기대한다.
    /// </summary>
    public void RefreshFromCombatRuntime(
        InventoryRuntime runtimeInventory,
        PlayerWeaponController playerWeaponController,
        int selectedWeaponIndex,
        bool canEdit,
        AttachmentTooltipUI tooltipUI,
        InventoryUIController controller)
    {
        boundInventoryRuntime = runtimeInventory;
        boundPlayerWeaponController = playerWeaponController;
        boundSelectedWeaponIndex = selectedWeaponIndex;
        boundCanEdit = canEdit;

        if (controller != null)
            inventoryUIController = controller;

        // InGame 바인딩 해제
        boundRunData = null;

        currentMode = AttachmentListDataMode.CombatRuntime;
        Refresh();
    }

    /// <summary>
    /// 기존 Combat 전용 흐름 호환용.
    /// Inspector에 InventoryRuntime이 꽂혀 있는 예전 씬에서도 최대한 덜 터지게 둔다.
    /// </summary>
    public void Refresh()
    {
        ClearSpawnedItems();

        if (contentRoot == null)
        {
            Debug.LogWarning("[AttachmentInventoryListUI] Refresh failed: contentRoot is null.", this);
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogWarning("[AttachmentInventoryListUI] Refresh failed: itemPrefab is null.", this);
            return;
        }

        IReadOnlyList<WeaponAttachmentData> attachments = GetCurrentAttachmentSource();
        if (attachments == null)
            return;

        for (int i = 0; i < attachments.Count; i++)
        {
            WeaponAttachmentData attachment = attachments[i];
            if (attachment == null)
                continue;

            AttachmentInventoryItemUI itemInstance = Instantiate(itemPrefab, contentRoot);

            // 현재 item 스크립트가 attachment + controller만 받는 구조를 유지한다.
            // drag 가능 여부 최종 판단은 item 내부에서 controller.CanProcessAttachmentEdit()를 보게 하면 된다.
            itemInstance.Bind(attachment, inventoryUIController);

            spawnedItems.Add(itemInstance);
        }
    }

    /// <summary>
    /// 현재 바인딩 모드 기준으로 실제 attachment 목록을 가져온다.
    /// </summary>
    private IReadOnlyList<WeaponAttachmentData> GetCurrentAttachmentSource()
    {
        switch (currentMode)
        {
            case AttachmentListDataMode.RunData:
                if (boundRunData == null || boundRunData.inventory == null)
                    return null;

                return boundRunData.inventory.spareAttachments;

            case AttachmentListDataMode.CombatRuntime:
                if (boundInventoryRuntime != null)
                    return boundInventoryRuntime.UnequippedAttachments;

                // 예전 combat 전용 inspector fallback
                if (inventoryRuntime != null)
                    return inventoryRuntime.UnequippedAttachments;

                Debug.LogWarning("[AttachmentInventoryListUI] Combat source is null.", this);
                return null;

            default:
                // 예전 combat 전용 inspector fallback
                if (inventoryRuntime != null)
                    return inventoryRuntime.UnequippedAttachments;

                return null;
        }
    }

    /// <summary>
    /// 기존에 생성된 아이템 전부 삭제.
    /// </summary>
    private void ClearSpawnedItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }
}