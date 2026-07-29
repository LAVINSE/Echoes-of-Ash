using System;
using System.Collections.Generic;
using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Attributes;
using SW.Base;
using SW.Stat;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 전투 참여자의 HP, 방어막, 피해 및 상태 이상을 관리하는 기본 엔티티입니다.
    /// </summary>
    public abstract class BattleEntity : SWMonoBehaviour, IDamageable, ITargetable, IStatusReceiver
    {
        #region 필드
        [SerializeField, SWReadOnly] private int currentHp;
        [SerializeField, SWReadOnly] private int currentBlock;
        [SerializeField, SWReadOnly] private bool isDead;

        private SWStat maxHpStat;

        /// <summary>상태 이상 생명주기 관리자입니다.</summary>
        private readonly StatusController statusController = new();
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
        /// <summary>
        /// 최대 HP 능력치와 현재 HP를 초기화합니다.
        /// </summary>
        /// <param name="maxHpOverride">적용할 최대 HP 능력치 재정의입니다.</param>
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

        /// <summary>
        /// 전투 중 생성된 능력치와 이벤트 구독을 초기화합니다.
        /// </summary>
        public virtual void ResetEntity()
        {
            statusController.ResetAll();
            
            if (maxHpStat != null)
            {
                maxHpStat.OnValueChanged -= HandleMaxHpValueChanged;
                Destroy(maxHpStat);
                maxHpStat = null;
            }
        }

        /// <summary>
        /// 객체가 제거될 때 전투 엔티티의 런타임 상태를 정리합니다.
        /// </summary>
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
        /// 피해를 입습니다.
        /// 방어막 먼저 소모합니다.
        /// </summary>
        /// <param name="amount">피해량입니다.</param>
        /// <returns>방어막을 제외하고 실제로 잃은 HP입니다.</returns>
        public int TakeDamage(int amount)
        {
            if (isDead)
            {
                return 0;
            }

            int finalAmount = damageCalculator.Calculate(amount, this);

            // 방어막 선 차감
            int blockAbsorbed = Mathf.Min(currentBlock, finalAmount);

            if (blockAbsorbed > 0)
            {
                currentBlock -= blockAbsorbed;
                OnBlockChanged?.Invoke(currentBlock);
            }

            // 남은 피해를 HP에 적용
            int healthPointLoss = Mathf.Min(currentHp, finalAmount - blockAbsorbed);

            if (healthPointLoss > 0)
            {
                currentHp -= healthPointLoss;
                OnHpChanged?.Invoke(currentHp, MaxHp);
            }

            OnDamaged?.Invoke(healthPointLoss, amount);

            if (currentHp <= 0)
            {
                Die();
            }

            return healthPointLoss;
        }

        /// <summary>
        /// 방어막을 얻습니다.
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
        /// 최대 HP 능력치 값이 변경되었을 때 현재 HP를 보정합니다.
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
        /// <summary>상태 이상 생명주기 관리자입니다 (판정·뷰 조회용).</summary>
        public StatusController StatusController => statusController;

        /// <summary>
        /// 상태 이상 정의 목록을 등록합니다. 조립 지점(BattleManager)에서 스폰 직후 호출합니다.
        /// </summary>
        /// <param name="statusDatas">상태 이상 정의 목록입니다.</param>
        public void SetStatusDatas(IReadOnlyList<StatusEffectData> statusDatas)
            => statusController.SetDatabase(statusDatas);

        /// <summary>
        /// 상태 이상을 적용합니다.
        /// </summary>
        /// <param name="statusType">상태 이상 유형입니다.</param>
        /// <param name="stack">중첩 수치입니다.</param>
        public void ApplyStatus(EStatusEffectType statusType, int stack)
            => statusController.ApplyStatus(statusType, stack);

        /// <summary>
        /// 해당 상태 이상의 현재 중첩 수치를 반환합니다.
        /// </summary>
        /// <param name="statusType">상태 이상 유형입니다.</param>
        /// <returns>상태 이상 중첩 수치입니다.</returns>
        public int GetStatusStack(EStatusEffectType statusType)
            => statusController.GetStatusStack(statusType);

        /// <summary>
        /// 라운드 종료 시점의 상태 이상 중첩 감소를 처리합니다. 조립 지점이 라운드 종료마다 호출합니다.
        /// </summary>
        public void TickStatusRound()
            => statusController.TickRound();
        #endregion // 상태이상
    }
}
