using SW.Attributes;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    [System.Serializable]
    [SWAddTypeMenu("유틸리티/드로우")]
    public class DrawEffect : EffectBlock
    {
        #region 필드
        [SerializeField, Min(1)] private int count = 1;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        public override void Apply(EffectContext context)
        {
            context.DrawRequest?.Invoke(count);
        }

        public override string GetDescription()
        {
            return $"드로우 +{count}";
        }
    }

}