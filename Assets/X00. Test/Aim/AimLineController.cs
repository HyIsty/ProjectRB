using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimLineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private PlayerWeaponController weaponController;
    [SerializeField] private Transform endMarker;

    [Header("Ray Settings")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float fallbackPreviewLength = 20f;

    [Header("Line Colors")]
    [SerializeField] private Color clearColor = Color.green;
    [SerializeField] private Color blockedColor = Color.red;

    [Header("Visual Z")]
    [SerializeField] private float previewZ = -1f;

    private LineRenderer lineRenderer;

    public Vector2 PreviewEndPoint { get; private set; }
    public bool IsBlockedByObstacle { get; private set; }
    public bool IsActive { get; private set; }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (aimController == null)
            aimController = GetComponent<PlayerAimController>();

        if (weaponController == null)
            weaponController = GetComponent<PlayerWeaponController>();

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        HideVisuals();
    }

    /// <summary>
    /// 조준 시작 시 호출.
    /// </summary>
    public void BeginAim()
    {
        IsActive = true;
        ShowVisuals();
    }

    /// <summary>
    /// 조준 종료 시 호출.
    /// </summary>
    public void EndAim()
    {
        IsActive = false;
        HideVisuals();
    }

    /// <summary>
    /// PlayerInputManager가 전달한 포인터 스크린 좌표를 기준으로
    /// 조준선을 갱신한다.
    /// </summary>
    public void TickAimPreviewFromScreenPosition(Vector2 screenPosition)
    {
        if (!IsActive)
            return;

        if (mainCamera == null || aimController == null)
            return;

        Vector3 screenPos = new Vector3(
            screenPosition.x,
            screenPosition.y,
            -mainCamera.transform.position.z
        );

        Vector3 pointerWorld = mainCamera.ScreenToWorldPoint(screenPos);
        pointerWorld.z = 0f;

        Vector3 origin = aimController.ShootOrigin;
        Vector2 direction = ((Vector2)pointerWorld - (Vector2)origin);

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        float previewLength = GetPreviewLength();

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, previewLength, obstacleMask);

        if (hit.collider != null)
        {
            IsBlockedByObstacle = true;
            PreviewEndPoint = hit.point;
            SetLineColor(blockedColor);
        }
        else
        {
            IsBlockedByObstacle = false;
            PreviewEndPoint = (Vector2)origin + direction * previewLength;
            SetLineColor(clearColor);
        }

        Vector3 startPos = new Vector3(origin.x, origin.y, previewZ);
        Vector3 endPos = new Vector3(PreviewEndPoint.x, PreviewEndPoint.y, previewZ);

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        if (endMarker != null)
        {
            endMarker.position = new Vector3(PreviewEndPoint.x, PreviewEndPoint.y, endMarker.position.z);
        }
    }

    private float GetPreviewLength()
    {
        if (weaponController != null &&
            weaponController.HasCurrentWeapon() &&
            weaponController.CurrentWeaponRuntime != null &&
            weaponController.CurrentWeaponRuntime.HasBaseData)
        {
            return weaponController.CurrentWeaponRuntime.CurrentMaxRange;
        }

        return fallbackPreviewLength;
    }

    private void ShowVisuals()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = true;

        if (endMarker != null)
            endMarker.gameObject.SetActive(true);
    }

    private void HideVisuals()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;

        if (endMarker != null)
            endMarker.gameObject.SetActive(false);
    }

    private void SetLineColor(Color color)
    {
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}