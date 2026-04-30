using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 시야각(FOV)과 LOS를 검사해서
/// 어떤 적이 보이는지 / 안 보이는지 판정한다.
///
/// 현재 구현 규칙:
/// - 플레이어는 마우스 방향을 바라본다.
/// - 적이 FOV 각도 안에 있어야 한다.
/// - 플레이어와 적 사이에 장애물이 없어야 한다.
/// - 별도의 최대 시야 거리는 없다.
/// </summary>
public class PlayerFOVController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform visionOrigin;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("FOV Settings")]
    [SerializeField, Range(1f, 360f)] private float viewAngle = 90f;
    [SerializeField] private float enemyListRefreshInterval = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugLines = false;

    private readonly List<EnemyVisibilityController> enemyList = new List<EnemyVisibilityController>();
    private float refreshTimer;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        RefreshEnemyList();
        ForceRefreshNow();
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        // 일정 주기마다 적 목록 새로 읽기
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            RefreshEnemyList();
            refreshTimer = enemyListRefreshInterval;
        }

        Vector2 facingDirection = GetMouseFacingDirection();
        if (facingDirection.sqrMagnitude <= 0.0001f)
            return;

        UpdateEnemyVisibility(facingDirection);
    }

    /// <summary>
    /// 필요할 때 외부에서 강제로 가시성을 즉시 다시 계산하고 싶으면 호출한다.
    /// 예: 룸 생성 직후, 적 추가 스폰 직후 등.
    /// </summary>
    public void ForceRefreshNow()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Vector2 facingDirection = GetMouseFacingDirection();

        // 마우스가 정확히 원점과 겹치는 특이 케이스 방어
        if (facingDirection.sqrMagnitude <= 0.0001f)
            facingDirection = Vector2.right;

        UpdateEnemyVisibility(facingDirection);
    }

    /// <summary>
    /// 현재 씬 안의 EnemyVisibilityController들을 다시 수집한다.
    /// </summary>
    private void RefreshEnemyList()
    {
        enemyList.Clear();

        EnemyVisibilityController[] foundEnemies = FindObjectsByType<EnemyVisibilityController>(FindObjectsSortMode.None);

        for (int i = 0; i < foundEnemies.Length; i++)
        {
            if (foundEnemies[i] != null)
                enemyList.Add(foundEnemies[i]);
        }
    }

    /// <summary>
    /// 플레이어 시야 원점을 반환한다.
    /// visionOrigin이 없으면 플레이어 transform.position을 사용한다.
    /// </summary>
    private Vector2 GetVisionOrigin2D()
    {
        if (visionOrigin != null)
            return visionOrigin.position;

        return transform.position;
    }

    /// <summary>
    /// 마우스 방향을 월드 기준 2D 정규화 벡터로 계산한다.
    /// </summary>
    private Vector2 GetMouseFacingDirection()
    {
        Vector3 mouseScreenPosition = Input.mousePosition;

        // 2D 카메라에서도 월드 변환이 안정적으로 되도록 z를 카메라 거리로 맞춘다.
        mouseScreenPosition.z = Mathf.Abs(mainCamera.transform.position.z);

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        Vector2 origin = GetVisionOrigin2D();

        Vector2 direction = (Vector2)mouseWorldPosition - origin;
        return direction.normalized;
    }

    /// <summary>
    /// 모든 적에 대해:
    /// 1) 시야각 안에 있는지
    /// 2) 장애물에 막히지 않았는지
    /// 를 검사해서 보임/숨김을 결정한다.
    /// </summary>
    private void UpdateEnemyVisibility(Vector2 facingDirection)
    {
        Vector2 origin = GetVisionOrigin2D();
        float halfViewAngle = viewAngle * 0.5f;

        for (int i = 0; i < enemyList.Count; i++)
        {
            EnemyVisibilityController enemy = enemyList[i];

            if (enemy == null)
                continue;

            Vector2 target = enemy.GetVisibilityPoint2D();
            Vector2 toEnemy = target - origin;

            bool shouldBeVisible = false;

            // 적이 원점과 거의 겹치는 이상 케이스
            if (toEnemy.sqrMagnitude <= 0.0001f)
            {
                shouldBeVisible = true;
            }
            else
            {
                // 1. 시야각 검사
                float angleToEnemy = Vector2.Angle(facingDirection, toEnemy.normalized);

                if (angleToEnemy <= halfViewAngle)
                {
                    // 2. LOS 검사
                    // obstacleLayer만 검사하므로,
                    // 문서 기준대로 "장애물만 LOS를 막고 유닛은 막지 않음" 규칙과 맞춘다.
                    RaycastHit2D hit = Physics2D.Linecast(origin, target, obstacleLayer);
                    shouldBeVisible = hit.collider == null;
                }
            }

            enemy.SetVisible(shouldBeVisible);

            // 디버그 라인 표시
            if (drawDebugLines)
            {
                Color lineColor;

                if (shouldBeVisible)
                    lineColor = Color.green;
                else
                    lineColor = Color.red;

                Debug.DrawLine(origin, target, lineColor);
            }
        }
    }
}