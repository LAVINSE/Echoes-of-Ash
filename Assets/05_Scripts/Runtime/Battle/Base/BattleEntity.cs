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

        /// <summary>상태이상 중첩 저장</summary>
        private readonly Dictionary<EStatusEffectType, int> statusStacks = new();
        /// <summary>피해 공식</summary>
        private IDamageCalculator damageCalculator = new DefaultDamageCalculator();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 HP</summary>
        public int CurrentHp => currentHp;
        /// <summary>최대 HP</summary>
        public int MaxHp => maxHpStat != null ? Mathf.RoundToInt(maxHpStat.Value) : 0;
        /// <summary>현재 방어막</summary>
        public int CurrentBlock => currentBlock;

        /// <summary>표시 이름</summary>
        public abstract string DisplayName { get; }
        /// <summary>타겟이 유효한지 (사망 시 false)</summary>
        public virtual bool IsTargetable => !isDead;
        /// <summary>사망 여부</summary>
        public bool IsDead => isDead;

        /// <summary>최대 HP 스탯 객체</summary>
        public SWStat MaxHpStat => maxHpStat;

        /// <summary>피해가 적용된 이후 호출</summary>
        public event Action<int, int> OnDamaged;
        /// <summary>방어막 변경 시 호출</summary>
        public event Action<int> OnBlockChanged;
        /// <summary>HP 변경 시 호출</summary>
        public event Action<int, int> OnHpChanged;
        /// <summary>사망 시 1회 호출</summary>
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
        /// 피해 계산기를 교체한다
        /// </summary>
        /// <param name="calculator">계산기</param>
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
        /// <param name="amount">피해량</param>
        /// <returns>방어막을 제외하고 실제로 잃은 HP</returns>
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
        /// <param name="amount">방어막 획득량</param>
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
        /// 방어막을 제거한다
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
        /// HP를 회복한다
        /// </summary>
        /// <param name="amount">회복량</param>
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
        /// 사망처리
        /// </summary>
        private void Die()
        {
            isDead = true;
            OnDied?.Invoke(this);
        }

        /// <summary>
        /// MaxHp 스탯 값 변경 콜백
        /// </summary>
        /// <param name="stat">스탯</param>
        /// <param name="currentValue">현재 값</param>
        /// <param name="prevValue">이전 값</param>
        private void HandleMaxHpValueChanged(SWStat stat, float currentValue, float prevValue)
        {
            currentHp = Mathf.Min(currentHp, MaxHp);
            OnHpChanged?.Invoke(currentHp, MaxHp);
        }
        #endregion // 피해 - 방어막

        #region 상태이상
        /// <summary>
        /// 상태이상을 적용한다
        /// </summary>
        /// <param name="statusType">상태이상 타입</param>
        /// <param name="stack">중첩 수치</param>
        public void ApplyStatus(EStatusEffectType statusType, int stack)
        {
            statusStacks.TryGetValue(statusType, out int currentStack);
            statusStacks[statusType] = Mathf.Max(0, currentStack + stack);
        }

        /// <summary>
        /// 해당 상태이상의 현재 중첩 수치를 반환한다
        /// </summary>
        /// <param name="statusType">상태이상 타입</param>
        /// <returns>상태이상 중첩 수치</returns>
        public int GetStatusStack(EStatusEffectType statusType)
            => statusStacks.TryGetValue(statusType, out int stack) ? stack : 0;
        #endregion // 상태이상
    }
}