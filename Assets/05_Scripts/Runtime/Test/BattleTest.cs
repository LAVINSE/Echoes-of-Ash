using System.Collections.Generic;
using EchoesOfAsh.Battle;
using EchoesOfAsh.Card;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Test
{
    public class BattleTest : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("전투")]
        [SerializeField] private BattleManager battleManager;

        [SWGroup("랜덤 시드 값")]
        [SerializeField] private bool useFixedSeed = true;
        [SerializeField] private int seed = 12345;

        private bool isRun;
        private bool isSubscribed;
        private int selectedEnemyIndex;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        #region 테스트
        [SWButton("전투 시작")]
        private void TestRun()
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

            if (!battleManager.StartBattle())
            {
                return;
            }

            selectedEnemyIndex = 0;
            isRun = true;
        }

        [SWButton("테스트 초기화")]
        private void TestReset()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            battleManager?.ResetBattle();
            isRun = false;
        }

        /// <summary>
        /// 전투 이벤트 로그 구독을 설정합니다. (중복 구독 방지)
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
        private void OnGUI()
        {
            if (!isRun)
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
        /// 파티 HP / 방어막 / 공유 SAN / AP를 표시합니다.
        /// </summary>
        private void DrawPartyStatus()
        {
            var character = battleManager.Character;
            var partySanity = battleManager.PartySanityHolder;
            var apSystem = battleManager.ApSystem;

            if (character == null || partySanity == null || apSystem == null)
            {
                return;
            }

            GUILayout.Label("=== 파티 ===");
            GUILayout.Label($"{character.DisplayName}  " +
                            $"HP {character.CurrentHp}/{character.MaxHp}  " +
                            $"방어막 {character.CurrentBlock}  " +
                            $"{(character.IsDead ? "[사망]" : "[생존]")}");
            GUILayout.Label($"공유 SAN {partySanity.CurrentSanity}/{partySanity.MaxSanity}  " +
                            $"[{(partySanity.CurrentSanityType == ESanityType.Madness ? "광기" : "평정")}]");
            GUILayout.Label($"AP {apSystem.CurrentAp}");
        }

        /// <summary>
        /// 적별 HP / SAN / 의도를 표시하고 단일 대상 지정을 처리합니다.
        /// </summary>
        private void DrawEnemyStatus()
        {
            GUILayout.Label("=== 적 ===");

            IReadOnlyList<EnemyEntity> enemies = battleManager.EnemyEntities;
            IReadOnlyList<EnemyAI> enemyAis = battleManager.EnemyAIs;

            for (int index = 0; index < enemies.Count; index++)
            {
                EnemyEntity enemy = enemies[index];

                GUILayout.BeginHorizontal();

                string selectedMark = index == selectedEnemyIndex ? "▶" : "  ";
                string status = $"{selectedMark} {enemy.DisplayName}  " +
                                $"HP {enemy.CurrentHp}/{enemy.MaxHp}  " +
                                $"방어막 {enemy.CurrentBlock}  " +
                                $"SAN {enemy.CurrentSanity}/{enemy.MaxSanity} " +
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
                EnemyAI enemyAi = enemyAis[index];

                if (!enemy.IsDead && enemyAi.NextAction != null)
                {
                    var action = enemyAi.NextAction;
                    string intents = string.Join(", ", action.GetIntentTypes());
                    GUILayout.Label($"    의도: '{action.ActionName}' [{intents}]  " +
                                    $"피해 {action.GetIntentDamageValue()}  " +
                                    $"SAN 압박 {action.GetIntentSanityPressureValue()}");
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

            GUILayout.Label($"=== 손패 ({deckSystem.Hand.Count}장)  덱 {deckSystem.DrawPileCount} / 버림 {deckSystem.DiscardPileCount} ===");

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

            battleManager.PlayCard(card, designatedTarget);
        }

        /// <summary>
        /// 지정된 적을 반환합니다. 사망 상태면 첫 생존 적으로 대체합니다.
        /// </summary>
        /// <returns>지정 대상 적입니다. 생존 적이 없으면 null입니다.</returns>
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