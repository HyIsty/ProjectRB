using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 전역 사운드 재생을 담당하는 싱글톤 매니저.
/// 
/// 역할:
/// - BGM 재생 / 정지
/// - SFX 1회 재생
/// - 씬 이름에 따라 자동으로 BGM 교체
/// 
/// 주의:
/// - SceneManager는 씬 로딩용으로 쓰는 게 아니라,
///   현재 씬 이름 확인과 sceneLoaded 이벤트 감지만 한다.
/// - 실제 씬 로딩은 기존처럼 GameSceneManager를 계속 쓰면 된다.
/// </summary>
[DisallowMultipleComponent]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("AudioSource (Optional)")]
    [Tooltip("비어 있으면 Awake에서 자동 생성한다. BGM 전용 AudioSource.")]
    [SerializeField] private AudioSource bgmSource;

    [Tooltip("비어 있으면 Awake에서 자동 생성한다. SFX 전용 AudioSource.")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Scene BGM Clips")]
    [SerializeField] private AudioClip titleBgm;
    [SerializeField] private AudioClip inGameBgm;
    [SerializeField] private AudioClip combatBgm;
    [SerializeField] private AudioClip victoryBgm;
    [SerializeField] private AudioClip defeatBgm;

    [Header("BGM Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultBgmVolume = 0.7f;

    [SerializeField] private bool autoPlayBgmOnSceneLoaded = true;

    [Tooltip("알 수 없는 씬에 들어갔을 때 BGM을 끌지 여부.")]
    [SerializeField] private bool stopBgmOnUnknownScene = false;

    [Header("UI SFX")]
    [SerializeField] private AudioClip uiClickSfx;
    [SerializeField] private AudioClip uiHoverSfx;
    [Range(0f, 1f)]
    [SerializeField] private float uiClickVolume = 0.65f;
    [Range(0f, 1f)]
    [SerializeField] private float uiHoverVolume = 0.3f;

    [Header("Combat SFX")]
    [SerializeField] private AudioClip unitMoveSfx;

    [Range(0f, 1f)]
    [SerializeField] private float unitMoveVolume = 0.55f;

    [Header("Reload / Ammo SFX")]
    [SerializeField] private AudioClip emptyFireSfx;
    [SerializeField] private AudioClip reloadSuccessSfx;

    [Range(0f, 1f)]
    [SerializeField] private float emptyFireVolume = 0.65f;
    [Range(0f, 1f)]
    [SerializeField] private float reloadSuccessVolume = 0.75f;

    [Header("Gun SFX")]
    [SerializeField] private AudioClip pistolShotSfx;
    [SerializeField] private AudioClip shotgunShotSfx;
    [SerializeField] private AudioClip rifleShotSfx;
    [SerializeField] private AudioClip sniperShotSfx;

    [Header("Gun SFX Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float pistolShotVolume = 0.75f;
    [Range(0f, 1f)]
    [SerializeField] private float shotgunShotVolume = 0.85f;
    [Range(0f, 1f)]
    [SerializeField] private float rifleShotVolume = 0.75f;
    [Range(0f, 1f)]
    [SerializeField] private float sniperShotVolume = 0.9f;

    [Header("Hit / Death SFX")]
    [SerializeField] private AudioClip unitHitSfx;
    [SerializeField] private AudioClip unitDeathSfx;
    [SerializeField] private AudioClip wallHitSfx;

    [Range(0f, 1f)]
    [SerializeField] private float unitHitVolume = 0.75f;
    [Range(0f, 1f)]
    [SerializeField] private float unitDeathVolume = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float wallHitVolume = 0.75f;

    [Header("Turn SFX")]
    [SerializeField] private AudioClip turnStartSfx;
    [SerializeField] private AudioClip turnEndSfx;

    [Range(0f, 1f)]
    [SerializeField] private float turnStartVolume = 0.55f;
    [Range(0f, 1f)]
    [SerializeField] private float turnEndVolume = 0.55f;

    [Header("Shop SFX")]
    [SerializeField] private AudioClip shopBuySfx;

    [Range(0f, 1f)]
    [SerializeField] private float shopBuyVolume = 0.7f;
    [Header("Attachment SFX")]
    [SerializeField] private AudioClip attachmentEquipSfx;

    [Range(0f, 1f)]
    [SerializeField] private float attachmentEquipVolume = 0.7f;

    [Header("BGM Fade")]
    [SerializeField] private float bgmFadeDuration = 0.8f;

    private Coroutine bgmFadeCoroutine;

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

        EnsureAudioSources();

        // 중복 등록 방지 후 씬 로드 이벤트 등록
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        // 게임 첫 씬은 sceneLoaded 이벤트가 이미 지나간 뒤일 수 있으므로
        // Start에서 현재 씬 BGM을 한 번 직접 적용한다.
        if (autoPlayBgmOnSceneLoaded)
            PlayBgmForSceneName(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            Instance = null;
        }
    }

    /// <summary>
    /// 씬이 로드될 때 자동으로 호출된다.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoPlayBgmOnSceneLoaded)
            return;

        PlayBgmForSceneName(scene.name);
    }

    /// <summary>
    /// 씬 이름을 기준으로 맞는 BGM을 재생한다.
    /// </summary>
    public void PlayBgmForSceneName(string sceneName)
    {
        AudioClip selectedClip = GetBgmClipForScene(sceneName);

        if (selectedClip == null)
        {
            if (stopBgmOnUnknownScene)
                StopBgm();

            Debug.Log($"[SoundManager] No BGM assigned for scene: {sceneName}");
            return;
        }

        PlayBgm(selectedClip, defaultBgmVolume, true);
    }

    /// <summary>
    /// 씬 이름에 맞는 BGM 클립을 반환한다.
    /// 프로젝트 씬 이름 규칙에 맞춰서 여기만 관리하면 된다.
    /// </summary>
    private AudioClip GetBgmClipForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "TitleSc":
                return titleBgm;

            case "InGameSc":
                return inGameBgm;

            case "CombatSc":
                return combatBgm;

            case "VictorySc":
                return victoryBgm;

            case "DefeatSc":
                return defeatBgm;

            default:
                return null;
        }
    }
    /// <summary>
    /// BGM을 재생한다.
    /// 기존 BGM이 있으면 페이드 아웃 후 새 BGM으로 교체하고 페이드 인한다.
    /// </summary>
    public void PlayBgm(AudioClip bgmClip, float volume = 1f, bool loop = true)
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("[SoundManager] PlayBgm 실패: bgmClip이 null입니다.");
            return;
        }

        if (bgmSource == null)
        {
            Debug.LogWarning("[SoundManager] PlayBgm 실패: bgmSource가 없습니다.");
            return;
        }

        // 같은 BGM이 이미 재생 중이면 다시 시작하지 않고 볼륨만 맞춘다.
        if (bgmSource.clip == bgmClip && bgmSource.isPlaying)
        {
            bgmSource.loop = loop;
            bgmSource.volume = Mathf.Clamp01(volume);
            return;
        }

        // 기존 페이드가 돌고 있으면 중단한다.
        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmFadeCoroutine = StartCoroutine(FadeToBgm(bgmClip, Mathf.Clamp01(volume), loop));
    }

    /// <summary>
    /// 현재 재생 중인 BGM을 페이드 아웃 후 정지한다.
    /// </summary>
    public void StopBgm()
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("[SoundManager] StopBgm 실패: bgmSource가 없습니다.");
            return;
        }

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);

        bgmFadeCoroutine = StartCoroutine(FadeOutAndStopBgm());
    }

    /// <summary>
    /// BGM 볼륨을 0까지 줄인 뒤 정지한다.
    /// </summary>
    private IEnumerator FadeOutAndStopBgm()
    {
        float duration = Mathf.Max(0.01f, bgmFadeDuration);
        float startVolume = bgmSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;

            bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);

            yield return null;
        }

        bgmSource.Stop();
        bgmSource.clip = null;
        bgmSource.volume = startVolume;

        bgmFadeCoroutine = null;
    }

    /// <summary>
    /// 효과음을 1회 재생한다.
    /// </summary>
    public void PlaySfxOneShot(AudioClip sfxClip, float volumeScale = 1f)
    {
        if (sfxClip == null)
        {
            Debug.LogWarning("[SoundManager] PlaySfxOneShot failed: sfxClip is null.");
            return;
        }

        if (sfxSource == null)
        {
            Debug.LogWarning("[SoundManager] PlaySfxOneShot failed: sfxSource is missing.");
            return;
        }

        sfxSource.PlayOneShot(sfxClip, Mathf.Clamp01(volumeScale));
    }

    /// <summary>
    /// AudioSource가 비어 있으면 자동 생성한다.
    /// </summary>
    private void EnsureAudioSources()
    {
        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    /// <summary>
    /// 공통 UI 클릭 사운드를 재생한다.
    /// 모든 버튼 클릭에 사용한다.
    /// </summary>
    public void PlayUiClick()
    {
        PlaySfxOneShot(uiClickSfx, uiClickVolume);
    }

    /// <summary>
    /// 공통 UI 호버 사운드를 재생한다.
    /// 마우스가 버튼 위에 올라갔을 때 사용한다.
    /// </summary>
    public void PlayUiHover()
    {
        PlaySfxOneShot(uiHoverSfx, uiHoverVolume);
    }

    /// <summary>
    /// 유닛이 실제로 이동에 성공했을 때 재생한다.
    /// 플레이어와 적 이동에 공통으로 사용한다.
    /// </summary>
    public void PlayUnitMove()
    {
        PlaySfxOneShot(unitMoveSfx, unitMoveVolume);
    }

    /// <summary>
    /// 탄이 없는 상태에서 사격을 시도했을 때 재생한다.
    /// </summary>
    public void PlayEmptyFire()
    {
        PlaySfxOneShot(emptyFireSfx, emptyFireVolume);
    }

    /// <summary>
    /// 실제로 한 발 이상 장전에 성공했을 때 재생한다.
    /// </summary>
    public void PlayReloadSuccess()
    {
        PlaySfxOneShot(reloadSuccessSfx, reloadSuccessVolume);
    }

    /// <summary>
    /// 권총 사격음을 재생한다.
    /// </summary>
    public void PlayPistolShot()
    {
        PlaySfxOneShot(pistolShotSfx, pistolShotVolume);
    }

    /// <summary>
    /// 샷건 사격음을 재생한다.
    /// </summary>
    public void PlayShotgunShot()
    {
        PlaySfxOneShot(shotgunShotSfx, shotgunShotVolume);
    }

    /// <summary>
    /// 라이플 단발 사격음을 재생한다.
    /// 라이플 공격이 3발이면 이 함수를 3번 호출하면 된다.
    /// </summary>
    public void PlayRifleShot()
    {
        PlaySfxOneShot(rifleShotSfx, rifleShotVolume);
    }

    /// <summary>
    /// 저격총 사격음을 재생한다.
    /// </summary>
    public void PlaySniperShot()
    {
        PlaySfxOneShot(sniperShotSfx, sniperShotVolume);
    }

    /// <summary>
    /// 유닛이 피해를 받았지만 아직 살아있을 때 재생한다.
    /// </summary>
    public void PlayUnitHit()
    {
        PlaySfxOneShot(unitHitSfx, unitHitVolume);
    }

    /// <summary>
    /// 유닛이 사망했을 때 재생한다.
    /// </summary>
    public void PlayUnitDeath()
    {
        PlaySfxOneShot(unitDeathSfx, unitDeathVolume);
    }

    /// <summary>
    /// 총알이 벽 / 엄폐물 / 장애물에 막혔을 때 재생한다.
    /// </summary>
    public void PlayWallHit()
    {
        PlaySfxOneShot(wallHitSfx, wallHitVolume);
    }

    /// <summary>
    /// 플레이어 턴이 실제로 시작됐을 때 재생한다.
    /// </summary>
    public void PlayTurnStart()
    {
        PlaySfxOneShot(turnStartSfx, turnStartVolume);
    }

    /// <summary>
    /// 플레이어 턴이 실제로 종료됐을 때 재생한다.
    /// </summary>
    public void PlayTurnEnd()
    {
        PlaySfxOneShot(turnEndSfx, turnEndVolume);
    }
    /// <summary>
    /// 상점에서 실제 구매가 성공했을 때 재생한다.
    /// </summary>
    public void PlayShopBuy()
    {
        PlaySfxOneShot(shopBuySfx, shopBuyVolume);
    }

    /// <summary>
    /// 현재 BGM을 페이드 아웃한 뒤 새 BGM으로 교체하고 페이드 인한다.
    /// Time.unscaledDeltaTime을 써서 게임 시간이 멈춰도 UI/씬 전환 BGM 페이드는 동작한다.
    /// </summary>
    private IEnumerator FadeToBgm(AudioClip newClip, float targetVolume, bool loop)
    {
        float duration = Mathf.Max(0.01f, bgmFadeDuration);

        // 1. 기존 BGM 페이드 아웃
        if (bgmSource.isPlaying && bgmSource.clip != null)
        {
            float startVolume = bgmSource.volume;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = timer / duration;

                bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);

                yield return null;
            }
        }

        // 2. 새 BGM으로 교체
        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.loop = loop;
        bgmSource.volume = 0f;
        bgmSource.Play();

        // 3. 새 BGM 페이드 인
        float fadeInTimer = 0f;

        while (fadeInTimer < duration)
        {
            fadeInTimer += Time.unscaledDeltaTime;
            float t = fadeInTimer / duration;

            bgmSource.volume = Mathf.Lerp(0f, targetVolume, t);

            yield return null;
        }

        bgmSource.volume = targetVolume;
        bgmFadeCoroutine = null;
    }

    /// <summary>
    /// 부착물이 실제로 장착됐을 때 재생한다.
    /// </summary>
    public void PlayAttachmentEquip()
    {
        PlaySfxOneShot(attachmentEquipSfx, attachmentEquipVolume);
    }
}