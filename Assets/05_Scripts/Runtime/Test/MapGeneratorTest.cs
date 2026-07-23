using System.Collections.Generic;
using System.Text;
using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Map;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Test
{
    /// <summary>
    /// 고정된 시드로 맵 생성 결과와 보스 노드 도달 가능 여부를 검증합니다.
    /// </summary>
    public class MapGeneratorTest : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("데이터")]
        [SerializeField] private MapConfigData mapConfigData;

        [SWGroup("무작위 시드")]
        [SerializeField] private int randomSeed = 12345;

        private readonly StringBuilder stringBuilder = new();
        private readonly List<MapNode> nextNodeBuffer = new();
        #endregion // 필드

        #region 함수
        /// <summary>
        /// 맵을 생성하고 검증 결과를 로그로 출력합니다.
        /// </summary>
        [SWButton("맵 생성 및 검증")]
        private void RunMapGenerationTest()
        {
            if (mapConfigData == null)
            {
                SWLog.LogError("[MapGeneratorTest] 검증 실패: 맵 생성 규칙 데이터가 없습니다.");
                return;
            }

            SWRandom.SetSeed(randomSeed);
            SWLog.Log($"[MapGeneratorTest] 무작위 시드를 {randomSeed}(으)로 고정했습니다.");

            MapGenerator mapGenerator = new MapGenerator();
            MapGraph mapGraph = mapGenerator.GenerateMapGraph(mapConfigData);

            if (mapGraph == null)
            {
                return;
            }

            LogFloorComposition(mapGraph);
            LogMadnessOnlyStatistics(mapGraph);
            LogBossReachability(mapGraph);
        }

        /// <summary>
        /// 각 층의 노드 구성을 로그로 출력합니다.
        /// </summary>
        /// <param name="mapGraph">출력할 맵 그래프입니다.</param>
        private void LogFloorComposition(MapGraph mapGraph)
        {
            stringBuilder.Clear();
            stringBuilder.AppendLine("[MapGeneratorTest] === 층별 구성 (입구 → 보스) ===");

            for (int floor = 0; floor < mapGraph.FloorCount; floor++)
            {
                stringBuilder.Append($"층 {floor,2}: ");

                foreach (MapNode node in mapGraph.Nodes)
                {
                    if (node.Floor != floor)
                    {
                        continue;
                    }

                    string madnessOnlyMark = node.IsMadnessOnly ? "†" : string.Empty;
                    string nodeTypeDisplayName = GetNodeTypeDisplayName(node.NodeType);
                    stringBuilder.Append(
                        $"[{node.Identifier,2}|레인 {node.Lane} {nodeTypeDisplayName}{madnessOnlyMark}] ");
                }

                stringBuilder.AppendLine();
            }

            SWLog.Log(stringBuilder.ToString());
        }

        /// <summary>
        /// 광기 전용 경로와 광기 전용 노드의 개수를 로그로 출력합니다.
        /// </summary>
        /// <param name="mapGraph">통계를 계산할 맵 그래프입니다.</param>
        private void LogMadnessOnlyStatistics(MapGraph mapGraph)
        {
            int madnessOnlyEdgeCount = 0;

            foreach (MapEdge edge in mapGraph.Edges)
            {
                if (edge.IsMadnessOnly)
                {
                    madnessOnlyEdgeCount++;
                }
            }

            int madnessOnlyNodeCount = 0;

            foreach (MapNode node in mapGraph.Nodes)
            {
                if (node.IsMadnessOnly)
                {
                    madnessOnlyNodeCount++;
                }
            }

            SWLog.Log(
                $"[MapGeneratorTest] 전체 경로 {mapGraph.Edges.Count}개 중 광기 전용 경로는 {madnessOnlyEdgeCount}개이며, "
                + $"광기 전용 노드는 {madnessOnlyNodeCount}개입니다.");
        }

        /// <summary>
        /// 입구에서 보스 노드까지 일반 경로만으로 도달할 수 있는지 검증하고 결과를 로그로 출력합니다.
        /// </summary>
        /// <param name="mapGraph">도달 가능 여부를 검증할 맵 그래프입니다.</param>
        private void LogBossReachability(MapGraph mapGraph)
        {
            HashSet<int> reachableNodeIdentifiers = new();
            Queue<int> pendingNodeIdentifiers = new();

            foreach (MapNode entryNode in mapGraph.EntryNodes)
            {
                reachableNodeIdentifiers.Add(entryNode.Identifier);
                pendingNodeIdentifiers.Enqueue(entryNode.Identifier);
            }

            while (pendingNodeIdentifiers.Count > 0)
            {
                int currentNodeIdentifier = pendingNodeIdentifiers.Dequeue();
                mapGraph.GetNextNodes(currentNodeIdentifier, nextNodeBuffer, false);

                foreach (MapNode nextNode in nextNodeBuffer)
                {
                    if (reachableNodeIdentifiers.Add(nextNode.Identifier))
                    {
                        pendingNodeIdentifiers.Enqueue(nextNode.Identifier);
                    }
                }
            }

            bool isBossReachable = mapGraph.BossNode != null
                && reachableNodeIdentifiers.Contains(mapGraph.BossNode.Identifier);

            if (isBossReachable)
            {
                SWLog.Log(
                    $"[MapGeneratorTest] 도달성 검증 통과: 일반 경로만으로 보스 노드에 도달할 수 있습니다. "
                    + $"도달 가능한 노드는 {reachableNodeIdentifiers.Count}/{mapGraph.Nodes.Count}개입니다.");
                return;
            }

            SWLog.LogError("[MapGeneratorTest] 도달성 검증 실패: 보스 노드에 도달할 수 없습니다.");
        }

        /// <summary>
        /// 노드 타입의 한글 표시 이름을 반환합니다.
        /// </summary>
        /// <param name="nodeType">표시 이름을 조회할 노드 타입입니다.</param>
        /// <returns>노드 타입의 한글 표시 이름입니다.</returns>
        private string GetNodeTypeDisplayName(EMapNodeType nodeType)
        {
            switch (nodeType)
            {
                case EMapNodeType.Battle:
                    return "전투";
                case EMapNodeType.Elite:
                    return "엘리트";
                case EMapNodeType.Rest:
                    return "휴식";
                case EMapNodeType.Event:
                    return "이벤트";
                case EMapNodeType.Shop:
                    return "상점";
                case EMapNodeType.Storage:
                    return "보관";
                case EMapNodeType.Boss:
                    return "보스";
                default:
                    return nodeType.ToString();
            }
        }
        #endregion // 함수
    }
}
