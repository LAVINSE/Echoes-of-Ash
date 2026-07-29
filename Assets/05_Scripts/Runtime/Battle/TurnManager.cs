using System;
using EchoesOfAsh.Data;
using EchoesOfAsh.Deck;
using EchoesOfAsh.Enum;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 전투 턴의 진행 단계를 관리하는 상태 머신입니다.
    /// </summary>
    public class TurnManager
    {
        #region 필드
        private int currentTurn;
        private ETurnPhase currentPhase = ETurnPhase.None;

        private readonly ApSystem apSystem;
        private readonly DeckSystem deckSystem;
        private readonly BattleBalanceData balanceData;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 턴 - 1부터 시작합니다.</summary>
        public int CurrentTurn => currentTurn;
        /// <summary>현재 턴 진행 단계입니다.</summary>
        public ETurnPhase CurrentPhase => currentPhase;

        /// <summary>턴 단계 변경 시 호출됩니다.</summary>
        public event Action<ETurnPhase> OnPhaseChanged;
        /// <summary>턴 시작 시 AP 지급, 드로우 전에 호출됩니다.</summary>
        public event Action<int> OnTurnStarted;
        /// <summary>턴 시작 자원 처리 후 호출됩니다.</summary>
        public event Action<int> OnTurnStartHook;
        /// <summary>플레이어 턴 종료 시 손패 버림 후 호출됩니다.</summary>
        public event Action<int> OnTurnEnded;
        /// <summary>적 행동 단계 진입 시 호출됩니다.</summary>
        public event Action<int> OnEnemyActionsStarted;
        /// <summary>라운드 종료 시 호출됩니다.</summary>
        public event Action<int> OnRoundEnded;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 전투 턴의 진행 단계를 관리하는 상태 머신을 생성합니다.
        /// </summary>
        /// <param name="apSystem">AP 시스템입니다.</param>
        /// <param name="deckSystem">덱 시스템입니다.</param>
        /// <param name="balanceData">전투 규칙 데이터입니다.</param>
        public TurnManager(ApSystem apSystem, DeckSystem deckSystem, BattleBalanceData balanceData)
        {
            if (apSystem == null || deckSystem == null || balanceData == null)
            {
                SWLog.LogError("[TurnManager] 생성 실패: 의존성 중 null이 있습니다");
            }

            this.apSystem = apSystem;
            this.deckSystem = deckSystem;
            this.balanceData = balanceData;
        }
        #endregion // 생성자

        #region 턴
        /// <summary>
        /// 전투 시작합니다.
        /// </summary>
        public void StartBattle()
        {
            if (currentPhase != ETurnPhase.None)
            {
                SWLog.LogError($"[TurnManager] StartBattle 실패: 이미 진행 중입니다 (단계: {currentPhase})");
                return;
            }

            currentTurn = 0;
            StartNextTurn();
        }

        /// <summary>
        /// 플레이어 턴 종료, 적 행동 및 라운드 종료를 처리한 뒤 다음 턴으로 진행합니다.
        /// 진행 중 전투가 종료되면 그 지점에서 즉시 중단합니다.
        /// </summary>
        public void EndPlayerTurn()
        {
            if (currentPhase != ETurnPhase.PlayerAction)
            {
                SWLog.LogError($"[TurnManager] EndPlayerTurn 무시: 플레이어 행동 단계가 아닙니다 (단계: {currentPhase})");
                return;
            }

            SetPhase(ETurnPhase.TurnEnd);

            // 손패 버림 -> 턴 종료
            deckSystem.DiscardHand();
            OnTurnEnded?.Invoke(currentTurn);

            if (currentPhase == ETurnPhase.BattleEnd)
            {
                return;
            }

            // 적 행동 단계
            SetPhase(ETurnPhase.EnemyAction);
            OnEnemyActionsStarted?.Invoke(currentTurn);

            if (currentPhase == ETurnPhase.BattleEnd)
            {
                return;
            }

            // 라운드 종료
            OnRoundEnded?.Invoke(currentTurn);

            if (currentPhase == ETurnPhase.BattleEnd)
            {
                return;
            }

            StartNextTurn();
        }

        /// <summary>
        /// 전투 종료 상태로 전환합니다.
        /// </summary>
        public void EndBattle()
        {
            if (currentPhase == ETurnPhase.BattleEnd)
            {
                return;
            }

            SetPhase(ETurnPhase.BattleEnd);
        }

        /// <summary>
        /// 턴 번호를 증가시키고 자원 지급과 카드 뽑기를 처리한 뒤 플레이어 행동 단계로 전환합니다.
        /// </summary>
        private void StartNextTurn()
        {
            currentTurn++;
            SetPhase(ETurnPhase.TurnStart);

            // 턴 시작
            OnTurnStarted?.Invoke(currentTurn);

            apSystem.StartTurn();
            deckSystem.Draw(balanceData.DrawPerTurn);

            OnTurnStartHook?.Invoke(currentTurn);

            if (currentPhase == ETurnPhase.BattleEnd)
            {
                return;
            }

            SetPhase(ETurnPhase.PlayerAction);
        }

        /// <summary>
        /// 턴 단계를 변경하고 변경 이벤트 호출됩니다.
        /// </summary>
        /// <param name="phase">변경할 단계입니다.</param>
        private void SetPhase(ETurnPhase phase)
        {
            if (currentPhase == phase)
            {
                return;
            }

            currentPhase = phase;
            OnPhaseChanged?.Invoke(currentPhase);
        }
        #endregion // 턴
    }
}
