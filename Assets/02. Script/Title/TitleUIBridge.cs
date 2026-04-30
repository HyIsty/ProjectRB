using UnityEngine;

public class TitleUIBridge : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string InGameSceneName = "InGameSc";

    private void Start()
    {
        // 활성 런이 없는데 InGame 씬에 들어온 경우 방어 처리
        if (GameSceneManager.Instance == null)
        {
            Debug.LogError("[TitleUIBridge] GameSceneManager.Instance is null.");
            return;
        }
    }

    public void OnClickRunStart()
    {
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadSceneAsyncByName(InGameSceneName);

        if (RunGameManager.Instance != null)
            RunGameManager.Instance.StartNewRunFromTitle();
    }
}
