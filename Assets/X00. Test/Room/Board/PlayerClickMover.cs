using UnityEngine;

/// <summary>
/// 플레이어의 그리드 이동 "실행"만 담당하는 스크립트.
///
/// 중요:
/// - 이 스크립트는 더 이상 직접 입력을 읽지 않는다.
/// - 입력 감지는 앞으로 PlayerInputManager가 담당한다.
/// - 이 스크립트는 외부에서 이동 요청을 받으면,
///   실제로 이동 가능한지만 검사하고 실행만 한다.
///
/// 현재 최소 구현 규칙:
/// - 상하좌우 1칸만 이동 가능
/// - 대각선 이동 불가
/// - 보드 밖 이동 불가
/// - CanEnterTile을 통과해야 이동 가능
/// - 턴제 / AP 소모 / 경로 탐색 없음
/// - 즉시 1칸 스냅 이동
/// </summary>
[RequireComponent(typeof(GridUnit))]
public class PlayerClickMover : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private GridUnit gridUnit;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private UnitStatusController statusController;

    private void Awake()
    {
        if (gridUnit == null)
            gridUnit = GetComponent<GridUnit>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (statusController == null)
            statusController = GetComponent<UnitStatusController>();
    }

    /// <summary>
    /// New Input System 등 외부 입력 시스템이
    /// "현재 포인터의 스크린 좌표"를 넘겨줬을 때 이동을 시도한다.
    ///
    /// 예:
    /// - InputAction "Point"의 Vector2 값을 그대로 넣기
    /// </summary>
    public bool TryMoveFromScreenPosition(Vector2 screenPosition)
    {
        if (!TryConvertScreenToGrid(screenPosition, out Vector2Int targetGridPos))
            return false;

        return TryMoveToGrid(targetGridPos);
    }

    /// <summary>
    /// 특정 그리드 좌표로 이동을 시도한다.
    ///
    /// 반환값:
    /// - true  = 실제로 이동 성공
    /// - false = 이동 불가
    /// </summary>
    public bool TryMoveToGrid(Vector2Int targetGridPos)
    {
        // 필수 참조 체크
        if (gridUnit == null || gridUnit.BoardManager == null)
            return false;

        if (TurnManager.Instance == null)
        {
            Debug.LogWarning("PlayerClickMover: TurnManager is not assigned.");
            return false;
        }

        if (!TurnManager.Instance.IsPlayerTurn)
            return false;

        // 기존 CanMove / CanEnterTile / 인접칸 검사 통과 뒤
        if (!TurnManager.Instance.TrySpendPlayerAP(1))
            return false;


        // 상태이상 등으로 이동 불가면 중단
        if (statusController != null && !statusController.CanMove)
            return false;

        // 맵 밖이면 이동 불가
        if (!gridUnit.BoardManager.IsInsideBoard(targetGridPos))
            return false;



        // 현재 위치
        Vector2Int currentPos = gridUnit.CurrentGridPos;

        // 현재 위치와 목표 위치 차이
        Vector2Int delta = targetGridPos - currentPos;

        // 맨해튼 거리 1칸만 허용 = 상하좌우 1칸
        int manhattanDistance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
        bool isAdjacentOneStep = manhattanDistance == 1;

        if (!isAdjacentOneStep)
            return false;

        // 실제 진입 가능한 타일인지 확인
        bool canEnterTile = gridUnit.BoardManager.CanEnterTile(targetGridPos);

        if (!canEnterTile)
            return false;


        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayUnitMove();
        // 최종 이동 실행
        gridUnit.BoardManager.MoveUnit(gridUnit, targetGridPos);
        return true;
    }

    /// <summary>
    /// 스크린 좌표를 월드 좌표로 바꾼 뒤,
    /// 다시 보드 기준 그리드 좌표로 변환한다.
    /// </summary>
    private bool TryConvertScreenToGrid(Vector2 screenPosition, out Vector2Int gridPos)
    {
        gridPos = default;

        if (gridUnit == null || gridUnit.BoardManager == null)
            return false;

        if (mainCamera == null)
            return false;

        // ScreenToWorldPoint에 넘길 z 값 보정
        Vector3 screenPos = new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z);

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        gridPos = gridUnit.BoardManager.WorldToGrid(worldPos);
        return true;
    }
}