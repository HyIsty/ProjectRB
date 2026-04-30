using UnityEngine;

/// <summary>
/// 카메라 자식으로 둔 Quad가 항상 화면 전체를 덮도록
/// 위치/회전/크기를 자동 조정하는 스크립트.
/// 
/// 전제:
/// - 이 오브젝트는 Camera의 자식이어야 한다.
/// - Orthographic Camera 기준이다.
/// </summary>
[ExecuteAlways]
public class CameraOverlayQuadFitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Overlay Settings")]
    [SerializeField] private float distanceFromCamera = 1f;
    [SerializeField] private float padding = 1.05f;

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = GetComponentInParent<Camera>();

        if (targetCamera == null)
            return;

        if (!targetCamera.orthographic)
            return;

        // 카메라 바로 앞에 위치시킨다.
        transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);
        transform.localRotation = Quaternion.identity;

        float worldHeight = targetCamera.orthographicSize * 2f;
        float worldWidth = worldHeight * targetCamera.aspect;

        // Quad 기본 크기는 1x1이므로 화면 크기에 맞춰 scale 조정
        transform.localScale = new Vector3(worldWidth * padding, worldHeight * padding, 1f);
    }
}