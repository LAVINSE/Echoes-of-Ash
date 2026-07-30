using System;
using UnityEngine;

namespace EchoesOfAsh.Interface
{
    /// <summary>
    /// 체력과 방어막을 보유하고 피해를 받을 수 있는 대상의 기능을 정의합니다.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>현재 HP입니다.</summary>
        public int CurrentHp { get; }
        /// <summary>최대 HP입니다.</summary>
        public int MaxHp { get; }
        /// <summary>현재 방어막입니다.</summary>
        public int CurrentBlock { get; }

        /// <summary>피해가 적용된 후 실제 HP 손실량과 원본 피해량을 전달합니다.</summary>
        public event Action<int, int> OnDamaged;

        /// <summary>
        /// 피해를 입는다. 방어막 먼저 소모합니다.
        /// </summary>
        /// <param name="amount">피해량입니다.</param>
        /// <returns>방어막을 제외하고 실제로 잃은 HP입니다.</returns>
        public int TakeDamage(int amount);

        /// <summary>
        /// 방어막을 얻습니다.
        /// </summary>
        /// <param name="amount">방어막 획득량입니다.</param>
        public void GainBlock(int amount);
    }
}
