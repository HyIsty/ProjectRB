using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시작 -> 보스까지 런 맵을 절차 생성하는 헬퍼.
/// 현재 minimum-safe 방향:
/// - 총 15개 depth 사용 (시작층 + 중간층들 + 보스층)
/// - 시작층 1개, 보스층 1개
/// - 중간층은 2~4개 노드 랜덤
/// - 각 노드는 다음 depth의 1~2개 노드와 연결
/// - 모든 다음 depth 노드는 적어도 1개 incoming 연결 보장
/// </summary>
public static class RunMapGenerator
{
    /// <summary>
    /// totalDepth = 15 라면 depth 0이 Start, depth 14가 Boss다.
    /// </summary>


    private const int FixedRewardDepth = 6; // 1-based 7층 가정
    public static RunMapData GenerateMap(
        int totalDepth,
        int minNodesPerDepth,
        int maxNodesPerDepth,
        float xSpacing,
        float ySpacing)
    {
        RunMapData mapData = new RunMapData();

        // depth별 노드 묶음
        List<List<MapNodeData>> layers = new List<List<MapNodeData>>();

        for (int depth = 0; depth < totalDepth; depth++)
        {
            List<MapNodeData> currentLayer = new List<MapNodeData>();

            int nodeCount;

            // 시작층
            if (depth == 0)
            {
                nodeCount = 1;
            }
            // 보스층
            else if (depth == totalDepth - 1)
            {
                nodeCount = 1;
            }
            else
            {
                nodeCount = Random.Range(minNodesPerDepth, maxNodesPerDepth + 1);
            }

            for (int i = 0; i < nodeCount; i++)
            {
                MapNodeData node = new MapNodeData();
                node.nodeId = $"node_{depth}_{i}";
                node.depth = depth;
                node.roomType = ResolveRoomType(depth, totalDepth);
                node.uiPosition = CalculateNodeUIPosition(
                    depth,
                    totalDepth,
                    i,
                    nodeCount,
                    xSpacing,
                    ySpacing
                );

                currentLayer.Add(node);
                mapData.allNodes.Add(node);
            }

            layers.Add(currentLayer);
        }

        // 시작 / 보스 노드 설정
        layers[0][0].roomType = RoomType.Start;
        layers[totalDepth - 1][0].roomType = RoomType.Boss;

        mapData.startNodeId = layers[0][0].nodeId;
        mapData.bossNodeId = layers[totalDepth - 1][0].nodeId;
        mapData.currentNodeId = mapData.startNodeId;

        // 시작 노드는 이미 서 있는 위치니까 방문/클리어 처리
        layers[0][0].isVisited = true;
        layers[0][0].isCleared = true;

        // depth 연결 생성
        for (int depth = 0; depth < layers.Count - 1; depth++)
        {
            ConnectLayersMonotonic(layers[depth], layers[depth + 1]);
        }

        return mapData;
    }

    /// <summary>
    /// 중간층 방 타입 랜덤 결정.
    /// Start / Boss는 depth 기준으로 고정되므로 여기선 안 뽑는다.
    /// </summary>
    private static RoomType ResolveRoomType(int depth, int totalDepth)
    {
        if (depth == 0)
            return RoomType.Start;

        if (depth == totalDepth - 1)
            return RoomType.Boss;
        if (depth == FixedRewardDepth)
            return RoomType.Reward;


        // 가중치 테이블
        // Combat를 제일 자주, Rest/Random/Shop은 적당히 섞음
        int roll = Random.Range(0, 100);

        if (roll < 52) return RoomType.Combat;
        if (roll < 68) return RoomType.Shop;
        if (roll < 84) return RoomType.Rest;
        return RoomType.Random;
    }

    /// <summary>
    /// 맵 UI용 좌표 계산.
    /// X축은 depth, Y축은 같은 depth 내에서 균등 분배.
    /// </summary>
    private static Vector2 CalculateNodeUIPosition(
        int depth,
        int totalDepth,
        int indexInLayer,
        int nodeCountInLayer,
        float xSpacing,
        float ySpacing)
    {
        // 맵 전체 너비의 절반만큼 왼쪽으로 당겨서
        // ContentRoot 중심 기준으로 맵 전체가 가운데 오게 만든다.
        float totalWidth = (totalDepth - 1) * xSpacing;
        float x = depth * xSpacing - totalWidth * 0.5f;

        // 같은 depth 안에서는 Y를 가운데 기준으로 정렬
        float centeredOffset = (nodeCountInLayer - 1) * 0.5f;
        float y = (centeredOffset - indexInLayer) * ySpacing;

        return new Vector2(x, y);
    }

    private static void ConnectLayersMonotonic(List<MapNodeData> currentLayer, List<MapNodeData> nextLayer)
    {
        // 위 -> 아래 순서로 정렬
        currentLayer.Sort((a, b) => b.uiPosition.y.CompareTo(a.uiPosition.y));
        nextLayer.Sort((a, b) => b.uiPosition.y.CompareTo(a.uiPosition.y));

        // 현재층 각 노드는 다음층의 "비슷한 높이" 노드로 연결
        for (int i = 0; i < currentLayer.Count; i++)
        {
            MapNodeData fromNode = currentLayer[i];

            // 현재층 인덱스를 다음층 인덱스 범위로 비례 매핑
            float t = currentLayer.Count == 1 ? 0.5f : (float)i / (currentLayer.Count - 1);
            int primaryIndex = Mathf.RoundToInt(t * (nextLayer.Count - 1));

            AddNextLink(fromNode, nextLayer[primaryIndex].nodeId);

            // 가끔만 보조 연결 허용
            // 이때도 바로 옆 노드만 연결해서 크로스 가능성 줄임
            if (nextLayer.Count > 1 && Random.value < 0.35f)
            {
                int offset = Random.value < 0.5f ? -1 : 1;
                int secondaryIndex = Mathf.Clamp(primaryIndex + offset, 0, nextLayer.Count - 1);

                AddNextLink(fromNode, nextLayer[secondaryIndex].nodeId);
            }
        }

        // 다음층 모든 노드가 최소 1개 incoming 연결 갖도록 보정
        for (int j = 0; j < nextLayer.Count; j++)
        {
            MapNodeData targetNode = nextLayer[j];
            bool hasIncoming = false;

            for (int i = 0; i < currentLayer.Count; i++)
            {
                if (currentLayer[i].nextNodeIds.Contains(targetNode.nodeId))
                {
                    hasIncoming = true;
                    break;
                }
            }

            if (!hasIncoming)
            {
                // target 쪽 높이에 가장 가까운 현재층 노드에 연결
                float t = nextLayer.Count == 1 ? 0.5f : (float)j / (nextLayer.Count - 1);
                int nearestCurrentIndex = Mathf.RoundToInt(t * (currentLayer.Count - 1));

                AddNextLink(currentLayer[nearestCurrentIndex], targetNode.nodeId);
            }
        }
    }

    private static void AddNextLink(MapNodeData fromNode, string nextNodeId)
    {
        if (fromNode == null || string.IsNullOrEmpty(nextNodeId))
            return;

        if (!fromNode.nextNodeIds.Contains(nextNodeId))
            fromNode.nextNodeIds.Add(nextNodeId);
    }
}