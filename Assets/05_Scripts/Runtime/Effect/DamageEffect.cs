using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Attributes;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    [System.Serializable]
    [SWAddTypeMenu("공격/피해")]
    public class DamageEffect : EffectBlock
    {
        #region 필드
        [SerializeField, Min(0)] private int damage;
        [SerializeField, Min(1)] private int times;
        #endregion // 필드

        #region 프로퍼티
        /// <summary> 피해량 </summary>
        public int Damage => damage;
        /// <summary> 반복 횟수</summary>
        public int Times => times;
        public override EIntentType? IntentContribution => EIntentType.Attack;
        #endregion // 프로퍼티

        public override void Apply(EffectContext context)
        {
            foreach (var target in context.Targets)
            {
                if (target is not IDamageable damageable)
                {
                    continue;
                }

                for (int i = 0; i < times; i++)
                {
                    damageable.TakeDamage(damage);
                }
            }
        }

        public override string GetDescription()
        {
            return times > 1 ? $"{damage} 피해 x{times}" : $"{damage} 피해";
        }
    }

}