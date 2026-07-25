using System.Collections.Generic;
using EchoesOfAsh.Battle;
using EchoesOfAsh.Card;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Util;

namespace EchoesOfAsh.Effect.Trigger
{
    /// <summary>
    /// 전투 중 트리거 효과를 등록하고 발동합니다.
    /// </summary>
    public class TriggerEffectController
    {
        #region 데이터
        /// <summary>
        /// 소유자와 트리거 효과의 등록 단위입니다.
        /// </summary>
        private readonly struct TriggerEntry
        {
            /// <summary>효과 소유자입니다. 사망 시 발동하지 않습니다.</summary>
            public readonly CharacterEntity Owner;
            /// <summary>트리거 효과입니다.</summary>
            public readonly TriggerEffect Effect;

            /// <summary>
            /// 트리거 효과 등록 단위를 생성합니다.
            /// </summary>
            /// <param name="owner">효과 소유자입니다.</param>
            /// <param name="effect">트리거 효과입니다.</param>
            public TriggerEntry(CharacterEntity owner, TriggerEffect effect)
            {
                Owner = owner;
                Effect = effect;
            }
        }
        #endregion // 데이터

        #region 필드
        private readonly EffectExecutor effectExecutor;
        private readonly ISanityHolder partySanityHolder;

        private readonly List<TriggerEntry> entries = new();
        private readonly List<ITargetable> ownerTargetBuffer = new();
        #endregion // 필드

        #region 생성자
        /// <summary>
        /// 트리거 효과 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="effectExecutor">효과 실행기입니다.</param>
        /// <param name="partySanityHolder">파티 공유 정신력입니다.</param>
        public TriggerEffectController(EffectExecutor effectExecutor, ISanityHolder partySanityHolder)
        {
            if (effectExecutor == null || partySanityHolder == null)
            {
                SWLog.LogError("[TriggerEffectController] 생성 실패: 의존성 중 null이 있습니다");
            }

            this.effectExecutor = effectExecutor;
            this.partySanityHolder = partySanityHolder;
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 소유자의 트리거 효과 목록을 등록합니다. 등록 순서 = 발화 순서입니다.
        /// </summary>
        /// <param name="owner">효과 소유자입니다.</param>
        /// <param name="triggerEffects">등록할 트리거 효과 목록입니다.</param>
        public void Register(CharacterEntity owner, IReadOnlyList<TriggerEffect> triggerEffects)
        {
            if (owner == null)
            {
                SWLog.LogError("[TriggerEffectController] Register 실패: 소유자가 null입니다");
                return;
            }

            if (triggerEffects == null)
            {
                return;
            }

            foreach (TriggerEffect triggerEffect in triggerEffects)
            {
                if (triggerEffect == null)
                {
                    continue;
                }

                if (triggerEffect.Effects.Count == 0)
                {
                    SWLog.LogWarning($"[TriggerEffectController] '{owner.DisplayName}' 트리거 효과의 블록이 비어 있어 등록을 건너뜁니다");
                    continue;
                }

                if (triggerEffect.TriggerType == ETriggerType.TakeDamage
                    || triggerEffect.TriggerType == ETriggerType.DealDamage
                    || triggerEffect.TriggerType == ETriggerType.BattleEnd)
                {
                    // 피격, 가해 및 전투 종료 발화 지점은 관련 기능 구현 시 연결합니다.
                    SWLog.LogWarning($"[TriggerEffectController] '{owner.DisplayName}' {triggerEffect.TriggerType} 트리거는 아직 발화 지점이 없습니다");
                }

                entries.Add(new TriggerEntry(owner, triggerEffect));
            }
        }

        /// <summary>
        /// 등록된 트리거 효과를 전부 제거합니다.
        /// </summary>
        public void Clear()
        {
            entries.Clear();
        }

        /// <summary>
        /// 지정한 시점의 트리거 효과를 등록 순으로 발화합니다.
        /// 소유자 사망 또는 정신력 조건 불충족 항목은 건너뜁니다.
        /// </summary>
        /// <param name="triggerType">발화할 시점입니다.</param>
        public void Raise(ETriggerType triggerType)
        {
            foreach (TriggerEntry entry in entries)
            {
                if (entry.Effect.TriggerType != triggerType)
                {
                    continue;
                }

                if (entry.Owner == null || entry.Owner.IsDead)
                {
                    continue;
                }

                if (!IsSanityConditionMet(entry.Effect.SanityCondition))
                {
                    continue;
                }

                ownerTargetBuffer.Clear();
                ownerTargetBuffer.Add(entry.Owner);

                effectExecutor.Execute(entry.Effect.Effects, entry.Owner, ownerTargetBuffer);
            }
        }

        /// <summary>
        /// 정신력 구간 조건 충족 여부를 판정합니다.
        /// </summary>
        /// <param name="sanityCondition">판정할 조건입니다.</param>
        /// <returns>충족하면 true입니다.</returns>
        private bool IsSanityConditionMet(ESanityCondition sanityCondition)
        {
            if (sanityCondition == ESanityCondition.None)
            {
                return true;
            }

            if (partySanityHolder == null)
            {
                return false;
            }

            bool isMadness = partySanityHolder.CurrentSanityType == ESanityType.Madness;
            return sanityCondition == ESanityCondition.MadnessOnly ? isMadness : !isMadness;
        }

        /// <summary>
        /// 턴 시작 처리입니다. TurnManager.OnTurnStarted에 구독됩니다 (방어막 리셋 이후 — 구독 순서 계약).
        /// </summary>
        /// <param name="turnNumber">현재 턴 번호입니다.</param>
        public void HandleTurnStarted(int turnNumber)
        {
            Raise(ETriggerType.TurnStart);
        }

        /// <summary>
        /// 카드 사용 완료 처리입니다. CardPlayService.OnCardPlayed에 구독됩니다.
        /// </summary>
        /// <param name="card">사용한 카드입니다.</param>
        /// <param name="sanityType">적용된 정신력 구간입니다.</param>
        public void HandleCardPlayed(CardInstance card, ESanityType sanityType)
        {
            Raise(ETriggerType.CardPlayed);
        }
        #endregion // 함수
    }
}
