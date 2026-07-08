using System;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Stat;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Sanity
{
    /// <summary>
    /// 정신력 게이지
    /// </summary>
    public class SanityHolder : ISanityHolder, IDisposable
    {
        #region 필드
        private int currentSanity;
        private ESanityType currentSanityType;
        private bool isDisposed;

        private readonly SWStat maxSanityStat;
        private readonly int sanityThreshold;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 정신력</summary>
        public int CurrentSanity => currentSanity;
        /// <summary>현재 정신력 타입</summary>
        public ESanityType CurrentSanityType => currentSanityType;

        /// <summary>최대 정신력 스탯 객체</summary>
        public SWStat MaxSanityStat => maxSanityStat;
        /// <summary>최대 정신력</summary>
        public int MaxSanity => maxSanityStat != null ? Mathf.RoundToInt(maxSanityStat.Value) : 0;
        /// <summary>정신력 전환 임계값</summary>
        public int SanityThreshold => sanityThreshold;

        /// <summary>정신력 변경 시 호출</summary>
        public event Action<int, int> OnSanityChanged;
        /// <summary>정신력 타입 변경 시 호출</summary>
        public event Action<ESanityType> OnSanityTypeChanged;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 정신력 홀더를 생성한다
        /// </summary>
        /// <param name="maxSanityOverride">스탯 재정의 설정</param>
        /// <param name="sanityThreshold">정신력 전환 임계값</param>
        /// <param name="startSanity">시작 정신력 값</param>
        public SanityHolder(SWStatOverride maxSanityOverride, int sanityThreshold, int startSanity)
        {
            maxSanityStat = maxSanityOverride.CreateStat();

            if (maxSanityStat == null)
            {
                SWLog.LogError("[SanityHolder] 생성 실패: MaxSanity 스탯 재정의가 비어있습니다");
                return;
            }

            this.sanityThreshold = Mathf.Max(0, sanityThreshold);
            this.currentSanity = Mathf.Clamp(startSanity, 0, MaxSanity);
            this.currentSanityType = GetSanityType(this.currentSanity);

            maxSanityStat.OnValueChanged += HandleMaxSanityValueChanged;
        }
        #endregion // 생성자

        #region 정신력 값
        /// <summary>
        /// 정신력 변화
        /// </summary>
        /// <param name="delta">변화량</param>
        public void ChangeSanity(int delta)
        {
            if (isDisposed || maxSanityStat == null)
            {
                SWLog.LogError("[SanityHolder] ChangeSanity 실패: 유효하지 않습니다");
                return;
            }

            SetSanity(currentSanity + delta);
        }

        /// <summary>
        /// 정신력을 지정 값으로 설정한다
        /// </summary>
        /// <param name="value">설정할 값</param>
        private void SetSanity(int value)
        {
            int clampedValue = Mathf.Clamp(value, 0, MaxSanity);

            if (clampedValue == currentSanity)
            {
                return;
            }

            currentSanity = clampedValue;
            OnSanityChanged?.Invoke(currentSanity, MaxSanity);
            RefreshSanityType();
        }
        #endregion // 정신력 값

        #region 정신력 구간
        private void RefreshSanityType()
        {
            ESanityType sanityType = GetSanityType(currentSanity);

            if (sanityType == currentSanityType)
            {
                return;
            }

            currentSanityType = sanityType;
            OnSanityTypeChanged?.Invoke(sanityType);
        }

        /// <summary>
        /// 정신력 구간 타입을 반환한다
        /// 임계값 미만 - 광기
        /// 임계값 이상 - 평정
        /// </summary>
        /// <param name="sanity">정신력</param>
        /// <returns>정신력 타입</returns>
        private ESanityType GetSanityType(int sanity)
        {
            return sanity < sanityThreshold ? ESanityType.Madness : ESanityType.Calm;
        }
        #endregion // 정신력 구간

        #region 핸들러
        /// <summary>
        /// 최대 정신력 스탯 값 변경 콜백
        /// </summary>
        /// <param name="stat">스탯</param>
        /// <param name="currentValue">현재 값</param>
        /// <param name="prevValue">이전 값</param>
        private void HandleMaxSanityValueChanged(SWStat stat, float currentValue, float prevValue)
        {
            if (currentSanity > MaxSanity)
            {
                SetSanity(MaxSanity);
            }
            else
            {
                OnSanityChanged?.Invoke(currentSanity, MaxSanity);
            }
        }
        #endregion // 핸들러
        
        #region 해제
        /// <summary>
        /// 스탯 객체 및 구독 해제
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            if(maxSanityStat != null)
            {
                maxSanityStat.OnValueChanged -= HandleMaxSanityValueChanged;
                UnityEngine.Object.Destroy(maxSanityStat);
            }
        }
        #endregion // 해제
    }
}