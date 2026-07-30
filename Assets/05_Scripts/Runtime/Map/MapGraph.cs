using System.Collections.Generic;
using EchoesOfAsh.Enum;
using SW.Util;

namespace EchoesOfAsh.Map
{
    /// <summary>
    /// 던전의 노드와 경로를 관리하는 맵 그래프입니다.
    /// </summary>
    public class MapGraph
    {
        #region 필드
        private MapNode bossNode;
        private int floorCount;

        private readonly List<MapNode> nodes = new();
        private readonly List<MapEdge> edges = new();
        private readonly List<MapNode> entryNodes = new();
        private readonly Dictionary<int, MapNode> nodeByIdentifier = new();
        private readonly Dictionary<int, List<MapEdge>> edgesByFromNodeIdentifier = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>맵을 구성하는 모든 노드입니다.</summary>
        public IReadOnlyList<MapNode> Nodes => nodes;
        /// <summary>노드를 연결하는 모든 경로입니다.</summary>
        public IReadOnlyList<MapEdge> Edges => edges;
        /// <summary>입구 층에 배치된 노드 목록입니다.</summary>
        public IReadOnlyList<MapNode> EntryNodes => entryNodes;
        /// <summary>보스 노드입니다.</summary>
        public MapNode BossNode => bossNode;
        /// <summary>보스 층을 포함한 전체 층 수입니다.</summary>
        public int FloorCount => floorCount;
        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 맵 그래프에 노드를 추가합니다.
        /// </summary>
        /// <param name="node">추가할 노드입니다.</param>
        public void AddNode(MapNode node)
        {
            if (node == null || nodeByIdentifier.ContainsKey(node.Identifier))
            {
                SWLog.LogError("[MapGraph] AddNode 실패: 노드가 없거나 식별자가 중복됩니다.");
                return;
            }

            nodes.Add(node);
            nodeByIdentifier.Add(node.Identifier, node);

            if (node.Floor == 0)
            {
                entryNodes.Add(node);
            }

            int requiredFloorCount = node.Floor + 1;

            if (requiredFloorCount > floorCount)
            {
                floorCount = requiredFloorCount;
            }
        }

        /// <summary>
        /// 맵 그래프에 경로를 추가합니다.
        /// </summary>
        /// <param name="edge">추가할 경로입니다.</param>
        public void AddEdge(MapEdge edge)
        {
            if (edge == null || HasEdge(edge.FromNodeIdentifier, edge.ToNodeIdentifier))
            {
                return;
            }

            edges.Add(edge);

            if (!edgesByFromNodeIdentifier.TryGetValue(edge.FromNodeIdentifier, out List<MapEdge> outgoingEdges))
            {
                outgoingEdges = new List<MapEdge>();
                edgesByFromNodeIdentifier.Add(edge.FromNodeIdentifier, outgoingEdges);
            }

            outgoingEdges.Add(edge);
        }

        /// <summary>
        /// 맵 그래프의 보스 노드를 지정합니다.
        /// </summary>
        /// <param name="node">보스 노드로 지정할 노드입니다.</param>
        public void SetBossNode(MapNode node)
        {
            bossNode = node;
        }

        /// <summary>
        /// 식별자에 해당하는 노드를 반환합니다.
        /// </summary>
        /// <param name="nodeIdentifier">조회할 노드의 식별자입니다.</param>
        /// <returns>식별자에 해당하는 노드입니다. 노드가 없으면 null입니다.</returns>
        public MapNode GetNode(int nodeIdentifier)
        {
            return nodeByIdentifier.TryGetValue(nodeIdentifier, out MapNode node) ? node : null;
        }

        /// <summary>
        /// 두 노드 사이에 경로가 있는지 확인합니다.
        /// </summary>
        /// <param name="fromNodeIdentifier">출발 노드의 식별자입니다.</param>
        /// <param name="toNodeIdentifier">도착 노드의 식별자입니다.</param>
        /// <returns>두 노드 사이에 경로가 있으면 true입니다.</returns>
        public bool HasEdge(int fromNodeIdentifier, int toNodeIdentifier)
        {
            return TryGetEdge(fromNodeIdentifier, toNodeIdentifier, out _);
        }

        /// <summary>
        /// 두 노드 사이의 경로를 조회합니다.
        /// </summary>
        /// <param name="fromNodeIdentifier">출발 노드의 식별자입니다.</param>
        /// <param name="toNodeIdentifier">도착 노드의 식별자입니다.</param>
        /// <param name="edge">조회한 경로입니다.</param>
        /// <returns>두 노드 사이의 경로를 찾으면 true입니다.</returns>
        public bool TryGetEdge(int fromNodeIdentifier, int toNodeIdentifier, out MapEdge edge)
        {
            edge = null;

            if (!edgesByFromNodeIdentifier.TryGetValue(fromNodeIdentifier, out List<MapEdge> outgoingEdges))
            {
                return false;
            }

            foreach (MapEdge candidateEdge in outgoingEdges)
            {
                if (candidateEdge.ToNodeIdentifier == toNodeIdentifier)
                {
                    edge = candidateEdge;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 지정한 노드에서 이동할 수 있는 다음 노드를 결과 목록에 추가합니다.
        /// 잿불에 잠식된 노드는 결과에서 제외합니다.
        /// </summary>
        /// <param name="fromNodeIdentifier">출발 노드의 식별자입니다.</param>
        /// <param name="resultNodes">결과를 저장할 목록입니다. 기존 요소는 제거됩니다.</param>
        /// <param name="includeMadnessOnlyEdges">광기 전용 경로를 포함할지 여부입니다.</param>
        public void GetNextNodes(int fromNodeIdentifier, List<MapNode> resultNodes, bool includeMadnessOnlyEdges)
        {
            if (resultNodes == null)
            {
                SWLog.LogError("[MapGraph] GetNextNodes 실패: 결과 목록이 없습니다.");
                return;
            }

            resultNodes.Clear();

            if (!edgesByFromNodeIdentifier.TryGetValue(fromNodeIdentifier, out List<MapEdge> outgoingEdges))
            {
                return;
            }

            foreach (MapEdge edge in outgoingEdges)
            {
                if (!includeMadnessOnlyEdges && edge.IsMadnessOnly)
                {
                    continue;
                }

                MapNode destinationNode = GetNode(edge.ToNodeIdentifier);

                if (destinationNode == null || destinationNode.IsAshConsumed)
                {
                    continue;
                }

                resultNodes.Add(destinationNode);
            }
        }

        /// <summary>
        /// 지정한 층 이하의 모든 노드를 잿불에 잠식된 상태로 변경합니다.
        /// </summary>
        /// <param name="floor">잠식할 마지막 층입니다.</param>
        public void ConsumeFloorsByAsh(int floor)
        {
            foreach (MapNode node in nodes)
            {
                if (node.Floor <= floor)
                {
                    node.SetAshConsumed();
                }
            }
        }

        /// <summary>
        /// 저장된 노드와 경로 목록으로 던전 지도를 다시 만듭니다.
        /// 방문, 잠식, 광기 전용 상태도 각 노드에 저장된 값으로 복원됩니다.
        /// </summary>
        /// <param name="savedNodes">저장된 노드 목록입니다.</param>
        /// <param name="savedEdges">저장된 경로 목록입니다.</param>
        /// <returns>재구축에 성공했으면 true입니다.</returns>
        public bool RestoreFrom(IReadOnlyList<MapNode> savedNodes, IReadOnlyList<MapEdge> savedEdges)
        {
            if (savedNodes == null || savedNodes.Count == 0 || savedEdges == null)
            {
                SWLog.LogError("[MapGraph] RestoreFrom 실패: 저장된 노드 또는 경로가 없습니다.");
                return false;
            }

            foreach (MapNode node in savedNodes)
            {
                if (node != null)
                {
                    AddNode(node);
                }
            }

            foreach (MapEdge edge in savedEdges)
            {
                if (edge != null)
                {
                    AddEdge(edge);
                }
            }

            foreach (MapNode node in nodes)
            {
                if (node.NodeType == EMapNodeType.Boss)
                {
                    SetBossNode(node);
                    break;
                }
            }

            if (bossNode == null)
            {
                SWLog.LogError("[MapGraph] RestoreFrom 실패: 보스 노드를 찾지 못했습니다.");
                return false;
            }

            return true;
        }
        #endregion // 함수
    }
}
