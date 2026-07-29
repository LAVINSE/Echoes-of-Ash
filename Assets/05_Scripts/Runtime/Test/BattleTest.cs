using System.Collections.Generic;
using EchoesOfAsh.Battle;
using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using EchoesOfAsh.Dungeon;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Test
{
    /// <summary>
    /// 전투 시작부터 카드 사용과 턴 종료까지 전체 흐름을 검증하는 테스트입니다.
    /// </summary>
    public class BattleTest : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("전투")]
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private List<CardData> startingCards = new();
        [SerializeField] private List<SanityEventData> sanityEvents = new();
        [SerializeField] private EnemyEncounterData enemyEncounterData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private List<CharacterData> characterDatas = new();

        [SWGroup("랜덤 시드 값")]
        [SerializeField] private bool useFixedSeed = true;
        [SerializeField] private int seed = 12345;

        [SWGroup("테스트 UI")]
        [Tooltip("OnGUI 테스트 패널 표시 여부")]
        [SerializeField] private bool isShowTestGui = true;

        private bool isRun;
        private bool isSubscribed;
        private int selectedEnemyIndex;
        private int selectedAllyIndex;
        #endregion // 필드


        #region 테스트
        /// <summary>
        /// 설정된 데이터로 전투를 시작하고 시험 화면을 활성화합니다.
        /// </summary>
        [SWButton("전투 시작")]
        private void RunTest()
        {
            if (!Application.isPlaying || isRun)
            {
                return;
            }

            if (battleManager == null)
            {
                SWLog.LogError("[BattleTest] BattleManager 참조가 비어 있습니다");
                return;
            }

            // 런 시드 고정 — 같은 시드 = 같은 셔플/무작위 결과 (D3)
            if (useFixedSeed)
            {
                SWRandom.SetSeed(seed);
                SWLog.Log($"[BattleTest] SWRandom 시드 고정: {seed}");
            }

            Subscribe();

            DungeonState testRunState = new DungeonState(seed, partyData, characterDatas, startingCards, sanityEvents);

            if (!battleManager.StartBattle(testRunState, enemyEncounterData))
            {
                return;
            }

            selectedEnemyIndex = 0;
            isRun = true;
        }

        /// <summary>
        /// 진행 중인 시험 전투와 이벤트 구독을 초기화합니다.
        /// </summary>
        [SWButton("테스트 초기화")]
        private void ResetTest()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            battleManager?.ResetBattle();
            isRun = false;
        }

        /// <summary>
        /// 중복 구독을 방지하면서 전투 이벤트 기록 구독을 설정합니다.
        /// </summary>
        private void Subscribe()
        {
            if (isSubscribed)
            {
                return;
            }

            isSubscribed = true;

            battleManager.OnBattleStarted += ()
                => SWLog.Log("[BattleTest] --- 전투 시작 ---");

            battleManager.OnBattleEnded += result
                => SWLog.Log($"[BattleTest] ★ 전투 종료: {(result == EBattleResult.Victory ? "승리" : "패배")}");
        }
        #endregion // 테스트

        #region 테스트 UI
        /// <summary>
        /// 전투 상태와 조작 버튼으로 구성된 시험 화면을 그립니다.
        /// </summary>
        private void OnGUI()
        {
            if (!isRun || !isShowTestGui)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(20f, 20f, 560f, 860f));

            DrawTurnStatus();
            GUILayout.Space(10f);
            DrawPartyStatus();
            GUILayout.Space(10f);
            DrawEnemyStatus();
            GUILayout.Space(10f);
            DrawHand();
            GUILayout.Space(10f);
            DrawTurnControls();

            GUILayout.EndArea();
        }

        /// <summary>
        /// 턴 번호 / 진행 단계 / 전투 결과를 표시합니다.
        /// </summary>
        private void DrawTurnStatus()
        {
            TurnManager turnManager = battleManager.TurnManager;

            if (turnManager == null)
            {
                return;
            }

            GUILayout.Label($"=== 턴 {turnManager.CurrentTurn} — [{turnManager.CurrentPhase}] ===");

            if (!battleManager.IsBattleRunning && battleManager.BattleResult != EBattleResult.None)
            {
                GUILayout.Label($"★ 전투 결과: {(battleManager.BattleResult == EBattleResult.Victory ? "승리" : "패배")}");
            }
        }

        /// <summary>
        /// 파티원별 HP, 방어막, 공유 정신력, AP를 표시하고 아군 지정을 처리합니다.
        /// </summary>
        private void DrawPartyStatus()
        {
            var partyMembers = battleManager.Party;
            var partySanity = battleManager.PartySanityHolder;
            var apSystem = battleManager.ApSystem;

            if (partyMembers == null || partyMembers.Count == 0 || partySanity == null || apSystem == null)
            {
                return;
            }

            GUILayout.Label("=== 파티 (아군 선택 = Self 카드 대상) ===");

            for (int index = 0; index < partyMembers.Count; index++)
            {
                var member = partyMembers[index];
                string selectionPrefix = index == selectedAllyIndex ? "▶ " : "   ";

                if (GUILayout.Button($"{selectionPrefix}{member.DisplayName}  HP {member.CurrentHp}/{member.MaxHp}  " +
                                     $"방어막 {member.CurrentBlock}  {(member.IsDead ? "[사망]" : "[생존]")}"))
                {
                    selectedAllyIndex = index;
                }
            }

            GUILayout.Label($"공유 정신력 {partySanity.CurrentSanity}/{partySanity.MaxSanity}  " +
                            $"[{(partySanity.CurrentSanityType == ESanityType.Madness ? "광기" : "평정")}]");
            GUILayout.Label($"AP {apSystem.CurrentAp}");
        }

        /// <summary>
        /// 적별 HP, 정신력, 의도를 표시하고 단일 대상 지정을 처리합니다.
        /// </summary>
        private void DrawEnemyStatus()
        {
            GUILayout.Label("=== 적 ===");

            IReadOnlyList<EnemyEntity> enemies = battleManager.EnemyEntities;
            IReadOnlyList<EnemyAI> enemyAIs = battleManager.EnemyAIs;

            for (int index = 0; index < enemies.Count; index++)
            {
                EnemyEntity enemy = enemies[index];

                GUILayout.BeginHorizontal();

                string selectedMark = index == selectedEnemyIndex ? "▶" : "  ";
                string status = $"{selectedMark} {enemy.DisplayName}  " +
                                $"HP {enemy.CurrentHp}/{enemy.MaxHp}  " +
                                $"방어막 {enemy.CurrentBlock}  " +
                                $"정신력 {enemy.CurrentSanity}/{enemy.MaxSanity} " +
                                $"[{(enemy.CurrentSanityType == ESanityType.Madness ? "광기" : "평정")}]  " +
                                $"{(enemy.IsDead ? "[사망]" : "")}";

                GUILayout.Label(status, GUILayout.Width(420f));

                GUI.enabled = !enemy.IsDead;

                if (GUILayout.Button("대상 지정", GUILayout.Width(80f)))
                {
                    selectedEnemyIndex = index;
                }

                GUI.enabled = true;

                GUILayout.EndHorizontal();

                // 의도 표시 — M5에서 아이콘 UI로 대체 예정
                EnemyAI enemyAI = enemyAIs[index];

                if (!enemy.IsDead && enemyAI.NextAction != null)
                {
                    var action = enemyAI.NextAction;
                    string intents = string.Join(", ", action.GetIntentTypes());
                    GUILayout.Label($"    의도: '{action.ActionName}' [{intents}]  " +
                                    $"피해 {action.GetIntentDamageValue()}  " +
                                    $"정신력 압박 {action.GetIntentSanityPressureValue()}");
                }
            }
        }

        /// <summary>
        /// 손패를 표시하고 카드 사용을 처리합니다.
        /// </summary>
        private void DrawHand()
        {
            var deckSystem = battleManager.DeckSystem;
            var cardPlayService = battleManager.CardPlayService;

            if (deckSystem == null || cardPlayService == null)
            {
                return;
            }

            GUILayout.Label($"=== 손패 ({deckSystem.Hand.Count}장)  덱 {deckSystem.DrawPileCount} / 버림 {deckSystem.DiscardPileCount} / 제외 {deckSystem.ExclusionPileCount} ===");

            bool isPlayerAction = battleManager.TurnManager != null
                && battleManager.TurnManager.CurrentPhase == ETurnPhase.PlayerAction;

            // 사용 시 손패가 변형되므로 인덱스 순회 + 사용 즉시 중단
            for (int index = 0; index < deckSystem.Hand.Count; index++)
            {
                CardInstance card = deckSystem.Hand[index];

                GUILayout.BeginHorizontal();

                bool canPlay = isPlayerAction && cardPlayService.CanPlay(card);
                string sanityTag = card.TargetingType == ETargetingType.Single ? " [단일]" : "";

                GUI.enabled = canPlay;

                if (GUILayout.Button($"{card.DisplayName}  (AP {card.ApCost}){sanityTag}", GUILayout.Width(320f)))
                {
                    PlayCard(card);

                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                    return;
                }

                GUI.enabled = true;

                GUILayout.Label(canPlay ? "" : "사용 불가");
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 턴 종료 버튼을 표시합니다.
        /// </summary>
        private void DrawTurnControls()
        {
            bool isPlayerAction = battleManager.TurnManager != null
                && battleManager.TurnManager.CurrentPhase == ETurnPhase.PlayerAction;

            GUI.enabled = isPlayerAction;

            if (GUILayout.Button("턴 종료 ▶", GUILayout.Width(120f), GUILayout.Height(32f)))
            {
                battleManager.EndTurn();
            }

            GUI.enabled = true;
        }

        /// <summary>
        /// 카드를 사용합니다. 단일 대상 카드는 지정 적을 대상으로 전달합니다.
        /// </summary>
        /// <param name="card">사용할 카드입니다.</param>
        private void PlayCard(CardInstance card)
        {
            ITargetable designatedTarget = null;

            if (card.TargetingType == ETargetingType.Single)
            {
                designatedTarget = GetSelectedEnemy();

                if (designatedTarget == null)
                {
                    SWLog.LogWarning("[BattleTest] 단일 대상 카드 사용 실패: 지정 가능한 적이 없습니다");
                    return;
                }
            }
            else if (card.TargetingType == ETargetingType.Self)
            {
                var partyMembers = battleManager.Party;
                int allyIndex = Mathf.Clamp(selectedAllyIndex, 0, partyMembers.Count - 1);

                // 사망 아군 선택 시 null 전달 → ResolveSelf가 시전자 폴백 (폴백 검증 경로)
                designatedTarget = partyMembers[allyIndex];
            }

            battleManager.PlayCard(card, designatedTarget);
        }

        /// <summary>
        /// 지정된 적을 반환합니다. 사망 상태면 첫 생존 적으로 대체합니다.
        /// </summary>
        /// <returns>지정 대상 적입니다. 생존 적이 없으면 <see langword="null"/>입니다.</returns>
        private EnemyEntity GetSelectedEnemy()
        {
            IReadOnlyList<EnemyEntity> enemies = battleManager.EnemyEntities;

            if (selectedEnemyIndex < enemies.Count && !enemies[selectedEnemyIndex].IsDead)
            {
                return enemies[selectedEnemyIndex];
            }

            for (int index = 0; index < enemies.Count; index++)
            {
                if (!enemies[index].IsDead)
                {
                    selectedEnemyIndex = index;
                    return enemies[index];
                }
            }

            return null;
        }
        #endregion // 테스트 UI
    }
}
