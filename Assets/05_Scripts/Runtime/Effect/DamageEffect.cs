using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Attributes;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// 대상에게 지정한 횟수만큼 피해를 적용하는 효과입니다.
    /// </summary>
    [System.Serializable]
    [SWAddTypeMenu("공격/피해")]
    public class DamageEffect : EffectBlock
    {
        #region 필드
        [SerializeField, Min(0)] private int damage;
        [SerializeField, Min(1)] private int times;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>피해량입니다.</summary>
        public int Damage => damage;
        /// <summary>반복 횟수입니다.</summary>
        public int Times => times;
        /// <inheritdoc />
        public override EIntentType? IntentContribution => EIntentType.Attack;
        #endregion // 프로퍼티

        /// <inheritdoc />
        public override void Apply(EffectContext context)
        {
            foreach (ITargetable target in context.Targets)
            {
                if (target is not IDamageable damageable)
                {
                    continue;
                }

                for (int iteration = 0; iteration < times; iteration++)
                {
                    damageable.TakeDamage(damage);
                }
            }
        }

        /// <inheritdoc />
        public override string GetDescription()
        {
            return times > 1 ? $"{damage} 피해 x{times}" : $"{damage} 피해";
        }
    }

}
