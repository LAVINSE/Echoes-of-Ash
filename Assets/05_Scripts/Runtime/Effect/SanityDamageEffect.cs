using EchoesOfAsh.Interface;
using SW.Attributes;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    [System.Serializable]
    [SWAddTypeMenu("정신력/공격 (캐릭터 전용)")]
    public class SanityDamageEffect : EffectBlock
    {
        #region 필드
        [SerializeField, Min(0)] private int sanityDamange;
        #endregion // 필드

        #region 프로퍼티
        public int SanityDamage => sanityDamange;
        #endregion // 프로퍼티

        public override void Apply(EffectContext context)
        {
            foreach(var target in context.Targets)
            {
                if (target is ISanityHolder sanityHolder)
                {
                    sanityHolder.ChangeSanity(-sanityDamange);
                }
                else
                {
                    SWLog.LogError($"[SanityDamageEffect] {target.DisplayName}: ISanityHolder 미구현");
                }
            }
        }

        public override string GetDescription()
        {
            return $"정신력 타격 -{sanityDamange}";
        }
    }
}