using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// New Input System 기반 중앙 입력 관리자.
///
/// 구조 원칙:
/// - 싱글톤 + DontDestroyOnLoad
/// - 씬이 바뀌면 scene-side 참조는 다시 바인딩
/// - Combat / Shop / InGame 입력을 sceneMode로 분기
/// - 실제 이동/사격/리로드/조준 계산은 각 실행 스크립트가 담당
/// - 맵 토글은 여기서 입력만 받고, 실제 열기/닫기는 외부 이벤트로 넘긴다
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance { get; private set; }

    public enum InputSceneMode
    {
        None,
        InGame,
        Combat,
        Shop
    }

    public enum PlayerInputMode
    {
        None,
        MovePreview,
        AimHold
    }

    [Header("Scene Mode")]
    [SerializeField] private InputSceneMode sceneMode = InputSceneMode.None;

    [Header("Core References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InventoryUIController inventoryUIController;
    [SerializeField] private PlayerWeaponController playerWeaponController;
    [SerializeField] private PlayerClickMover playerClickMover;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private UnitStatusController statusController;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private AmmoDeckRuntime deckRuntime;

    [Header("Preview References")]
    [SerializeField] private PlayerAimController playerAimController;
    [SerializeField] private MoveRangeHighlighter moveRangeHighlighter;
    [SerializeField] private AimLineController aimLineController;

    [Header("External Action Hooks")]
    [SerializeField] private UnityEvent onReloadRequested;
    [SerializeField] private UnityEvent onEndTurnRequested;
    /// <summary>
    /// 런 맵 토글 요청 이벤트.
    /// 실제 맵 열기/닫기는 외부 컨트롤러가 처리한다.
    /// </summary>
    public event Action MapToggleRequested;

    [Header("Options")]
    [SerializeField] private bool blockWorldInputWhenPointerOverUI = true;

    // Input Actions
    private InputAction pointAction;
    private InputAction moveModeAction;
    private InputAction aimAction;
    private InputAction shootAction;
    private InputAction slot1Action;
    private InputAction slot2Action;
    private InputAction inventoryAction;
    private InputAction reloadAction;
    private InputAction endTurnAction;
    private InputAction mapToggleAction;

    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    public PlayerInputMode CurrentMode { get; private set; } = PlayerInputMode.None;

    public bool IsMovePreviewing => CurrentMode == PlayerInputMode.MovePreview;
    public bool IsAimHolding => CurrentMode == PlayerInputMode.AimHold;
    public InputSceneMode SceneMode => sceneMode;

    private void Awake()
    {
        // 싱글톤 중복 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        CacheActions();
        BindActions();

        // 씬 전환 시 이전 씬 참조를 정리
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnbindActions();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 씬이 바뀌면 이전 씬 참조를 제거한다.
    /// 새로운 씬에서 다시 BindXXX를 호출해야 한다.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearSceneBindings();
    }

    /// <summary>
    /// 매 프레임 포인터 위치 기반 preview 갱신.
    /// Combat 씬에서만 월드 preview를 갱신한다.
    /// </summary>
    private void Update()
    {
        if (sceneMode != InputSceneMode.Combat)
            return;

        Vector2 pointerScreenPos = ReadPointerScreenPosition();

        // 플레이어 비주얼/조준 방향은 포인터를 보게 유지
        if (playerAimController != null)
            playerAimController.TickAimFromScreenPosition(pointerScreenPos);

        // 이동칸 hover 강조
        if (CurrentMode == PlayerInputMode.MovePreview && moveRangeHighlighter != null)
            moveRangeHighlighter.TickHoverFromScreenPosition(pointerScreenPos);

        // 조준선 갱신
        if (CurrentMode == PlayerInputMode.AimHold && aimLineController != null)
            aimLineController.TickAimPreviewFromScreenPosition(pointerScreenPos);
    }

    private void CacheActions()
    {
        InputActionAsset actions = playerInput.actions;

        pointAction = actions.FindAction("Point", true);
        moveModeAction = actions.FindAction("MoveMode", true);
        aimAction = actions.FindAction("Aim", true);
        shootAction = actions.FindAction("Shoot", true);
        slot1Action = actions.FindAction("Slot1", true);
        slot2Action = actions.FindAction("Slot2", true);
        inventoryAction = actions.FindAction("Inventory", true);
        reloadAction = actions.FindAction("Reload", true);
        endTurnAction = actions.FindAction("EndTurn", true);

        // 아직 액션 에셋에 추가 안 했을 수도 있으니 optional 처리
        mapToggleAction = actions.FindAction("MapToggle", false);
    }

    private void BindActions()
    {
        if (moveModeAction != null)
            moveModeAction.performed += OnMoveModePerformed;

        if (aimAction != null)
        {
            aimAction.started += OnAimStarted;
            aimAction.canceled += OnAimCanceled;
        }

        if (shootAction != null)
            shootAction.performed += OnShootPerformed;

        if (slot1Action != null)
            slot1Action.performed += OnSlot1Performed;

        if (slot2Action != null)
            slot2Action.performed += OnSlot2Performed;

        if (inventoryAction != null)
            inventoryAction.performed += OnInventoryPerformed;

        if (reloadAction != null)
            reloadAction.performed += OnReloadPerformed;

        if (endTurnAction != null)
            endTurnAction.performed += OnEndTurnPerformed;

        if (mapToggleAction != null)
            mapToggleAction.performed += OnMapTogglePerformed;
    }

    private void UnbindActions()
    {
        if (moveModeAction != null)
            moveModeAction.performed -= OnMoveModePerformed;

        if (aimAction != null)
        {
            aimAction.started -= OnAimStarted;
            aimAction.canceled -= OnAimCanceled;
        }

        if (shootAction != null)
            shootAction.performed -= OnShootPerformed;

        if (slot1Action != null)
            slot1Action.performed -= OnSlot1Performed;

        if (slot2Action != null)
            slot2Action.performed -= OnSlot2Performed;

        if (inventoryAction != null)
            inventoryAction.performed -= OnInventoryPerformed;

        if (reloadAction != null)
            reloadAction.performed -= OnReloadPerformed;

        if (endTurnAction != null)
            endTurnAction.performed -= OnEndTurnPerformed;

        if (mapToggleAction != null)
            mapToggleAction.performed -= OnMapTogglePerformed;
    }

    /// <summary>
    /// 현재 씬 모드 설정.
    /// InGame / Combat / Shop 진입 시 각 씬에서 한 번 호출.
    /// </summary>
    public void SetSceneMode(InputSceneMode mode)
    {
        ForceExitAllPreviewModes();
        sceneMode = mode;
    }

    /// <summary>
    /// 인벤토리 UI 바인딩.
    /// InGame / Combat / Shop 모두 사용 가능.
    /// </summary>
    public void BindInventory(InventoryUIController controller)
    {
        inventoryUIController = controller;
    }

    /// <summary>
    /// Combat용 TurnManager 바인딩.
    /// Shop / InGame에서는 null이어도 된다.
    /// </summary>
    public void BindTurnManager(TurnManager manager)
    {
        turnManager = manager;
    }

    /// <summary>
    /// Combat용 preview 시스템 바인딩.
    /// </summary>
    public void BindPreviewSystems(MoveRangeHighlighter highlighter, AimLineController lineController = null)
    {
        moveRangeHighlighter = highlighter;

        if (lineController != null)
            aimLineController = lineController;
    }

    /// <summary>
    /// runtime-spawned player 바인딩.
    /// CombatScene에서 플레이어 생성 후 호출.
    /// InGame / Shop에서는 호출하지 않아도 된다.
    /// </summary>
    public void BindPlayer(GameObject playerObject)
    {
        if (playerObject == null)
        {
            Debug.LogError("[PlayerInputManager] BindPlayer failed: playerObject is null.");
            return;
        }

        playerClickMover = playerObject.GetComponent<PlayerClickMover>();
        playerShooter = playerObject.GetComponent<PlayerShooter>();
        playerAimController = playerObject.GetComponent<PlayerAimController>();
        playerWeaponController = playerObject.GetComponent<PlayerWeaponController>();
        statusController = playerObject.GetComponent<UnitStatusController>();
        aimLineController = playerObject.GetComponentInChildren<AimLineController>(true);
        deckRuntime = playerObject.GetComponentInChildren<AmmoDeckRuntime>();

        Debug.Log("[PlayerInputManager] Player bound successfully.");
    }

    /// <summary>
    /// 씬 바뀔 때 이전 씬 참조 제거.
    /// persistent singleton이 stale reference를 쥐고 있지 않게 한다.
    /// </summary>
    public void ClearSceneBindings()
    {
        inventoryUIController = null;
        turnManager = null;

        moveRangeHighlighter = null;
        aimLineController = null;

        playerWeaponController = null;
        playerClickMover = null;
        playerShooter = null;
        playerAimController = null;
        statusController = null;
        deckRuntime = null;

        ForceExitAllPreviewModes();
    }

    private void OnMoveModePerformed(InputAction.CallbackContext context)
    {
        if (sceneMode != InputSceneMode.Combat)
            return;

        if (IsInventoryBlockingGameplayInput())
            return;

        if (statusController != null && !statusController.CanMove)
            return;

        if (CurrentMode == PlayerInputMode.MovePreview)
            ExitMovePreviewMode();
        else
            EnterMovePreviewMode();
    }

    private void OnAimStarted(InputAction.CallbackContext context)
    {
        if (sceneMode != InputSceneMode.Combat)
            return;

        if (IsInventoryBlockingGameplayInput())
            return;

        if (!CanUseWorldPointer())
            return;

        // MovePreview 상태에서 RMB는 이동 실행
        if (CurrentMode == PlayerInputMode.MovePreview)
        {
            TryMoveUsingCurrentPointer();
            ExitMovePreviewMode();
            return;
        }

        if (statusController != null && !statusController.CanShoot)
            return;

        EnterAimMode();
    }

    private void OnAimCanceled(InputAction.CallbackContext context)
    {
        if (sceneMode != InputSceneMode.Combat)
            return;

        if (CurrentMode == PlayerInputMode.AimHold)
            ExitAimMode();
    }

    private void OnShootPerformed(InputAction.CallbackContext context)
    {
        if (sceneMode != InputSceneMode.Combat)
            return;

        if (IsInventoryBlockingGameplayInput())
            return;

        if (!CanUseWorldPointer())
            return;

        if (CurrentMode != PlayerInputMode.AimHold)
            return;

        if (playerShooter != null)
            playerShooter.TryShootRequested();
    }

    private void OnSlot1Performed(InputAction.CallbackContext context)
    {
        ForceExitAllPreviewModes();

        if (playerWeaponController != null)
            playerWeaponController.TrySwitchWeapon(0);

        // 인벤토리가 열려 있으면 UI 선택도 같이 반영
        if (inventoryUIController != null && inventoryUIController.IsOpen)
            inventoryUIController.SelectWeaponSlot0();
    }

    private void OnSlot2Performed(InputAction.CallbackContext context)
    {
        ForceExitAllPreviewModes();

        if (playerWeaponController != null)
            playerWeaponController.TrySwitchWeapon(1);

        if (inventoryUIController != null && inventoryUIController.IsOpen)
            inventoryUIController.SelectWeaponSlot1();
    }

    private void OnInventoryPerformed(InputAction.CallbackContext context)
    {
        ForceExitAllPreviewModes();

        if (inventoryUIController != null)
            inventoryUIController.ToggleInventory();
    }

    private void OnReloadPerformed(InputAction.CallbackContext context)
    {
        if (sceneMode != InputSceneMode.Combat)
            return;

        if (IsInventoryBlockingGameplayInput())
            return;

        if (playerWeaponController == null || deckRuntime == null)
            return;

        ForceExitAllPreviewModes();

        onReloadRequested?.Invoke();
        playerWeaponController.TryReloadCurrentWeapon(deckRuntime);
    }

    private void OnEndTurnPerformed(InputAction.CallbackContext context)
    {
        if (sceneMode != InputSceneMode.Combat)
            return;

        if (!context.performed)
            return;

        if (turnManager == null)
            return;

        if (IsInventoryBlockingGameplayInput())
            return;

        ForceExitAllPreviewModes();
        onEndTurnRequested?.Invoke();
        turnManager.RequestEndPlayerTurn();
    }

    /// <summary>
    /// Tab 맵 토글 입력.
    /// 현재 정책:
    /// - InGame: 항상 보이므로 무시
    /// - Combat: 허용
    /// - Shop: 허용
    /// - None: 무시
    /// </summary>
    private void OnMapTogglePerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!CanUseMapToggleInCurrentScene())
            return;

        // 맵 오버레이 열 때 미리 월드 preview 상태 정리
        ForceExitAllPreviewModes();

        MapToggleRequested?.Invoke();
    }

    private bool CanUseMapToggleInCurrentScene()
    {
        return sceneMode == InputSceneMode.Combat
            || sceneMode == InputSceneMode.Shop;
    }

    private void EnterMovePreviewMode()
    {
        ExitAimModeInternal();

        CurrentMode = PlayerInputMode.MovePreview;

        if (moveRangeHighlighter != null)
            moveRangeHighlighter.ShowMoveOptions();
    }

    private void ExitMovePreviewMode()
    {
        if (CurrentMode != PlayerInputMode.MovePreview)
            return;

        if (moveRangeHighlighter != null)
            moveRangeHighlighter.HideAll();

        CurrentMode = PlayerInputMode.None;
    }

    private void EnterAimMode()
    {
        ExitMovePreviewModeInternal();

        CurrentMode = PlayerInputMode.AimHold;

        if (aimLineController != null)
            aimLineController.BeginAim();
    }

    private void ExitAimMode()
    {
        if (CurrentMode != PlayerInputMode.AimHold)
            return;

        if (aimLineController != null)
            aimLineController.EndAim();

        CurrentMode = PlayerInputMode.None;
    }

    private void ExitMovePreviewModeInternal()
    {
        if (moveRangeHighlighter != null)
            moveRangeHighlighter.HideAll();
    }

    private void ExitAimModeInternal()
    {
        if (aimLineController != null)
            aimLineController.EndAim();
    }

    private void ForceExitAllPreviewModes()
    {
        if (moveRangeHighlighter != null)
            moveRangeHighlighter.HideAll();

        if (aimLineController != null)
            aimLineController.EndAim();

        CurrentMode = PlayerInputMode.None;
    }

    private void TryMoveUsingCurrentPointer()
    {
        if (playerClickMover == null)
            return;

        Vector2 pointerScreenPos = ReadPointerScreenPosition();

        if (moveRangeHighlighter != null)
        {
            if (moveRangeHighlighter.TryGetMoveOptionFromScreenPosition(pointerScreenPos, out Vector2Int targetGridPos))
                playerClickMover.TryMoveToGrid(targetGridPos);

            return;
        }

        playerClickMover.TryMoveFromScreenPosition(pointerScreenPos);
    }

    private Vector2 ReadPointerScreenPosition()
    {
        if (pointAction != null)
            return pointAction.ReadValue<Vector2>();

        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Vector2.zero;
    }

    private bool IsInventoryOpen()
    {
        return inventoryUIController != null && inventoryUIController.IsOpen;
    }

    /// <summary>
    /// 인벤토리가 열려 있으면 Combat gameplay 입력은 막는다.
    /// 현재 합의된 방향은 슬롯 1/2만 예외 허용이다.
    /// </summary>
    private bool IsInventoryBlockingGameplayInput()
    {
        return IsInventoryOpen();
    }

    /// <summary>
    /// 월드 포인터 입력 가능 여부.
    /// UI 위에 포인터가 올라간 경우 월드 입력 차단 옵션을 사용한다.
    /// </summary>
    private bool CanUseWorldPointer()
    {
        if (!blockWorldInputWhenPointerOverUI)
            return true;

        return !IsPointerOverUI();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        Vector2 pointerScreenPos = ReadPointerScreenPosition();

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = pointerScreenPos
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, uiRaycastResults);

        return uiRaycastResults.Count > 0;
    }

    public void UnbindInventory(InventoryUIController controller)
        {
            if (inventoryUIController == controller)
                inventoryUIController = null;
        }
}