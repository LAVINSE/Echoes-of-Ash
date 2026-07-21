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
    /// 전투 HUD 뷰
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

        #region 프로퍼티
        #endregion // 프로퍼티

        #region 초기화
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

        /// <summary>
        /// 턴 종료 버튼 콜백
        /// </summary>
        private void HandleEndTurnClicked()
        {
            endTurnRequest?.Invoke();
        }

        /// <summary>
        /// 턴 단계 변경 시 버튼 활성 상태 갱신 콜백
        /// 플레이어 행동 단계에만 턴 종료 가능
        /// </summary>
        /// <param name="phase">현재 턴 단계</param>
        private void HandlePhaseChanged(ETurnPhase phase)
        {
            if (endTurnButton != null)
            {
                endTurnButton.interactable = phase == ETurnPhase.PlayerAction;
            }
        }

        /// <summary>
        /// AP 변경시 표시를 갱신 콜백
        /// </summary>
        /// <param name="currentAp">현재 AP</param>
        private void HandleApChanged(int currentAp)
        {
            if (apText != null)
            {
                apText.text = currentAp.ToString();
            }
        }
        
        /// <summary>
        /// 덱/버림 더미 수 변경 시 카운터 갱신 콜백
        /// </summary>
        /// <param name="drawCount">덱 남은 수</param>
        /// <param name="discardCount">버림 더미 수</param>
        private void HandlePileChanged(int drawCount, int discardCount)
        {
            if (drawPileText != null)
            {
                drawPileText.text = drawCount.ToString();
            }
            
            if(discardPileText != null)
            {
                discardPileText.text = discardCount.ToString();
            }
        }
    }
}