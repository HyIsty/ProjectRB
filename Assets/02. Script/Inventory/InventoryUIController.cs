using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 UI 전체 상태 허브.
/// 이제 Combat 전용이 아니라 InGame / Combat 둘 다 사용할 수 있도록
/// scene mode + data binding 구조로 분리한다.
/// </summary>
public class InventoryUIController : MonoBehaviour
{
    public enum InventorySceneMode
    {
        InGame,
        Combat
    }

    public enum InventoryTabType
    {
        Attachments,
        Deck
    }

    [Header("Scene Mode")]
    [SerializeField] private InventorySceneMode sceneMode = InventorySceneMode.Combat;

    [Header("Root References")]
    [SerializeField] private GameObject inventoryPanelRoot;
    [SerializeField] private GameObject attachmentsTabRoot;
    [SerializeField] private GameObject deckTabRoot;

    [Header("Weapon Slot Button Icons")]
    [SerializeField] private Image weaponSlotIcon0;
    [SerializeField] private Image weaponSlotIcon1;

    [Header("Popup / Tooltip")]
    [SerializeField] private GameObject weaponInspectPopupRoot;
    [SerializeField] private InventoryWeaponInspectPanelUI weaponInspectPanelUI;
    [SerializeField] private AttachmentTooltipUI attachmentTooltipUI;

    [Header("Child Tab Controllers")]
    [SerializeField] private AttachmentInventoryListUI attachmentListUI;
    [SerializeField] private InventoryDeckTabUI deckTabUI;

    [Header("Optional Combat Default References")]
    // Combat 씬에서는 runtime-spawned player가 생긴 뒤 바인딩될 수 있으므로
    // Inspector에서 비워둬도 된다.
    [SerializeField] private PlayerWeaponController playerWeaponController;
    [SerializeField] private InventoryRuntime inventoryRuntime;
    [SerializeField] private AmmoDeckRuntime ammoDeckRuntime;
    [SerializeField] private AttachmentDragGhostUI attachmentDragGhostUI;

    [Header("State")]
    [SerializeField] private InventoryTabType currentTab = InventoryTabType.Attachments;
    [SerializeField] private int selectedWeaponIndex = 0;
    [SerializeField] private bool isOpen = false;

    // InGame 씬용 바인딩 데이터
    private RunData boundRunData;

    /// <summary>
    /// Combat 씬에서는 attachment 편집 금지.
    /// InGame 씬에서만 장착/교체/해제 허용.
    /// </summary>
    public bool CanEditAttachments => sceneMode == InventorySceneMode.InGame;

    /// <summary>
    /// tooltip은 양쪽 씬 모두 허용.
    /// </summary>
    public bool CanShowAttachmentTooltip => true;

    public int SelectedWeaponIndex => selectedWeaponIndex;
    public InventorySceneMode SceneMode => sceneMode;
    public InventoryTabType CurrentTab => currentTab;

    public bool IsOpen => isOpen;
    public AttachmentDragGhostUI AttachmentDragGhostUI => attachmentDragGhostUI;

    private Coroutine bindCoroutine;
    private void Awake()
    {
        ApplyOpenState(false, true);
        ApplyTabState(currentTab, true);

        if (weaponInspectPopupRoot != null)
            weaponInspectPopupRoot.SetActive(false);
    }


    private void OnEnable()
    {
        bindCoroutine = StartCoroutine(CoBindInputManagerNextFrame());
    }

    private void OnDisable()
    {
        if (bindCoroutine != null)
        {
            StopCoroutine(bindCoroutine);
            bindCoroutine = null;
        }

        if (PlayerInputManager.Instance != null)
            PlayerInputManager.Instance.UnbindInventory(this);
    }

    private IEnumerator CoBindInputManagerNextFrame()
    {
        // 한 프레임 쉬어서 singleton Awake / scene load 정리가 끝날 시간을 준다.
        yield return null;

        // 그래도 없으면 몇 프레임 더 기다려도 된다.
        int retry = 0;
        while (PlayerInputManager.Instance == null && retry < 30)
        {
            retry++;
            yield return null;
        }

        if (PlayerInputManager.Instance == null)
        {
            Debug.LogWarning("[InventoryUIController] PlayerInputManager.Instance is still null.");
            yield break;
        }

        PlayerInputManager.Instance.BindInventory(this);
    }

    /// <summary>
    /// InGame 씬용.
    /// RunGameManager.CurrentRunData를 바인딩한다.
    /// </summary>
    public void BindRunData(RunData runData)
    {
        boundRunData = runData;

        // Combat 전용 runtime 참조는 끊어준다.
        playerWeaponController = null;
        inventoryRuntime = null;
        ammoDeckRuntime = null;

        RefreshAll();
    }

    /// <summary>
    /// Combat 씬용.
    /// runtime-spawned player와 combat runtime을 바인딩한다.
    /// </summary>
    public void BindCombatContext(
        PlayerWeaponController weaponController,
        InventoryRuntime invenRuntime,
        AmmoDeckRuntime runtimeDeck)
    {
        boundRunData = null;

        playerWeaponController = weaponController;
        inventoryRuntime = invenRuntime;
        ammoDeckRuntime = runtimeDeck;

        RefreshAll();
    }

    /// <summary>
    /// 인벤토리 열기/닫기 토글.
    /// 기존 Input / 버튼 흐름에서 이 함수 재사용 가능.
    /// </summary>
    public void ToggleInventory()
    {
        ApplyOpenState(!isOpen, false);

        if (isOpen)
            RefreshAll();
    }

    [ContextMenu("Open Inventory")]
    public void OpenInventory()
    {
        ApplyOpenState(true, false);
        RefreshAll();
    }

    [ContextMenu("Close Inventory")]
    public void CloseInventory()
    {
        ApplyOpenState(false, false);
    }

    /// <summary>
    /// 탭 버튼에서 호출.
    /// </summary>
    public void SelectAttachmentsTab()
    {
        SetCurrentTab(InventoryTabType.Attachments);
    }

    /// <summary>
    /// 탭 버튼에서 호출.
    /// </summary>
    public void SelectDeckTab()
    {
        SetCurrentTab(InventoryTabType.Deck);
    }

    /// <summary>
    /// 무기 슬롯 버튼 0에서 호출.
    /// </summary>
    public void SelectWeaponSlot0()
    {
        SelectWeaponSlot(0);
    }

    /// <summary>
    /// 무기 슬롯 버튼 1에서 호출.
    /// </summary>
    public void SelectWeaponSlot1()
    {
        SelectWeaponSlot(1);
    }

    public void SelectWeaponSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 1)
            return;

        if (!HasWeaponInSlot(slotIndex))
        {
            CloseWeaponInspector();
            return;
        }

        selectedWeaponIndex = slotIndex;

        OpenOrRefreshInspectPopup();
        RefreshCurrentTabOnly();
    }

    /// <summary>
    /// drag/drop 장착 변경 허용 여부를 child script들이 물어볼 때 사용.
    /// Combat에서는 false.
    /// </summary>
    public bool CanProcessAttachmentEdit()
    {
        return CanEditAttachments;
    }

    /// <summary>
    /// 전체 UI 새로고침.
    /// 인벤토리 열려 있을 때 주로 사용.
    /// </summary>
    public void RefreshAll()
    {
        if (!isOpen)
            return;

        RefreshWeaponSlotButtonIcons();
        RefreshCurrentTabOnly();
        RefreshInspectPopupIfOpen();
    }

    private void SetCurrentTab(InventoryTabType nextTab)
    {
        currentTab = nextTab;
        ApplyTabState(currentTab, false);

        if (isOpen)
            RefreshCurrentTabOnly();
    }

    private void ApplyOpenState(bool open, bool force)
    {
        if (!force && isOpen == open)
            return;

        isOpen = open;

        if (inventoryPanelRoot != null)
            inventoryPanelRoot.SetActive(isOpen);

        // 인벤토리 닫힐 때 inspect popup도 같이 닫는다.
        if (!isOpen && weaponInspectPopupRoot != null)
            weaponInspectPopupRoot.SetActive(false);
    }

    private void ApplyTabState(InventoryTabType tab, bool force)
    {
        if (attachmentsTabRoot != null)
            attachmentsTabRoot.SetActive(tab == InventoryTabType.Attachments);

        if (deckTabRoot != null)
            deckTabRoot.SetActive(tab == InventoryTabType.Deck);
    }

    private void RefreshCurrentTabOnly()
    {
        switch (currentTab)
        {
            case InventoryTabType.Attachments:
                RefreshAttachmentsTab();
                break;

            case InventoryTabType.Deck:
                RefreshDeckTab();
                break;
        }
    }

    private void RefreshAttachmentsTab()
    {
        if (attachmentListUI == null)
            return;

        if (sceneMode == InventorySceneMode.InGame)
        {
            // InGame은 RunData 기준
            attachmentListUI.RefreshFromRunData(
                boundRunData,
                selectedWeaponIndex,
                CanEditAttachments,
                attachmentTooltipUI,
                this);
        }
        else
        {
            // Combat은 runtime 기준
            attachmentListUI.RefreshFromCombatRuntime(
                inventoryRuntime,
                playerWeaponController,
                selectedWeaponIndex,
                CanEditAttachments,
                attachmentTooltipUI,
                this);
        }
    }

    private void RefreshDeckTab()
    {
        if (deckTabUI == null)
            return;

        if (sceneMode == InventorySceneMode.InGame)
        {
            // RunData는 draw/discard/loaded를 저장하지 않으므로
            // InGame에서는 전체 run deck 기준의 단순 표시를 사용한다.
            deckTabUI.RefreshFromRunData(boundRunData);
        }
        else
        {
            deckTabUI.RefreshFromCombatRuntime(
                ammoDeckRuntime,
                playerWeaponController);
        }
    }

    private void OpenOrRefreshInspectPopup()
    {
        if (weaponInspectPopupRoot == null || weaponInspectPanelUI == null)
            return;

        weaponInspectPopupRoot.SetActive(true);
        RefreshInspectPopupIfOpen();
    }

    private void RefreshInspectPopupIfOpen()
    {
        if (weaponInspectPopupRoot == null || !weaponInspectPopupRoot.activeSelf)
            return;

        if (weaponInspectPanelUI == null)
            return;

        if (sceneMode == InventorySceneMode.InGame)
        {
            WeaponLoadoutData runWeapon = GetRunWeaponLoadout(selectedWeaponIndex);

            weaponInspectPanelUI.RefreshFromRunDataWeapon(
                runWeapon,
                selectedWeaponIndex,
                CanEditAttachments,
                attachmentTooltipUI,
                this);
        }
        else
        {
            WeaponRuntime runtimeWeapon = GetCombatWeaponRuntime(selectedWeaponIndex);

            weaponInspectPanelUI.RefreshFromCombatWeapon(
                runtimeWeapon,
                selectedWeaponIndex,
                CanEditAttachments,
                attachmentTooltipUI,
                this);
        }
    }

    private WeaponLoadoutData GetRunWeaponLoadout(int slotIndex)
    {
        if (boundRunData == null)
            return null;


        if (boundRunData.equippedWeapons == null)
            return null;

        if (slotIndex < 0 || slotIndex >= boundRunData.equippedWeapons.Length)
            return null;

        return boundRunData.equippedWeapons[slotIndex];
    }

    private WeaponRuntime GetCombatWeaponRuntime(int slotIndex)
    {
        if (playerWeaponController == null)
            return null;

        // 네 실제 API 이름에 맞게 바꾸면 된다.
        return playerWeaponController.GetWeaponRuntimeByIndex(slotIndex);
    }

    /// <summary>
    /// 예전 drag/hover 스크립트 호환용.
    /// attachment tooltip을 표시한다.
    /// </summary>
    public void ShowAttachmentTooltip(WeaponAttachmentData attachment, Vector2 screenPosition)
    {
        if (attachmentTooltipUI == null || attachment == null)
            return;

        attachmentTooltipUI.Show(attachment, screenPosition);
    }

    /// <summary>
    /// 예전 drag/hover 스크립트 호환용.
    /// tooltip 위치만 갱신한다.
    /// </summary>
    public void UpdateAttachmentTooltipPosition(Vector2 screenPosition)
    {
        if (attachmentTooltipUI == null)
            return;

        attachmentTooltipUI.Follow(screenPosition);
    }

    /// <summary>
    /// 예전 drag/hover 스크립트 호환용.
    /// attachment tooltip을 숨긴다.
    /// </summary>
    public void HideAttachmentTooltip()
    {
        if (attachmentTooltipUI == null)
            return;

        attachmentTooltipUI.Hide();
    }

    /// <summary>
    /// 현재 선택 무기 슬롯 인덱스를 외부 얇은 입력 스크립트가 물을 수 있게 한다.
    /// </summary>
    public WeaponRuntime GetSelectedWeaponIndex()
    {
        return playerWeaponController.CurrentWeaponRuntime;
    }

    /// <summary>
    /// 현재 inspect popup을 강제로 새로고침해야 할 때 호출.
    /// drop/equip/unequip 후 재사용 가능.
    /// </summary>
    public void RefreshInventoryViews()
    {
        RefreshAll();
    }

    /// <summary>
    /// 인벤토리에서 inspect popup 쪽으로 드롭했을 때,
    /// 현재 선택된 무기에 부착물을 장착(또는 같은 슬롯 부착물 교체)한다.
    /// InGame 씬에서만 동작한다.
    /// </summary>
    public void TryEquipAttachmentByPopupDrop(WeaponAttachmentData draggedAttachment)
    {
        if (!CanProcessAttachmentEdit())
            return;

        if (draggedAttachment == null)
            return;

        if (sceneMode == InventorySceneMode.InGame)
        {
            TryEquipAttachmentToRunDataWeapon(draggedAttachment);
            RefreshAll();
            return;
        }

        if (sceneMode == InventorySceneMode.Combat)
        {
            // Combat에서는 현재 편집 금지 방향이면 사실 여기 들어오면 안 됨
            // 그래도 안전하게 막아둔다.
            Debug.Log("[InventoryUIController] Equip blocked in Combat mode.");
            return;
        }
    }

    /// <summary>
    /// InGame RunData 기준으로 현재 선택된 무기에 부착물을 장착한다.
    /// 
    /// 규칙:
    /// - 선택된 무기가 실제로 있어야 한다.
    /// - 선택된 무기가 해당 부착물 슬롯 타입을 지원해야 한다.
    /// - 부착물이 해당 무기 타입에 장착 가능해야 한다.
    /// - 검증 실패 시 인벤토리에서 제거하지 않는다.
    /// </summary>
    private void TryEquipAttachmentToRunDataWeapon(WeaponAttachmentData draggedAttachment)
    {
        if (boundRunData == null)
            return;

        if (draggedAttachment == null)
            return;

        WeaponLoadoutData targetWeapon = GetRunWeaponLoadout(selectedWeaponIndex);

        if (targetWeapon == null || !targetWeapon.hasWeapon || targetWeapon.weaponData == null)
            return;

        // 핵심 수정:
        // 인벤토리에서 제거하기 전에, 먼저 이 무기에 장착 가능한 부착물인지 검사한다.
        if (!CanEquipAttachmentToWeapon(targetWeapon.weaponData, draggedAttachment))
        {
            Debug.Log(
                $"[InventoryUIController] Attachment equip blocked. " +
                $"Weapon={targetWeapon.weaponData.weaponName}, " +
                $"Attachment={draggedAttachment.attachmentName}, " +
                $"Type={draggedAttachment.attachmentType}"
            );

            return;
        }

        if (targetWeapon.equippedAttachments == null)
            targetWeapon.equippedAttachments = new List<WeaponAttachmentData>();

        if (boundRunData.inventory == null)
            boundRunData.inventory = new InventoryData();

        if (boundRunData.inventory.spareAttachments == null)
            boundRunData.inventory.spareAttachments = new List<WeaponAttachmentData>();

        // 여기부터는 장착이 확정된 상태.
        // 그래서 이제 인벤토리에서 제거해도 된다.
        bool removedFromInventory = boundRunData.inventory.spareAttachments.Remove(draggedAttachment);

        if (!removedFromInventory)
        {
            Debug.LogWarning("[InventoryUIController] Dragged attachment was not found in spare inventory.");
            return;
        }

        // 같은 타입 기존 장착물 찾기.
        int existingIndex = FindEquippedAttachmentIndexByType(
            targetWeapon.equippedAttachments,
            draggedAttachment.attachmentType
        );

        if (existingIndex >= 0)
        {
            // 같은 슬롯 타입의 기존 부착물이 있으면 교체한다.
            WeaponAttachmentData oldAttachment = targetWeapon.equippedAttachments[existingIndex];

            if (oldAttachment != null)
                boundRunData.inventory.spareAttachments.Add(oldAttachment);

            targetWeapon.equippedAttachments[existingIndex] = draggedAttachment;
        }
        else
        {
            // 같은 슬롯 타입이 없으면 새로 장착한다.
            targetWeapon.equippedAttachments.Add(draggedAttachment);
        }

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayAttachmentEquip();
    }

    /// <summary>
    /// 현재 선택된 무기에서 지정 타입의 부착물을 해제하여
    /// 인벤토리(잉여 부착물 목록)로 되돌린다.
    /// InGame 씬에서만 동작한다.
    /// </summary>
    public void TryUnequipAttachmentFromSelectedWeapon(AttachmentType attachmentType)
    {
        if (!CanProcessAttachmentEdit())
        {
            Debug.Log("[InventoryUIController] TryUnequipAttachmentFromSelectedWeapon blocked: attachment edit is disabled.");
            return;
        }

        if (sceneMode != InventorySceneMode.InGame)
        {
            Debug.Log("[InventoryUIController] TryUnequipAttachmentFromSelectedWeapon ignored in Combat mode.");
            return;
        }

        WeaponLoadoutData targetWeapon = GetRunWeaponLoadout(selectedWeaponIndex);
        if (targetWeapon == null || targetWeapon.weaponData == null)
        {
            Debug.LogWarning("[InventoryUIController] No valid selected weapon to unequip from.");
            return;
        }

        if (targetWeapon.equippedAttachments == null || targetWeapon.equippedAttachments.Count == 0)
            return;

        if (boundRunData == null)
        {
            Debug.LogWarning("[InventoryUIController] boundRunData is null.");
            return;
        }

        if (boundRunData.inventory == null)
            boundRunData.inventory = new InventoryData();

        if (boundRunData.inventory.spareAttachments == null)
            boundRunData.inventory.spareAttachments = new List<WeaponAttachmentData>();

        int index = FindEquippedAttachmentIndexByType(targetWeapon.equippedAttachments, attachmentType);
        if (index < 0)
            return;

        WeaponAttachmentData removed = targetWeapon.equippedAttachments[index];
        targetWeapon.equippedAttachments.RemoveAt(index);

        if (removed != null)
            boundRunData.inventory.spareAttachments.Add(removed);

        RefreshAll();
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayAttachmentEquip();
    }

    /// <summary>
    /// 해당 무기가 특정 attachment type을 지원하는지 검사.
    /// </summary>
    private bool WeaponSupportsAttachmentType(WeaponData weaponData, AttachmentType attachmentType)
    {
        if (weaponData == null)
            return false;

        if (weaponData.allowedAttachmentTypes == null)
            return false;

        for (int i = 0; i < weaponData.allowedAttachmentTypes.Length; i++)
        {
            if (weaponData.allowedAttachmentTypes[i] == attachmentType)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 특정 무기에 특정 부착물이 장착 가능한지 최종 검사한다.
    /// 
    /// 검사 내용:
    /// 1. 무기가 해당 부착물 슬롯 타입을 지원하는지 확인
    /// 2. 부착물이 해당 무기 타입을 허용하는지 확인
    /// </summary>
    private bool CanEquipAttachmentToWeapon(WeaponData weaponData, WeaponAttachmentData attachmentData)
    {
        if (weaponData == null || attachmentData == null)
            return false;

        // 1. 무기 자체가 이 슬롯 타입을 지원하는지 검사.
        // 예: 스코프 슬롯이 없는 권총이면 Scope 부착물 거부.
        if (!WeaponSupportsAttachmentType(weaponData, attachmentData.attachmentType))
            return false;

        // 2. 부착물 자체가 이 무기 타입을 허용하는지 검사.
        // allowedWeaponTypes를 아직 안 쓰거나 비워둔 데이터가 있다면,
        // null/empty는 일단 전체 허용으로 처리한다.
        if (attachmentData.allowedWeaponTypes == null || attachmentData.allowedWeaponTypes.Count == 0)
            return true;

        for (int i = 0; i < attachmentData.allowedWeaponTypes.Count; i++)
        {
            if (attachmentData.allowedWeaponTypes[i] == weaponData.weaponType)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 장착 목록에서 해당 attachment type의 인덱스를 찾는다.
    /// 없으면 -1 반환.
    /// </summary>
    private int FindEquippedAttachmentIndexByType(
        List<WeaponAttachmentData> equippedAttachments,
        AttachmentType attachmentType)
    {
        if (equippedAttachments == null)
            return -1;

        for (int i = 0; i < equippedAttachments.Count; i++)
        {
            WeaponAttachmentData attachment = equippedAttachments[i];
            if (attachment == null)
                continue;

            if (attachment.attachmentType == attachmentType)
                return i;
        }

        return -1;
    }

    public bool HasWeaponInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 1)
            return false;

        if (sceneMode == InventorySceneMode.InGame)
        {
            if (boundRunData == null || boundRunData.equippedWeapons == null)
                return false;

            if (slotIndex >= boundRunData.equippedWeapons.Length)
                return false;

            WeaponLoadoutData loadout = boundRunData.equippedWeapons[slotIndex];
            return loadout != null && loadout.hasWeapon;
        }

        if (sceneMode == InventorySceneMode.Combat)
        {
            WeaponRuntime runtime = GetCombatWeaponRuntime(slotIndex);
            return runtime != null && runtime.HasBaseData;
        }

        return false;
    }

    public bool HasSelectedWeapon()
    {
        return HasWeaponInSlot(selectedWeaponIndex);
    }

    public bool CanEditSelectedWeapon()
    {
        return CanProcessAttachmentEdit() && HasSelectedWeapon();
    }

    public void CloseWeaponInspector()
    {
        if (weaponInspectPopupRoot != null)
            weaponInspectPopupRoot.SetActive(false);
    }

    /// <summary>
    /// 상단 총기 슬롯 버튼 아이콘을 현재 장착 무기 상태로 갱신한다.
    /// InGame이면 RunData, Combat이면 WeaponRuntime 기준으로 본다.
    /// </summary>
    private void RefreshWeaponSlotButtonIcons()
    {
        RefreshWeaponSlotButtonIcon(0, weaponSlotIcon0);
        RefreshWeaponSlotButtonIcon(1, weaponSlotIcon1);
    }

    /// <summary>
    /// 특정 슬롯의 버튼 아이콘 하나 갱신.
    /// </summary>
    private void RefreshWeaponSlotButtonIcon(int slotIndex, Image targetIcon)
    {
        if (targetIcon == null)
            return;

        Sprite weaponSprite = GetWeaponSpriteForSlot(slotIndex);

        if (weaponSprite != null)
        {
            targetIcon.enabled = true;
            targetIcon.sprite = weaponSprite;
            targetIcon.color = Color.white;
            return;
        }

        targetIcon.sprite = null;
        targetIcon.enabled = false;
    }

    /// <summary>
    /// 현재 scene mode 기준으로 슬롯의 무기 스프라이트를 가져온다.
    /// </summary>
    private Sprite GetWeaponSpriteForSlot(int slotIndex)
    {
        if (sceneMode == InventorySceneMode.InGame)
        {
            WeaponLoadoutData loadout = GetRunWeaponLoadout(slotIndex);
            if (loadout == null || loadout.weaponData == null)
                return null;

            return loadout.weaponData.weaponSprite;
        }

        if (sceneMode == InventorySceneMode.Combat)
        {
            WeaponRuntime runtime = GetCombatWeaponRuntime(slotIndex);
            if (runtime == null || !runtime.HasBaseData || runtime.BaseData == null)
                return null;

            return runtime.BaseData.weaponSprite;
        }

        return null;
    }
}