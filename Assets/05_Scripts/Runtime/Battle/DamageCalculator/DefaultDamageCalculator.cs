using System;
using EchoesOfAsh.Interface;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 기본 피해 계산기입니다.
    /// </summary>
    public class DefaultDamageCalculator : IDamageCalculator
    {
        /// <inheritdoc />
        public int Calculate(int baseAmount, ITargetable target)
            => Math.Max(0, baseAmount);
    }
}
