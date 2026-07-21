using SW.Attributes;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// 덱에서 지정한 수만큼 카드를 뽑는 효과입니다.
    /// </summary>
    [System.Serializable]
    [SWAddTypeMenu("유틸리티/드로우")]
    public class DrawEffect : EffectBlock
    {
        #region 필드
        [SerializeField, Min(1)] private int count = 1;
        #endregion // 필드


        /// <inheritdoc />
        public override void Apply(EffectContext context)
        {
            context.DrawRequest?.Invoke(count);
        }

        /// <inheritdoc />
        public override string GetDescription()
        {
            return $"드로우 +{count}";
        }
    }

}
