using System.Collections.Generic;
using EchoesOfAsh.Battle;
using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using EchoesOfAsh.Deck;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using EchoesOfAsh.Sanity;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Test
{
    /// <summary>
    /// 카드 실행 테스트입니다.
    /// 빈 게임 오브젝트에 부착하고 적 및 캐릭터 엔티티 컴포넌트를 연결하여 카드 사용 흐름을 검증합니다.
    /// </summary>
    public class CardPlayTest : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("데이터")]
        [SerializeField] private PartyData partyData;
        [SerializeField] private BattleBalanceData balanceData;
        [SerializeField] private List<CardData> startingCards = new();

        [SWGroup("전투원")]
        [SerializeField] private CharacterEntity characterEntity;
        [SerializeField] private CharacterData characterData;
        [SerializeField] private EnemyEntity enemyEntity;
        [SerializeField] private EnemyData enemyData;

        [SWGroup("랜덤 시드 값")]
        [SerializeField] private bool useFixedSeed = true;
        [SerializeField] private int seed = 12345;

        private bool isRunning;

        private SanityHolder partySanityHolder;
        private DeckSystem deckSystem;
        private ApSystem apSystem;
        private EffectExecutor effectExecutor;
        private CardPlayService cardPlayService;

        private readonly List<ITargetable> targetBuffer = new();
        #endregion // 필드


        #region 테스트
        /// <summary>
        /// 카드 사용 시험에 필요한 전투원과 덱, 효과 실행기를 생성합니다.
        /// </summary>
        [SWButton("테스트 시작")]
        private void RunTest()
        {
            if (!Application.isPlaying || isRunning)
            {
                return;
            }


            if (partyData == null || balanceData == null || startingCards.Count == 0
                || characterEntity == null || characterData == null
                || enemyEntity == null || enemyData == null)
            {
                SWLog.LogError("[CardPlayTest] 데이터/전투원 참조가 비어 있습니다");
                return;
            }

            // 같은 값을 사용하면 카드 섞기와 무작위 결과를 다시 확인할 수 있습니다.
            if (useFixedSeed)
            {
                SWRandom.SetSeed(seed);
                SWLog.Log($"[CardPlayTest] SWRandom 시드 고정: {seed}");
            }

            // 전투원 초기화
            characterEntity.Init(characterData);
            enemyEntity.Init(enemyData);

            // 파티 공유 정신력
            partySanityHolder = new SanityHolder(
                partyData.MaxSanityStat, partyData.SanityThreshold, partyData.StartSanity);

            partySanityHolder.OnSanityTypeChanged += type =>
                SWLog.Log($"[CardPlayTest] ★ 파티 정신력 구간 전환: {(type == ESanityType.Madness ? "광기" : "평정")}");

            // 덱 구성
            List<CardInstance> cardInstances = new();
            foreach (CardData cardData in startingCards)
            {
                if (cardData != null)
                {
                    cardInstances.Add(new CardInstance(cardData));
                }
            }

            deckSystem = new DeckSystem(cardInstances, balanceData);
            apSystem = new ApSystem(balanceData);

            // 효과 실행기
            effectExecutor = new EffectExecutor(
                partySanityHolder,
                drawRequest: count => deckSystem.Draw(count),
                discardRequest: count => deckSystem.DiscardRandom(count),
                apChangeRequest: delta => apSystem.Change(delta));

            cardPlayService = new CardPlayService(
                apSystem, deckSystem, effectExecutor, partySanityHolder);

            cardPlayService.OnCardPlayed += (card, sanityType) =>
                SWLog.Log($"[CardPlayTest] 카드 사용 완료: '{card.DisplayName}' " +
                          $"(적용 구간: {(sanityType == ESanityType.Madness ? "광기" : "평정")})");

            deckSystem.OnOverdraw += card =>
                SWLog.Log($"[CardPlayTest] 손패 초과 — '{card.DisplayName}' 버림 더미로 이동");

            isRunning = true;
            SWLog.Log("[CardPlayTest] --- 테스트 준비 완료. '턴 시작'으로 AP 지급 + 드로우 ---");
        }

        /// <summary>
        /// 카드 사용 시험 상태와 시험 중 생성한 객체를 초기화합니다.
        /// </summary>
        [SWButton("테스트 초기화")]
        private void ResetTest()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Cleanup();
        }

        /// <summary>
        /// 객체가 제거될 때 시험을 위해 만든 객체를 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            Cleanup();
        }

        /// <summary>
        /// 시험 중 생성한 정신력과 전투 엔티티 자원을 해제합니다.
        /// </summary>
        private void Cleanup()
        {
            partySanityHolder?.Dispose();
            partySanityHolder = null;

            deckSystem = null;
            apSystem = null;
            effectExecutor = null;
            cardPlayService = null;

            isRunning = false;
        }
        #endregion // 테스트

        #region 테스트 UI
        /// <summary>
        /// 카드 사용 시험 상태와 조작 버튼을 그립니다.
        /// </summary>
        private void OnGUI()
        {
            if (!isRunning)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(20f, 20f, 520f, 720f));

            DrawPartyStatus();
            GUILayout.Space(10f);
            DrawEnemyStatus();
            GUILayout.Space(10f);
            DrawTurnControls();
            GUILayout.Space(10f);
            DrawHand();

            GUILayout.EndArea();
        }

        /// <summary>
        /// 파티 전투원의 현재 상태를 그립니다.
        /// </summary>
        private void DrawPartyStatus()
        {
            GUILayout.Label("=== 파티 ===");
            GUILayout.Label($"{characterEntity.DisplayName}  " +
                            $"HP {characterEntity.CurrentHp}/{characterEntity.MaxHp}  " +
                            $"방어막 {characterEntity.CurrentBlock}");
            GUILayout.Label($"공유 정신력 {partySanityHolder.CurrentSanity}/{partySanityHolder.MaxSanity}  " +
                            $"[{(partySanityHolder.CurrentSanityType == ESanityType.Madness ? "광기" : "평정")}]  " +
                            $"임계값 {partySanityHolder.SanityThreshold}");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("정신력 -10")) partySanityHolder.ChangeSanity(-10);
            if (GUILayout.Button("정신력 -30 (광기 유도)")) partySanityHolder.ChangeSanity(-30);
            if (GUILayout.Button("정신력 +10")) partySanityHolder.ChangeSanity(10);

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 적 전투원의 현재 상태를 그립니다.
        /// </summary>
        private void DrawEnemyStatus()
        {
            GUILayout.Label("=== 적 ===");

            string status = $"{enemyEntity.DisplayName}  " +
                            $"HP {enemyEntity.CurrentHp}/{enemyEntity.MaxHp}  " +
                            $"방어막 {enemyEntity.CurrentBlock}  " +
                            $"{(enemyEntity.IsDead ? "[사망]" : "[생존]")}";

            status += $"  정신력 {enemyEntity.CurrentSanity}/{enemyEntity.MaxSanity} " +
                      $"[{(enemyEntity.CurrentSanityType == ESanityType.Madness ? "광기" : "평정")}]";

            GUILayout.Label(status);
        }

        /// <summary>
        /// 행동력과 턴 진행 상태, 턴 종료 버튼을 그립니다.
        /// </summary>
        private void DrawTurnControls()
        {
            GUILayout.Label($"=== AP: {apSystem.CurrentAp}  |  " +
                            $"덱 {deckSystem.DrawPileCount} / 버림 {deckSystem.DiscardPileCount} ===");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button($"턴 시작 (AP +{apSystem.ApPerTurn}, 드로우 {balanceData.DrawPerTurn})"))
            {
                apSystem.StartTurn();
                deckSystem.Draw(balanceData.DrawPerTurn);
                SWLog.Log($"[CardPlayTest] 턴 시작 — AP {apSystem.CurrentAp}, 손패 {deckSystem.Hand.Count}장");
            }

            if (GUILayout.Button("드로우 +1")) deckSystem.Draw(1);
            if (GUILayout.Button("무작위 버림 1")) deckSystem.DiscardRandom(1);

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// 현재 손패와 카드 사용 버튼을 그립니다.
        /// </summary>
        private void DrawHand()
        {
            GUILayout.Label($"=== 손패 ({deckSystem.Hand.Count}/{deckSystem.MaxHandSize}) — 버튼 클릭 = 적 대상 사용 ===");

            // 카드를 사용하면 손패가 바뀔 수 있으므로 미리 복사한 목록을 확인합니다.
            List<CardInstance> handSnapshot = new(deckSystem.Hand);

            foreach (CardInstance card in handSnapshot)
            {
                GUILayout.BeginHorizontal();

                bool canPlay = cardPlayService.CanPlay(card);
                string sanityTag = card.IsSanityEffect ? " [정신력 반응]" : "";

                GUI.enabled = canPlay && !enemyEntity.IsDead;

                if (GUILayout.Button($"{card.DisplayName}  (AP {card.ApCost}){sanityTag}", GUILayout.Width(300f)))
                {
                    PlayCardOnEnemy(card);
                }

                GUI.enabled = true;

                GUILayout.Label(canPlay ? "" : "AP 부족");
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 적을 대상으로 카드를 사용합니다.
        /// 현재 검증 단계에서는 별도의 대상 해석기를 사용하지 않고 적 하나를 대상으로 고정합니다.
        /// </summary>
        /// <param name="card">사용할 카드입니다.</param>
        private void PlayCardOnEnemy(CardInstance card)
        {
            targetBuffer.Clear();

            if (card.TargetingType != ETargetingType.Self)
            {
                targetBuffer.Add(enemyEntity);
            }

            cardPlayService.Play(card, characterEntity, targetBuffer);
        }
        #endregion // 테스트 UI
    }

}
