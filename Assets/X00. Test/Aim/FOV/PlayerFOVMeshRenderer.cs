using UnityEngine;

/// <summary>
/// 플레이어 기준으로 FOV(Fan-shaped) 메쉬를 매 프레임 생성한다.
/// 
/// 현재 설계 목적:
/// - 플레이어는 마우스 방향을 바라본다.
/// - 시야각 범위 안으로 여러 개의 raycast를 쏜다.
/// - 장애물에 닿으면 그 지점에서 시야가 끊긴다.
/// - 그렇게 얻은 점들로 FOV 메쉬를 만든다.
/// 
/// 중요:
/// - 이 스크립트는 "시각화용" FOV 메쉬 생성기다.
/// - 사격 가능 여부를 강제하지 않는다.
/// - 사격은 나중에 별도 로직으로 계속 분리할 수 있다.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlayerFOVMeshRenderer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform viewOrigin;

    [Header("FOV Settings")]
    [SerializeField, Range(1f, 179f)] private float viewAngle = 90f;
    [SerializeField] private float visualViewDistance = 10f;
    [SerializeField, Min(3)] private int rayCount = 120;

    [Header("Obstacle")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRays = false;

    private MeshFilter meshFilter;
    private Mesh fovMesh;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (viewOrigin == null)
            viewOrigin = transform.parent != null ? transform.parent : transform;

        // 런타임에서 계속 갱신될 메쉬를 생성한다.
        fovMesh = new Mesh();
        fovMesh.name = "PlayerFOVMesh";
        fovMesh.MarkDynamic();

        meshFilter.sharedMesh = fovMesh;
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null || viewOrigin == null)
            return;

        RebuildMesh();
    }

    /// <summary>
    /// 현재 마우스 방향과 장애물 충돌 결과를 바탕으로 FOV 메쉬를 다시 만든다.
    /// </summary>
    public void RebuildMesh()
    {
        Vector2 origin = viewOrigin.position;
        Vector2 facingDirection = GetMouseFacingDirection(origin);

        // 마우스 방향 계산이 실패할 정도로 가까우면 기본 방향을 사용한다.
        if (facingDirection.sqrMagnitude <= 0.0001f)
            facingDirection = Vector2.right;

        float centerAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
        float startAngle = centerAngle - (viewAngle * 0.5f);
        float angleStep = viewAngle / rayCount;

        // 꼭짓점 1개 + 각 ray 끝점(rayCount + 1개)
        Vector3[] vertices = new Vector3[rayCount + 2];
        int[] triangles = new int[rayCount * 3];

        // 메쉬 기준 원점 vertex
        vertices[0] = transform.InverseTransformPoint(origin);

        for (int i = 0; i <= rayCount; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector2 direction = AngleToDirection(angle);

            // 사격과 동일하게 장애물 레이어만 LOS 차단에 사용
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, visualViewDistance, obstacleMask);

            Vector2 endPoint;
            if (hit.collider != null)
            {
                endPoint = hit.point;
            }
            else
            {
                endPoint = origin + direction * visualViewDistance;
            }

            vertices[i + 1] = transform.InverseTransformPoint(endPoint);

            if (i < rayCount)
            {
                int triangleIndex = i * 3;

                // 부채꼴(triangle fan) 형태로 삼각형을 생성
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = i + 1;
                triangles[triangleIndex + 2] = i + 2;
            }

            if (drawDebugRays)
            {
                Color rayColor = hit.collider != null ? Color.red : Color.green;
                Debug.DrawLine(origin, endPoint, rayColor);
            }
        }

        fovMesh.Clear();
        fovMesh.vertices = vertices;
        fovMesh.triangles = triangles;
        fovMesh.RecalculateBounds();
        fovMesh.RecalculateNormals();
    }

    /// <summary>
    /// 플레이어 위치를 기준으로 현재 마우스 방향을 2D 벡터로 계산한다.
    /// </summary>
    private Vector2 GetMouseFacingDirection(Vector2 origin)
    {
        Vector3 mouseScreenPosition = Input.mousePosition;

        // orthographic camera 기준으로 플레이어 평면(z)까지의 거리를 맞춘다.
        mouseScreenPosition.z = Mathf.Abs(viewOrigin.position.z - mainCamera.transform.position.z);

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        return ((Vector2)mouseWorldPosition - origin).normalized;
    }

    /// <summary>
    /// 각도(도)를 2D 방향 벡터로 변환한다.
    /// </summary>
    private Vector2 AngleToDirection(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    /// <summary>
    /// 무기 교체 / 강화 반영용 시야각 변경 함수.
    /// </summary>
    public void SetViewAngle(float newAngle)
    {
        viewAngle = Mathf.Clamp(newAngle, 1f, 179f);
    }

    /// <summary>
    /// 시각화용 FOV 거리 변경 함수.
    /// 실제 게임 룰 시야 거리와는 분리해서 써도 된다.
    /// </summary>
    public void SetVisualViewDistance(float newDistance)
    {
        visualViewDistance = Mathf.Max(0.1f, newDistance);
    }

    /// <summary>
    /// ray 개수 변경 함수.
    /// 값이 높을수록 더 부드럽지만 raycast 수가 늘어난다.
    /// </summary>
    public void SetRayCount(int newRayCount)
    {
        rayCount = Mathf.Max(3, newRayCount);
    }
}