using SW.Attributes;
using SW.Base;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 전투 규칙 밸런스 데이터입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "BattleBalance", menuName = "EchoesOfAsh/Data/BattleBalance")]
    public class BattleBalanceData : SWScriptableObject
    {
        #region 필드
        [SWGroup("턴 / 드로우")]
        [SerializeField, Min(1)] private int drawPerTurn = 5;
        [SerializeField, Min(1)] private int maxHandSize = 10;

        [SWGroup("AP")]
        [Tooltip("턴 시작 시 지급하는 AP")]
        [SerializeField, Min(0)] private int apPerTurn = 3;
        [Tooltip("턴 종료 시 다음 턴으로 이월할 수 있는 행동력의 최댓값")]
        [SerializeField, Min(0)] private int apCarryOverMax = 2;

        [SWGroup("정신력 이벤트")]
        [SerializeField] private SanityEventData sanityEvent;

        [SWGroup("광기 이벤트")]
        [Tooltip("광기 진입 직후(임계값 부근) 발생 확률 0~1")]
        [SerializeField, Range(0f, 1f)] private float madnessEventBaseChance = 0.3f;
        [Tooltip("SAN 0에서의 최대 발생 확률 0~1")]
        [SerializeField, Range(0f, 1f)] private float madnessEventMaxChance = 0.6f;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>턴마다 뽑는 카드 수입니다.</summary>
        public int DrawPerTurn => drawPerTurn;
        /// <summary>최대 손패 수입니다.</summary>
        public int MaxHandSize => maxHandSize;

        /// <summary>턴마다 지급하는 행동력입니다.</summary>
        public int ApPerTurn => apPerTurn;
        /// <summary>다음 턴으로 이월할 수 있는 행동력의 최댓값입니다.</summary>
        public int ApCarryOverMax => apCarryOverMax;

        /// <summary>정신력 이벤트입니다.</summary>
        public SanityEventData SanityEvent => sanityEvent;
        #endregion // 프로퍼티

        /// <summary>
        /// 현재 정신력 기준 광기 이벤트 발생 확률을 반환합니다.
        /// 임계값 부근 = 기본 확률, 정신력 0 = 최대 확률로 선형 증가
        /// </summary>
        /// <param name="currentSanity">현재 정신력</param>
        /// <param name="sanityThreshold">광기 전환 임계값</param>
        /// <returns>발생 확률 0~1</returns>
        public float GetMadnessEventChance(int currentSanity, int sanityThreshold)
        {
            if (sanityThreshold <= 0)
            {
                return madnessEventBaseChance;
            }

            float depth = 1f - Mathf.Clamp01((float)currentSanity / sanityThreshold);
            return Mathf.Lerp(madnessEventBaseChance, madnessEventMaxChance, depth);
        }
    }
}
