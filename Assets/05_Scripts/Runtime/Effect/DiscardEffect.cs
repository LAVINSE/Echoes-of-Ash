using UnityEngine;

namespace EchoesOfAsh.Effect
{
    public class DiscardEffect : EffectBlock
    {
        #region 필드
        [SerializeField, Min(1)] private int count = 1; 
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        public override void Apply(EffectContext context)
        {
            context.DiscardRequest?.Invoke(count);
        }

        public override string GetDescription()
        {
            return $"카드 {count}장 버림";
        }
    }
}