using System;
using System.Collections.Generic;
using System.Text;
using EchoesOfAsh.Battle;
using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Map;
using EchoesOfAsh.Save;
using EchoesOfAsh.View.UI;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoesOfAsh.Dungeon
{
    /// <summary>
    /// 던전의 시작과 종료, 맵 이동, 노드 화면, 전투 전환, 런 중 저장을 관리합니다.
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
            /// <summary>휴식, 이벤트, 보관 등 노드 화면을 진행하는 상태입니다.</summary>
            Node,
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
        [SerializeField] private PartyData partyData;
        [Tooltip("파티를 구성할 캐릭터 목록입니다 (임시 조치 — P2-M4 4-4 편성 화면으로 대체 예정)")]
        [SerializeField] private List<CharacterData> characterDatas = new();
        [Tooltip("카드 데이터베이스입니다. 저장된 덱을 코드명으로 복원할 때 사용합니다.")]
        [SerializeField] private SWIODatabase cardDatabase;
        [Tooltip("편성 화면에서 선택할 수 있는 보유 캐릭터 목록입니다 (임시 조치 — 메타 저장 도입 시 대체)")]
        [SerializeField] private List<CharacterData> availableCharacters = new();
        [Tooltip("캐릭터 데이터베이스입니다. 저장된 파티를 코드명으로 복원할 때 사용합니다 (카드 DB와 같은 에셋 연결 가능)")]
        [SerializeField] private SWIODatabase characterDatabase;
        [Tooltip("아이템 codeName 복원용 데이터베이스입니다 (저장 스키마 v3)")]
        [SerializeField] private SWIODatabase itemDatabase;
        [Tooltip("유물 복원용 데이터베이스입니다. 저장된 코드명으로 유물을 되찾습니다.")]
        [SerializeField] private SWIODatabase relicDatabase;

        [SWGroup("던전 구성")]
        [Tooltip("던전 생성에 사용할 시드입니다. 0이면 실행 시 무작위 시드를 생성합니다.")]
        [SerializeField] private int dungeonSeed;
        [Tooltip("이 던전의 챕터 구성 데이터입니다. 맵 규칙, 조우 풀, 노드 이벤트, 정신력 이벤트, 상태이상 정의를 소유합니다.")]
        [SerializeField] private DungeonChapterData chapterData;
        [Tooltip("던전 시작 시 사용할 카드 목록입니다.")]
        [SerializeField] private List<CardData> startingCards = new();

        [SWGroup("뷰")]
        [SerializeField] private MapView mapView;
        [SerializeField] private NodeScreenView nodeScreenView;
        [SerializeField] private PartySetupView partySetupView;

        [SWGroup("씬")]
        [Tooltip("마을 복귀 시 로드할 씬 이름입니다.")]
        [SerializeField] private string townSceneName = "Town";

        [SWGroup("테스트")]
        [Tooltip("테스트 유물 획득 버튼이 사용할 유물입니다 (임시 조치 - 보상 화면 도입 시 제거).")]
        [SerializeField] private RelicData testRelicData;

        private DungeonState dungeonState;
        private EDungeonPhase currentPhase = EDungeonPhase.None;
        private MapNode currentBattleNode;
        private EnemyEncounterData currentEncounterData;
        private DungeonEventData currentEventData;
        private bool isBattleEventSubscribed;

        private readonly List<CharacterData> selectedParty = new();
        private readonly List<MapNode> availableNodeBuffer = new();
        private readonly List<ItemStackData> dropRollBuffer = new();
        private readonly StringBuilder stringBuilder = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 챕터의 맵 생성 규칙입니다. 챕터가 없으면 null입니다.</summary>
        private MapConfigData MapConfig => chapterData != null ? chapterData.MapConfigData : null;

        /// <summary>현재 던전 상태입니다. 던전을 시작하지 않았으면 null입니다.</summary>
        public DungeonState DungeonState => dungeonState;
        /// <summary>현재 던전 화면의 진행 상태입니다.</summary>
        public EDungeonPhase CurrentPhase => currentPhase;
        /// <summary>던전을 진행하고 있는지 여부입니다.</summary>
        public bool IsDungeonRunning => currentPhase == EDungeonPhase.Map
            || currentPhase == EDungeonPhase.Node
            || currentPhase == EDungeonPhase.Battle;

        /// <summary>파티가 광기 구간인지 여부입니다. 광기 복도 통행 판정에 사용합니다.</summary>
        public bool IsPartyMadness => dungeonState != null && partyData != null
            && dungeonState.CarriedSanity < partyData.SanityThreshold;

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
        /// 씬 시작 시 마을에서 전달된 출발 요청을 소비합니다. 요청이 없으면 아무것도 하지 않습니다 (씬 단독 테스트 보존).
        /// </summary>
        private void Start()
        {
            EDungeonLaunchMode launchMode = DungeonLaunchRequest.Consume();

            switch (launchMode)
            {
                case EDungeonLaunchMode.NewDungeon:
                    OpenPartySetup();
                    break;
                case EDungeonLaunchMode.Resume:
                    ResumeDungeon();
                    break;
            }
        }

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

        #region 파티 편성
        /// <summary>
        /// 파티 편성 화면을 표시합니다. 화면이 없으면 기본 파티로 바로 시작합니다.
        /// </summary>
        [SWButton("파티 편성 후 던전 시작")]
        public void OpenPartySetup()
        {
            if (IsDungeonRunning)
            {
                SWLog.LogWarning("[DungeonManager] OpenPartySetup 무시: 던전이 이미 진행 중입니다.");
                return;
            }

            RefreshAvailableCharactersFromTown();

            if (partySetupView == null || availableCharacters.Count == 0)
            {
                SWLog.Log("[DungeonManager] 편성 화면 미배선: 기본 파티로 던전을 시작합니다.");
                StartDungeon();
                return;
            }

            partySetupView.Show(availableCharacters, startingCards, HandlePartyConfirmed);
        }

        /// <summary>
        /// 편성 확정을 처리하고 던전을 시작합니다.
        /// </summary>
        /// <param name="members">확정된 파티 구성입니다 (선택 순서 = 파티 순서).</param>
        private void HandlePartyConfirmed(List<CharacterData> members)
        {
            selectedParty.Clear();
            selectedParty.AddRange(members);

            if (partySetupView != null)
            {
                partySetupView.Hide();
            }

            StartDungeon();
        }

        /// <summary>
        /// 마을 저장의 보유 캐릭터 명단으로 편성 후보를 갱신합니다.
        /// 명단이 비어 있으면 인스펙터 목록을 유지합니다 (구저장·씬 단독 테스트 폴백). 등록 순서 = 영입 순서입니다.
        /// </summary>
        private void RefreshAvailableCharactersFromTown()
        {
            List<string> ownedCodeNames = TownSaveService.Current.ownedCharacterCodeNames;

            if (ownedCodeNames == null || ownedCodeNames.Count == 0)
            {
                return;
            }

            if (characterDatabase == null)
            {
                SWLog.LogWarning("[DungeonManager] 보유 캐릭터 갱신 실패: 캐릭터 데이터베이스가 없습니다 - 인스펙터 목록을 유지합니다.");
                return;
            }

            availableCharacters.Clear();

            foreach (string codeName in ownedCodeNames)
            {
                CharacterData characterData = characterDatabase.GetDataByCodeName<CharacterData>(codeName);

                if (characterData == null)
                {
                    SWLog.LogWarning($"[DungeonManager] 보유 캐릭터 갱신: 코드명 '{codeName}' 캐릭터를 찾지 못해 건너뜁니다.");
                    continue;
                }

                availableCharacters.Add(characterData);
            }
        }
        #endregion // 파티 편성

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

            if (battleManager == null || partyData == null)
            {
                SWLog.LogError("[DungeonManager] StartDungeon 실패: 필수 참조가 없습니다.");
                return;
            }

            if (chapterData == null || MapConfig == null)
            {
                SWLog.LogError("[DungeonManager] StartDungeon 실패: 챕터 구성 데이터 또는 맵 규칙이 없습니다.");
                return;
            }

            List<CharacterData> partyMembers = selectedParty.Count > 0 ? selectedParty : characterDatas;

            if (partyMembers.Count == 0)
            {
                SWLog.LogError("[DungeonManager] StartDungeon 실패: 파티 캐릭터 목록이 비어 있습니다.");
                return;
            }

            int seed = dungeonSeed != 0 ? dungeonSeed : Environment.TickCount;
            SWRandom.SetSeed(seed);

            dungeonState = new DungeonState(seed, partyData, partyMembers, startingCards, chapterData.SanityEventDatas);
            MapGenerator mapGenerator = new MapGenerator();
            MapGraph mapGraph = mapGenerator.GenerateMapGraph(MapConfig);

            if (mapGraph == null)
            {
                SWLog.LogError("[DungeonManager] StartDungeon 실패: 맵을 생성하지 못했습니다.");
                return;
            }

            dungeonState.SetCarriedSanity(partyData.StartSanity);
            dungeonState.SetMapGraph(mapGraph);
            currentBattleNode = null;
            currentEventData = null;
            currentPhase = EDungeonPhase.Map;

            battleManager.SetChapterStatusDatas(chapterData.StatusDatas);
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
            SaveDungeon();
        }

        /// <summary>
        /// 저장된 스냅샷으로 던전을 복원하고 맵 선택 상태로 전환합니다.
        /// 진입 처리가 완료되지 않은 노드는 진입 처리를 다시 실행합니다.
        /// </summary>
        [SWButton("던전 이어하기")]
        public void ResumeDungeon()
        {
            if (IsDungeonRunning)
            {
                SWLog.LogWarning("[DungeonManager] ResumeDungeon 무시: 던전이 이미 진행 중입니다.");
                return;
            }

            if (battleManager == null || chapterData == null || MapConfig == null
                || partyData == null || cardDatabase == null)
            {
                SWLog.LogError("[DungeonManager] ResumeDungeon 실패: 필수 참조가 없습니다.");
                return;
            }

            DungeonSaveData saveData = DungeonSaveService.Load();

            if (saveData == null)
            {
                SWLog.LogWarning("[DungeonManager] ResumeDungeon 실패: 로드할 저장 데이터가 없습니다.");
                return;
            }

            MapGraph mapGraph = new MapGraph();

            if (!mapGraph.RestoreFrom(saveData.mapNodes, saveData.mapEdges))
            {
                SWLog.LogError("[DungeonManager] ResumeDungeon 실패: 맵을 복원하지 못했습니다.");
                return;
            }

            // 저장된 파티 복원 (스키마 v2). 구버전 저장은 인스펙터 기본 파티로 폴백합니다
            List<CharacterData> partyMembers = characterDatas;

            if (saveData.partyCharacterCodeNames != null && saveData.partyCharacterCodeNames.Count > 0)
            {
                if (characterDatabase == null)
                {
                    SWLog.LogError("[DungeonManager] ResumeDungeon 실패: 캐릭터 데이터베이스가 없습니다.");
                    return;
                }

                List<CharacterData> restoredParty = new();

                foreach (string codeName in saveData.partyCharacterCodeNames)
                {
                    CharacterData characterData = characterDatabase.GetDataByCodeName<CharacterData>(codeName);

                    if (characterData == null)
                    {
                        SWLog.LogError($"[DungeonManager] ResumeDungeon 실패: 코드명 '{codeName}' 캐릭터를 찾지 못했습니다.");
                        return;
                    }

                    restoredParty.Add(characterData);
                }

                partyMembers = restoredParty;
            }
            else
            {
                SWLog.LogWarning("[DungeonManager] 구버전 저장: 파티를 인스펙터 기본 목록으로 복원합니다.");
            }
            
            // 재개 후 난수는 비연속 (P2-D1 스냅샷 - 같은 시드 재설정 시 소비된 난수열 재등장 방지)
            SWRandom.SetSeed(Environment.TickCount);
            dungeonState = new DungeonState(saveData.seed, partyData, partyMembers, startingCards, chapterData.SanityEventDatas);


            foreach (DungeonCardSaveData cardSave in saveData.deckCards)
            {
                CardData cardData = cardDatabase.GetDataByCodeName<CardData>(cardSave.cardCodeName);

                if (cardData == null)
                {
                    SWLog.LogError($"[DungeonManager] ResumeDungeon 실패: 코드명 '{cardSave.cardCodeName}' 카드를 찾지 못했습니다.");
                    return;
                }

                dungeonState.AddCard(new CardInstance(cardData, cardSave.isUpgrade));
            }

            // 소지 드랍 복원 (P2-M6)
            foreach (ItemCountSaveData itemSave in saveData.carriedItems)
            {
                ItemData itemData = itemDatabase.GetDataByCodeName<ItemData>(itemSave.codeName);

                if (itemData == null)
                {
                    SWLog.LogWarning($"[DungeonManager] 코드명 '{itemSave.codeName}' 아이템을 찾지 못해 건너뜁니다.");
                    continue;
                }

                dungeonState.AddCarriedItem(itemData, itemSave.count);
            }

            // 보유 유물 복원 (P2-M7 - 미등록 코드명은 경고 후 건너뜁니다: 유물 유실은 런 진행을 막지 않습니다)
            foreach (string relicCodeName in saveData.relicCodeNames)
            {
                RelicData relicData = relicDatabase != null
                    ? relicDatabase.GetDataByCodeName<RelicData>(relicCodeName)
                    : null;

                if (relicData == null)
                {
                    SWLog.LogWarning($"[DungeonManager] 코드명 '{relicCodeName}' 유물을 찾지 못해 건너뜁니다.");
                    continue;
                }

                dungeonState.AddRelic(relicData);
            }

            if (dungeonState.Deck.Count == 0)
            {
                SWLog.LogError("[DungeonManager] ResumeDungeon 실패: 복원한 덱이 비어 있습니다.");
                return;
            }

            dungeonState.SetMapGraph(mapGraph);
            dungeonState.RestoreProgress(
                saveData.currentNodeIdentifier,
                saveData.isCurrentNodeResolved,
                saveData.carriedSanity,
                saveData.moveCount,
                saveData.ashConsumedFloor,
                saveData.hasMadnessEventOccurred);

            currentBattleNode = null;
            currentEncounterData = null;
            currentEventData = null;
            currentPhase = EDungeonPhase.Map;

            battleManager.SetChapterStatusDatas(chapterData.StatusDatas);
            SubscribeBattleEvents();

            SWLog.Log($"[DungeonManager] 던전을 복원했습니다. 시드: {saveData.seed}, "
                + $"노드: {saveData.currentNodeIdentifier}, 진입 처리 완료: {saveData.isCurrentNodeResolved}");
            OnDungeonStarted?.Invoke();

            if (mapView != null)
            {
                mapView.Initialize(dungeonState.MapGraph, nodeIdentifier => MoveToNode(nodeIdentifier));
                mapView.Show();
                RefreshMapViewState();
            }

            LogAvailableNodes("저장 복원");

            if (!saveData.isCurrentNodeResolved)
            {
                MapNode unresolvedNode = FindNodeByIdentifier(saveData.currentNodeIdentifier);

                if (unresolvedNode != null)
                {
                    SWLog.Log("[DungeonManager] 진입 처리가 완료되지 않은 노드를 다시 실행합니다.");
                    HandleNodeEntry(unresolvedNode);
                }
            }
        }

        /// <summary>
        /// 던전을 종료하고 저장 파일을 삭제한 뒤 결과를 알립니다.
        /// </summary>
        /// <param name="isVictory">던전에서 승리했는지 여부입니다.</param>
        private void EndDungeon(bool isVictory)
        {
            ResolveCarriedItems(isVictory);

            if (mapView != null)
            {
                mapView.Hide();
            }

            if (nodeScreenView != null)
            {
                nodeScreenView.Hide();
            }

            currentPhase = EDungeonPhase.Ended;
            currentBattleNode = null;
            currentEncounterData = null;
            currentEventData = null;

            selectedParty.Clear();

            // 런 종료 = 스냅샷 소멸. 회수/해금 반영은 메타 저장 소관 (P2-M6/M7)
            DungeonSaveService.DeleteSave();

            SWLog.Log($"[DungeonManager] 던전을 종료했습니다. 결과: {(isVictory ? "승리" : "패배")}");
            OnDungeonEnded?.Invoke(isVictory);
        }

        /// <summary>
        /// 마을 씬으로 복귀합니다. 진행 중인 던전에서는 호출할 수 없습니다 (중간 탈출 없음 - 기획서 7-2).
        /// </summary>
        [SWButton("마을로 복귀")]
        public void ReturnToTown()
        {
            if (IsDungeonRunning)
            {
                SWLog.LogWarning("[DungeonManager] ReturnToTown 무시: 던전 진행 중에는 복귀할 수 없습니다.");
                return;
            }

            SceneManager.LoadScene(townSceneName);
        }
        #endregion // 던전

        #region 이동
        /// <summary>
        /// 현재 위치에서 이동할 수 있는 노드를 결과 목록에 추가합니다.
        /// 잿불에 잠식된 노드는 제외하고, 광기 전용 경로는 파티가 광기 구간일 때만 포함합니다.
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
                IsPartyMadness);
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

            if (!AdvanceAshErosion(targetNode))
            {
                return true;
            }

            // 진입 확정 직후, 처리 직전 저장 - 복원 시 진입 처리를 다시 실행 (노드 스킵 불가)
            SaveDungeon();

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
        /// 맵의 모든 노드에서 지정한 식별자에 해당하는 노드를 반환합니다.
        /// </summary>
        /// <param name="nodeIdentifier">찾을 노드의 식별자입니다.</param>
        /// <returns>식별자에 해당하는 노드입니다. 노드가 없으면 null입니다.</returns>
        private MapNode FindNodeByIdentifier(int nodeIdentifier)
        {
            if (dungeonState == null || dungeonState.MapGraph == null)
            {
                return null;
            }

            foreach (MapNode node in dungeonState.MapGraph.Nodes)
            {
                if (node.Identifier == nodeIdentifier)
                {
                    return node;
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
                    ShowNodeScreen(GetNodeEventData(EMapNodeType.Rest), "휴식");
                    break;

                case EMapNodeType.Event:
                    ShowNodeScreen(GetNodeEventData(EMapNodeType.Event), "이벤트");
                    break;

                case EMapNodeType.Storage:
                    TransferCarriedToStorage();
                    ShowNodeScreen(GetNodeEventData(EMapNodeType.Storage), "보관");
                    break;
                default:
                    SWLog.Log($"[DungeonManager] {node.NodeType} 노드에 진입했습니다.");
                    LogAvailableNodes("노드 통과");
                    RefreshMapViewState();
                    MarkNodeResolvedAndSave();
                    break;
            }
        }

        /// <summary>
        /// 챕터에서 노드 타입에 매핑된 이벤트를 조회합니다. 챕터가 없으면 null (통과 처리)입니다.
        /// </summary>
        /// <param name="nodeType">진입한 노드의 타입입니다.</param>
        /// <returns>표시할 이벤트 데이터입니다. 없으면 null입니다.</returns>
        private DungeonEventData GetNodeEventData(EMapNodeType nodeType)
        {
            return chapterData != null ? chapterData.GetRandomEventData(nodeType) : null;
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

        #region 노드 화면
        /// <summary>
        /// 선택지형 노드 화면을 표시합니다. 뷰나 데이터가 없으면 통과 처리합니다.
        /// </summary>
        /// <param name="eventData">표시할 이벤트 데이터입니다.</param>
        /// <param name="nodeContext">로그에 표시할 노드 상황입니다.</param>
        private void ShowNodeScreen(DungeonEventData eventData, string nodeContext)
        {
            if (nodeScreenView == null || eventData == null)
            {
                SWLog.Log($"[DungeonManager] {nodeContext} 노드 통과: 뷰 또는 데이터가 없습니다.");
                LogAvailableNodes("노드 통과");
                RefreshMapViewState();
                MarkNodeResolvedAndSave();
                return;
            }

            currentPhase = EDungeonPhase.Node;
            currentEventData = eventData;
            nodeScreenView.Show(eventData, HandleEventChoiceSelected);
        }

        /// <summary>
        /// 노드 화면의 선택지 선택을 처리하고 맵 선택 상태로 복귀합니다.
        /// </summary>
        /// <param name="choiceIndex">선택한 선택지 인덱스입니다.</param>
        private void HandleEventChoiceSelected(int choiceIndex)
        {
            if (currentEventData == null || choiceIndex < 0 || choiceIndex >= currentEventData.Choices.Count)
            {
                SWLog.LogError($"[DungeonManager] 노드 화면 선택 실패: 인덱스 {choiceIndex}가 유효하지 않습니다.");
                return;
            }

            DungeonEventChoice selectedChoice = currentEventData.Choices[choiceIndex];
            SWLog.Log($"[DungeonManager] 노드 화면 선택: '{selectedChoice.ChoiceText}'");

            if (nodeScreenView != null)
            {
                nodeScreenView.Hide();
            }

            currentEventData = null;
            currentPhase = EDungeonPhase.Map;

            if (selectedChoice.SanityDelta != 0)
            {
                ChangeDungeonSanity(selectedChoice.SanityDelta);
            }

            LogAvailableNodes("노드 화면 완료");
            RefreshMapViewState();
            MarkNodeResolvedAndSave();
        }
        #endregion // 노드 화면

        #region 정신력
        /// <summary>
        /// 던전 수위에서 파티 정신력을 변화시킵니다.
        /// 전투 중에는 전투 정신력이 진실 원본이므로 호출을 무시합니다.
        /// 상한 보정은 전투 진입 시 정신력 홀더 생성 과정에서 처리합니다.
        /// </summary>
        /// <param name="delta">변화량입니다.</param>
        public void ChangeDungeonSanity(int delta)
        {
            if (dungeonState == null)
            {
                SWLog.LogWarning("[DungeonManager] ChangeDungeonSanity 무시: 던전 상태가 없습니다.");
                return;
            }

            if (currentPhase == EDungeonPhase.Battle)
            {
                SWLog.LogWarning("[DungeonManager] ChangeDungeonSanity 무시: 전투 중에는 전투 정신력이 진실 원본입니다.");
                return;
            }

            dungeonState.SetCarriedSanity(dungeonState.CarriedSanity + delta);

            SWLog.Log($"[DungeonManager] 파티 정신력이 변화했습니다. "
                + $"현재: {dungeonState.CarriedSanity}, 광기 여부: {IsPartyMadness}");
            RefreshMapViewState();
        }

        /// <summary>
        /// 광기 통행 검증용으로 파티 정신력을 20 감소시킵니다.
        /// </summary>
        [SWButton("테스트: 정신력 -20")]
        private void TestReduceSanity()
        {
            ChangeDungeonSanity(-20);
        }
        #endregion // 정신력

        #region 저장
        /// <summary>
        /// 현재 던전 상태를 스냅샷으로 저장합니다.
        /// </summary>
        private void SaveDungeon()
        {
            if (dungeonState == null)
            {
                return;
            }

            DungeonSaveService.Save(dungeonState);
        }

        /// <summary>
        /// 현재 노드의 진입 처리를 완료로 기록하고 저장합니다.
        /// </summary>
        private void MarkNodeResolvedAndSave()
        {
            if (dungeonState == null)
            {
                return;
            }

            dungeonState.SetCurrentNodeResolved();
            SaveDungeon();
        }
        #endregion // 저장

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

            EnemyEncounterData enemyEncounterData = chapterData.GetRandomEncounter(node.NodeType);

            if (enemyEncounterData == null)
            {
                SWLog.LogError("[DungeonManager] 전투 시작 실패: 사용할 조우가 없어 던전을 종료합니다.");
                EndDungeon(false);
                return;
            }

            currentEncounterData = enemyEncounterData;

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

            // 드랍 굴림 - 보스 포함 모든 승리 직후 소지 목록으로 이동합니다 (회수 판정은 던전 종료 시)
            if (currentEncounterData != null && currentEncounterData.DropTable != null)
            {
                dropRollBuffer.Clear();
                currentEncounterData.DropTable.Roll(dropRollBuffer);

                foreach (ItemStackData drop in dropRollBuffer)
                {
                    dungeonState.AddCarriedItem(drop.ItemData, drop.Count);
                    SWLog.Log($"[DungeonManager] 드랍 획득: {drop.ItemData.DisplayName} x{drop.Count}");
                }
            }

            currentEncounterData = null;

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
            MarkNodeResolvedAndSave();
        }
        #endregion // 전투

        #region 잿불 침식
        /// <summary>
        /// 이동 횟수를 누적하고 간격에 도달하면 잿불 침식을 한 층 전진시킵니다.
        /// 침식이 현재 층에 도달하면 던전을 패배로 종료합니다.
        /// </summary>
        /// <param name="currentNode">이동을 마친 현재 노드입니다.</param>
        /// <returns>던전을 계속 진행할 수 있으면 true입니다.</returns>
        private bool AdvanceAshErosion(MapNode currentNode)
        {
            int advanceInterval = chapterData.MapConfigData.AshAdvanceInterval;

            if (advanceInterval <= 0)
            {
                return true;
            }

            int moveCount = dungeonState.IncrementMoveCount();

            if (moveCount % advanceInterval != 0)
            {
                return true;
            }

            int consumedFloor = dungeonState.AshConsumedFloor + 1;
            dungeonState.SetAshConsumedFloor(consumedFloor);
            dungeonState.MapGraph.ConsumeFloorsByAsh(consumedFloor);

            SWLog.Log($"[DungeonManager] 잿불 침식이 전진했습니다. 잠식 층: 0~{consumedFloor}");

            if (currentNode.Floor <= consumedFloor)
            {
                SWLog.Log("[DungeonManager] 파티가 잿불에 잠식되었습니다. 던전을 종료합니다.");
                EndDungeon(false);
                return false;
            }

            RefreshMapViewState();
            return true;
        }
        #endregion // 잿불 침식

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

        /// <summary>
        /// 소지 아이템 전량을 거점(메타)으로 전송합니다. 전송 즉시 저장되므로 이후 사망해도 보존됩니다.
        /// </summary>
        private void TransferCarriedToStorage()
        {
            if (dungeonState == null || dungeonState.CarriedItems.Count == 0)
            {
                SWLog.Log("[DungeonManager] 보관 전송: 소지 아이템이 없습니다.");
                return;
            }

            foreach (ItemStackData stack in dungeonState.CarriedItems)
            {
                TownSaveService.AddItem(stack.ItemData.CodeName, stack.Count);
            }

            TownSaveService.Save();
            SWLog.Log($"[DungeonManager] 보관 전송 완료: {dungeonState.CarriedItems.Count}종을 거점으로 보냈습니다.");
            dungeonState.ClearCarriedItems();
        }

        /// <summary>
        /// 던전 종료 시 소지 아이템의 회수를 판정합니다.
        /// 승리 = 전량 회수, 패배 = 기본 자원만 회수하고 나머지는 소실됩니다 (기획서 2-1).
        /// </summary>
        /// <param name="isVictory">던전 승리 여부입니다.</param>
        private void ResolveCarriedItems(bool isVictory)
        {
            if (dungeonState == null || dungeonState.CarriedItems.Count == 0)
            {
                return;
            }

            int recoveredCount = 0;

            foreach (ItemStackData stack in dungeonState.CarriedItems)
            {
                if (!isVictory && !stack.ItemData.IsBaseResource)
                {
                    SWLog.Log($"[DungeonManager] 회수 실패(소실): {stack.ItemData.DisplayName} x{stack.Count}");
                    continue;
                }

                TownSaveService.AddItem(stack.ItemData.CodeName, stack.Count);
                recoveredCount++;
            }

            if (recoveredCount > 0)
            {
                TownSaveService.Save();
            }

            SWLog.Log($"[DungeonManager] 회수 판정 완료: {recoveredCount}종 회수 ({(isVictory ? "생존 귀환" : "기본 자원만")})");
            dungeonState.ClearCarriedItems();
        }

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

        /// <summary>
        /// 테스트 유물을 획득합니다 (임시 조치 - 보상 화면 도입 시 제거). 획득 반영은 다음 전투 시작부터입니다.
        /// </summary>
        [SWButton("테스트 유물 획득")]
        public void AddTestRelic()
        {
            if (dungeonState == null)
            {
                SWLog.LogWarning("[DungeonManager] AddTestRelic 무시: 진행 중인 던전이 없습니다.");
                return;
            }

            if (dungeonState.AddRelic(testRelicData))
            {
                SWLog.Log($"[DungeonManager] 유물 획득: {testRelicData.DisplayName}");
                DungeonSaveService.Save(dungeonState);
            }
        }
    }
}