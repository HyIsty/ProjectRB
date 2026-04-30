using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton Pattern으로 씬 전환을 한 곳에서 관리한다.
/// Unity 기본 SceneManager와 이름 충돌을 피하기 위해 클래스명은 GameSceneManager를 사용한다.
/// </summary>
[DisallowMultipleComponent]
public class GameSceneManager : MonoBehaviour
{
    /// <summary>
    /// 다른 스크립트에서 GameSceneManager.Instance로 접근한다.
    /// 예) GameSceneManager.Instance.LoadSceneByName("Inven_Test");
    /// </summary>
    public static GameSceneManager Instance { get; private set; }

    private void Awake()
    {
        // [Singleton 1단계] 중복 인스턴스 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // [Singleton 2단계] 전역 참조 등록
        Instance = this;

        // [Singleton 3단계] 씬 변경 후에도 유지
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// sceneName으로 즉시 씬을 로드한다.
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        if (!IsValidSceneName(sceneName))
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[GameSceneManager] LoadSceneByName 실패: Build Settings에 없는 씬입니다. sceneName={sceneName}");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 현재 활성 씬을 다시 로드한다.
    /// </summary>
    public void ReloadCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            Debug.LogWarning("[GameSceneManager] ReloadCurrentScene 실패: 현재 활성 씬이 유효하지 않습니다.");
            return;
        }

        LoadSceneByName(activeScene.name);
    }

    /// <summary>
    /// sceneName으로 비동기 로드를 시작하고 AsyncOperation을 반환한다.
    /// 호출 측에서 completed 콜백 연결이나 progress 표시를 구현할 수 있다.
    /// </summary>
    public AsyncOperation LoadSceneAsyncByName(string sceneName, LoadSceneMode loadSceneMode = LoadSceneMode.Single)
    {
        if (!IsValidSceneName(sceneName))
        {
            return null;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"[GameSceneManager] LoadSceneAsyncByName 실패: Build Settings에 없는 씬입니다. sceneName={sceneName}");
            return null;
        }

        return SceneManager.LoadSceneAsync(sceneName, loadSceneMode);
    }

    /// <summary>
    /// sceneName이 비어 있지 않은지 검사한다.
    /// </summary>
    private bool IsValidSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[GameSceneManager] sceneName이 비어 있습니다.");
            return false;
        }

        return true;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
}


