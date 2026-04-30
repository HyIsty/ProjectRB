using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI Button에 공통 hover / click 사운드를 붙이는 스크립트.
///
/// 사용법:
/// - Button 컴포넌트가 있는 GameObject에 붙인다.
/// - 사운드 클립은 이 스크립트가 아니라 SoundManager에서 관리한다.
/// - 이 스크립트는 버튼 이벤트가 발생하면 SoundManager를 호출하기만 한다.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonSoundBinder : MonoBehaviour, IPointerEnterHandler
{
    private Button button;

    private void Awake()
    {
        // 같은 GameObject에 붙어있는 Button 컴포넌트를 가져온다.
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        // 버튼 클릭 이벤트에 클릭 사운드를 연결한다.
        if (button != null)
            button.onClick.AddListener(PlayClickSound);
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼질 때 이벤트 연결을 해제한다.
        // 이걸 안 하면 켜졌다 꺼질 때 중복 등록될 수 있다.
        if (button != null)
            button.onClick.RemoveListener(PlayClickSound);
    }

    /// <summary>
    /// 마우스가 버튼 위에 올라왔을 때 호출된다.
    /// EventSystem이 자동으로 호출해준다.
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button == null)
            return;

        // 비활성화된 버튼에는 호버 사운드를 내지 않는다.
        if (!button.interactable)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayUiHover();
    }

    /// <summary>
    /// 버튼 클릭 시 호출된다.
    /// </summary>
    private void PlayClickSound()
    {
        if (button == null)
            return;

        if (!button.interactable)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayUiClick();
    }
}