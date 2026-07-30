using EchoesOfAsh.Interface;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 기본 피해 계산 결과에 활성 상태 이상의 배율을 적용합니다.
    /// </summary>
    public class StatusDamageCalculator : IDamageCalculator
    {
        #region 필드
        /// <summary>배율 적용 전의 기반 계산기입니다.</summary>
        private readonly IDamageCalculator baseCalculator;
        #endregion // 필드

        #region 생성자
        /// <summary>
        /// 상태 이상 배율 피해 계산기를 생성합니다.
        /// </summary>
        /// <param name="baseCalculator">기반 계산기입니다. null이면 기본 계산기를 사용합니다.</param>
        public StatusDamageCalculator(IDamageCalculator baseCalculator = null)
        {
            this.baseCalculator = baseCalculator ?? new DefaultDamageCalculator();
        }
        #endregion // 생성자

        /// <inheritdoc />
        public int Calculate(int baseAmount, ITargetable target)
        {
            int amount = baseCalculator.Calculate(baseAmount, target);

            if (target is BattleEntity entity)
            {
                float multiplier = entity.StatusController.GetDamageTakenMultiplier();

                if (!Mathf.Approximately(multiplier, 1f))
                {
                    amount = Mathf.FloorToInt(amount * multiplier);
                }
            }

            return Mathf.Max(0, amount);
        }
    }
}
