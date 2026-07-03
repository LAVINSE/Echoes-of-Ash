using System;
using UnityEngine;

namespace EchoesOfAsh.Interface
{
    public interface IDamageable
    {
        /// <summary> 현재 HP </summary>
        public int CurrentHp { get; }
        /// <summary> 최대 HP </summary>
        public int MaxHp { get; }
        /// <summary> 현재 방어막 </summary>
        public int CurrentBlock { get; }

        /// <summary> 피해가 적용된 이후 호출 (실제 HP 손실량, 피해 원본량)</summary>
        public event Action<int, int> OnDamaged;

        /// <summary>
        /// 피해를 입는다. 방어막 먼저 소모
        /// </summary>
        /// <param name="amount">피해량</param>
        /// <returns>방어막을 제외하고 실제로 잃은 HP</returns>
        public int TakeDamage(int amount);

        /// <summary>
        /// 방어막을 얻는다
        /// </summary>
        /// <param name="amount">방어막 획득량</param>
        public void GainBlock(int amount);
    }
}