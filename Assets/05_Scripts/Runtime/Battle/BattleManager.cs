using System;
using System.Collections.Generic;
using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using EchoesOfAsh.Deck;
using EchoesOfAsh.Dungeon;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using EchoesOfAsh.Sanity;
using EchoesOfAsh.View;
using EchoesOfAsh.View.UI;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 전투 매니저입니다.
    /// </summary>
    public class BattleManager : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("데이터")]
        [SerializeField] private PartyData partyData;
        [SerializeField] private BattleBalanceData balanceData;
        [Tooltip("1인기준으로 테스트, 나중에 확장")]
        [SerializeField] private CharacterData characterData;

        [SWGroup("배치")]
        [SerializeField] private Transform characterRoot;
        [SerializeField] private Transform enemyRoot;

        [SWGroup("뷰")]
        [SerializeField] private HandView handView;
        [SerializeField] private EnemyView enemyViewPrefab;
        [SerializeField] private PartyStatusView partyStatusView;
        [SerializeField] private CardTooltipView cardTooltipView;
        [SerializeField] private BattleHUDView battleHUDView;
        [SerializeField] private MadnessOverlayView madnessOverlayView;

        private CharacterEntity characterEntity;

        private SanityHolder partySanityHolder;
        private DeckSystem deckSystem;
        private ApSystem apSystem;
        private EffectExecutor effectExecutor;
        private CardPlayService cardPlayService;
        private TargetResolver targetResolver;
        private TurnManager turnManager;
        private MadnessEventRunner madnessEventRunner;

        private DungeonState dungeonState;
        private EnemyEncounterData currentEncounter;

        private EBattleResult battleResult = EBattleResult.None;
        private bool isBattleRunning;

        private readonly List<CharacterEntity> party = new();
        private readonly List<EnemyEntity> enemyEntities = new();
        private readonly List<EnemyAI> enemyAIs = new();
        private readonly List<ITargetable> cardTargetBuffer = new();
        private readonly List<ITargetable> enemyTargetBuffer = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>전투 진행 여부입니다.</summary>
        public bool IsBattleRunning => isBattleRunning;
        /// <summary>전투 결과 타입입니다.</summary>
        public EBattleResult BattleResult => battleResult;

        /// <summary>파티원 엔티티입니다.</summary>
        public CharacterEntity Character => characterEntity;
        /// <summary>적 엔티티 목록 (스폰 순서 = 행동 순서)입니다.</summary>
        public IReadOnlyList<EnemyEntity> EnemyEntities => enemyEntities;
        /// <summary>적 인공지능 목록 (적 엔티티 목록 인덱스와 일치)입니다.</summary>
        public IReadOnlyList<EnemyAI> EnemyAIs => enemyAIs;

        /// <summary>파티 공유 정신력입니다.</summary>
        public ISanityHolder PartySanityHolder => partySanityHolder;
        /// <summary>덱 시스템입니다.</summary>
        public DeckSystem DeckSystem => deckSystem;
        /// <summary>AP 시스템입니다.</summary>
        public ApSystem ApSystem => apSystem;
        /// <summary>카드 사용 파이프라인입니다.</summary>
        public CardPlayService CardPlayService => cardPlayService;
        /// <summary>턴 매니저입니다.</summary>
        public TurnManager TurnManager => turnManager;

        /// <summary>전투 시작 시 호출됩니다.</summary>
        public event Action OnBattleStarted;
        /// <summary>전투 종료 시 호출됩니다.</summary>
        public event Action<EBattleResult> OnBattleEnded;
        #endregion // 프로퍼티

        #region 초기화
        private void OnDestroy()
        {
            ResetBattle();
        }

        /// <summary>
        /// 전투 상태를 초기화합니다.
        /// </summary>
        public void ResetBattle()
        {
            isBattleRunning = false;

            if (handView != null)
            {
                handView.Release();
            }

            if (partyStatusView != null)
            {
                partyStatusView.Release();
            }

            if (cardTooltipView != null)
            {
                cardTooltipView.Release();
            }

            if (battleHUDView != null)
            {
                battleHUDView.Release();
            }

            if (madnessOverlayView != null)
            {
                madnessOverlayView.Release();
            }

            if (turnManager != null)
            {
                turnManager.OnTurnStarted -= HandleTurnStarted;
                turnManager.OnEnemyActionsStarted -= HandleEnemyActionsStarted;
                turnManager.OnRoundEnded -= HandleRoundEnded;

                if (madnessEventRunner != null)
                {
                    turnManager.OnTurnStartHook -= madnessEventRunner.HandleTurnStartHook;
                    madnessEventRunner = null;
                }

                turnManager = null;
            }

            deckSystem = null;
            apSystem = null;
            effectExecutor = null;
            cardPlayService = null;
            targetResolver = null;

            partySanityHolder?.Dispose();
            partySanityHolder = null;

            DestroyEntity(ref characterEntity);
            party.Clear();

            for (int i = 0; i < enemyEntities.Count; i++)
            {
                EnemyEntity enemyEntity = enemyEntities[i];
                DestroyEntity(ref enemyEntity);
            }

            enemyEntities.Clear();
            enemyAIs.Clear();
        }

        /// <summary>
        /// 생성한 엔티티를 제거합니다.
        /// </summary>
        /// <typeparam name="TBattleEntity">제거할 엔티티입니다.</typeparam>
        /// <param name="battleEntity">제거할 엔티티입니다.</param>
        private void DestroyEntity<TBattleEntity>(ref TBattleEntity battleEntity)
           where TBattleEntity : BattleEntity
        {
            if (battleEntity == null)
            {
                battleEntity = null;
                return;
            }

            GameObject battleEntityGameObject = battleEntity.gameObject;

            battleEntity.ResetEntity();
            battleEntity = null;

            if (battleEntityGameObject != null)
            {
                Destroy(battleEntityGameObject);
            }
        }
        #endregion // 초기화

        #region 전투
        /// <summary>
        /// 전투 시작합니다.
        /// </summary>
        /// <returns>시작 성공 여부입니다.</returns>
        public bool StartBattle(DungeonState dungeonState, EnemyEncounterData encounter)
        {
            if (isBattleRunning)
            {
                SWLog.LogWarning("[BattleManager] StartBattle 무시: 이미 전투가 진행 중입니다");
                return false;
            }

            this.dungeonState = dungeonState;
            this.currentEncounter = encounter;

            if (!ValidateData())
            {
                return false;
            }

            // 이전 전투 상태 초기화
            ResetBattle();

            battleResult = EBattleResult.None;

            SetupParty();
            SetupEnemies();
            SetupSystems();

            isBattleRunning = true;
            OnBattleStarted?.Invoke();

            turnManager.StartBattle();
            return true;
        }

        /// <summary>
        /// 전투 시작에 필요한 데이터를 검증합니다.
        /// </summary>
        /// <returns>검증 성공 여부입니다.</returns>
        private bool ValidateData()
        {
            if (partyData == null || balanceData == null || characterData == null)
            {
                SWLog.LogError("[BattleManager] 데이터 검증 실패 참조가 비어있습니다");
                return false;
            }

            if (dungeonState == null || dungeonState.Deck.Count == 0)
            {
                SWLog.LogError("[BattleManager] 검증 실패: 런 상태가 없거나 덱이 비어 있습니다");
                return false;
            }

            if (currentEncounter == null || currentEncounter.EnemyCount == 0)
            {
                SWLog.LogError("[BattleManager] 검증 실패: 조우 데이터가 비어 있습니다");
                return false;
            }

            if (currentEncounter.EnemyCount > 3)
            {
                SWLog.LogError($"[BattleManager] 조우 적 {currentEncounter.EnemyCount}체입니다 - 기준 1~3");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 파티원 생성 및 정신력 설정합니다.
        /// </summary>
        public void SetupParty()
        {
            characterEntity = new GameObject(characterData.name).AddComponent<CharacterEntity>();

            if (characterRoot != null)
            {
                characterEntity.transform.SetParent(characterRoot, false);
            }

            characterEntity.Init(characterData);
            characterEntity.OnDied += HandleCharacterDied;

            party.Add(characterEntity);

            int startSanity = dungeonState.HasCarriedSanity ? dungeonState.CarriedSanity : partyData.StartSanity;
            partySanityHolder = new SanityHolder(partyData.MaxSanityStat, partyData.SanityThreshold, startSanity);
        }

        /// <summary>
        /// 조우 데이터에 따라 적을 생성하고 배치합니다.
        /// </summary>
        private void SetupEnemies()
        {
            foreach (var entry in currentEncounter.Entries)
            {
                if (entry == null || entry.EnemyData == null)
                {
                    SWLog.LogError("[BattleManager] 조우 항목에 null Enemy가 있습니다");
                    continue;
                }

                EnemyEntity enemyEntity = new GameObject(entry.EnemyData.name).AddComponent<EnemyEntity>();

                if (enemyRoot != null)
                {
                    enemyEntity.transform.SetParent(enemyRoot, false);
                }

                enemyEntity.transform.localPosition = entry.SpawnPosition;

                enemyEntity.Init(entry.EnemyData);
                enemyEntity.OnDied += HandleEnemyDied;

                enemyEntities.Add(enemyEntity);

                EnemyAI enemyAI = new EnemyAI(enemyEntity);
                enemyAIs.Add(enemyAI);

                if (enemyViewPrefab != null)
                {
                    EnemyView enemyView = Instantiate(enemyViewPrefab, enemyEntity.transform);
                    enemyView.Init(enemyEntity, enemyAI);
                }
            }
        }

        /// <summary>
        /// 시스템 생성합니다.
        /// </summary>
        private void SetupSystems()
        {
            deckSystem = new DeckSystem(dungeonState.Deck, balanceData);
            apSystem = new ApSystem(balanceData);

            effectExecutor = new EffectExecutor
            (
                partySanityHolder,
                drawRequest: count => deckSystem.Draw(count),
                discardRequest: count => deckSystem.DiscardRandom(count),
                apChangeRequest: delta => apSystem.Change(delta)
            );

            cardPlayService = new CardPlayService(apSystem, deckSystem, effectExecutor, partySanityHolder);
            targetResolver = new TargetResolver();

            turnManager = new TurnManager(apSystem, deckSystem, balanceData);
            turnManager.OnTurnStarted += HandleTurnStarted;
            turnManager.OnEnemyActionsStarted += HandleEnemyActionsStarted;
            turnManager.OnRoundEnded += HandleRoundEnded;

            madnessEventRunner = new MadnessEventRunner(partySanityHolder, effectExecutor, balanceData, dungeonState.SanityEventDatas, characterEntity);
            turnManager.OnTurnStartHook += madnessEventRunner.HandleTurnStartHook;

            if (handView != null)
            {
                handView.Init(deckSystem, cardPlayService, apSystem);
            }

            if (partyStatusView != null)
            {
                partyStatusView.Init(characterEntity, partySanityHolder);
            }

            if (cardTooltipView != null)
            {
                cardTooltipView.Init(partySanityHolder);
            }

            if (battleHUDView != null)
            {
                battleHUDView.Init(turnManager, apSystem, deckSystem, EndTurn);
            }

            if (madnessOverlayView != null)
            {
                madnessOverlayView.Init(partySanityHolder);
            }
        }

        /// <summary>
        /// 전투를 종료합니다.
        /// </summary>
        /// <param name="battleResult">전투 결과입니다.</param>
        private void EndBattle(EBattleResult battleResult)
        {
            if (!isBattleRunning)
            {
                return;
            }

            isBattleRunning = false;
            this.battleResult = battleResult;

            turnManager.EndBattle();

            deckSystem.ResetDeckSystem();
            apSystem.ResetAp();

            if (handView != null)
            {
                handView.Release();
            }

            if (partyStatusView != null)
            {
                partyStatusView.Release();
            }

            if (cardTooltipView != null)
            {
                cardTooltipView.Release();
            }

            if (battleHUDView != null)
            {
                battleHUDView.Release();
            }

            if (madnessOverlayView != null)
            {
                madnessOverlayView.Release();
            }
            
            // 던전 지속 정신력 기록
            if (dungeonState != null && partySanityHolder != null)
            {
                dungeonState.SetCarriedSanity(partySanityHolder.CurrentSanity);
            }

            SWLog.Log($"[BattleManager] 전투 종료: {battleResult} (턴 {turnManager.CurrentTurn})");
            OnBattleEnded?.Invoke(battleResult);
        }
        #endregion // 전투

        #region 플레이어 행동
        /// <summary>
        /// 카드를 사용합니다.
        /// </summary>
        /// <param name="card">사용할 카드입니다.</param>
        /// <param name="target">단일 대상 카드의 지정 대상입니다.</param>
        /// <returns>성공 여부입니다.</returns>
        public bool PlayCard(CardInstance card, ITargetable target = null)
        {
            if (!isBattleRunning || turnManager.CurrentPhase != ETurnPhase.PlayerAction)
            {
                SWLog.LogWarning("[BattleManager] PlayCard 무시: 플레이어 행동 단계가 아닙니다");
                return false;
            }

            if (card == null)
            {
                SWLog.LogError("[BattleManager] PlayCard 실패: 카드가 null입니다");
                return false;
            }

            if (!targetResolver.Resolve(card.TargetingType, characterEntity, target, enemyEntities, cardTargetBuffer))
            {
                return false;
            }

            return cardPlayService.Play(card, characterEntity, cardTargetBuffer);
        }

        /// <summary>
        /// 플레이어 턴을 종료합니다.
        /// </summary>
        public void EndTurn()
        {
            if (!isBattleRunning)
            {
                return;
            }

            turnManager.EndPlayerTurn();
        }
        #endregion // 플레이어 행동 

        #region 이벤트
        /// <summary>
        /// 턴 시작 처리합니다.
        /// </summary>
        /// <param name="turnNumber">턴 번호입니다.</param>
        private void HandleTurnStarted(int turnNumber)
        {
            if (characterEntity != null && !characterEntity.IsDead)
            {
                characterEntity.ResetBlock();
            }
        }

        /// <summary>
        /// 적 행동 단계 처리합니다.
        /// </summary>
        /// <param name="turnNumber">턴 번호입니다.</param>
        private void HandleEnemyActionsStarted(int turnNumber)
        {
            for (int i = 0; i < enemyEntities.Count; i++)
            {
                if (!isBattleRunning)
                {
                    return;
                }

                EnemyEntity enemyEntity = enemyEntities[i];

                if (enemyEntity.IsDead)
                {
                    continue;
                }

                enemyEntity.ResetBlock();

                EnemyAI enemyAI = enemyAIs[i];

                if (!enemyAI.SelectTargets(party, enemyTargetBuffer))
                {
                    continue;
                }

                enemyAI.PlayAction(effectExecutor, enemyTargetBuffer);
            }
        }

        /// <summary>
        /// 라운드 종료 처리합니다.
        /// </summary>
        /// <param name="turnNumber">턴 번호입니다.</param>
        private void HandleRoundEnded(int turnNumber)
        {
            foreach (var enemyAI in EnemyAIs)
            {
                enemyAI.PrepareNextTurn();
            }
        }

        /// <summary>
        /// 적 사망 처리합니다.
        /// </summary>
        /// <param name="deadEntity">사망한 엔티티입니다.</param>
        private void HandleEnemyDied(BattleEntity deadEntity)
        {
            if (!isBattleRunning)
            {
                return;
            }

            foreach (var enemy in enemyEntities)
            {
                if (!enemy.IsDead)
                {
                    return;
                }
            }

            EndBattle(EBattleResult.Victory);
        }

        /// <summary>
        /// 파티원 사망 처리합니다.
        /// 지금은 1인 기준, 나중에 파티원 처리예정입니다.
        /// </summary>
        /// <param name="deadEntity">사망한 엔티티입니다.</param>
        private void HandleCharacterDied(BattleEntity deadEntity)
        {
            if (!isBattleRunning)
            {
                return;
            }

            EndBattle(EBattleResult.Defeat);
        }
        #endregion // 이벤트
    }
}
