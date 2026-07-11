using System;
using System.Collections.Generic;
using EchoesOfAsh.Card;
using EchoesOfAsh.Deck;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 카드 사용 파이프라인
    /// </summary>
    public class CardPlayService
    {
        #region 필드
        private readonly ApSystem apSystem;
        private readonly DeckSystem deckSystem;
        private readonly EffectExecutor effectExecutor;
        private readonly ISanityHolder partySanityHolder;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>카드 사용 완료 시 호출 (사용 카드, 적용된 정신력 구간)입니다.</summary>
        public event Action<CardInstance, ESanityType> OnCardPlayed;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 카드 사용 파이프라인을 생성합니다.
        /// </summary>
        /// <param name="apSystem">AP 시스템입니다.</param>
        /// <param name="deckSystem">덱 시스템입니다.</param>
        /// <param name="effectExecutor">효과 실행기입니다.</param>
        /// <param name="partySanityHolder">파티 정신력입니다.</param>
        public CardPlayService(ApSystem apSystem, DeckSystem deckSystem, EffectExecutor effectExecutor, ISanityHolder partySanityHolder)
        {
            if (apSystem == null || deckSystem == null || effectExecutor == null || partySanityHolder == null)
            {
                SWLog.LogError("[CardPlayService] 생성 실패: 의존성 중 null이 있습니다");
            }

            this.apSystem = apSystem;
            this.deckSystem = deckSystem;
            this.effectExecutor = effectExecutor;
            this.partySanityHolder = partySanityHolder;
        }
        #endregion // 생성자

        #region 판정
        /// <summary>
        /// 카드 사용이 가능한지 확인합니다.
        /// </summary>
        /// <param name="card">확인할 카드입니다.</param>
        /// <returns>사용 가능 여부입니다.</returns>
        public bool CanPlay(CardInstance card)
        {
            if (card == null)
            {
                return false;
            }

            return deckSystem.IsInHand(card) && apSystem.CanSpend(card.ApCost);
        }

        /// <summary>
        /// 대상 목록이 카드 사용에 유효한지 확인합니다.
        /// </summary>
        /// <param name="card">사용할 카드입니다.</param>
        /// <param name="targets">대상 목록입니다.</param>
        /// <returns>대상 유효 여부입니다.</returns>
        public bool AreTargetsValid(CardInstance card, IReadOnlyList<ITargetable> targets)
        {
            if (card == null)
            {
                return false;
            }

            if (card.TargetingType == ETargetingType.Self)
            {
                return true;
            }

            if (targets == null || targets.Count == 0)
            {
                return false;
            }

            foreach (var target in targets)
            {
                if (target == null || !target.IsTargetable)
                {
                    return false;
                }
            }

            return true;
        }
        #endregion // 판정
        
        #region 사용
        /// <summary>
        /// 카드를 사용합니다.
        /// </summary>
        /// <param name="card">사용할 카드입니다.</param>
        /// <param name="caster">시전자입니다.</param>
        /// <param name="targets">효과 대상 목록입니다.</param>
        /// <returns>사용 성공 여부입니다.</returns>
        public bool Play(CardInstance card, ITargetable caster, IReadOnlyList<ITargetable> targets)
        {
            // 사용가능한 상태인지 확인
            if (!CanPlay(card))
            {
                SWLog.LogWarning($"[CardPlayService] 사용 불가: '{card?.DisplayName}' (손패 미포함 또는 AP 부족)");
                return false;
            }

            // 유효한 상태인지 확인
            if (!AreTargetsValid(card, targets))
            {
                SWLog.LogWarning($"[CardPlayService] 사용 불가: '{card.DisplayName}' 대상이 유효하지 않습니다");
                return false;
            }

            // 손패 분리
            if (!deckSystem.BeginPlay(card))
            {
                SWLog.LogError($"[CardPlayService] 손패 분리 실패: '{card.DisplayName}'");
                return false;
            }

            // AP 차감 시도
            if (!apSystem.TrySpend(card.ApCost))
            {
                SWLog.LogError($"[CardPlayService] Ap 차감 실패: '{card.DisplayName}'");
                deckSystem.EndPlay(card);
                return false;
            }

            ESanityType sanityType = partySanityHolder.CurrentSanityType;
            var effectBlocks = card.GetEffectBlocks(sanityType);

            effectExecutor.Execute(effectBlocks, caster, targets);

            // 버림 더미 이동
            deckSystem.EndPlay(card);

            OnCardPlayed?.Invoke(card, sanityType);
            return true;
        }
        #endregion // 사용
    }
}
