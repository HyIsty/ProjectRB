using UnityEngine;

/// <summary>
/// 런타임에 생성되는 플레이어를 자동으로 찾아서 따라가는 카메라.
/// 
/// 목적:
/// - 플레이어가 씬 시작부터 없어도 동작
/// - 방 생성 후 플레이어가 스폰되면 자동으로 타겟 획득
/// - 플레이어가 파괴되고 다시 생성돼도 다시 찾음
/// - 현재 최소 구현에서는 플레이어를 항상 화면 중앙에 둠
/// 
/// 사용 방법:
/// 1. Main Camera에 이 스크립트를 붙인다.
/// 2. Player 프리팹의 Tag를 "Player"로 설정한다.
/// 3. 플레이어가 런타임에 생성되면 카메라가 자동으로 따라간다.
/// </summary>
public class RuntimePlayerCameraFollow : MonoBehaviour
{
    [Header("Target Search")]
    [Tooltip("찾을 대상의 태그. Player 프리팹에 같은 태그를 지정해야 한다.")]
    [SerializeField] private string targetTag = "Player";

    [Tooltip("타겟이 없을 때 다시 찾는 간격(초)")]
    [SerializeField] private float searchInterval = 0.2f;

    [Header("Follow Settings")]
    [Tooltip("카메라가 플레이어 기준으로 유지할 오프셋. 2D에서는 보통 z = -10 사용.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("true면 즉시 고정, false면 약간 부드럽게 따라감")]
    [SerializeField] private bool snapToTarget = true;

    [Tooltip("부드럽게 따라갈 때의 속도")]
    [SerializeField] private float followSpeed = 15f;

    // 현재 추적 중인 타겟
    private Transform target;

    // 다음 탐색 시각
    private float nextSearchTime;

    private void Start()
    {
        // 시작 시점에 이미 플레이어가 있을 수도 있으니 한 번 찾는다.
        TryFindTarget(forceSearch: true);

        // 찾았다면 시작하자마자 위치를 맞춘다.
        if (target != null)
            SnapCameraToTarget();
    }

    private void LateUpdate()
    {
        // 타겟이 없거나, 이전 플레이어가 파괴되었으면 다시 찾는다.
        if (target == null)
        {
            TryFindTarget(forceSearch: false);

            // 아직도 없으면 이번 프레임은 종료
            if (target == null)
                return;

            // 새 타겟을 찾은 프레임에는 바로 카메라를 맞춰서 화면이 덜 튄다.
            SnapCameraToTarget();
            return;
        }

        // 타겟이 있으면 따라간다.
        FollowTarget();
    }

    /// <summary>
    /// targetTag를 가진 오브젝트를 찾아 타겟으로 등록한다.
    /// 매 프레임 Find를 돌리면 비효율적이므로 searchInterval 간격으로만 시도한다.
    /// </summary>
    private void TryFindTarget(bool forceSearch)
    {
        if (!forceSearch && Time.time < nextSearchTime)
            return;

        nextSearchTime = Time.time + searchInterval;

        GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);

        if (targetObject != null)
            target = targetObject.transform;
    }

    /// <summary>
    /// 현재 타겟 위치로 카메라를 즉시 이동시킨다.
    /// 플레이어가 방 생성 직후 튀는 느낌을 줄이기 위해 사용한다.
    /// </summary>
    private void SnapCameraToTarget()
    {
        if (target == null)
            return;

        transform.position = target.position + offset;
    }

    /// <summary>
    /// 현재 타겟을 따라간다.
    /// snapToTarget이 true면 즉시 고정,
    /// false면 Lerp로 약간 부드럽게 따라간다.
    /// </summary>
    private void FollowTarget()
    {
        Vector3 desiredPosition = target.position + offset;

        if (snapToTarget)
        {
            transform.position = desiredPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position,
                desiredPosition,
                followSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// 나중에 다른 시스템(BoardManager, RoomManager 등)에서
    /// 플레이어 생성 직후 직접 카메라 타겟을 넣고 싶을 때 사용할 수 있다.
    /// 지금 최소 구현에서는 없어도 되지만 확장용으로 열어둔다.
    /// </summary>
    public void SetTarget(Transform newTarget, bool snapImmediately = true)
    {
        target = newTarget;

        if (snapImmediately && target != null)
            SnapCameraToTarget();
    }

    /// <summary>
    /// 현재 타겟을 해제한다.
    /// 방 전환 등으로 플레이어가 사라지는 구조에서 필요하면 사용할 수 있다.
    /// </summary>
    public void ClearTarget()
    {
        target = null;
    }
}