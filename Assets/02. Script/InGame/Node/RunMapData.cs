using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인게임 런 맵에서 사용하는 방 타입.
/// </summary>
public enum RoomType
{
    Start,
    Combat,
    Reward,
    Shop,
    Rest,
    Random,
    Boss
}

/// <summary>
/// 런 맵의 노드 1개를 나타내는 데이터.
/// 버튼 프리팹은 이 데이터를 보고 표시/활성 여부를 결정한다.
/// </summary>
[Serializable]
public class MapNodeData
{
    [Header("Identity")]
    public string nodeId;

    [Tooltip("0 = 시작층, 마지막 = 보스층")]
    public int depth;

    public RoomType roomType;

    [Header("Graph")]
    [Tooltip("이 노드에서 다음으로 갈 수 있는 노드 ID 목록")]
    public List<string> nextNodeIds = new List<string>();

    [Header("State")]
    public bool isVisited;
    public bool isCleared;

    [Tooltip("Random 방은 처음 들어갈 때 한 번만 결과를 확정하고 저장한다.")]
    public bool hasResolvedRandomType;
    public RoomType resolvedRandomType;

    [Header("UI Layout")]
    [Tooltip("맵 UI에서 버튼을 찍을 때 사용할 좌표")]
    public Vector2 uiPosition;
}


/// <summary>
/// 한 런 전체의 맵 데이터.
/// 씬이 바뀌어도 이 데이터는 RunData 안에 계속 살아있어야 한다.
/// </summary>
[Serializable]
public class RunMapData
{
    [Tooltip("런 전체 노드 목록")]
    public List<MapNodeData> allNodes = new List<MapNodeData>();

    [Tooltip("플레이어가 현재 서 있는 노드 ID")]
    public string currentNodeId;

    public string startNodeId;
    public string bossNodeId;
}

public enum RunMapViewMode
{
    Hidden,
    InGamePersistentClickable, // 항상 보임, 클릭 가능, 레이어상 맨 아래
    OverlayClickable,          // Shop 등: Tab 오버레이, 클릭 가능
    OverlayReadOnly            // Combat 등: Tab 오버레이, 클릭 불가
}