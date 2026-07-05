using EchoesOfAsh.Enum;
using SWTools;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    [System.Serializable]
    [SWAddTypeMenu("정신력/변화")]
    public class SanityChangeEffect : EffectBlock
    {
        #region 필드
        [SerializeField] private int delta;
        #endregion // 필드

        #region 프로퍼티
        public override EIntentType? IntentContribution => delta < 0 ? EIntentType.SanityPressure : EIntentType.Buff;
        #endregion // 프로퍼티

        public override void Apply(EffectContext context)
        {
            context.PartySanity?.ChangeSanity(delta);
        }

        public override string GetDescription()
        {
            return delta >= 0 ? $"정신력 +{delta}" : $"정신력 {delta}";
        }
    }
}