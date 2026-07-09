using EchoesOfAsh.Interface;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 기본 피해 계산기
    /// </summary>
    public class DefaultDamageCalculator : IDamageCalculator
    {
        public int Calculate(int baseAmount, ITargetable target)
           => Mathf.Max(0, baseAmount);
    }
}