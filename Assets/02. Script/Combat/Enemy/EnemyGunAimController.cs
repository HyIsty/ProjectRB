using UnityEngine;

/// <summary>
/// 적 유닛의 총기 피벗을 목표 방향으로 회전시키는 스크립트.
/// 
/// 현재 최소 구현:
/// - target이 있으면 target 방향으로 GunPivot을 계속 회전시킨다.
/// - 보통 target은 플레이어 Transform이다.
/// </summary>
public class EnemyGunAimController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform aimOrigin;
    [SerializeField] private Transform rotateTarget;
    [SerializeField] private Transform target;

    [Header("Rotation")]
    [SerializeField] private bool rotateVisual = true;
    [SerializeField] private float angleOffset = 0f;

    [Header("Stability")]
    [SerializeField] private float minAimDistance = 0.1f;

    private Vector2 aimDirection = Vector2.right;

    public Vector2 AimDirection => aimDirection;

    private void Awake()
    {
        if (aimOrigin == null)
            aimOrigin = transform;

        if (rotateTarget == null)
            rotateTarget = transform;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        AimAtWorldPosition(target.position);
    }

    /// <summary>
    /// 외부에서 조준 대상 플레이어를 연결할 때 사용한다.
    /// CombatManager나 EnemyAIController에서 호출하면 된다.
    /// </summary>
    public void BindTarget(Transform newTarget)
    {
        target = newTarget;
    }

    /// <summary>
    /// 특정 월드 위치를 향해 총기 피벗을 회전시킨다.
    /// </summary>
    public void AimAtWorldPosition(Vector3 worldPosition)
    {
        Vector2 originPosition = aimOrigin.position;
        Vector2 targetPosition = worldPosition;

        Vector2 rawDirection = targetPosition - originPosition;

        if (rawDirection.sqrMagnitude < minAimDistance * minAimDistance)
            return;

        aimDirection = rawDirection.normalized;

        if (!rotateVisual || rotateTarget == null)
            return;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        rotateTarget.rotation = Quaternion.Euler(0f, 0f, angle + angleOffset);
    }
}