using UnityEngine;

/// <summary>
/// 적의 보임 / 숨김 상태를 제어하는 스크립트.
/// 완전히 숨길 때는 시각 요소(Renderer, Canvas)와
/// 타겟팅용 Collider2D를 함께 꺼서,
/// 플레이어가 보이지 않는 적을 클릭/호버하지 못하게 한다.
/// </summary>
public class EnemyVisibilityController : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Transform visibilityPoint;
    [Header("Optional Hover / Target Collider")]
    [SerializeField] private Collider2D targetCollider;

    [Header("Optional Hit Collider (Always On)")]
    [SerializeField] private Collider2D hitCollider;

    private Renderer[] cachedRenderers; 
    private Canvas[] cachedCanvases;

    /// <summary>
    /// 현재 적이 플레이어에게 보이는 상태인지.
    /// </summary>
    public bool IsVisible { get; private set; }

    private void Awake()
    {
        // visualRoot를 따로 지정하지 않으면 자기 자신 기준으로 찾는다.
        if (visualRoot == null)
            visualRoot = transform;

        cachedRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        cachedCanvases = visualRoot.GetComponentsInChildren<Canvas>(true);

        if (hitCollider != null)
            hitCollider.enabled = true;
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;

        // 월드 스페이스 HP UI 같은 Canvas도 같이 On/Off
        for (int i = 0; i < cachedCanvases.Length; i++)
        {
            if (cachedCanvases[i] != null)
                cachedCanvases[i].enabled = visible;
        }

        // 타겟팅/호버용 콜라이더가 있다면 같이 On/Off
        // hover / 클릭 선택용 Collider는 visible일 때만 활성화
        if (targetCollider != null)
            targetCollider.enabled = visible;

        // 사격 판정용 Collider는 절대 끄지 않는다.
        if (hitCollider != null)
            hitCollider.enabled = true;
    }

    /// <summary>
    /// FOV 각도 및 LOS 검사에 사용할 적의 기준 위치를 반환한다.
    /// visibilityPoint가 지정되어 있으면 그 위치를 쓰고,
    /// 없으면 적의 transform.position을 사용한다.
    /// </summary>
    public Vector2 GetVisibilityPoint2D()
    {
        if (visibilityPoint != null)
            return visibilityPoint.position;

        return transform.position;
    }
}