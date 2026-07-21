using EchoesOfAsh.Enum;
using SW.Attributes;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// 대상에게 지정한 양의 방어막을 부여하는 효과입니다.
    /// </summary>
    [System.Serializable]
    [SWAddTypeMenu("방어/방어막 획득")]
    public class BlockGainEffect : EffectBlock
    {
        #region 필드
        [SerializeField, Min(0)] private int amount;
        #endregion // 필드

        #region 프로퍼티
        /// <inheritdoc />
        public override EIntentType? IntentContribution => EIntentType.Defense;
        #endregion // 프로퍼티

        /// <inheritdoc />
        public override void Apply(EffectContext context)
        {
            context.CasterDamageable?.GainBlock(amount);
        }

        /// <inheritdoc />
        public override string GetDescription()
        {
            return $"방어막 +{amount}";
        }
    }
}
