using System;
using System.Collections.Generic;
using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Deck
{
    /// <summary>
    /// 덱 / 손패 / 버림 더미
    /// </summary>
    public class DeckSystem
    {
        #region 필드
        private readonly int maxHandSize;

        private readonly List<CardInstance> drawPile = new();
        private readonly List<CardInstance> hand = new();
        private readonly List<CardInstance> discardPile = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 손패</summary>
        public IReadOnlyList<CardInstance> Hand => hand;

        /// <summary>덱 남은 수 (뽑을 더미)</summary>
        public int DrawPileCount => drawPile.Count;
        /// <summary>버림 더미 수</summary>
        public int DiscardPileCount => discardPile.Count;
        /// <summary>최대 손패 수</summary>
        public int MaxHandSize => maxHandSize;

        /// <summary>손패 변경 시 호출</summary>
        public event Action OnHandChanged;
        /// <summary>덱/버림 더미 수 변경 시 호출 (덱 수, 버림 수)</summary>
        public event Action<int, int> OnPileChanged;
        /// <summary>손패 초과로 카드가 버려질 때 호출</summary>
        public event Action<CardInstance> OnOverdraw;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 덱 시스템을 생성한다
        /// </summary>
        /// <param name="startingCards">시작 카드 목록</param>
        /// <param name="balanceData">전투 규칙 데이터</param>
        public DeckSystem(IEnumerable<CardInstance> startingCards, BattleBalanceData balanceData)
        {
            if (balanceData == null)
            {
                SWLog.LogError("[DeckSystem] 생성 실패: BattleBalanceData가 null입니다");
                maxHandSize = 10;
            }
            else
            {
                maxHandSize = balanceData.MaxHandSize;
            }

            if (startingCards != null)
            {
                foreach (var card in startingCards)
                {
                    if (card != null)
                    {
                        drawPile.Add(card);
                    }
                }
            }

            SWRandom.Shuffle(drawPile);
        }
        #endregion // 생성자

        #region 초기화
        /// <summary>
        /// DeckSystem 리셋
        /// </summary>
        public void ResetDeckSystem()
        {
            foreach (var card in drawPile)
            {
                card.ResetBattleApCost();
            }

            foreach (var card in hand)
            {
                card.ResetBattleApCost();
            }
            
            foreach(var card in discardPile)
            {
                card.ResetBattleApCost();
            }
        }
        #endregion // 초기화

        #region 드로우
        /// <summary>
        /// 카드를 n장 뽑는다
        /// 덱이 비면 버림 더미를 셔플해 이어서 뽑는다
        /// 손패가 가득 차면 뽑은 카드는 버림 더미로 이동
        /// </summary>
        /// <param name="count">수량</param>
        /// <returns>실제 손패에 들어간 카드 수</returns>
        public int Draw(int count)
        {
            int drawToHand = 0;

            for (int i = 0; i < count; i++)
            {
                if (drawPile.Count == 0)
                {
                    ReshuffleDiscardIntoDrawPile();

                    //셔플 후에도 비어 있으면 뽑을 카드가 없음
                    if (drawPile.Count == 0)
                    {
                        break;
                    }
                }

                int lastIndex = drawPile.Count - 1;
                CardInstance card = drawPile[lastIndex];
                drawPile.RemoveAt(lastIndex);

                if (hand.Count >= maxHandSize)
                {
                    // 손패 초과 - 뽑은 카드는 버림 더미로
                    discardPile.Add(card);
                    OnOverdraw?.Invoke(card);
                    continue;
                }

                hand.Add(card);
                drawToHand++;
            }

            NotifyChanged();
            return drawToHand;
        }

        /// <summary>
        /// 버림 더미를 덱으로 옮기로 셔플한다
        /// </summary>
        private void ReshuffleDiscardIntoDrawPile()
        {
            if (discardPile.Count == 0)
            {
                return;
            }

            drawPile.AddRange(discardPile);
            discardPile.Clear();

            SWRandom.Shuffle(drawPile);
        }
        #endregion // 드로우

        #region 버림
        /// <summary>
        /// 손패의 특정 카드를 버림 더미로 보낸다
        /// </summary>
        /// <param name="card">버릴 카드</param>
        /// <returns>성공 여부</returns>
        public bool Discard(CardInstance card)
        {
            if (!hand.Remove(card))
            {
                SWLog.LogError($"[DeckSystem] Discard 실패: '{card?.DisplayName}' 카드가 손패에 없습니다");
                return false;
            }

            discardPile.Add(card);
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 손패에서 무작위로 n장을 버린다
        /// </summary>
        /// <param name="count">버릴 수</param>
        /// <returns>실제 버린 카드 수</returns>
        public int DiscardRandom(int count)
        {
            int discard = 0;

            for (int i = 0; i < count && hand.Count > 0; i++)
            {
                int index = SWRandom.Range(0, hand.Count);
                CardInstance card = hand[index];
                hand.RemoveAt(index);

                discardPile.Add(card);
                discard++;
            }

            if (discard > 0)
            {
                NotifyChanged();
            }

            return discard;
        }

        /// <summary>
        /// 손패 전체를 버림 더미로 보낸다
        /// </summary>
        public void DiscardHand()
        {
            if (hand.Count == 0)
            {
                return;
            }

            foreach (var card in hand)
            {
                discardPile.Add(card);
            }

            hand.Clear();
            NotifyChanged();
        }
        #endregion // 버림

        #region 카드 사용
        /// <summary>
        /// 카드가 현재 손패에 있는지 확인한다
        /// </summary>
        /// <param name="card">확인할 카드</param>
        /// <returns>손패 포함 여부</returns>
        public bool IsInHand(CardInstance card)
            => hand.Contains(card);

        /// <summary>
        /// 사용을 위해 카드를 손패에서 분리한다 (사용 중 상태)
        /// </summary>
        /// <param name="card">사용할 카드</param>
        /// <returns>분리 성공 여부</returns>
        public bool BeginPlay(CardInstance card)
        {
            if (!hand.Remove(card))
            {
                return false;
            }

            NotifyChanged();
            return true;
        }

        /// <summary>
        /// 사용이 끝난 카드를 버림 더미로 보낸다
        /// </summary>
        /// <param name="card"></param>
        public void EndPlay(CardInstance card)
        {
            if (card == null)
            {
                return;
            }

            discardPile.Add(card);
            NotifyChanged();
        }
        #endregion // 카드 사용

        /// <summary>
        /// 변경 이벤트를 실행한다
        /// </summary>
        public void NotifyChanged()
        {
            OnHandChanged?.Invoke();
            OnPileChanged?.Invoke(drawPile.Count, discardPile.Count);
        }
    }
}