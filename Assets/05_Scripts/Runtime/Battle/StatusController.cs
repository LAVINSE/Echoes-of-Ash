using System;
using System.Collections.Generic;
using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 전투원 1인의 상태 이상 관리하는 클래스
    /// </summary>
    public class StatusController
    {
        #region 필드
        /// <summary>상태이상별 현재 중첩입니다.</summary>
        private readonly Dictionary<EStatusEffectType, int> stacks = new();
        /// <summary>적용 순서 활성 상태이상 목록</summary>
        private readonly List<EStatusEffectType> activeOrder = new();
        /// <summary>상태이상별 정의 데이터</summary>
        private readonly Dictionary<EStatusEffectType, StatusEffectData> dataByType = new();
        /// <summary>라운드 감소 순회용 버퍼</summary>
        private readonly List<EStatusEffectType> tickBuffer = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>적용 순서 활성 상태이상 목록</summary>
        public IReadOnlyList<EStatusEffectType> ActiveStatuses => activeOrder;

        /// <summary>상태 이상 중첩이 변경될 때 유형과 현재 중첩(만료 = 0)을 전달합니다.</summary>
        public event Action<EStatusEffectType, int> OnStatusChanged;
        #endregion // 프로퍼티

        /// <summary>
        /// 상태 이상 정의 목록을 등록합니다. 중복 유형은 먼저 등록된 정의를 유지합니다.
        /// </summary>
        /// <param name="statusDatas">상태 이상 정의 목록입니다.</param>
        public void SetDatabase(IReadOnlyList<StatusEffectData> statusDatas)
        {
            dataByType.Clear();

            if (statusDatas == null)
            {
                return;
            }

            foreach (StatusEffectData statusData in statusDatas)
            {
                if (statusData == null || statusData.StatusEffectType == EStatusEffectType.None)
                {
                    continue;
                }

                if (!dataByType.TryAdd(statusData.StatusEffectType, statusData))
                {
                    SWLog.LogWarning($"[StatusController] 상태 이상 정의 중복: {statusData.StatusEffectType} — {statusData.name} 무시");
                }
            }
        }

        /// <summary>
        /// 해당 유형의 정의 데이터를 반환합니다. 미등록이면 null입니다.
        /// </summary>
        /// <param name="statusType">상태 이상 유형입니다.</param>
        /// <returns>정의 데이터 또는 null입니다.</returns>
        public StatusEffectData GetData(EStatusEffectType statusType)
            => dataByType.TryGetValue(statusType, out StatusEffectData statusData) ? statusData : null;

        #region 부여 - 조회
        /// <summary>
        /// 상태 이상 중첩을 가감합니다. 음수 전달 시 감소하며 0 미만으로 내려가지 않습니다.
        /// </summary>
        /// <param name="statusType">상태 이상 유형입니다.</param>
        /// <param name="stack">가감할 중첩 수치입니다.</param>
        public void ApplyStatus(EStatusEffectType statusType, int stack)
        {
            if (statusType == EStatusEffectType.None)
            {
                SWLog.LogWarning("[StatusController] ApplyStatus 무시: 유형이 None입니다");
                return;
            }

            if (!dataByType.ContainsKey(statusType))
            {
                SWLog.LogWarning($"[StatusController] {statusType} 정의(StatusEffectData) 미등록 — 기본 규칙(라운드마다 1 감소)으로 동작합니다");
            }

            stacks.TryGetValue(statusType, out int currentStack);
            SetStack(statusType, currentStack + stack);
        }

        /// <summary>
        /// 해당 상태 이상의 현재 중첩 수치를 반환합니다.
        /// </summary>
        /// <param name="statusType">상태 이상 유형입니다.</param>
        /// <returns>상태 이상 중첩 수치입니다.</returns>
        public int GetStatusStack(EStatusEffectType statusType)
            => stacks.TryGetValue(statusType, out int stack) ? stack : 0;

        /// <summary>
        /// 중첩을 확정 값으로 반영하고 활성 목록과 변경 이벤트를 정리합니다.
        /// </summary>
        /// <param name="statusType">상태 이상 유형입니다.</param>
        /// <param name="newStack">반영할 중첩 수치입니다.</param>
        private void SetStack(EStatusEffectType statusType, int newStack)
        {
            newStack = Mathf.Max(0, newStack);

            stacks.TryGetValue(statusType, out int currentStack);

            if (currentStack == newStack)
            {
                return;
            }

            if (newStack > 0)
            {
                stacks[statusType] = newStack;

                if (currentStack == 0)
                {
                    activeOrder.Add(statusType);
                }
            }
            else
            {
                stacks.Remove(statusType);
                activeOrder.Remove(statusType);
            }

            OnStatusChanged?.Invoke(statusType, newStack);
        }
        #endregion // 부여 - 조회

        #region 판정
        /// <summary>
        /// 활성 상태 이상 전체의 받는 피해 배율 곱을 반환합니다 (취약 = 1.5).
        /// 배율은 중첩 수와 무관하게 활성 여부로만 적용됩니다 (중첩 = 남은 라운드 수).
        /// </summary>
        /// <returns>받는 피해 배율입니다.</returns>
        public float GetDamageTakenMultiplier()
        {
            float multiplier = 1f;

            foreach (EStatusEffectType statusType in activeOrder)
            {
                if (dataByType.TryGetValue(statusType, out StatusEffectData statusData))
                {
                    multiplier *= statusData.DamageTakenMultiplier;
                }
            }

            return multiplier;
        }
        #endregion // 판정

        #region 라운드 감소
        /// <summary>
        /// 라운드 종료 시점의 중첩 감소를 처리합니다. 감소 규칙이 카운트다운인 유형만 1 감소하며 0 도달 시 만료됩니다.
        /// </summary>
        public void TickRound()
        {
            if (activeOrder.Count == 0)
            {
                return;
            }

            // 순회 중 만료 제거가 일어나므로 버퍼로 순회 (부여 순 고정)
            tickBuffer.Clear();
            tickBuffer.AddRange(activeOrder);

            foreach (EStatusEffectType statusType in tickBuffer)
            {
                EStatusDecayType decayType = dataByType.TryGetValue(statusType, out StatusEffectData statusData)
                    ? statusData.DecayType
                    : EStatusDecayType.TurnCountdown;

                if (decayType != EStatusDecayType.TurnCountdown)
                {
                    continue;
                }

                SetStack(statusType, GetStatusStack(statusType) - 1);
            }
        }
        #endregion // 라운드 감소

        #region 정리
        /// <summary>
        /// 모든 상태 이상을 제거합니다. 활성 중이던 유형은 만료(0) 이벤트를 발화합니다.
        /// </summary>
        public void ResetAll()
        {
            if (activeOrder.Count == 0)
            {
                return;
            }

            tickBuffer.Clear();
            tickBuffer.AddRange(activeOrder);

            foreach (EStatusEffectType statusType in tickBuffer)
            {
                SetStack(statusType, 0);
            }
        }
        #endregion // 정리
    }
}