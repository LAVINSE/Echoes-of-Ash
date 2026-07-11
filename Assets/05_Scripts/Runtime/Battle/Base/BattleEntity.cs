using System;
using System.Collections.Generic;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Base;
using SW.Stat;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    public abstract class BattleEntity : SWMonoBehaviour, IDamageable, ITargetable, IStatusReceiver
    {
        #region 필드
        private SWStat maxHpStat;

        private int currentHp;
        private int currentBlock;
        private bool isDead;

        /// <summary>상태 이상 중첩 저장입니다.</summary>
        private readonly Dictionary<EStatusEffectType, int> statusStacks = new();
        /// <summary>피해 공식입니다.</summary>
        private IDamageCalculator damageCalculator = new DefaultDamageCalculator();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 HP입니다.</summary>
        public int CurrentHp => currentHp;
        /// <summary>최대 HP입니다.</summary>
        public int MaxHp => maxHpStat != null ? Mathf.RoundToInt(maxHpStat.Value) : 0;
        /// <summary>현재 방어막입니다.</summary>
        public int CurrentBlock => currentBlock;

        /// <summary>표시 이름입니다.</summary>
        public abstract string DisplayName { get; }
        /// <summary>엔티티가 생존하여 대상으로 지정할 수 있는지 여부입니다.</summary>
        public virtual bool IsTargetable => !isDead;
        /// <summary>사망 여부입니다.</summary>
        public bool IsDead => isDead;

        /// <summary>최대 HP 능력치 객체입니다.</summary>
        public SWStat MaxHpStat => maxHpStat;

        /// <summary>피해가 적용된 이후 호출됩니다.</summary>
        public event Action<int, int> OnDamaged;
        /// <summary>방어막 변경 시 호출됩니다.</summary>
        public event Action<int> OnBlockChanged;
        /// <summary>현재 체력이 변경될 때 호출됩니다.</summary>
        public event Action<int, int> OnHpChanged;
        /// <summary>사망 시 1회 호출됩니다.</summary>
        public event Action<BattleEntity> OnDied;
        #endregion // 프로퍼티

        #region 초기화
        protected void SetupHp(SWStatOverride maxHpOverride)
        {
            if (maxHpStat != null)
            {
                SWLog.LogError($"[BattleEntity] {name}: HP가 이미 설정되었습니다");
                return;
            }

            maxHpStat = maxHpOverride?.CreateStat();

            if (maxHpStat == null)
            {
                SWLog.LogError($"[BattleEntity] {name}: MaxHP 스탯 재정의가 비어 있습니다");
                return;
            }

            currentHp = MaxHp;
            maxHpStat.OnValueChanged += HandleMaxHpValueChanged;
        }

        public virtual void ResetEntity()
        {
            if (maxHpStat != null)
            {
                maxHpStat.OnValueChanged -= HandleMaxHpValueChanged;
                Destroy(maxHpStat);
                maxHpStat = null;
            }
        }

        protected virtual void OnDestroy()
        {
            ResetEntity();
        }
        #endregion // 초기화

        #region 피해 - 방어막
        /// <summary>
        /// 피해 계산기를 교체합니다.
        /// </summary>
        /// <param name="calculator">계산기입니다.</param>
        public void SetDamageCalculator(IDamageCalculator calculator)
        {
            if (calculator == null)
            {
                SWLog.LogError($"[BattleEntity] {name}: 피해 계산기가 null입니다");
                return;
            }

            damageCalculator = calculator;
        }

        /// <summary>
        /// 피해를 입는다
        /// 방어막 먼저 소모
        /// </summary>
        /// <param name="amount">피해량입니다.</param>
        /// <returns>방어막을 제외하고 실제로 잃은 HP입니다.</returns>
        public int TakeDamage(int amount)
        {
            if (isDead)
            {
                return 0;
            }

            int fianlAmount = damageCalculator.Calculate(amount, this);

            // 방어막 선 차감
            int blockAbsorbed = Mathf.Min(currentBlock, fianlAmount);

            if (blockAbsorbed > 0)
            {
                currentBlock -= blockAbsorbed;
                OnBlockChanged?.Invoke(currentBlock);
            }

            // 남은 피해를 HP에 적용
            int hpLose = Mathf.Min(currentHp, fianlAmount - blockAbsorbed);

            if (hpLose > 0)
            {
                currentHp -= hpLose;
                OnHpChanged?.Invoke(currentHp, MaxHp);
            }

            OnDamaged?.Invoke(hpLose, amount);

            if (currentHp <= 0)
            {
                Die();
            }

            return hpLose;
        }

        /// <summary>
        /// 방어막을 얻는다
        /// </summary>
        /// <param name="amount">방어막 획득량입니다.</param>
        public void GainBlock(int amount)
        {
            if (isDead || amount <= 0)
            {
                return;
            }

            currentBlock += amount;
            OnBlockChanged?.Invoke(currentBlock);
        }

        /// <summary>
        /// 방어막을 제거합니다.
        /// </summary>
        public void ResetBlock()
        {
            if (currentBlock == 0)
            {
                return;
            }

            currentBlock = 0;
            OnBlockChanged?.Invoke(currentBlock);
        }

        /// <summary>
        /// 체력을 회복합니다.
        /// </summary>
        /// <param name="amount">회복량입니다.</param>
        public void Heal(int amount)
        {
            if (isDead || amount <= 0)
            {
                return;
            }

            int healHp = Mathf.Min(currentHp + amount, MaxHp);

            if (healHp == currentHp)
            {
                return;
            }

            currentHp = healHp;
            OnHpChanged?.Invoke(currentHp, MaxHp);
        }

        /// <summary>
        /// 엔티티를 사망 상태로 전환합니다.
        /// </summary>
        private void Die()
        {
            isDead = true;
            OnDied?.Invoke(this);
        }

        /// <summary>
        /// 최대 HP 능력치 값 변경 처리 메서드
        /// </summary>
        /// <param name="stat">능력치입니다.</param>
        /// <param name="currentValue">현재 값입니다.</param>
        /// <param name="prevValue">이전 값입니다.</param>
        private void HandleMaxHpValueChanged(SWStat stat, float currentValue, float prevValue)
        {
            currentHp = Mathf.Min(currentHp, MaxHp);
            OnHpChanged?.Invoke(currentHp, MaxHp);
        }
        #endregion // 피해 - 방어막

        #region 상태이상
        /// <summary>
        /// 상태 이상을 적용합니다.
        /// </summary>
        /// <param name="statusType">상태 이상 유형입니다.</param>
        /// <param name="stack">중첩 수치입니다.</param>
        public void ApplyStatus(EStatusEffectType statusType, int stack)
        {
            statusStacks.TryGetValue(statusType, out int currentStack);
            statusStacks[statusType] = Mathf.Max(0, currentStack + stack);
        }

        /// <summary>
        /// 해당 상태 이상의 현재 중첩 수치를 반환합니다.
        /// </summary>
        /// <param name="statusType">상태 이상 유형입니다.</param>
        /// <returns>상태 이상 중첩 수치입니다.</returns>
        public int GetStatusStack(EStatusEffectType statusType)
            => statusStacks.TryGetValue(statusType, out int stack) ? stack : 0;
        #endregion // 상태이상
    }
}
