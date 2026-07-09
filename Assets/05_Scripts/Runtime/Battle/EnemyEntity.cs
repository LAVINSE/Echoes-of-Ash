using System;
using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using EchoesOfAsh.Sanity;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 적 Entity
    /// </summary>
    public class EnemyEntity : BattleEntity, ISanityHolder
    {
        #region 필드
        private EnemyData enemyData;

        private SanityHolder sanityHolder;

        private int actionIndex;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>적 데이터</summary>
        public EnemyData EnemyData => enemyData;

        /// <summary>표시 이름</summary>
        public override string DisplayName => enemyData != null ? enemyData.DisplayName : name;

        /// <summary>행동 패턴 순환 인덱스</summary>
        public int ActionIndex
        {
            get => actionIndex;
            set => actionIndex = Mathf.Max(0, value);
        }

        /// <summary>현재 정신력</summary>
        public int CurrentSanity => sanityHolder?.CurrentSanity ?? 0;
        /// <summary>최대 정신력</summary>
        public int MaxSanity => sanityHolder?.MaxSanity ?? 0;
        /// <summary>정신력 전환 임계값</summary>
        public int SanityThreshold => sanityHolder?.SanityThreshold ?? 0;
        /// <summary>현재 정신력 타입</summary>
        public ESanityType CurrentSanityType => sanityHolder?.CurrentSanityType ?? ESanityType.Calm;

        /// <summary>정신력 변경 시 호출</summary>
        public event Action<int, int> OnSanityChanged
        {
            add
            {
                if (sanityHolder != null)
                {
                    sanityHolder.OnSanityChanged += value;
                }
            }
            remove
            {
                if (sanityHolder != null)
                {
                    sanityHolder.OnSanityChanged -= value;
                }
            }
        }
        /// <summary>정신력 타입 변경 시 호출</summary>
        public event Action<ESanityType> OnSanityTypeChanged
        {
            add
            {
                if (sanityHolder != null)
                {
                    sanityHolder.OnSanityTypeChanged += value;
                }
            }
            remove
            {
                if (sanityHolder != null)
                {
                    sanityHolder.OnSanityTypeChanged -= value;
                }
            }
        }
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="data">적 데이터</param>
        public void Init(EnemyData data)
        {
            if (data == null)
            {
                SWLog.LogError($"[EnemyEntity] {name}: EnemyData가 null입니다");
                return;
            }

            enemyData = data;
            SetupHp(data.MaxHpStat);

            sanityHolder = new(data.MaxSanityStat, data.SanityThreshold, data.StartSanity);

            actionIndex = 0;
        }

        public override void ResetEntity()
        {
            sanityHolder?.Dispose();
            sanityHolder = null;
            base.ResetEntity();
        }
        #endregion // 초기화

        #region 정신력
        /// <summary>
        /// 정신력 변화
        /// </summary>
        /// <param name="delta">변화량</param>
        public void ChangeSanity(int delta)
        {
            if (sanityHolder == null)
            {
                SWLog.LogError($"[EnemyEntity] {name}: SanityHolder가 초기화되지 않았습니다");
                return;
            }

            sanityHolder.ChangeSanity(delta);
        }
        #endregion // 정신력
    }
}