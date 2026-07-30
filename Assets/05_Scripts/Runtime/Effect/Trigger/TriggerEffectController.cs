using System;
using System.Collections.Generic;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Util;

namespace EchoesOfAsh.Effect.Trigger
{
    /// <summary>
    /// 전투 중 조건에 맞는 자동 효과를 등록하고 실행합니다.
    /// 효과는 등록한 순서대로 실행하며, 실행 중에 같은 효과가 다시 요청되면 무시합니다.
    /// </summary>
    public class TriggerEffectController
    {
        #region 데이터
        /// <summary>
        /// 소유자와 트리거 효과의 등록 단위입니다.
        /// </summary>
        private readonly struct TriggerEntry
        {
            /// <summary>효과 소유자입니다. 파티 범위 등록(공용 유물)이면 null입니다.</summary>
            public readonly ITargetable Owner;
            /// <summary>파티 공용 효과를 실행할 때 사용할 시전자를 반환합니다. 개인 효과에서는 사용하지 않습니다.</summary>
            public readonly Func<ITargetable> CasterProvider;
            /// <summary>경고 로그에 사용할 출처 이름입니다.</summary>
            public readonly string SourceName;
            /// <summary>트리거 효과입니다.</summary>
            public readonly TriggerEffect Effect;

            /// <summary>파티 범위 등록 여부입니다.</summary>
            public bool IsPartyScoped => Owner == null;

            /// <summary>
            /// 트리거 효과 등록 단위를 생성합니다.
            /// </summary>
            /// <param name="owner">효과 소유자입니다. 파티 범위면 null입니다.</param>
            /// <param name="casterProvider">파티 범위의 시전자 공급자입니다. 소유자 등록이면 null입니다.</param>
            /// <param name="sourceName">경고 로그용 출처 이름입니다.</param>
            /// <param name="effect">트리거 효과입니다.</param>
            public TriggerEntry(ITargetable owner, Func<ITargetable> casterProvider, string sourceName, TriggerEffect effect)
            {
                Owner = owner;
                CasterProvider = casterProvider;
                SourceName = sourceName;
                Effect = effect;
            }
        }
        #endregion // 데이터

        #region 필드
        private readonly EffectExecutor effectExecutor;
        private readonly ISanityHolder partySanityHolder;

        private readonly List<TriggerEntry> entries = new();
        private readonly List<ITargetable> casterTargetBuffer = new();

        /// <summary>자동 효과를 실행 중인지 여부입니다. 실행 중에 들어온 추가 요청은 무시합니다.</summary>
        private bool isRaising;
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

        #region 등록
        /// <summary>
        /// 캐릭터 패시브와 전용 유물 효과를 등록합니다. 등록한 순서대로 실행합니다.
        /// 소유자 사망 시 발동하지 않습니다.
        /// </summary>
        /// <param name="owner">효과 소유자입니다.</param>
        /// <param name="triggerEffects">등록할 트리거 효과 목록입니다.</param>
        public void Register(ITargetable owner, IReadOnlyList<TriggerEffect> triggerEffects)
        {
            if (owner == null)
            {
                SWLog.LogError("[TriggerEffectController] Register 실패: 소유자가 null입니다");
                return;
            }

            RegisterInternal(owner, null, owner.DisplayName, triggerEffects);
        }

        /// <summary>
        /// 파티 공용 유물 효과를 등록합니다. 등록한 순서대로 실행합니다.
        /// 효과를 실행할 때 공급 함수에서 시전자를 가져옵니다.
        /// </summary>
        /// <param name="triggerEffects">등록할 트리거 효과 목록입니다.</param>
        /// <param name="casterProvider">효과를 실행할 시전자를 반환하는 함수입니다. null이면 실행하지 않습니다.</param>
        /// <param name="sourceName">경고 로그용 출처 이름입니다.</param>
        public void Register(IReadOnlyList<TriggerEffect> triggerEffects, Func<ITargetable> casterProvider, string sourceName)
        {
            if (casterProvider == null)
            {
                SWLog.LogError($"[TriggerEffectController] Register 실패: '{sourceName}' 시전자 공급자가 null입니다");
                return;
            }

            RegisterInternal(null, casterProvider, sourceName, triggerEffects);
        }

        /// <summary>
        /// 등록 공통 처리입니다. 빈 효과 블록은 건너뜁니다.
        /// </summary>
        /// <param name="owner">효과 소유자입니다. 파티 범위면 null입니다.</param>
        /// <param name="casterProvider">파티 범위의 시전자 공급자입니다.</param>
        /// <param name="sourceName">경고 로그용 출처 이름입니다.</param>
        /// <param name="triggerEffects">등록할 트리거 효과 목록입니다.</param>
        private void RegisterInternal(ITargetable owner, Func<ITargetable> casterProvider, string sourceName, IReadOnlyList<TriggerEffect> triggerEffects)
        {
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
                    SWLog.LogWarning($"[TriggerEffectController] '{sourceName}' 트리거 효과의 블록이 비어 있어 등록을 건너뜁니다");
                    continue;
                }

                entries.Add(new TriggerEntry(owner, casterProvider, sourceName, triggerEffect));
            }
        }

        /// <summary>
        /// 등록된 트리거 효과를 전부 제거합니다.
        /// </summary>
        public void Clear()
        {
            entries.Clear();
        }
        #endregion // 등록

        #region 자동 효과 실행
        /// <summary>
        /// 지정한 시점에 해당하는 자동 효과를 등록한 순서대로 실행합니다.
        /// </summary>
        /// <param name="triggerType">효과를 실행할 시점입니다.</param>
        public void Raise(ETriggerType triggerType)
        {
            RaiseInternal(triggerType, null);
        }

        /// <summary>
        /// 피해를 받거나 주는 대상과 관련된 자동 효과를 실행합니다.
        /// 해당 대상의 개인 효과와 파티 공용 효과만 실행합니다.
        /// </summary>
        /// <param name="triggerType">효과를 실행할 시점입니다.</param>
        /// <param name="instigator">피해를 받거나 준 대상입니다.</param>
        public void RaiseFor(ETriggerType triggerType, ITargetable instigator)
        {
            if (instigator == null)
            {
                return;
            }

            RaiseInternal(triggerType, instigator);
        }

        /// <summary>
        /// 실행 조건을 확인하고 조건에 맞는 자동 효과를 처리합니다.
        /// 소유자가 사망했거나 정신력 조건이 맞지 않으면 건너뜁니다.
        /// </summary>
        /// <param name="triggerType">효과를 실행할 시점입니다.</param>
        /// <param name="instigator">관련 대상입니다. null이면 모든 공용 효과를 확인합니다.</param>
        private void RaiseInternal(ETriggerType triggerType, ITargetable instigator)
        {
            if (isRaising)
            {
                return;
            }

            isRaising = true;

            try
            {
                foreach (TriggerEntry entry in entries)
                {
                    if (entry.Effect.TriggerType != triggerType)
                    {
                        continue;
                    }

                    // 개인 효과는 관련 대상의 것만 실행하고 파티 공용 효과는 항상 실행합니다.
                    if (instigator != null && !entry.IsPartyScoped && !ReferenceEquals(entry.Owner, instigator))
                    {
                        continue;
                    }

                    ITargetable caster = entry.IsPartyScoped ? entry.CasterProvider?.Invoke() : entry.Owner;

                    if (caster == null || !caster.IsTargetable)
                    {
                        continue;
                    }

                    if (!IsSanityConditionMet(entry.Effect.SanityCondition))
                    {
                        continue;
                    }

                    casterTargetBuffer.Clear();
                    casterTargetBuffer.Add(caster);

                    effectExecutor.Execute(entry.Effect.Effects, caster, casterTargetBuffer);
                }
            }
            finally
            {
                isRaising = false;
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
        #endregion // 자동 효과 실행

        #region 이벤트
        /// <summary>
        /// 방어막이 초기화된 뒤 턴 시작 자동 효과를 실행합니다.
        /// </summary>
        /// <param name="turnNumber">현재 턴 번호입니다.</param>
        public void HandleTurnStarted(int turnNumber)
        {
            Raise(ETriggerType.TurnStart);
        }
        #endregion // 이벤트
    }
}
