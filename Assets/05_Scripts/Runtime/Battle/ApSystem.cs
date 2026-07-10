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
        /// <summary>현재 AP</summary>
        public int CurrentAp => currentAp;

        /// <summary>AP 턴당 지급량</summary>
        public int ApPerTurn => balanceData != null ? balanceData.ApPerTurn : 0;
        /// <summary>AP 이월 상한</summary>
        public int ApCarryOverMax => balanceData != null ? balanceData.ApCarryOverMax : 0;

        /// <summary>AP 변경 시 호출</summary>
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
        /// 남은 AP를 이월 상한으로 턴당 지급량을 더한다
        /// </summary>
        public void StartTurn()
        {
            int carriedOver = Mathf.Min(CurrentAp, ApCarryOverMax);
            SetAp(carriedOver + ApPerTurn);
        }

        /// <summary>
        /// AP를 증감한다
        /// </summary>
        /// <param name="delta">변화량</param>
        public void Change(int delta)
        {
            SetAp(currentAp + delta);
        }

        /// <summary>
        /// 비용만큼 AP 소모를 시도한다
        /// </summary>
        /// <param name="cost">비용</param>
        /// <returns>성공 여부</returns>
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
        /// AP 소모가 가능한지 확인한다
        /// </summary>
        /// <param name="cost">비용</param>
        /// <returns>소모 가능 여부</returns>
        public bool CanSpend(int cost)
            => cost >= 0 && currentAp >= cost;

        /// <summary>
        /// AP를 0으로 초기화한다
        /// </summary>
        public void ResetAp()
        {
            SetAp(0);
        }

        /// <summary>
        /// AP를 지정 값으로 설정한다
        /// </summary>
        /// <param name="value">설정할 값</param>
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