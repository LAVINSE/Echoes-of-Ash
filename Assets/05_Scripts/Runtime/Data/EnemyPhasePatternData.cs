using System.Collections.Generic;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 페이즈별 행동 패턴 데이터
    /// </summary>
    [System.Serializable]
    public class EnemyPhasePatternData
    {
        #region 필드
        [SerializeField] private string phaseName;
        [Tooltip("현재 HP 비율이 이 값 이하가 되면 이 패턴으로 전환")]
        [SerializeField, Range(0f, 1f)] private float hpThresholdRatio = 0.5f;
        [SerializeField] private List<EnemyActionData> actionPatterns = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>페이즈 이름</summary>
        public string PhaseName => phaseName;

        /// <summary>HP 전환 임계 비율</summary>
        public float HpThresholdRatio => hpThresholdRatio;

        /// <summary>이 페이지의 행동 순환 패턴</summary>
        public IReadOnlyList<EnemyActionData> ActionPatterns => actionPatterns;
        #endregion // 프로퍼티
    }
}

