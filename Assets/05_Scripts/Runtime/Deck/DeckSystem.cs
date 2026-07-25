using System;
using System.Collections.Generic;
using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Deck
{
    /// <summary>
    /// 덱 / 손패 / 버림 더미입니다.
    /// </summary>
    public class DeckSystem
    {
        #region 필드
        private readonly int maxHandSize;

        private readonly List<CardInstance> drawPile = new();
        private readonly List<CardInstance> hand = new();
        private readonly List<CardInstance> discardPile = new();
        private readonly List<CardInstance> exclusionPile = new();
        private Func<CardInstance, bool> drawExclusionPredicate;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 손패입니다.</summary>
        public IReadOnlyList<CardInstance> Hand => hand;

        /// <summary>덱 남은 수 (뽑을 더미)입니다.</summary>
        public int DrawPileCount => drawPile.Count;
        /// <summary>버림 더미 수입니다.</summary>
        public int DiscardPileCount => discardPile.Count;
        /// <summary>드로우 제외 더미 수입니다. 전투불능 파티원의 전용 카드가 대기합니다.</summary>
        public int ExclusionPileCount => exclusionPile.Count;
        /// <summary>최대 손패 수입니다.</summary>
        public int MaxHandSize => maxHandSize;

        /// <summary>손패 변경 시 호출됩니다.</summary>
        public event Action OnHandChanged;
        /// <summary>덱/버림 더미 수 변경 시 호출 (덱 수, 버림 수)입니다.</summary>
        public event Action<int, int> OnPileChanged;
        /// <summary>손패 초과로 카드가 버려질 때 호출됩니다.</summary>
        public event Action<CardInstance> OnOverdraw;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 덱 시스템을 생성합니다.
        /// </summary>
        /// <param name="startingCards">시작 카드 목록입니다.</param>
        /// <param name="balanceData">전투 규칙 데이터입니다.</param>
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
        /// DeckSystem 리셋입니다.
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

            foreach (var card in discardPile)
            {
                card.ResetBattleApCost();
            }

            foreach (var card in exclusionPile)
            {
                card.ResetBattleApCost();
            }
        }
        #endregion // 초기화

        #region 드로우
        /// <summary>
        /// 지정한 수만큼 카드를 뽑습니다.
        /// 덱이 비면 버림 더미를 섞어 덱을 다시 구성한 뒤 계속 뽑습니다.
        /// 손패가 가득 차면 뽑은 카드는 버림 더미로 이동입니다.
        /// </summary>
        /// <param name="count">수량입니다.</param>
        /// <returns>실제 손패에 들어간 카드 수입니다.</returns>
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

                // 제외 대상은 손패 대신 제외 더미로 이동하고, 이번 드로우는 소모하지 않습니다
                if (IsExcluded(card))
                {
                    exclusionPile.Add(card);
                    i--;
                    continue;
                }

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
        /// 버림 더미의 카드를 덱으로 옮긴 뒤 순서를 무작위로 섞습니다.
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
        /// 손패의 특정 카드를 버림 더미로 보냅니다.
        /// </summary>
        /// <param name="card">버릴 카드입니다.</param>
        /// <returns>성공 여부입니다.</returns>
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
        /// 손패에서 지정한 수만큼 카드를 무작위로 버립니다.
        /// </summary>
        /// <param name="count">버릴 수입니다.</param>
        /// <returns>실제 버린 카드 수입니다.</returns>
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
        /// 손패 전체를 버림 더미로 보냅니다.
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

        #region 드로우 제외
        /// <summary>
        /// 드로우 제외 판정을 설정하고 즉시 제외 상태를 갱신합니다.
        /// 판정이 true인 카드는 덱과 버림 더미에서 제외 더미로 이동해 드로우되지 않습니다.
        /// </summary>
        /// <param name="predicate">제외 판정입니다. null이면 제외 없음입니다.</param>
        public void SetDrawExclusion(Func<CardInstance, bool> predicate)
        {
            drawExclusionPredicate = predicate;
            RefreshDrawExclusion();
        }

        /// <summary>
        /// 현재 판정 기준으로 제외 상태를 다시 계산합니다.
        /// 제외 대상이 된 카드는 덱과 버림 더미에서 제외 더미로 이동하고,
        /// 제외가 해제된 카드는 버림 더미로 복귀합니다. 손패는 대상이 아닙니다.
        /// </summary>
        public void RefreshDrawExclusion()
        {
            bool isChanged = false;

            // 제외 해제 카드 → 버림 더미 복귀 (부활 대비 — 역방향 순회로 안전 제거)
            for (int index = exclusionPile.Count - 1; index >= 0; index--)
            {
                CardInstance card = exclusionPile[index];

                if (!IsExcluded(card))
                {
                    exclusionPile.RemoveAt(index);
                    discardPile.Add(card);
                    isChanged = true;
                }
            }

            isChanged |= MoveExcludedCards(drawPile);
            isChanged |= MoveExcludedCards(discardPile);

            if (isChanged)
            {
                NotifyChanged();
            }
        }

        /// <summary>
        /// 더미에서 제외 대상 카드를 제외 더미로 이동합니다.
        /// </summary>
        /// <param name="pile">검사할 더미입니다.</param>
        /// <returns>이동한 카드가 있으면 true입니다.</returns>
        private bool MoveExcludedCards(List<CardInstance> pile)
        {
            bool isMoved = false;

            for (int index = pile.Count - 1; index >= 0; index--)
            {
                CardInstance card = pile[index];

                if (IsExcluded(card))
                {
                    pile.RemoveAt(index);
                    exclusionPile.Add(card);
                    isMoved = true;
                }
            }

            return isMoved;
        }

        /// <summary>
        /// 카드가 드로우 제외 대상인지 판정합니다.
        /// </summary>
        /// <param name="card">판정할 카드입니다.</param>
        /// <returns>제외 대상이면 true입니다.</returns>
        private bool IsExcluded(CardInstance card)
        {
            return drawExclusionPredicate != null && card != null && drawExclusionPredicate(card);
        }
        #endregion // 드로우 제외

        #region 카드 사용
        /// <summary>
        /// 카드가 현재 손패에 있는지 확인합니다.
        /// </summary>
        /// <param name="card">확인할 카드입니다.</param>
        /// <returns>손패 포함 여부입니다.</returns>
        public bool IsInHand(CardInstance card)
            => hand.Contains(card);

        /// <summary>
        /// 카드 사용을 시작할 수 있도록 카드를 손패에서 분리합니다.
        /// </summary>
        /// <param name="card">사용할 카드입니다.</param>
        /// <returns>분리 성공 여부입니다.</returns>
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
        /// 사용이 끝난 카드를 버림 더미로 보냅니다.
        /// </summary>
        /// <param name="card">사용이 끝난 카드입니다.</param>
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
        /// 변경 이벤트를 실행합니다.
        /// </summary>
        public void NotifyChanged()
        {
            OnHandChanged?.Invoke();
            OnPileChanged?.Invoke(drawPile.Count, discardPile.Count);
        }
    }
}
