using EchoesOfAsh.Interface;
using SW.Attributes;
using SW.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// 대상에게 정신력 기반 피해를 적용하는 효과입니다.
    /// </summary>
    [System.Serializable]
    [SWAddTypeMenu("정신력/공격 (캐릭터 전용)")]
    public class SanityDamageEffect : EffectBlock
    {
        #region 필드
        [FormerlySerializedAs("sanityDamange")]
        [SerializeField, Min(0)] private int sanityDamage;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>정신력 피해량입니다.</summary>
        public int SanityDamage => sanityDamage;
        #endregion // 프로퍼티

        /// <inheritdoc />
        public override void Apply(EffectContext context)
        {
            foreach (ITargetable target in context.Targets)
            {
                if (target is ISanityHolder sanityHolder)
                {
                    sanityHolder.ChangeSanity(-sanityDamage);
                }
                else
                {
                    SWLog.LogError($"[SanityDamageEffect] {target.DisplayName}: ISanityHolder 미구현");
                }
            }
        }

        /// <inheritdoc />
        public override string GetDescription()
        {
            return $"정신력 타격 -{sanityDamage}";
        }
    }
}
