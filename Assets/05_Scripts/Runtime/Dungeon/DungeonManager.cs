using System;
using System.Collections.Generic;
using System.Text;
using EchoesOfAsh.Battle;
using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Map;
using EchoesOfAsh.View.UI;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Dungeon
{
    /// <summary>
    /// 던전의 시작과 종료, 맵 이동, 전투 전환을 관리합니다.
    /// </summary>
    public class DungeonManager : SWMonoBehaviour
    {
        #region 열거형
        /// <summary>
        /// 던전 화면의 진행 상태입니다.
        /// </summary>
        public enum EDungeonPhase
        {
            /// <summary>던전을 시작하지 않은 상태입니다.</summary>
            None,
            /// <summary>맵에서 이동할 노드를 선택하는 상태입니다.</summary>
            Map,
            /// <summary>전투를 진행하는 상태입니다.</summary>
            Battle,
            /// <summary>던전이 종료된 상태입니다.</summary>
            Ended,
        }
        #endregion // 열거형

        #region 필드
        [SWGroup("참조")]
        [SerializeField] private BattleManager battleManager;

        [SWGroup("데이터")]
        [SerializeField] private MapConfigData mapConfigData;

        [SWGroup("던전 구성")]
        [Tooltip("던전 생성에 사용할 시드입니다. 0이면 실행 시 무작위 시드를 생성합니다.")]
        [SerializeField] private int dungeonSeed;
        [Tooltip("던전 시작 시 사용할 카드 목록입니다.")]
        [SerializeField] private List<CardData> startingCards = new();
        [Tooltip("던전에서 발생할 수 있는 정신력 이벤트 데이터 목록입니다.")]
        [SerializeField] private List<SanityEventData> sanityEventDatas = new();
        [Tooltip("전투 노드에 진입할 때 선택할 수 있는 적 조우 데이터 목록입니다.")]
        [SerializeField] private List<EnemyEncounterData> enemyEncounterDatas = new();

        [SWGroup("뷰")]
        [SerializeField] private MapView mapView;

        private DungeonState dungeonState;
        private EDungeonPhase currentPhase = EDungeonPhase.None;
        private MapNode currentBattleNode;
        private bool isBattleEventSubscribed;

        private readonly List<MapNode> availableNodeBuffer = new();
        private readonly StringBuilder stringBuilder = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 던전 상태입니다. 던전을 시작하지 않았으면 null입니다.</summary>
        public DungeonState DungeonState => dungeonState;
        /// <summary>현재 던전 화면의 진행 상태입니다.</summary>
        public EDungeonPhase CurrentPhase => currentPhase;
        /// <summary>던전을 진행하고 있는지 여부입니다.</summary>
        public bool IsDungeonRunning => currentPhase == EDungeonPhase.Map
            || currentPhase == EDungeonPhase.Battle;

        /// <summary>던전이 시작될 때 호출됩니다.</summary>
        public event Action OnDungeonStarted;
        /// <summary>던전이 종료될 때 승리 여부와 함께 호출됩니다.</summary>
        public event Action<bool> OnDungeonEnded;
        /// <summary>노드에 진입할 때 진입한 노드와 함께 호출됩니다.</summary>
        public event Action<MapNode> OnNodeEntered;
        #endregion // 프로퍼티

        #region 유니티 이벤트 함수
        /// <summary>
        /// 객체가 제거될 때 전투 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeBattleEvents();
        }
        #endregion // 유니티 이벤트 함수

        #region 초기화
        /// <summary>
        /// 전투 종료 이벤트를 구독합니다.
        /// </summary>
        private void SubscribeBattleEvents()
        {
            if (isBattleEventSubscribed || battleManager == null)
            {
                return;
            }

            battleManager.OnBattleEnded += HandleBattleEnded;
            isBattleEventSubscribed = true;
        }

        /// <summary>
        /// 전투 종료 이벤트 구독을 해제합니다.
        /// </summary>
        private void UnsubscribeBattleEvents()
        {
            if (!isBattleEventSubscribed || battleManager == null)
            {
                return;
            }

            battleManager.OnBattleEnded -= HandleBattleEnded;
            isBattleEventSubscribed = false;
        }
        #endregion // 초기화

        #region 던전
        /// <summary>
        /// 던전 상태와 맵을 생성하고 맵 선택 상태로 전환합니다.
        /// </summary>
        [SWButton("던전 시작")]
        public void StartDungeon()
        {
            if (IsDungeonRunning)
            {
                SWLog.LogWarning("[DungeonManager] StartDungeon 무시: 던전이 이미 진행 중입니다.");
                return;
            }

            if (battleManager == null || mapConfigData == null)
            {
                SWLog.LogError("[DungeonManager] StartDungeon 실패: 필수 참조가 없습니다.");
                return;
            }

            if (enemyEncounterDatas.Count == 0)
            {
                SWLog.LogError("[DungeonManager] StartDungeon 실패: 적 조우 데이터 목록이 비어 있습니다.");
                return;
            }

            int seed = dungeonSeed != 0 ? dungeonSeed : Environment.TickCount;
            SWRandom.SetSeed(seed);

            DungeonState newDungeonState = new DungeonState(seed, startingCards, sanityEventDatas);
            MapGenerator mapGenerator = new MapGenerator();
            MapGraph mapGraph = mapGenerator.GenerateMapGraph(mapConfigData);

            if (mapGraph == null)
            {
                SWLog.LogError("[DungeonManager] StartDungeon 실패: 맵을 생성하지 못했습니다.");
                return;
            }

            newDungeonState.SetMapGraph(mapGraph);
            dungeonState = newDungeonState;
            currentBattleNode = null;
            currentPhase = EDungeonPhase.Map;

            SubscribeBattleEvents();

            SWLog.Log($"[DungeonManager] 던전을 시작했습니다. 시드: {seed}");
            OnDungeonStarted?.Invoke();

            if (mapView != null)
            {
                mapView.Initialize(mapGraph, nodeIdentifier => MoveToNode(nodeIdentifier));
                mapView.Show();
                RefreshMapViewState();
            }

            LogAvailableNodes("맵 진입");
        }

        /// <summary>
        /// 던전을 종료하고 결과를 알립니다.
        /// </summary>
        /// <param name="isVictory">던전에서 승리했는지 여부입니다.</param>
        private void EndDungeon(bool isVictory)
        {
            if (mapView != null)
            {
                mapView.Hide();
            }

            currentPhase = EDungeonPhase.Ended;
            currentBattleNode = null;

            SWLog.Log($"[DungeonManager] 던전을 종료했습니다. 결과: {(isVictory ? "승리" : "패배")}");
            OnDungeonEnded?.Invoke(isVictory);
        }
        #endregion // 던전

        #region 이동
        /// <summary>
        /// 현재 위치에서 이동할 수 있는 노드를 결과 목록에 추가합니다.
        /// 광기 전용 경로와 잿불에 잠식된 노드는 제외합니다.
        /// </summary>
        /// <param name="resultNodes">결과를 저장할 목록입니다. 기존 요소는 제거됩니다.</param>
        public void GetAvailableNodes(List<MapNode> resultNodes)
        {
            if (resultNodes == null)
            {
                SWLog.LogError("[DungeonManager] GetAvailableNodes 실패: 결과 목록이 없습니다.");
                return;
            }

            resultNodes.Clear();

            if (dungeonState == null || dungeonState.MapGraph == null)
            {
                return;
            }

            if (dungeonState.CurrentNodeIdentifier < 0)
            {
                foreach (MapNode entryNode in dungeonState.MapGraph.EntryNodes)
                {
                    if (!entryNode.IsAshConsumed)
                    {
                        resultNodes.Add(entryNode);
                    }
                }

                return;
            }

            dungeonState.MapGraph.GetNextNodes(
                dungeonState.CurrentNodeIdentifier,
                resultNodes,
                false);
        }

        /// <summary>
        /// 이동 가능 여부를 확인하고 지정한 노드로 이동합니다.
        /// </summary>
        /// <param name="nodeIdentifier">이동할 노드의 식별자입니다.</param>
        /// <returns>노드로 이동했으면 true입니다.</returns>
        public bool MoveToNode(int nodeIdentifier)
        {
            if (currentPhase != EDungeonPhase.Map)
            {
                SWLog.LogWarning("[DungeonManager] MoveToNode 무시: 맵 선택 상태가 아닙니다.");
                return false;
            }

            GetAvailableNodes(availableNodeBuffer);
            MapNode targetNode = FindAvailableNode(nodeIdentifier);

            if (targetNode == null)
            {
                SWLog.LogWarning(
                    $"[DungeonManager] MoveToNode 실패: 식별자 {nodeIdentifier}의 노드로 이동할 수 없습니다.");
                return false;
            }

            dungeonState.SetCurrentNode(targetNode.Identifier);
            targetNode.SetVisited();

            SWLog.Log(
                $"[DungeonManager] 노드로 이동했습니다. 식별자: {targetNode.Identifier}, "
                + $"층: {targetNode.Floor}, 타입: {targetNode.NodeType}");
            OnNodeEntered?.Invoke(targetNode);

            HandleNodeEntry(targetNode);
            return true;
        }

        /// <summary>
        /// 이동 가능한 노드 중 지정한 식별자에 해당하는 노드를 반환합니다.
        /// </summary>
        /// <param name="nodeIdentifier">찾을 노드의 식별자입니다.</param>
        /// <returns>식별자에 해당하는 노드입니다. 노드가 없으면 null입니다.</returns>
        private MapNode FindAvailableNode(int nodeIdentifier)
        {
            foreach (MapNode candidateNode in availableNodeBuffer)
            {
                if (candidateNode.Identifier == nodeIdentifier)
                {
                    return candidateNode;
                }
            }

            return null;
        }

        /// <summary>
        /// 진입한 노드의 타입에 맞는 동작을 실행합니다.
        /// </summary>
        /// <param name="node">진입한 노드입니다.</param>
        private void HandleNodeEntry(MapNode node)
        {
            switch (node.NodeType)
            {
                case EMapNodeType.Battle:
                case EMapNodeType.Elite:
                case EMapNodeType.Boss:
                    StartBattleForNode(node);
                    break;

                case EMapNodeType.Rest:
                    SWLog.Log("[DungeonManager] 휴식 노드에 진입했습니다. 정신력 회복 기능은 아직 적용되지 않았습니다.");
                    LogAvailableNodes("휴식 완료");
                    RefreshMapViewState();
                    break;

                default:
                    SWLog.Log($"[DungeonManager] {node.NodeType} 노드에 진입했습니다.");
                    LogAvailableNodes("노드 통과");
                    RefreshMapViewState();
                    break;
            }
        }

        /// <summary>
        /// 이동 가능한 첫 번째 노드로 이동합니다.
        /// </summary>
        [SWButton("테스트: 첫 번째 노드로 이동")]
        private void MoveToFirstAvailableNode()
        {
            GetAvailableNodes(availableNodeBuffer);

            if (availableNodeBuffer.Count == 0)
            {
                SWLog.LogWarning("[DungeonManager] 이동 가능한 노드가 없습니다.");
                return;
            }

            MoveToNode(availableNodeBuffer[0].Identifier);
        }
        #endregion // 이동

        #region 전투
        /// <summary>
        /// 전투 노드에 사용할 적 조우 데이터를 선택하고 전투를 시작합니다.
        /// </summary>
        /// <param name="node">진입한 전투 노드입니다.</param>
        private void StartBattleForNode(MapNode node)
        {
            currentBattleNode = node;

            if (mapView != null)
            {
                mapView.Hide();
            }

            currentPhase = EDungeonPhase.Battle;

            int encounterIndex = SWRandom.Range(0, enemyEncounterDatas.Count);
            EnemyEncounterData enemyEncounterData = enemyEncounterDatas[encounterIndex];

            if (!battleManager.StartBattle(dungeonState, enemyEncounterData))
            {
                SWLog.LogError("[DungeonManager] 전투 시작 실패: 던전을 종료합니다.");
                EndDungeon(false);
            }
        }

        /// <summary>
        /// 전투 결과에 따라 던전을 종료하거나 맵 선택 상태로 복귀합니다.
        /// </summary>
        /// <param name="battleResult">종료된 전투의 결과입니다.</param>
        private void HandleBattleEnded(EBattleResult battleResult)
        {
            if (currentPhase != EDungeonPhase.Battle)
            {
                return;
            }

            if (battleResult != EBattleResult.Victory)
            {
                EndDungeon(false);
                return;
            }

            if (currentBattleNode != null && currentBattleNode.NodeType == EMapNodeType.Boss)
            {
                EndDungeon(true);
                return;
            }

            currentBattleNode = null;
            currentPhase = EDungeonPhase.Map;

            if (mapView != null)
            {
                mapView.Show();
                RefreshMapViewState();
            }

            LogAvailableNodes("맵 복귀");
        }
        #endregion // 전투

        #region 맵 표시
        /// <summary>
        /// 맵 화면의 노드 상태를 현재 던전 진행 상황에 맞게 갱신합니다.
        /// </summary>
        private void RefreshMapViewState()
        {
            if (mapView == null || dungeonState == null)
            {
                return;
            }

            GetAvailableNodes(availableNodeBuffer);
            mapView.RefreshNodeStates(dungeonState.CurrentNodeIdentifier, availableNodeBuffer);
        }
        #endregion // 맵 표시

        #region 로그
        /// <summary>
        /// 현재 이동할 수 있는 노드 목록을 로그로 출력합니다.
        /// </summary>
        /// <param name="logContext">로그에 표시할 현재 상황입니다.</param>
        private void LogAvailableNodes(string logContext)
        {
            GetAvailableNodes(availableNodeBuffer);

            if (availableNodeBuffer.Count == 0)
            {
                SWLog.Log($"[DungeonManager] {logContext}: 이동 가능한 노드가 없습니다.");
                return;
            }

            stringBuilder.Clear();
            stringBuilder.Append($"[DungeonManager] {logContext} - 이동 가능한 노드: ");

            foreach (MapNode node in availableNodeBuffer)
            {
                stringBuilder.Append($"[{node.Identifier}|층 {node.Floor} {node.NodeType}] ");
            }

            SWLog.Log(stringBuilder.ToString());
        }
        #endregion // 로그
    }
}
