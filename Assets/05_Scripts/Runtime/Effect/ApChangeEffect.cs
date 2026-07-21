using SW.Attributes;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// 파티의 행동력을 지정한 값만큼 변경하는 효과입니다.
    /// </summary>
    [System.Serializable]
    [SWAddTypeMenu("유틸리티/AP 증감")]
    public class ApChangeEffect : EffectBlock
    {
        #region 필드
        [SerializeField] private int delta;
        #endregion // 필드


        /// <inheritdoc />
        public override void Apply(EffectContext context)
        {
            context.ApChangeRequest?.Invoke(delta);
        }

        /// <inheritdoc />
        public override string GetDescription()
        {
            return delta >= 0 ? $"AP +{delta}" : $"AP {delta}";
        }
    }
}
