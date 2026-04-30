using System.Collections;
using UnityEngine;

/// <summary>
/// Rest 노드에서 회복했을 때 잠깐 보여주는 화면 플래시 UI.
/// GameObject는 끄지 않고, CanvasGroup alpha만 조절한다.
/// </summary>
public class RestEffectUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing")]
    [SerializeField] private float fadeInTime = 0.12f;
    [SerializeField] private float holdTime = 0.18f;
    [SerializeField] private float fadeOutTime = 0.35f;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        HideImmediate();
    }

    public void Play()
    {
        // 이 오브젝트는 비활성화하면 안 된다.
        // Coroutine은 active GameObject에서만 정상 실행된다.
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        yield return Fade(0f, 1f, fadeInTime);
        yield return new WaitForSecondsRealtime(holdTime);
        yield return Fade(1f, 0f, fadeOutTime);

        playRoutine = null;
        HideImmediate();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);

            canvasGroup.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        canvasGroup.alpha = to;
    }

    private void HideImmediate()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}