using UnityEngine;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// 손패에서 지정한 수만큼 카드를 버리는 효과입니다.
    /// </summary>
    public class DiscardEffect : EffectBlock
    {
        #region 필드
        [SerializeField, Min(1)] private int count = 1;
        #endregion // 필드


        /// <inheritdoc />
        public override void Apply(EffectContext context)
        {
            context.DiscardRequest?.Invoke(count);
        }

        /// <inheritdoc />
        public override string GetDescription()
        {
            return $"카드 {count}장 버림";
        }
    }
}
