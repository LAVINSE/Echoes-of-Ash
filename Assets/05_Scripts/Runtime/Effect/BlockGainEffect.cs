using EchoesOfAsh.Enum;
using SWTools;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    [System.Serializable]
    [SWAddTypeMenu("방어/방어막 획득")]
    public class BlockGainEffect : EffectBlock
    {
        #region 필드
        [SerializeField, Min(0)] private int amount;
        #endregion // 필드

        #region 프로퍼티
        public override EIntentType? IntentContribution => EIntentType.Defense;
        #endregion // 프로퍼티

        public override void Apply(EffectContext context)
        {
            context.CasterDamageable?.GainBlock(amount);
        }

        public override string GetDescription()
        {
            return $"방어막 +{amount}";
        }
    }
}