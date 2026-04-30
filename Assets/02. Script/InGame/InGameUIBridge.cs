using UnityEngine;

/// <summary>
/// InGame 씬의 버튼 브릿지.
/// 버튼은 이 씬 안의 이 컴포넌트를 호출하고,
/// 이 컴포넌트가 내부에서 RunGameManager / GameSceneManager 싱글톤을 호출한다.
/// </summary>
public class InGameUIBridge : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "Title";
    [SerializeField] private string combatSceneName = "Combat";

    [Header("Optional UI")]
    [SerializeField] private GameObject rewardPanel;

    [SerializeField] private InventoryUIController inventoryUIController;


    private void Start()
    {
        // 활성 런이 없는데 InGame 씬에 들어온 경우 방어 처리
        if (RunGameManager.Instance == null)
        {
            Debug.LogError("[InGameUIBridge] RunGameManager.Instance is null.");
            return;
        }

        if (!RunGameManager.Instance.HasActiveRun)
        {
            Debug.LogWarning("[InGameUIBridge] No active run. Returning to title.");
            LoadTitleScene();
            return;
        }

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        if (inventoryUIController == null)
            return;

        PlayerInputManager.Instance.SetSceneMode(PlayerInputManager.InputSceneMode.InGame);
        inventoryUIController.BindRunData(RunGameManager.Instance.CurrentRunData);
        PushToBottomLayer();
    }

    /// <summary>
    /// 전투 진입 버튼용 함수.
    /// </summary>
    public void OnClickEnterCombat()
    {
        if (RunGameManager.Instance == null || !RunGameManager.Instance.HasActiveRun)
        {
            Debug.LogWarning("[InGameUIBridge] Cannot enter combat. No active run.");
            return;
        }

        LoadCombatScene();
    }

    /// <summary>
    /// 타이틀 복귀 버튼용 함수.
    /// 현재 런을 종료하고 타이틀로 이동한다.
    /// </summary>
    public void OnClickReturnToTitle()
    {
        if (RunGameManager.Instance != null)
            RunGameManager.Instance.EndRun();

        LoadTitleScene();
    }

    /// <summary>
    /// Combat 씬 로드.
    /// </summary>
    private void LoadCombatScene()
    {
        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("[InGameUIBridge] GameSceneManager.Instance is null.");
            return;
        }
        GameSceneManager.Instance.LoadSceneAsyncByName(combatSceneName);
    }

    /// <summary>
    /// Title 씬 로드.
    /// </summary>
    private void LoadTitleScene()
    {
        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("[InGameUIBridge] GameSceneManager.Instance is null.");
            return;
        }

        GameSceneManager.Instance.LoadSceneAsyncByName(titleSceneName);
    }
    private void PushToBottomLayer()
    {
            this.GetComponent<RectTransform>().SetAsFirstSibling();
    }

}