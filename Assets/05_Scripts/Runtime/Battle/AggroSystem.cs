using System.Collections.Generic;
using EchoesOfAsh.Data;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 파티원별 어그로 수치를 관리하는 시스템
    /// </summary>
    public class AggroSystem
    {
        #region 필드
        /// <summary>이 값 미만의 어그로는 만료로 간주하고 제거합니다.</summary>
        private const float MinimumAggro = 0.01f;

        private readonly BattleBalanceData balanceData;
        private CharacterEntity caster;

        private readonly Dictionary<CharacterEntity, float> aggroValues = new();
        private readonly List<EnemyEntity> registeredEnemies = new();
        private readonly List<CharacterEntity> tickBuffer = new();
        #endregion // 필드

        #region 생성자
        /// <summary>
        /// 어그로 시스템을 생성한다
        /// </summary>
        /// <param name="balanceData">전투 밸런스 데이터</param>
        public AggroSystem(BattleBalanceData balanceData)
        {
            if (balanceData == null)
            {
                SWLog.LogError("[AggroSystem] 생성 실패: BattleBalanceData가 null입니다");
            }

            this.balanceData = balanceData;
        }
        #endregion // 생성자

        #region 등록 - 정리
        /// <summary>
        /// 적의 피해 이벤트 등록
        /// </summary>
        /// <param name="enemyEntity">등록할 적 엔티티</param>
        public void RegisterEnemy(EnemyEntity enemyEntity)
        {
            if (enemyEntity == null || registeredEnemies.Contains(enemyEntity))
            {
                return;
            }

            enemyEntity.OnDamaged += HandleEnemyDamaged;
            registeredEnemies.Add(enemyEntity);
        }

        /// <summary>
        /// 모든 구독과 누적 상태를 정리합니다. 전투 리셋 시 호출합니다.
        /// </summary>
        public void Release()
        {
            foreach (EnemyEntity enemy in registeredEnemies)
            {
                if (enemy != null)
                {
                    enemy.OnDamaged -= HandleEnemyDamaged;
                }
            }

            registeredEnemies.Clear();
            aggroValues.Clear();
            caster = null;
        }
        #endregion // 등록 - 정리 

        #region 조회
        /// <summary>
        /// 해당 파티원의 현재 어그로 수치를 반환합니다.
        /// </summary>
        /// <param name="member">조회할 파티원입니다.</param>
        /// <returns>어그로 수치입니다.</returns>
        public float GetAggro(CharacterEntity member)
            => member != null && aggroValues.TryGetValue(member, out float aggro) ? aggro : 0f;

        /// <summary>
        /// 후보 중 어그로가 가장 높은 파티원을 반환합니다.
        /// 어그로가 같으면 파티 목록에서 앞에 있는 대상을 반환합니다. 모든 어그로가 0이면 null을 반환합니다.
        /// </summary>
        /// <param name="candidates">대상 지정 가능한 파티원 목록입니다.</param>
        /// <returns>최고 어그로 파티원입니다. 유효 어그로가 없으면 null입니다.</returns>
        public CharacterEntity PickTopAggro(IReadOnlyList<CharacterEntity> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            CharacterEntity topMember = null;
            float topAggro = 0f;

            foreach (CharacterEntity member in candidates)
            {
                float aggro = GetAggro(member);

                if (aggro > topAggro)
                {
                    topAggro = aggro;
                    topMember = member;
                }
            }

            return topMember;
        }
        #endregion // 조회

        /// <summary>
        /// 이후 적이 받는 피해를 지정한 시전자의 어그로에 더하도록 설정합니다.
        /// </summary>
        /// <param name="caster">카드 시전자입니다.</param>
        public void BeginAttribution(CharacterEntity caster)
        {
            this.caster = caster;
        }

        /// <summary>
        /// 피해와 시전자의 연결을 해제합니다. 이후 발생하는 피해는 어그로에 반영하지 않습니다.
        /// </summary>
        public void EndAttribution()
        {
            this.caster = null;
        }

        /// <summary>
        /// 적이 피해를 받으면 해당 피해량만큼 시전자의 어그로를 높입니다.
        /// 방어막으로 막힌 피해도 원래 피해량을 기준으로 어그로에 반영합니다.
        /// </summary>
        /// <param name="healthPointLoss">실제 HP 손실량입니다.</param>
        /// <param name="amount">원본 피해량입니다.</param>
        private void HandleEnemyDamaged(int healthPointLoss, int amount)
        {
            if (caster == null || amount <= 0)
            {
                return;
            }

            float weight = balanceData != null ? balanceData.AggroDamageWeight : 1f;

            aggroValues.TryGetValue(caster, out float currentAggro);
            aggroValues[caster] = currentAggro + amount * weight;
        }

        /// <summary>
        /// 라운드 종료 시점의 어그로 감쇠를 처리합니다.
        /// 상태 이상 감소 후 어그로를 줄이고 적의 다음 행동을 다시 계산합니다.
        /// </summary>
        public void TickRound()
        {
            if (aggroValues.Count == 0)
            {
                return;
            }

            float decayRate = balanceData != null ? balanceData.AggroRoundDecayRate : 0.5f;

            // 목록을 확인하는 동안 항목을 안전하게 제거하기 위해 키를 복사합니다.
            tickBuffer.Clear();
            tickBuffer.AddRange(aggroValues.Keys);

            foreach (CharacterEntity member in tickBuffer)
            {
                float decayed = aggroValues[member] * decayRate;

                if (decayed < MinimumAggro)
                {
                    aggroValues.Remove(member);
                }
                else
                {
                    aggroValues[member] = decayed;
                }
            }
        }
    }
}
