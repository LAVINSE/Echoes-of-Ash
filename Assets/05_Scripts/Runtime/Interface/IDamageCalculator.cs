using UnityEngine;

namespace EchoesOfAsh.Interface
{
    /// <summary>
    /// 피해 공식 계산
    /// </summary>
    public interface IDamageCalculator
    {
        /// <summary>
        /// 최종 피해량을 계산한다
        /// </summary>
        /// <param name="baseAmount">기본 피해량</param>
        /// <param name="target">피해를 받는 대상</param>
        /// <returns></returns>
        public int Calculate(int baseAmount, ITargetable target);
    }
}
