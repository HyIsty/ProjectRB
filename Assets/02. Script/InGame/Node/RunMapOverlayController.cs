using UnityEngine;

/// <summary>
/// 런 맵 오버레이의 씬별 정책 관리자.
/// 입력은 PlayerInputManager가 받고,
/// 이 컨트롤러는 PlayerInputManager의 MapToggleRequested 이벤트를 구독한다.
///
/// 현재 프로젝트 기준:
/// - 씬 이름은 모두 Sc 접미사 사용
/// - 현재 씬 이름 조회는 GameSceneManager.Instance.GetCurrentSceneName() 사용
/// </summary>
public class RunMapOverlayController : MonoBehaviour
{
    private enum SceneMapPolicy
    {
        Disabled,
        AlwaysVisibleClickable,
        ToggleClickable,
        ToggleReadOnly
    }

    [Header("Refs")]
    [SerializeField] private RunMapOverlayUI runMapOverlayUI;

    [Header("Scene Names")]
    [SerializeField] private string inGameSceneName = "InGameSc";
    [SerializeField] private string combatSceneName = "CombatSc";
    [SerializeField] private string titleSceneName = "TitleSc";
    [SerializeField] private string victorySceneName = "VictorySc";
    [SerializeField] private string defeatSceneName = "DefeatSc";

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private PlayerInputManager cachedInputManager;

    private void OnEnable()
    {
        TrySubscribeToInputManager();
    }

    private void Start()
    {
        RefreshNow();
    }

    private void OnDisable()
    {
        UnsubscribeFromInputManager();
    }

    private void TrySubscribeToInputManager()
    {
        if (PlayerInputManager.Instance == null)
        {
            if (debugLog)
                Debug.LogWarning("[RunMapOverlayController] PlayerInputManager.Instance is null.");
            return;
        }

        cachedInputManager = PlayerInputManager.Instance;
        cachedInputManager.MapToggleRequested -= HandleMapToggleRequested;
        cachedInputManager.MapToggleRequested += HandleMapToggleRequested;

        if (debugLog)
            Debug.Log("[RunMapOverlayController] Subscribed to PlayerInputManager.MapToggleRequested");
    }

    private void UnsubscribeFromInputManager()
    {
        if (cachedInputManager == null)
            return;

        cachedInputManager.MapToggleRequested -= HandleMapToggleRequested;
        cachedInputManager = null;

        if (debugLog)
            Debug.Log("[RunMapOverlayController] Unsubscribed from PlayerInputManager.MapToggleRequested");
    }

    public void RefreshNow()
    {
        if (runMapOverlayUI == null)
        {
            Debug.LogError("[RunMapOverlayController] RunMapOverlayUI is missing.");
            return;
        }

        if (RunGameManager.Instance != null && RunGameManager.Instance.HasActiveRun)
        {
            runMapOverlayUI.BindRunData(RunGameManager.Instance.CurrentRunData);
        }

        ApplyScenePolicy();
    }

    /// <summary>
    /// PlayerInputManager의 MapToggleRequested 이벤트를 받아 처리.
    /// </summary>
    public void HandleMapToggleRequested()
    {
        if (runMapOverlayUI == null)
            return;

        SceneMapPolicy policy = GetCurrentScenePolicy();

        switch (policy)
        {
            case SceneMapPolicy.AlwaysVisibleClickable:
                // InGame은 항상 보이는 정책
                if (debugLog)
                    Debug.Log("[RunMapOverlayController] Map toggle ignored in InGame.");
                return;

            case SceneMapPolicy.ToggleClickable:
                Toggle(RunMapViewMode.OverlayClickable);
                return;

            case SceneMapPolicy.ToggleReadOnly:
                Toggle(RunMapViewMode.OverlayReadOnly);
                return;

            case SceneMapPolicy.Disabled:
            default:
                if (debugLog)
                    Debug.Log("[RunMapOverlayController] Map toggle ignored in disabled scene.");
                return;
        }
    }

    private void ApplyScenePolicy()
    {
        if (runMapOverlayUI == null)
            return;

        SceneMapPolicy policy = GetCurrentScenePolicy();

        switch (policy)
        {
            case SceneMapPolicy.AlwaysVisibleClickable:
                runMapOverlayUI.SetMode(RunMapViewMode.InGamePersistentClickable);
                break;

            case SceneMapPolicy.ToggleClickable:
            case SceneMapPolicy.ToggleReadOnly:
                // Shop / Combat는 시작 시 숨김
                runMapOverlayUI.SetMode(RunMapViewMode.Hidden);
                break;

            case SceneMapPolicy.Disabled:
            default:
                runMapOverlayUI.SetMode(RunMapViewMode.Hidden);
                break;
        }

        if (debugLog)
            Debug.Log($"[RunMapOverlayController] ApplyScenePolicy -> {policy}");
    }

    private void Toggle(RunMapViewMode showMode)
    {
        if (runMapOverlayUI.CurrentMode == RunMapViewMode.Hidden)
        {
            if (RunGameManager.Instance != null && RunGameManager.Instance.HasActiveRun)
            {
                runMapOverlayUI.BindRunData(RunGameManager.Instance.CurrentRunData);
            }

            runMapOverlayUI.SetMode(showMode);
        }
        else
        {
            runMapOverlayUI.SetMode(RunMapViewMode.Hidden);
        }

        if (debugLog)
            Debug.Log($"[RunMapOverlayController] Toggle -> {runMapOverlayUI.CurrentMode}");
    }

    private SceneMapPolicy GetCurrentScenePolicy()
    {
        string sceneName = GetCurrentSceneNameSafe();

        if (sceneName == inGameSceneName)
            return SceneMapPolicy.AlwaysVisibleClickable;

        if (sceneName == combatSceneName)
            return SceneMapPolicy.ToggleReadOnly;

        if (sceneName == titleSceneName || sceneName == victorySceneName || sceneName == defeatSceneName)
            return SceneMapPolicy.Disabled;

        return SceneMapPolicy.Disabled;
    }

    private string GetCurrentSceneNameSafe()
    {
        if (GameSceneManager.Instance == null)
        {
            Debug.LogWarning("[RunMapOverlayController] GameSceneManager.Instance is null.");
            return string.Empty;
        }

        return GameSceneManager.Instance.GetCurrentSceneName();
    }
}