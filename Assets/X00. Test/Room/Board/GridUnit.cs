using UnityEngine;

/// <summary>
/// 플레이어/적 같은 "보드 위 유닛"이 공통으로 가지는 정보.
/// 현재 어느 타일에 있는지, 어떤 보드를 쓰는지 저장한다.
/// </summary>
public class GridUnit : MonoBehaviour
{
    private BoardManager boardManager;
    private Vector2Int currentGridPos;
    private OccupantType occupantType;

    /// <summary>
    /// 이 유닛이 속한 보드 매니저.
    /// </summary>
    public BoardManager BoardManager => boardManager;

    /// <summary>
    /// 현재 그리드 좌표.
    /// </summary>
    public Vector2Int CurrentGridPos => currentGridPos;

    /// <summary>
    /// 플레이어인지 적인지 구분.
    /// </summary>
    public OccupantType OccupantType => occupantType;

    /// <summary>
    /// 유닛을 처음 보드에 올릴 때 호출한다.
    /// </summary>
    public void Initialize(BoardManager boardManager, Vector2Int startGridPos, OccupantType occupantType)
    {
        this.boardManager = boardManager;
        this.occupantType = occupantType;

        SetGridPosition(startGridPos);
    }

    /// <summary>
    /// 현재 타일 좌표를 바꾸고, 월드 좌표도 같이 갱신한다.
    /// 지금은 최소구현이므로 즉시 이동(snap)한다.
    /// 나중에 부드러운 이동 애니메이션으로 바꾸기 쉽다.
    /// </summary>
    public void SetGridPosition(Vector2Int newGridPos)
    {
        currentGridPos = newGridPos;

        if (boardManager != null)
            transform.position = boardManager.GridToWorld(newGridPos);
    }
}