using System;
using System.Collections.Generic;
using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using EchoesOfAsh.Deck;
using EchoesOfAsh.Dungeon;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Effect.Trigger;
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
        [SerializeField] private BattleBalanceData balanceData;
        [Tooltip("이 전투에서 유효한 상태 이상 정의 목록입니다 (임시 조치)")]
        [SerializeField] private List<StatusEffectData> statusDatas;

        [SWGroup("배치")]
        [SerializeField] private Transform characterRoot;
        [SerializeField] private Transform enemyRoot;
        [SerializeField] private PartyFormation partyFormation;

        [SWGroup("뷰")]
        [SerializeField] private HandView handView;
        [SerializeField] private EnemyView enemyViewPrefab;
        [SerializeField] private CharacterView characterViewPrefab;
        [SerializeField] private PartyStatusView partyStatusView;
        [SerializeField] private CardTooltipView cardTooltipView;
        [SerializeField] private BattleHUDView battleHUDView;
        [SerializeField] private MadnessOverlayView madnessOverlayView;

        private SanityHolder partySanityHolder;
        private DeckSystem deckSystem;
        private ApSystem apSystem;
        private EffectExecutor effectExecutor;
        private CardPlayService cardPlayService;
        private TargetResolver targetResolver;
        private TurnManager turnManager;
        private SanityEventRunner sanityEventRunner;
        private AggroSystem aggroSystem;

        private DungeonState dungeonState;
        private EnemyEncounterData currentEncounter;

        private TriggerEffectController triggerEffectController;

        private EBattleResult battleResult = EBattleResult.None;
        private bool isBattleRunning;

        private readonly Dictionary<CardData, CharacterEntity> cardOwnerLookup = new();
        private readonly List<CharacterView> characterViews = new();
        private readonly List<CharacterEntity> party = new();
        private readonly List<EnemyEntity> enemyEntities = new();
        private readonly List<EnemyAI> enemyAIs = new();
        private readonly List<ITargetable> cardTargetBuffer = new();
        private readonly List<ITargetable> enemyTargetBuffer = new();
        /// <summary>챕터에서 주입된 상태이상 정의입니다. 미주입이면 인스펙터 목록으로 폴백합니다.</summary>
        private IReadOnlyList<StatusEffectData> chapterStatusDatas;
        /// <summary>피격/가해 트리거 배선용 구독 목록입니다. 해제 대칭을 위해 보관합니다.</summary>
        private readonly List<(BattleEntity entity, Action<int, int> handler)> damageTriggerHandlers = new();
        /// <summary>카드 실행 중 가해 트리거 귀속 시전자입니다. 귀속 구간 밖 피해(상태이상 틱 등)는 가해 트리거를 발화하지 않습니다.</summary>
        private CharacterEntity dealDamageAttributionCaster;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>전투 진행 여부입니다.</summary>
        public bool IsBattleRunning => isBattleRunning;
        /// <summary>전투 결과 타입입니다.</summary>
        public EBattleResult BattleResult => battleResult;

        /// <summary>파티원 엔티티 목록입니다 (스폰 순서 고정).</summary>
        public IReadOnlyList<CharacterEntity> Party => party;
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

        /// <summary>미주입(null) 시 인스펙터 목록을 사용하는 폴백입니다.</summary>
        private IReadOnlyList<StatusEffectData> ActiveStatusDatas => chapterStatusDatas ?? statusDatas;

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

            if(aggroSystem != null)
            {
                aggroSystem.Release();
            }

            if (turnManager != null)
            {
                turnManager.OnTurnStarted -= HandleTurnStarted;
                turnManager.OnEnemyActionsStarted -= HandleEnemyActionsStarted;
                turnManager.OnRoundEnded -= HandleRoundEnded;
                turnManager.OnRoundEnded -= HandleStatusRoundTick;

                if (triggerEffectController != null)
                {
                    turnManager.OnTurnStarted -= triggerEffectController.HandleTurnStarted;

                    if (cardPlayService != null)
                    {
                        cardPlayService.OnCardPlayed -= HandleCardPlayedForTrigger;
                    }

                    triggerEffectController.Clear();
                    triggerEffectController = null;
                }

                if (sanityEventRunner != null)
                {
                    turnManager.OnTurnStartHook -= sanityEventRunner.HandleTurnStartHook;
                    sanityEventRunner = null;
                }

                turnManager = null;
            }

            deckSystem = null;
            apSystem = null;
            effectExecutor = null;
            cardPlayService = null;
            targetResolver = null;
            aggroSystem = null;

            partySanityHolder?.Dispose();
            partySanityHolder = null;

            foreach (CharacterView characterView in characterViews)
            {
                if (characterView != null)
                {
                    characterView.Release();
                }
            }

            characterViews.Clear();

            foreach ((BattleEntity entity, Action<int, int> handler) subscription in damageTriggerHandlers)
            {
                if (subscription.entity != null)
                {
                    subscription.entity.OnDamaged -= subscription.handler;
                }
            }

            damageTriggerHandlers.Clear();
            dealDamageAttributionCaster = null;

            for (int i = 0; i < party.Count; i++)
            {
                CharacterEntity member = party[i];
                DestroyEntity(ref member);
            }

            cardOwnerLookup.Clear();
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

            triggerEffectController.Raise(ETriggerType.BattleStart);

            turnManager.StartBattle();
            return true;
        }

        /// <summary>
        /// 전투 시작에 필요한 데이터를 검증합니다.
        /// </summary>
        /// <returns>검증 성공 여부입니다.</returns>
        private bool ValidateData()
        {
            if (balanceData == null)
            {
                SWLog.LogError("[BattleManager] 데이터 검증 실패: 참조가 비어있습니다");
                return false;
            }

            if (dungeonState == null || dungeonState.Deck.Count == 0)
            {
                SWLog.LogError("[BattleManager] 검증 실패: 런 상태가 없거나 덱이 비어 있습니다");
                return false;
            }

            if (dungeonState.PartyData == null)
            {
                SWLog.LogError("[BattleManager] 검증 실패: 런 상태에 PartyData가 없습니다");
                return false;
            }

            if (dungeonState.CharacterDatas.Count == 0 || dungeonState.CharacterDatas.Count > 3)
            {
                SWLog.LogError($"[BattleManager] 파티 {dungeonState.CharacterDatas.Count}인입니다 - 기준 1~3");
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
        /// 파티원 생성 및 정신력 설정합니다. 스폰 순서 = 목록 순서 (발화 순서 결정성)입니다.
        /// </summary>
        private void SetupParty()
        {
            int partyCount = dungeonState.CharacterDatas.Count;

            for (int index = 0; index < partyCount; index++)
            {
                CharacterData characterData = dungeonState.CharacterDatas[index];
                CharacterEntity member = new GameObject(characterData.name).AddComponent<CharacterEntity>();

                if (characterRoot != null)
                {
                    member.transform.SetParent(characterRoot, false);
                }

                member.transform.localPosition = partyFormation != null
                ? partyFormation.GetSpawnPosition(partyCount, index)
                : Vector3.zero;

                member.Init(characterData);
                member.SetStatusDatas(ActiveStatusDatas);
                member.SetDamageCalculator(new StatusDamageCalculator());
                member.OnDied += HandleCharacterDied;

                if (characterViewPrefab != null)
                {
                    CharacterView characterView = Instantiate(characterViewPrefab, member.transform);
                    characterView.Init(member);
                    characterViews.Add(characterView);
                }

                party.Add(member);
            }

            PartyData partyData = dungeonState.PartyData;
            int startSanity = dungeonState.HasCarriedSanity ? dungeonState.CarriedSanity : partyData.StartSanity;
            partySanityHolder = new SanityHolder(partyData.MaxSanityStat, partyData.SanityThreshold, startSanity);
        }

        /// <summary>
        /// 조우 데이터에 따라 적을 생성하고 배치합니다.
        /// </summary>
        private void SetupEnemies()
        {
            aggroSystem = new AggroSystem(balanceData);

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
                enemyEntity.SetStatusDatas(ActiveStatusDatas);
                enemyEntity.SetDamageCalculator(new StatusDamageCalculator());
                enemyEntity.OnDied += HandleEnemyDied;

                enemyEntities.Add(enemyEntity);

                aggroSystem.RegisterEnemy(enemyEntity);

                EnemyAI enemyAI = new EnemyAI(enemyEntity, party, aggroSystem);
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
            
            BuildCardOwnerLookup();
            deckSystem.SetDrawExclusion(card =>
            {
                CharacterEntity owner = GetCardOwner(card);
                return owner != null && owner.IsDead;
            });

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

            triggerEffectController = new TriggerEffectController(effectExecutor, partySanityHolder);

            foreach (CharacterEntity character in party)
            {
                triggerEffectController.Register(character, character.CharacterData.Passives);
            }

            foreach (RelicData relic in dungeonState.Relics)
            {
                if (relic == null)
                {
                    continue;
                }

                if (relic.IsShared)
                {
                    // 공용 유물 - 시전자/대상 = 발화 시점 첫 생존자 (잠정 규칙)
                    triggerEffectController.Register(relic.TriggerEffects, () => GetDefaultCaster(), relic.DisplayName);
                }
                else
                {
                    CharacterEntity ownerEntity = FindPartyMember(relic.OwnerCharacter);

                    if (ownerEntity == null)
                    {
                        SWLog.LogWarning($"[BattleManager] 전용 유물 '{relic.DisplayName}'의 소유 캐릭터가 파티에 없어 등록을 건너뜁니다");
                        continue;
                    }

                    triggerEffectController.Register(ownerEntity, relic.TriggerEffects);
                }
            }

            turnManager = new TurnManager(apSystem, deckSystem, balanceData);
            turnManager.OnTurnStarted += HandleTurnStarted;
            turnManager.OnTurnStarted += triggerEffectController.HandleTurnStarted;
            turnManager.OnEnemyActionsStarted += HandleEnemyActionsStarted;
            turnManager.OnRoundEnded += HandleStatusRoundTick;
            turnManager.OnRoundEnded += HandleRoundEnded;

            cardPlayService.OnCardPlayed += HandleCardPlayedForTrigger;

            // 피격/가해 트리거 배선 (P2-M7 - 미배선 3종 해소)
            foreach (CharacterEntity character in party)
            {
                CharacterEntity damagedMember = character;
                Action<int, int> handler = (hpLose, original) => HandlePartyMemberDamaged(damagedMember, original);
                damagedMember.OnDamaged += handler;
                damageTriggerHandlers.Add((damagedMember, handler));
            }

            foreach (EnemyEntity enemy in enemyEntities)
            {
                Action<int, int> handler = (hpLose, original) => HandleEnemyDamagedForTrigger(original);
                enemy.OnDamaged += handler;
                damageTriggerHandlers.Add((enemy, handler));
            }

            sanityEventRunner = new SanityEventRunner(
                partySanityHolder,
                effectExecutor,
                dungeonState.SanityEventDatas,
                party,
                hasOccurred: () => dungeonState.HasMadnessEventOccurred,
                markOccurred: dungeonState.MarkMadnessEventOccurred);
            turnManager.OnTurnStartHook += sanityEventRunner.HandleTurnStartHook;

            if (handView != null)
            {
                handView.Init(deckSystem, cardPlayService, apSystem);
            }

            if (partyStatusView != null && party.Count > 0)
            {
                partyStatusView.Init(party, partySanityHolder);
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

            // 전투 종료 트리거 - 승리 시 1회 (잠정 규칙: 패배 = 던전 종료라 미발화). 효과의 SAN 변경이 아래 이월 기록에 반영됩니다
            if (battleResult == EBattleResult.Victory)
            {
                triggerEffectController?.Raise(ETriggerType.BattleEnd);
            }

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

        /// <summary>
        /// 라운드 종료 시 전투원 전체의 상태 이상 중첩 감소를 처리합니다. 순회 순서는 파티 → 적 (스폰 순 고정)입니다.
        /// </summary>
        /// <param name="turn">현재 턴입니다.</param>
        private void HandleStatusRoundTick(int turn)
        {
            foreach (CharacterEntity member in party)
            {
                if (member != null && !member.IsDead)
                {
                    member.TickStatusRound();
                }
            }

            foreach (EnemyEntity enemy in enemyEntities)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    enemy.TickStatusRound();
                }
            }

            aggroSystem?.TickRound();
        }

        /// <summary>
        /// 공용 카드의 시전자인 파티 첫 생존자를 반환합니다. 전원 사망이면 null입니다
        /// </summary>
        /// <returns>파티 첫 생존자입니다.</returns>
        private CharacterEntity GetDefaultCaster()
        {
            foreach (CharacterEntity member in party)
            {
                if (member != null && !member.IsDead)
                {
                    return member;
                }
            }

            return null;
        }

        /// <summary>
        /// 지정한 캐릭터 데이터로 스폰된 파티원을 찾습니다.
        /// </summary>
        /// <param name="characterData">찾을 캐릭터 데이터입니다.</param>
        /// <returns>해당 파티원입니다. 없으면 null입니다.</returns>
        private CharacterEntity FindPartyMember(CharacterData characterData)
        {
            if (characterData == null)
            {
                return null;
            }

            foreach (CharacterEntity member in party)
            {
                if (member != null && member.CharacterData == characterData)
                {
                    return member;
                }
            }

            return null;
        }

        /// <summary>
        /// 파티 구성원의 전용 카드 목록으로 카드 → 소유자 조회 표를 구성합니다.
        /// 강화 버전 데이터도 함께 등록해 강화 카드의 소유 판정 유실을 방지합니다.
        /// </summary>
        private void BuildCardOwnerLookup()
        {
            cardOwnerLookup.Clear();

            foreach (CharacterEntity member in party)
            {
                if (member == null || member.CharacterData == null)
                {
                    continue;
                }

                foreach (CardData cardData in member.CharacterData.ExclusiveCards)
                {
                    if (cardData == null)
                    {
                        continue;
                    }

                    if (!cardOwnerLookup.TryAdd(cardData, member))
                    {
                        SWLog.LogError($"[BattleManager] 전용 카드 '{cardData.name}'가 여러 캐릭터에 중복 등록되었습니다");
                        continue;
                    }

                    if (cardData.UpgradeCard != null)
                    {
                        cardOwnerLookup.TryAdd(cardData.UpgradeCard, member);
                    }
                }
            }
        }

        /// <summary>
        /// 카드의 소유 캐릭터를 반환합니다. 공용 카드면 null입니다.
        /// </summary>
        /// <param name="card">판정할 카드입니다.</param>
        /// <returns>소유 캐릭터입니다. 공용 카드면 null입니다.</returns>
        private CharacterEntity GetCardOwner(CardInstance card)
        {
            if (card == null || card.CardData == null)
            {
                return null;
            }

            return cardOwnerLookup.TryGetValue(card.CardData, out CharacterEntity owner) ? owner : null;
        }

        /// <summary>
        /// 카드의 시전자를 반환합니다.
        /// 전용 카드는 소유자가 시전하며, 공용 카드와 소유자 전투불능 시에는 파티 첫 생존자입니다.
        /// </summary>
        /// <param name="card">사용할 카드입니다.</param>
        /// <returns>시전자입니다. 전원 사망이면 null입니다.</returns>
        private CharacterEntity GetCasterFor(CardInstance card)
        {
            CharacterEntity owner = GetCardOwner(card);

            if (owner != null && !owner.IsDead)
            {
                return owner;
            }

            return GetDefaultCaster();
        }

        /// <summary>
        /// 챕터의 상태이상 정의 목록을 주입합니다. 조립 지점(DungeonManager)이 던전 시작 시 호출합니다.
        /// </summary>
        /// <param name="statusDatas">상태이상 정의 목록입니다. null이면 인스펙터 목록으로 폴백합니다.</param>
        public void SetChapterStatusDatas(IReadOnlyList<StatusEffectData> statusDatas)
            => chapterStatusDatas = statusDatas;
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

            CharacterEntity caster = GetCasterFor(card);

            if (caster == null)
            {
                SWLog.LogError("[BattleManager] PlayCard 실패: 생존한 파티원이 없습니다");
                return false;
            }

            if (!targetResolver.Resolve(card.TargetingType, caster, target, enemyEntities, cardTargetBuffer))
            {
                return false;
            }

            // 카드 실행 중 적이 받는 피해를 시전자 어그로와 가해 트리거로 귀속합니다
            bool isPlayed;
            aggroSystem?.BeginAttribution(caster);
            dealDamageAttributionCaster = caster;

            try
            {
                isPlayed = cardPlayService.Play(card, caster, cardTargetBuffer);
            }
            finally
            {
                dealDamageAttributionCaster = null;
                aggroSystem?.EndAttribution();
            }

            // 도발 부여 카드였다면 적 표적 표시를 즉시 갱신합니다 (무작위 재추첨 없음)
            if (isPlayed)
            {
                foreach (var enemyAI in enemyAIs)
                {
                    enemyAI.RefreshTauntPreview();
                }
            }


            return isPlayed;
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
            foreach (CharacterEntity member in party)
            {
                if (member != null && !member.IsDead)
                {
                    member.ResetBlock();
                }
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

                if (!enemyAI.SelectTargets(enemyTargetBuffer))
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
        /// 파티원 사망 처리합니다. 전원 사망 시 패배로 전환합니다.
        /// </summary>
        /// <param name="deadEntity">사망한 엔티티입니다.</param>
        private void HandleCharacterDied(BattleEntity deadEntity)
        {
            // 전투불능자의 전용 카드를 드로우 풀에서 제외합니다
            deckSystem?.RefreshDrawExclusion();

            foreach (CharacterEntity member in party)
            {
                if (member != null && !member.IsDead)
                {
                    return;
                }
            }

            EndBattle(EBattleResult.Defeat);
        }

        /// <summary>
        /// 카드 사용 완료를 트리거 컨트롤러에 중계합니다 (Effect.Trigger의 Card 의존 차단 - 조립 지점 어댑터).
        /// </summary>
        /// <param name="card">사용한 카드입니다.</param>
        /// <param name="sanityType">적용된 정신력 구간입니다.</param>
        private void HandleCardPlayedForTrigger(CardInstance card, ESanityType sanityType)
        {
            triggerEffectController?.Raise(ETriggerType.CardPlayed);
        }

        /// <summary>
        /// 파티원 피격을 피격 트리거로 중계합니다. 원본 피해 0 이하는 발화하지 않습니다 (방어막 전량 흡수는 피격으로 인정).
        /// </summary>
        /// <param name="damagedMember">피격당한 파티원입니다.</param>
        /// <param name="originalAmount">계산 전 원본 피해량입니다.</param>
        private void HandlePartyMemberDamaged(CharacterEntity damagedMember, int originalAmount)
        {
            if (!isBattleRunning || originalAmount <= 0)
            {
                return;
            }

            triggerEffectController?.RaiseFor(ETriggerType.TakeDamage, damagedMember);
        }

        /// <summary>
        /// 적 피격을 가해 트리거로 중계합니다. 카드 귀속 구간 안의 피해만 인정합니다 (상태이상 틱 등 귀속 밖 = 미발화 - 어그로와 동일 잠정 규칙).
        /// </summary>
        /// <param name="originalAmount">계산 전 원본 피해량입니다.</param>
        private void HandleEnemyDamagedForTrigger(int originalAmount)
        {
            if (!isBattleRunning || originalAmount <= 0 || dealDamageAttributionCaster == null)
            {
                return;
            }

            triggerEffectController?.RaiseFor(ETriggerType.DealDamage, dealDamageAttributionCaster);
        }
        #endregion // 이벤트
    }
}
