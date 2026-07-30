using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Map
{
    /// <summary>
    /// 설정 데이터에 따라 던전 맵 그래프를 생성합니다.
    /// </summary>
    public class MapGenerator
    {
        #region 필드
        private MapNode[,] nodeGrid;
        private int nextNodeIdentifier;
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 맵 생성 규칙에 따라 던전 맵 그래프를 생성합니다.
        /// </summary>
        /// <param name="mapConfigData">맵 생성에 사용할 규칙 데이터입니다.</param>
        /// <returns>생성한 맵 그래프입니다. 규칙 데이터가 없으면 null입니다.</returns>
        public MapGraph GenerateMapGraph(MapConfigData mapConfigData)
        {
            if (mapConfigData == null)
            {
                SWLog.LogError("[MapGenerator] GenerateMapGraph 실패: 맵 생성 규칙 데이터가 없습니다.");
                return null;
            }

            MapGraph mapGraph = new MapGraph();
            nodeGrid = new MapNode[mapConfigData.FloorCount, mapConfigData.LaneCount];
            nextNodeIdentifier = 0;

            CreateRandomWalkPaths(mapGraph, mapConfigData);
            AssignNodeTypes(mapGraph, mapConfigData);
            CreateMadnessOnlyEdges(mapGraph, mapConfigData);
            CreateAndAttachBossNode(mapGraph, mapConfigData);
            SetMadnessOnlyNodes(mapGraph);
            ApplyNodePositions(mapGraph, mapConfigData);

            nodeGrid = null;

            SWLog.Log($"[MapGenerator] 맵 생성 완료: 노드 {mapGraph.Nodes.Count}개, 경로 {mapGraph.Edges.Count}개, 입구 {mapGraph.EntryNodes.Count}곳입니다.");
            return mapGraph;
        }

        /// <summary>
        /// 무작위 이동 경로를 이용하여 노드와 경로의 기본 구조를 생성합니다.
        /// 모든 노드는 하나 이상의 이동 경로에 포함되므로 고립되지 않습니다.
        /// </summary>
        /// <param name="mapGraph">생성 결과를 저장할 맵 그래프입니다.</param>
        /// <param name="mapConfigData">맵 생성에 사용할 규칙 데이터입니다.</param>
        private void CreateRandomWalkPaths(MapGraph mapGraph, MapConfigData mapConfigData)
        {
            for (int pathIndex = 0; pathIndex < mapConfigData.PathCount; pathIndex++)
            {
                int lane = SWRandom.Range(0, mapConfigData.LaneCount);
                MapNode previousNode = GetOrCreateNode(mapGraph, 0, lane);

                for (int floor = 1; floor < mapConfigData.FloorCount; floor++)
                {
                    lane = Mathf.Clamp(lane + SWRandom.Range(-1, 2), 0, mapConfigData.LaneCount - 1);

                    MapNode currentNode = GetOrCreateNode(mapGraph, floor, lane);
                    mapGraph.AddEdge(new MapEdge(previousNode.Identifier, currentNode.Identifier, false));
                    previousNode = currentNode;
                }
            }
        }

        /// <summary>
        /// 지정한 격자의 노드를 반환하고, 노드가 없으면 전투 노드를 생성합니다.
        /// </summary>
        /// <param name="mapGraph">생성한 노드를 저장할 맵 그래프입니다.</param>
        /// <param name="floor">노드가 배치될 층입니다.</param>
        /// <param name="lane">노드가 배치될 세로 칸입니다.</param>
        /// <returns>지정한 격자에 배치된 노드입니다.</returns>
        private MapNode GetOrCreateNode(MapGraph mapGraph, int floor, int lane)
        {
            MapNode node = nodeGrid[floor, lane];

            if (node != null)
            {
                return node;
            }

            node = new MapNode(nextNodeIdentifier++, floor, lane, EMapNodeType.Battle, Vector2.zero);
            nodeGrid[floor, lane] = node;
            mapGraph.AddNode(node);
            return node;
        }

        /// <summary>
        /// 생성된 각 노드에 규칙에 맞는 노드 타입을 배정합니다.
        /// </summary>
        /// <param name="mapGraph">노드 타입을 배정할 맵 그래프입니다.</param>
        /// <param name="mapConfigData">노드 타입 가중치를 제공하는 규칙 데이터입니다.</param>
        private void AssignNodeTypes(MapGraph mapGraph, MapConfigData mapConfigData)
        {
            int lastFloor = mapConfigData.FloorCount - 1;
            bool containsStorageNode = false;

            foreach (MapNode node in mapGraph.Nodes)
            {
                if (node.Floor == 0)
                {
                    continue;
                }

                if (node.Floor == lastFloor)
                {
                    node.SetNodeType(EMapNodeType.Rest);
                    continue;
                }

                EMapNodeType nodeType = SelectWeightedNodeType(mapConfigData, node.Floor);
                node.SetNodeType(nodeType);

                if (nodeType == EMapNodeType.Storage)
                {
                    containsStorageNode = true;
                }
            }

            if (!containsStorageNode)
            {
                EnsureStorageNode(mapGraph, mapConfigData);
            }
        }

        /// <summary>
        /// 설정된 가중치에 따라 노드 타입을 선택합니다.
        /// </summary>
        /// <param name="mapConfigData">노드 타입 가중치를 제공하는 규칙 데이터입니다.</param>
        /// <param name="floor">노드가 배치된 층입니다.</param>
        /// <returns>가중치에 따라 선택한 노드 타입입니다.</returns>
        private EMapNodeType SelectWeightedNodeType(MapConfigData mapConfigData, int floor)
        {
            int eliteWeight = floor >= mapConfigData.EliteMinFloor ? mapConfigData.EliteWeight : 0;
            int totalWeight = mapConfigData.BattleWeight + eliteWeight + mapConfigData.RestWeight
                + mapConfigData.EventWeight + mapConfigData.ShopWeight + mapConfigData.StorageWeight;

            if (totalWeight <= 0)
            {
                return EMapNodeType.Battle;
            }

            int remainingWeight = SWRandom.Range(0, totalWeight);

            if ((remainingWeight -= mapConfigData.BattleWeight) < 0)
            {
                return EMapNodeType.Battle;
            }

            if ((remainingWeight -= eliteWeight) < 0)
            {
                return EMapNodeType.Elite;
            }

            if ((remainingWeight -= mapConfigData.RestWeight) < 0)
            {
                return EMapNodeType.Rest;
            }

            if ((remainingWeight -= mapConfigData.EventWeight) < 0)
            {
                return EMapNodeType.Event;
            }

            if ((remainingWeight -= mapConfigData.ShopWeight) < 0)
            {
                return EMapNodeType.Shop;
            }

            return EMapNodeType.Storage;
        }

        /// <summary>
        /// 보관 노드가 없으면 중간 구간의 전투 노드 하나를 보관 노드로 변경합니다.
        /// </summary>
        /// <param name="mapGraph">보관 노드를 확인할 맵 그래프입니다.</param>
        /// <param name="mapConfigData">맵의 층 수를 제공하는 규칙 데이터입니다.</param>
        private void EnsureStorageNode(MapGraph mapGraph, MapConfigData mapConfigData)
        {
            int middleSectionStartFloor = mapConfigData.FloorCount / 3;
            int middleSectionEndFloor = mapConfigData.FloorCount * 2 / 3;

            foreach (MapNode node in mapGraph.Nodes)
            {
                bool isInMiddleSection = node.Floor >= middleSectionStartFloor
                    && node.Floor <= middleSectionEndFloor;

                if (isInMiddleSection && node.NodeType == EMapNodeType.Battle)
                {
                    node.SetNodeType(EMapNodeType.Storage);
                    return;
                }
            }
        }

        /// <summary>
        /// 연결되지 않은 인접 노드 사이에 확률적으로 광기 전용 경로를 생성합니다.
        /// </summary>
        /// <param name="mapGraph">광기 전용 경로를 추가할 맵 그래프입니다.</param>
        /// <param name="mapConfigData">광기 전용 경로 생성 확률을 제공하는 규칙 데이터입니다.</param>
        private void CreateMadnessOnlyEdges(MapGraph mapGraph, MapConfigData mapConfigData)
        {
            if (mapConfigData.MadnessEdgeChance <= 0f)
            {
                return;
            }

            for (int floor = 0; floor < mapConfigData.FloorCount - 1; floor++)
            {
                for (int lane = 0; lane < mapConfigData.LaneCount; lane++)
                {
                    MapNode fromNode = nodeGrid[floor, lane];

                    if (fromNode == null)
                    {
                        continue;
                    }

                    for (int laneOffset = -1; laneOffset <= 1; laneOffset++)
                    {
                        int destinationLane = lane + laneOffset;

                        if (destinationLane < 0 || destinationLane >= mapConfigData.LaneCount)
                        {
                            continue;
                        }

                        MapNode toNode = nodeGrid[floor + 1, destinationLane];

                        if (toNode == null || mapGraph.HasEdge(fromNode.Identifier, toNode.Identifier))
                        {
                            continue;
                        }

                        if (SWRandom.Chance(mapConfigData.MadnessEdgeChance))
                        {
                            mapGraph.AddEdge(new MapEdge(fromNode.Identifier, toNode.Identifier, true));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 보스 노드를 생성하고 마지막 층의 모든 노드와 연결합니다.
        /// </summary>
        /// <param name="mapGraph">보스 노드를 추가할 맵 그래프입니다.</param>
        /// <param name="mapConfigData">보스 노드의 위치를 결정할 규칙 데이터입니다.</param>
        private void CreateAndAttachBossNode(MapGraph mapGraph, MapConfigData mapConfigData)
        {
            int bossFloor = mapConfigData.FloorCount;
            int bossLane = mapConfigData.LaneCount / 2;
            MapNode bossNode = new MapNode(
                nextNodeIdentifier++,
                bossFloor,
                bossLane,
                EMapNodeType.Boss,
                Vector2.zero);

            mapGraph.AddNode(bossNode);
            mapGraph.SetBossNode(bossNode);

            for (int lane = 0; lane < mapConfigData.LaneCount; lane++)
            {
                MapNode lastFloorNode = nodeGrid[mapConfigData.FloorCount - 1, lane];

                if (lastFloorNode != null)
                {
                    mapGraph.AddEdge(new MapEdge(lastFloorNode.Identifier, bossNode.Identifier, false));
                }
            }
        }

        /// <summary>
        /// 모든 진입 경로가 광기 전용인 노드를 광기 전용 노드로 설정합니다.
        /// </summary>
        /// <param name="mapGraph">광기 전용 노드를 판별할 맵 그래프입니다.</param>
        private void SetMadnessOnlyNodes(MapGraph mapGraph)
        {
            foreach (MapNode node in mapGraph.Nodes)
            {
                if (node.Floor == 0)
                {
                    continue;
                }

                bool hasIncomingEdge = false;
                bool hasNormalIncomingEdge = false;

                foreach (MapEdge edge in mapGraph.Edges)
                {
                    if (edge.ToNodeIdentifier != node.Identifier)
                    {
                        continue;
                    }

                    hasIncomingEdge = true;

                    if (!edge.IsMadnessOnly)
                    {
                        hasNormalIncomingEdge = true;
                        break;
                    }
                }

                if (hasIncomingEdge && !hasNormalIncomingEdge)
                {
                    node.SetMadnessOnly();
                }
            }
        }

        /// <summary>
        /// 층과 세로 칸의 간격에 작은 무작위 차이를 더해 각 노드의 화면 좌표를 정합니다.
        /// </summary>
        /// <param name="mapGraph">좌표를 설정할 맵 그래프입니다.</param>
        /// <param name="mapConfigData">노드 배치 간격을 제공하는 규칙 데이터입니다.</param>
        private void ApplyNodePositions(MapGraph mapGraph, MapConfigData mapConfigData)
        {
            float laneCenter = (mapConfigData.LaneCount - 1) * 0.5f;

            foreach (MapNode node in mapGraph.Nodes)
            {
                float horizontalOffset = SWRandom.Range(-mapConfigData.PositionOffset, mapConfigData.PositionOffset);
                float verticalOffset = SWRandom.Range(-mapConfigData.PositionOffset, mapConfigData.PositionOffset);

                if (node.NodeType == EMapNodeType.Boss)
                {
                    horizontalOffset = 0f;
                    verticalOffset = 0f;
                }

                Vector2 position = new Vector2(
                    node.Floor * mapConfigData.FloorSpacing + horizontalOffset,
                    (node.Lane - laneCenter) * mapConfigData.LaneSpacing + verticalOffset);

                node.SetPosition(position);
            }
        }
        #endregion // 함수
    }
}
