using System;
using EchoesOfAsh.Battle;
using EchoesOfAsh.Deck;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 전투의 행동력, 카드 더미 수와 턴 종료 입력을 표시하고 처리합니다.
    /// </summary>
    public class BattleHUDView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("턴 종료")]
        [SerializeField] private Button endTurnButton;

        [SWGroup("표시")]
        [SerializeField] private TextMeshProUGUI apText;
        [SerializeField] private TextMeshProUGUI drawPileText;
        [SerializeField] private TextMeshProUGUI discardPileText;

        private TurnManager turnManager;
        private ApSystem apSystem;
        private DeckSystem deckSystem;
        private Action endTurnRequest;
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 전투 진행 시스템을 연결하고 표시를 초기화합니다.
        /// </summary>
        /// <param name="turnManager">전투 턴 관리자입니다.</param>
        /// <param name="apSystem">행동력 시스템입니다.</param>
        /// <param name="deckSystem">덱 시스템입니다.</param>
        /// <param name="endTurnRequest">턴 종료 요청 함수입니다.</param>
        public void Init(TurnManager turnManager, ApSystem apSystem, DeckSystem deckSystem, Action endTurnRequest)
        {
            if (turnManager == null || apSystem == null || deckSystem == null)
            {
                SWLog.LogError("[BattleHUDView] 초기화 실패: 의존성 중 null이 있습니다");
                return;
            }

            Release();

            this.turnManager = turnManager;
            this.apSystem = apSystem;
            this.deckSystem = deckSystem;
            this.endTurnRequest = endTurnRequest;

            turnManager.OnPhaseChanged += HandlePhaseChanged;
            apSystem.OnApChanged += HandleApChanged;
            deckSystem.OnPileChanged += HandlePileChanged;

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(HandleEndTurnClicked);
            }

            HandlePhaseChanged(turnManager.CurrentPhase);
            HandleApChanged(apSystem.CurrentAp);
            HandlePileChanged(deckSystem.DrawPileCount, deckSystem.DiscardPileCount);
        }

        /// <summary>
        /// 연결된 이벤트와 버튼 입력을 해제합니다.
        /// </summary>
        public void Release()
        {
            if (turnManager != null)
            {
                turnManager.OnPhaseChanged -= HandlePhaseChanged;
            }

            if (apSystem != null)
            {
                apSystem.OnApChanged -= HandleApChanged;
            }

            if (deckSystem != null)
            {
                deckSystem.OnPileChanged -= HandlePileChanged;
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(HandleEndTurnClicked);
                endTurnButton.interactable = false;
            }

            turnManager = null;
            apSystem = null;
            deckSystem = null;
            endTurnRequest = null;
        }
        #endregion // 초기화

        #region 이벤트 처리
        /// <summary>
        /// 턴 종료 버튼 입력을 연결된 요청 함수에 전달합니다.
        /// </summary>
        private void HandleEndTurnClicked()
        {
            endTurnRequest?.Invoke();
        }

        /// <summary>
        /// 턴 단계가 변경되면 턴 종료 버튼의 활성 상태를 갱신합니다.
        /// 플레이어 행동 단계에서만 턴을 종료할 수 있습니다.
        /// </summary>
        /// <param name="phase">현재 턴 단계입니다.</param>
        private void HandlePhaseChanged(ETurnPhase phase)
        {
            if (endTurnButton != null)
            {
                endTurnButton.interactable = phase == ETurnPhase.PlayerAction;
            }
        }

        /// <summary>
        /// 행동력이 변경되면 표시 값을 갱신합니다.
        /// </summary>
        /// <param name="currentAp">현재 행동력입니다.</param>
        private void HandleApChanged(int currentAp)
        {
            if (apText != null)
            {
                apText.text = currentAp.ToString();
            }
        }

        /// <summary>
        /// 덱과 버림 더미의 카드 수가 변경되면 표시 값을 갱신합니다.
        /// </summary>
        /// <param name="drawCount">덱에 남은 카드 수입니다.</param>
        /// <param name="discardCount">버림 더미의 카드 수입니다.</param>
        private void HandlePileChanged(int drawCount, int discardCount)
        {
            if (drawPileText != null)
            {
                drawPileText.text = drawCount.ToString();
            }

            if (discardPileText != null)
            {
                discardPileText.text = discardCount.ToString();
            }
        }
        #endregion // 이벤트 처리
    }
}
