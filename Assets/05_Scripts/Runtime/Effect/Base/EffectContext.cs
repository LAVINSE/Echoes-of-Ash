using System;
using System.Collections.Generic;
using EchoesOfAsh.Interface;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// 효과 실행에 필요한 시전자, 대상 및 전투 요청을 전달하는 문맥입니다.
    /// </summary>
    public class EffectContext
    {
        #region 프로퍼티
        /// <summary>효과 시전자 - 카드를 낸 파티원, 스킬을 쓰는 적입니다.</summary>
        public ITargetable Caster { get; }
        /// <summary>시전자 자신이 피해를 입거나 방어막을 얻을 때 사용입니다.</summary>
        public IDamageable CasterDamageable { get; }
        /// <summary>파티 정신력입니다.</summary>
        public ISanityHolder PartySanity { get; }

        /// <summary>효과 대상 목록입니다.</summary>
        public IReadOnlyList<ITargetable> Targets { get; }

        /// <summary>카드 드로우 요청입니다.</summary>
        public Action<int> DrawRequest { get; }
        /// <summary>카드 버림 요청입니다.</summary>
        public Action<int> DiscardRequest { get; }
        /// <summary>행동력 증감 요청입니다.</summary>
        public Action<int> ApChangeRequest { get; }
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 효과 실행 문맥을 생성합니다.
        /// </summary>
        /// <param name="caster">효과 시전자입니다.</param>
        /// <param name="partySanity">파티 공유 정신력입니다.</param>
        /// <param name="targets">효과 대상 목록입니다.</param>
        /// <param name="drawRequest">카드 드로우 요청입니다.</param>
        /// <param name="discardRequest">카드 버림 요청입니다.</param>
        /// <param name="apChangeRequest">행동력 증감 요청입니다.</param>
        public EffectContext(
            ITargetable caster,
            ISanityHolder partySanity,
            IReadOnlyList<ITargetable> targets,
            Action<int> drawRequest,
            Action<int> discardRequest,
            Action<int> apChangeRequest)
        {
            Caster = caster;
            CasterDamageable = caster as IDamageable;
            PartySanity = partySanity;
            Targets = targets;
            DrawRequest = drawRequest;
            DiscardRequest = discardRequest;
            ApChangeRequest = apChangeRequest;
        }
        #endregion // 생성자
    }
}
