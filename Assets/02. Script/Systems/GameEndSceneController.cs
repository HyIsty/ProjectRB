using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VictorySc / DefeatSc 공통 종료 화면 컨트롤러.
/// 텍스트 없이 버튼 하나로 TitleSc로 돌아간다.
/// </summary>
public class GameEndSceneController : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button titleButton;

    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "TitleSc";

    private void Awake()
    {
        if (titleButton != null)
        {
            titleButton.onClick.RemoveAllListeners();
            titleButton.onClick.AddListener(GoToTitle);
        }
    }

    public void GoToTitle()
    {
        // 혹시 active run이 남아 있으면 정리한다.
        if (RunGameManager.Instance != null && RunGameManager.Instance.HasActiveRun)
            RunGameManager.Instance.EndRun();

        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("[GameEndSceneController] GameSceneManager is missing.");
            return;
        }

        GameSceneManager.Instance.LoadSceneAsyncByName(titleSceneName);
    }
}