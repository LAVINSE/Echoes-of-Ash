using System;
using EchoesOfAsh.Data;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 파티 공유 AP 시스템
    /// </summary>
    public class ApSystem
    {
        #region 필드
        private int currentAp;

        private readonly BattleBalanceData balanceData;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 행동력입니다.</summary>
        public int CurrentAp => currentAp;

        /// <summary>턴마다 지급하는 행동력입니다.</summary>
        public int ApPerTurn => balanceData != null ? balanceData.ApPerTurn : 0;
        /// <summary>다음 턴으로 이월할 수 있는 행동력의 최댓값입니다.</summary>
        public int ApCarryOverMax => balanceData != null ? balanceData.ApCarryOverMax : 0;

        /// <summary>AP 변경 시 호출됩니다.</summary>
        public event Action<int> OnApChanged;
        #endregion // 프로퍼티

        #region 생성자
        public ApSystem(BattleBalanceData balanceData)
        {
            if (balanceData == null)
            {
                SWLog.LogError("[ApSystem] 생성 실패: BattleBalanceData가 null입니다");
            }

            this.balanceData = balanceData;
        }
        #endregion // 생성자

        /// <summary>
        /// 턴 시작 처리
        /// 남은 행동력을 이월 가능한 최댓값으로 제한한 뒤 이번 턴의 행동력을 지급합니다.
        /// </summary>
        public void StartTurn()
        {
            int carriedOver = Mathf.Min(CurrentAp, ApCarryOverMax);
            SetAp(carriedOver + ApPerTurn);
        }

        /// <summary>
        /// 행동력을 증감합니다.
        /// </summary>
        /// <param name="delta">변화량입니다.</param>
        public void Change(int delta)
        {
            SetAp(currentAp + delta);
        }

        /// <summary>
        /// 비용만큼 행동력 소모를 시도합니다.
        /// </summary>
        /// <param name="cost">비용입니다.</param>
        /// <returns>성공 여부입니다.</returns>
        public bool TrySpend(int cost)
        {
            if (cost < 0)
            {
                SWLog.LogError($"[ApSystem] TrySpend 실패: 음수 비용({cost})은 허용하지 않습니다");
                return false;
            }

            if (currentAp < cost)
            {
                return false;
            }

            SetAp(currentAp - cost);
            return true;
        }

        /// <summary>
        /// 행동력 소모가 가능한지 확인합니다.
        /// </summary>
        /// <param name="cost">비용입니다.</param>
        /// <returns>소모 가능 여부입니다.</returns>
        public bool CanSpend(int cost)
            => cost >= 0 && currentAp >= cost;

        /// <summary>
        /// 행동력을 0으로 초기화합니다.
        /// </summary>
        public void ResetAp()
        {
            SetAp(0);
        }

        /// <summary>
        /// 행동력을 지정 값으로 설정합니다.
        /// </summary>
        /// <param name="value">설정할 값입니다.</param>
        private void SetAp(int value)
        {
            int clampedValue = Mathf.Max(0, value);

            if (clampedValue == currentAp)
            {
                return;
            }

            currentAp = clampedValue;
            OnApChanged?.Invoke(currentAp);
        }
    }
}
