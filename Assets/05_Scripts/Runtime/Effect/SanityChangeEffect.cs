using EchoesOfAsh.Enum;
using SW.Attributes;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// 파티의 정신력을 지정한 값만큼 변경하는 효과입니다.
    /// </summary>
    [System.Serializable]
    [SWAddTypeMenu("정신력/변화")]
    public class SanityChangeEffect : EffectBlock
    {
        #region 필드
        [SerializeField] private int delta;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>정신력 변화량입니다.</summary>
        public int Delta => delta;

        /// <inheritdoc />
        public override EIntentType? IntentContribution => delta < 0 ? EIntentType.SanityPressure : EIntentType.Buff;
        #endregion // 프로퍼티

        /// <inheritdoc />
        public override void Apply(EffectContext context)
        {
            context.PartySanity?.ChangeSanity(delta);
        }

        /// <inheritdoc />
        public override string GetDescription()
        {
            return delta >= 0 ? $"정신력 +{delta}" : $"정신력 {delta}";
        }
    }
}
