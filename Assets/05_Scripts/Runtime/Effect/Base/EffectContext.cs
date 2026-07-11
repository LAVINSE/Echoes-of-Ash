using System;
using System.Collections.Generic;
using EchoesOfAsh.Interface;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    public class EffectContext
    {
        /// <summary>효과 시전자 - 카드를 낸 파티원, 스킬을 쓰는 적입니다.</summary>
        public ITargetable Caster;
        /// <summary>시전자 자신이 피해를 입거나 방어막을 얻을 때 사용입니다.</summary>
        public IDamageable CasterDamageable;
        /// <summary>파티 정신력입니다.</summary>
        public ISanityHolder PartySanity;

        /// <summary>효과 대상 목록입니다.</summary>
        public IReadOnlyList<ITargetable> Targets;

        /// <summary>카드 드로우 요청입니다.</summary>
        public Action<int> DrawRequest;
        /// <summary>카드 버림 요청입니다.</summary>
        public Action<int> DiscardRequest;
        /// <summary>행동력 증감 요청입니다.</summary>
        public Action<int> ApChangeRequest;
    }
}
