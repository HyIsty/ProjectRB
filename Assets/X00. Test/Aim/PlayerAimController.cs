using UnityEngine;

/// <summary>
/// 플레이어의 조준 방향 계산과 회전만 담당하는 스크립트.
///
/// 핵심 구조:
/// - aimOrigin 기준으로 마우스 방향을 계산한다.
/// - rotateTarget을 그 방향으로 회전시킨다.
/// - 실제 총알 발사 위치는 firePoint를 사용한다.
/// 
/// 중요한 이유:
/// - firePoint는 rotateTarget의 자식일 가능성이 높다.
/// - firePoint 기준으로 조준 방향을 계산하면,
///   회전 → firePoint 위치 변경 → 방향 변경 → 다시 회전하는 떨림이 생길 수 있다.
/// </summary>
public class PlayerAimController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("조준 방향 계산 기준점. 보통 Player 본체 또는 GunPivot을 넣는다.")]
    [SerializeField] private Transform aimOrigin;

    [Tooltip("실제로 회전시킬 대상. 보통 GunPivot을 넣는다.")]
    [SerializeField] private Transform rotateTarget;

    [Tooltip("실제 발사 위치. 보통 GunPivot 자식 FirePoint를 넣는다.")]
    [SerializeField] private Transform firePoint;

    [Header("Rotation")]
    [SerializeField] private bool rotateVisual = true;

    [Tooltip("스프라이트 기본 바라보는 방향 보정값")]
    [SerializeField] private float angleOffset = 0f;

    [Header("Aim Stability")]
    [Tooltip("마우스가 조준 기준점에 너무 가까우면 이전 조준 방향을 유지한다.")]
    [SerializeField] private float minAimDistance = 0.25f;

    private Vector3 pointerWorldPosition;
    private Vector2 aimDirection = Vector2.right;

    /// <summary>
    /// 현재 포인터 월드 좌표.
    /// </summary>
    public Vector3 PointerWorldPosition => pointerWorldPosition;

    /// <summary>
    /// 현재 조준 방향.
    /// </summary>
    public Vector2 AimDirection => aimDirection;

    /// <summary>
    /// 발사 원점.
    /// firePoint가 있으면 firePoint 위치를 사용하고,
    /// 없으면 플레이어 위치를 사용한다.
    /// </summary>
    public Vector3 ShootOrigin
    {
        get
        {
            if (firePoint != null)
                return firePoint.position;

            return transform.position;
        }
    }

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // 조준 기준점이 비어 있으면 Player 본체를 기준으로 사용한다.
        if (aimOrigin == null)
            aimOrigin = transform;

        // 회전 대상이 비어 있으면 기존처럼 Player 본체를 회전한다.
        // 하지만 지금 구조에서는 GunPivot을 넣는 것이 좋다.
        if (rotateTarget == null)
            rotateTarget = transform;
    }

    /// <summary>
    /// 외부(PlayerInputManager)에서 전달한 포인터 스크린 좌표를 바탕으로
    /// 조준 방향과 회전을 갱신한다.
    /// </summary>
    public void TickAimFromScreenPosition(Vector2 screenPosition)
    {
        if (mainCamera == null)
            return;

        UpdatePointerWorldPosition(screenPosition);
        UpdateAimDirection();
        UpdateRotation();
    }

    /// <summary>
    /// 스크린 좌표를 월드 좌표로 변환한다.
    /// </summary>
    private void UpdatePointerWorldPosition(Vector2 screenPosition)
    {
        Vector3 screenPos = new Vector3(
            screenPosition.x,
            screenPosition.y,
            -mainCamera.transform.position.z
        );

        pointerWorldPosition = mainCamera.ScreenToWorldPoint(screenPos);
        pointerWorldPosition.z = 0f;
    }

    /// <summary>
    /// aimOrigin 기준으로 조준 방향을 계산한다.
    /// 
    /// firePoint 기준으로 계산하지 않는 이유:
    /// - firePoint는 총 회전에 따라 위치가 바뀐다.
    /// - 그 위치 변화가 다시 조준 각도에 영향을 줘서 떨림이 생긴다.
    /// </summary>
    private void UpdateAimDirection()
    {
        Vector2 originPosition = aimOrigin != null
            ? (Vector2)aimOrigin.position
            : (Vector2)transform.position;

        Vector2 pointerPosition = pointerWorldPosition;
        Vector2 rawDirection = pointerPosition - originPosition;

        float minDistanceSqr = minAimDistance * minAimDistance;

        // 마우스가 너무 가까우면 각도 계산이 불안정하므로 이전 방향 유지
        if (rawDirection.sqrMagnitude < minDistanceSqr)
            return;

        aimDirection = rawDirection.normalized;
    }

    /// <summary>
    /// 현재 조준 방향을 기준으로 rotateTarget을 회전시킨다.
    /// </summary>
    private void UpdateRotation()
    {
        if (!rotateVisual || rotateTarget == null)
            return;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        rotateTarget.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }

    /// <summary>
    /// 외부에서 발사 위치를 바꾸고 싶을 때 사용한다.
    /// 지금은 공통 FirePoint를 쓰고,
    /// 나중에 무기별 FirePoint 구조로 바꿀 때도 이 함수가 있으면 편하다.
    /// </summary>
    public void SetFirePoint(Transform newFirePoint)
    {
        firePoint = newFirePoint;
    }
}