using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방 하나의 런타임 보드를 생성하고 관리한다.
///
/// 현재 책임:
/// - RoomTemplateData를 읽어 타일 데이터 생성
/// - 바닥 / 엄폐물 비주얼 생성
/// - 플레이어 / 적 스폰 좌표 계산 및 반환
/// - 타일 판정 함수 제공
/// - 유닛 이동 처리
/// - occupant 등록 / 제거
///
/// 중요:
/// - 플레이어 / 적을 직접 스폰하지 않는다.
/// - 실제 유닛 생성은 CombatManager가 담당한다.
/// </summary>
public class BoardManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private Transform roomRoot;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject obstaclePrefab;

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 boardOrigin = Vector3.zero;

    [Header("Border Wall Sprites")]
    [SerializeField] private Sprite wallTopSprite;
    [SerializeField] private Sprite wallBottomSprite;
    [SerializeField] private Sprite wallLeftSprite;
    [SerializeField] private Sprite wallRightSprite;

    [SerializeField] private Sprite wallTopLeftCornerSprite;
    [SerializeField] private Sprite wallTopRightCornerSprite;
    [SerializeField] private Sprite wallBottomLeftCornerSprite;
    [SerializeField] private Sprite wallBottomRightCornerSprite;

    // 런타임 타일 데이터
    private TileData[,] tiles;
    private int width;
    private int height;

    // 이 보드가 생성한 비주얼 오브젝트들을 추적해서 나중에 ClearRoom 시 제거한다.
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    public int Width => width;
    public int Height => height;
    public float CellSize => cellSize;

    private void Awake()
    {
        if (roomRoot == null)
            roomRoot = transform;
    }

    /// <summary>
    /// RoomTemplate 데이터를 바탕으로 실제 보드를 생성하고,
    /// 플레이어 / 적 스폰 좌표를 반환한다.
    /// 
    /// 중요:
    /// - 유닛은 여기서 직접 생성하지 않는다.
    /// - CombatManager가 이 결과를 받아 실제 Instantiate를 수행한다.
    /// </summary>
    public CombatSpawnResult BuildRoom(RoomTemplateData template)
    {
        CombatSpawnResult result = new CombatSpawnResult();

        if (template == null)
        {
            Debug.LogWarning("BuildRoom failed: template is null.");
            return result;
        }

        ClearRoom();

        width = template.width;
        height = template.height;
        tiles = new TileData[width, height];

        List<Vector2Int> playerCandidates = new List<Vector2Int>();
        List<Vector2Int> enemyCandidates = new List<Vector2Int>();
        List<Vector2Int> optionalObstacleCandidates = new List<Vector2Int>();

        // 1) 문자열 맵을 읽어서 타일 데이터로 변환한다.
        // rows[0]은 맵의 맨 위 줄이다.
        for (int rowIndex = 0; rowIndex < height; rowIndex++)
        {
            string row = template.rows[rowIndex];
            int y = height - 1 - rowIndex;

            for (int x = 0; x < width; x++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                TileData tile = new TileData(gridPos);
                tiles[x, y] = tile;

                char symbol = row[x];

                switch (symbol)
                {
                    case '#':
                        tile.coverType = CoverType.Obstacle;
                        break;

                    case 'O':
                        optionalObstacleCandidates.Add(gridPos);
                        break;

                    case 'P':
                        tile.spawnMarker = SpawnMarkerType.PlayerCandidate;
                        playerCandidates.Add(gridPos);
                        break;

                    case 'E':
                        tile.spawnMarker = SpawnMarkerType.EnemyCandidate;
                        enemyCandidates.Add(gridPos);
                        break;
                }
            }
        }

        if (playerCandidates.Count == 0)
        {
            Debug.LogError($"Room template [{template.roomName}] has no player spawn candidate (P).");
            return result;
        }

        // 2) 플레이어 시작점 1개 선택
        Vector2Int chosenPlayerSpawn = playerCandidates[Random.Range(0, playerCandidates.Count)];

        // 3) 적 수 랜덤 선택
        int enemyCount = Random.Range(template.minEnemyCount, template.maxEnemyCount + 1);
        enemyCount = Mathf.Min(enemyCount, enemyCandidates.Count);

        List<Vector2Int> chosenEnemySpawns = PickRandomDistinct(enemyCandidates, enemyCount);

        // 스폰 예정 타일은 선택형 엄폐물 생성 금지
        HashSet<Vector2Int> reservedSpawnTiles = new HashSet<Vector2Int>();
        reservedSpawnTiles.Add(chosenPlayerSpawn);

        for (int i = 0; i < chosenEnemySpawns.Count; i++)
            reservedSpawnTiles.Add(chosenEnemySpawns[i]);

        // 4) 선택형 엄폐물 일부 생성
        for (int i = 0; i < optionalObstacleCandidates.Count; i++)
        {
            Vector2Int pos = optionalObstacleCandidates[i];

            if (reservedSpawnTiles.Contains(pos))
                continue;

            if (Random.value <= template.optionalObstacleChance)
                tiles[pos.x, pos.y].coverType = CoverType.Obstacle;
        }

        // 5) 비주얼 생성
        SpawnVisualTiles();

        // 6) CombatManager가 사용할 스폰 결과 반환
        result.playerSpawnGrid = chosenPlayerSpawn;
        result.enemySpawnGrids = chosenEnemySpawns;

        return result;
    }

    /// <summary>
    /// 보드 범위 안인지 체크한다.
    /// </summary>
    public bool IsInsideBoard(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < width &&
               gridPos.y >= 0 && gridPos.y < height;
    }

    /// <summary>
    /// 이동 가능한 타일인지 체크한다.
    /// - 바닥이어야 함
    /// - 엄폐물이 없어야 함
    /// - 누가 서 있지 않아야 함
    /// </summary>
    public bool CanEnterTile(Vector2Int gridPos)
    {
        if (!IsInsideBoard(gridPos))
            return false;

        TileData tile = tiles[gridPos.x, gridPos.y];

        return tile.baseType == TileBaseType.Floor &&
               tile.coverType == CoverType.None &&
               tile.occupantObject == null;
    }

    /// <summary>
    /// 스폰 가능한 타일인지 체크한다.
    /// </summary>
    public bool CanSpawnOnTile(Vector2Int gridPos)
    {
        if (!IsInsideBoard(gridPos))
            return false;

        TileData tile = tiles[gridPos.x, gridPos.y];

        return tile.baseType == TileBaseType.Floor &&
               tile.coverType == CoverType.None &&
               tile.occupantObject == null;
    }

    /// <summary>
    /// 사격 라인을 막는 타일인지 체크한다.
    /// </summary>
    public bool BlocksLineOfFire(Vector2Int gridPos)
    {
        if (!IsInsideBoard(gridPos))
            return true;

        TileData tile = tiles[gridPos.x, gridPos.y];

        return tile.coverType == CoverType.Obstacle ||
               tile.baseType == TileBaseType.Blocked;
    }

    /// <summary>
    /// 투척물이 튕겨야 하는 표면인지 체크한다.
    /// 현재 규칙상 엄폐물에는 튕기고, 플레이어/적에는 튕기지 않는다.
    /// </summary>
    public bool IsBounceSurface(Vector2Int gridPos)
    {
        if (!IsInsideBoard(gridPos))
            return true;

        TileData tile = tiles[gridPos.x, gridPos.y];

        return tile.coverType == CoverType.Obstacle ||
               tile.baseType == TileBaseType.Blocked;
    }

    /// <summary>
    /// 실제 유닛 이동 처리.
    /// - 타겟 타일이 비어 있고 이동 가능해야 한다.
    /// - 이전 타일 occupant 정보 제거
    /// - 새 타일 occupant 정보 등록
    /// - 유닛 transform 위치 갱신
    /// </summary>
    public bool MoveUnit(GridUnit unit, Vector2Int targetGridPos)
    {
        if (unit == null)
            return false;

        if (!IsInsideBoard(targetGridPos))
            return false;

        if (!CanEnterTile(targetGridPos))
            return false;

        Vector2Int currentGridPos = unit.CurrentGridPos;

        if (!IsInsideBoard(currentGridPos))
            return false;

        TileData currentTile = tiles[currentGridPos.x, currentGridPos.y];
        TileData targetTile = tiles[targetGridPos.x, targetGridPos.y];

        // 현재 타일 비우기
        if (currentTile.occupantObject == unit.gameObject)
        {
            currentTile.occupantType = OccupantType.None;
            currentTile.occupantObject = null;
        }

        // 새 타일 점유 등록
        targetTile.occupantType = unit.OccupantType;
        targetTile.occupantObject = unit.gameObject;

        // 유닛 위치 갱신
        unit.SetGridPosition(targetGridPos);

        return true;
    }

    /// <summary>
    /// CombatManager가 생성한 유닛을 보드에 등록한다.
    /// 유닛 생성 후 반드시 한 번 호출해야 한다.
    /// </summary>
    public bool RegisterSpawnedUnit(GridUnit unit, Vector2Int gridPos, OccupantType occupantType)
    {
        if (unit == null)
            return false;

        if (!CanSpawnOnTile(gridPos))
            return false;

        unit.Initialize(this, gridPos, occupantType);

        TileData tile = tiles[gridPos.x, gridPos.y];
        tile.occupantType = occupantType;
        tile.occupantObject = unit.gameObject;

        return true;
    }

    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환한다.
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return boardOrigin + new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0f);
    }

    /// <summary>
    /// 특정 타일 데이터 가져오기.
    /// </summary>
    public TileData GetTile(Vector2Int gridPos)
    {
        if (!IsInsideBoard(gridPos))
            return null;

        return tiles[gridPos.x, gridPos.y];
    }

    /// <summary>
    /// 바닥과 엄폐물 비주얼을 생성한다.
    /// 
    /// 현재 규칙:
    /// - 엄폐물 칸에는 Floor를 생성하지 않는다.
    /// - 나머지 칸에는 Floor를 생성한다.
    /// - 외곽에 있는 엄폐물은 벽 스프라이트로 표시한다.
    /// - 내부에 있는 엄폐물은 기존 obstaclePrefab 기본 스프라이트를 그대로 사용한다.
    /// </summary>
    private void SpawnVisualTiles()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = GridToWorld(gridPos);

                TileData tile = tiles[x, y];
                bool hasObstacle = tile.coverType == CoverType.Obstacle;

                // 엄폐물이 없는 칸에만 Floor 생성
                if (!hasObstacle && floorPrefab != null)
                {
                    GameObject floor = Instantiate(floorPrefab, worldPos, Quaternion.identity, roomRoot);
                    floor.name = $"Floor_{x}_{y}";
                    spawnedObjects.Add(floor);
                }

                // 엄폐물 칸이면 Obstacle 생성
                if (hasObstacle && obstaclePrefab != null)
                {
                    GameObject obstacle = Instantiate(obstaclePrefab, worldPos, Quaternion.identity, roomRoot);
                    obstacle.name = $"Obstacle_{x}_{y}";

                    // 외곽 벽이면 위치에 맞는 벽 스프라이트로 교체한다.
                    ApplyBorderWallSpriteIfNeeded(obstacle, x, y);

                    spawnedObjects.Add(obstacle);
                }
            }
        }
    }

    /// <summary>
    /// 생성된 obstacle 오브젝트가 외곽 벽 위치에 있다면,
    /// 해당 위치에 맞는 벽 스프라이트를 SpriteRenderer에 적용한다.
    /// 
    /// 내부 장애물은 기존 obstaclePrefab의 기본 스프라이트를 그대로 사용한다.
    /// </summary>
    private void ApplyBorderWallSpriteIfNeeded(GameObject obstacleObject, int x, int y)
    {
        if (obstacleObject == null)
            return;

        Sprite borderSprite = GetBorderWallSprite(x, y);

        // null이면 외곽 벽이 아니라는 뜻이므로 기존 obstacle sprite 유지
        if (borderSprite == null)
            return;

        SpriteRenderer spriteRenderer = obstacleObject.GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[BoardManager] Obstacle prefab has no SpriteRenderer. Cannot apply border wall sprite at ({x}, {y}).");
            return;
        }

        spriteRenderer.sprite = borderSprite;
    }

    /// <summary>
    /// 현재 좌표가 보드 외곽이면 외곽 방향에 맞는 벽 스프라이트를 반환한다.
    /// 외곽이 아니면 null을 반환한다.
    /// 
    /// 주의:
    /// 코너를 먼저 검사해야 한다.
    /// 코너 타일은 위/아래/왼쪽/오른쪽 조건에 동시에 걸리기 때문이다.
    /// </summary>
    private Sprite GetBorderWallSprite(int x, int y)
    {
        bool isLeft = x == 0;
        bool isRight = x == width - 1;
        bool isBottom = y == 0;
        bool isTop = y == height - 1;

        // 코너 먼저 체크
        if (isTop && isLeft)
            return wallTopLeftCornerSprite;

        if (isTop && isRight)
            return wallTopRightCornerSprite;

        if (isBottom && isLeft)
            return wallBottomLeftCornerSprite;

        if (isBottom && isRight)
            return wallBottomRightCornerSprite;

        // 직선 벽 체크
        if (isTop)
            return wallTopSprite;

        if (isBottom)
            return wallBottomSprite;

        if (isLeft)
            return wallLeftSprite;

        if (isRight)
            return wallRightSprite;

        // 외곽이 아니면 내부 장애물
        return null;
    }

    /// <summary>
    /// 리스트에서 중복 없이 랜덤하게 count개를 뽑는다.
    /// </summary>
    private List<Vector2Int> PickRandomDistinct(List<Vector2Int> source, int count)
    {
        List<Vector2Int> copy = new List<Vector2Int>(source);
        List<Vector2Int> result = new List<Vector2Int>();

        for (int i = 0; i < copy.Count; i++)
        {
            int randomIndex = Random.Range(i, copy.Count);

            Vector2Int temp = copy[i];
            copy[i] = copy[randomIndex];
            copy[randomIndex] = temp;
        }

        int finalCount = Mathf.Min(count, copy.Count);

        for (int i = 0; i < finalCount; i++)
            result.Add(copy[i]);

        return result;
    }

    /// <summary>
    /// 현재 방에 생성된 비주얼 오브젝트들을 정리한다.
    /// 
    /// 중요:
    /// 실제 플레이어/적 오브젝트 정리는 CombatManager가 담당하는 방향이다.
    /// 여기서는 BoardManager가 생성한 바닥/엄폐물 비주얼만 정리한다.
    /// </summary>
    public void ClearRoom()
    {
        for (int i = 0; i < spawnedObjects.Count; i++)
        {
            if (spawnedObjects[i] == null)
                continue;

            if (Application.isPlaying)
                Destroy(spawnedObjects[i]);
            else
                DestroyImmediate(spawnedObjects[i]);
        }

        spawnedObjects.Clear();
        tiles = null;
        width = 0;
        height = 0;
    }

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환한다.
    /// 
    /// 중요:
    /// 현재 GridToWorld()는 "타일 중심" 좌표를 반환하고 있으므로,
    /// 클릭 좌표도 가장 가까운 타일 중심을 기준으로 잡아야 한다.
    /// 그래서 FloorToInt가 아니라 RoundToInt를 사용한다.
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - boardOrigin.x) / cellSize);
        int y = Mathf.RoundToInt((worldPos.y - boardOrigin.y) / cellSize);

        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 특정 유닛을 현재 점유 타일에서 제거한다.
    /// 죽음 처리 시 BoardManager occupancy cleanup 용도.
    /// </summary>
    public bool RemoveUnit(GridUnit unit)
    {
        if (unit == null)
            return false;

        Vector2Int currentGridPos = unit.CurrentGridPos;

        if (!IsInsideBoard(currentGridPos))
            return false;

        TileData currentTile = tiles[currentGridPos.x, currentGridPos.y];

        // 현재 타일이 정말 이 유닛을 가리키고 있을 때만 비운다.
        if (currentTile.occupantObject == unit.gameObject)
        {
            currentTile.occupantType = OccupantType.None;
            currentTile.occupantObject = null;
            return true;
        }

        return false;
    }
}