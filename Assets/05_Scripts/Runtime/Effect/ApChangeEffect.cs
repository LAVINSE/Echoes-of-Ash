using SWTools;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    [System.Serializable]
    [SWAddTypeMenu("유틸리티/AP 증감")]
    public class ApChangeEffect : EffectBlock
    {
        #region 필드
        [SerializeField] private int delta;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        public override void Apply(EffectContext context)
        {
            context.ApChangeRequest?.Invoke(delta);
        }

        public override string GetDescription()
        {
            return delta >= 0 ? $"AP +{delta}" : $"AP {delta}";
        }
    }
}
