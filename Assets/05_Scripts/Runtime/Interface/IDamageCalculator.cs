using UnityEngine;

namespace EchoesOfAsh.Interface
{
    /// <summary>
    /// 피해 공식 계산
    /// </summary>
    public interface IDamageCalculator
    {
        /// <summary>
        /// 최종 피해량을 계산합니다.
        /// </summary>
        /// <param name="baseAmount">기본 피해량입니다.</param>
        /// <param name="target">피해를 받는 대상입니다.</param>
        /// <returns></returns>
        public int Calculate(int baseAmount, ITargetable target);
    }
}
