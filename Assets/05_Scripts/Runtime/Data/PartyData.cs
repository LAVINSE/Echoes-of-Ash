using SW.Attributes;
using SW.Base;
using SW.Stat;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 파티 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "Party", menuName = "EchoesOfAsh/Data/Party")]
    public class PartyData : SWScriptableObject
    {
        #region 필드
        [SWGroup("정신력")]
        [SerializeField] private SWStatOverride maxSanityStat;
        [Tooltip("정신력 전환 값")]
        [SerializeField, Min(0)] private int sanityThreshold;
        [Tooltip("전투 시작 시 정신력 값")]
        [SerializeField, Min(0)] private int startSanity;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>파티 최대 정신력 능력치입니다.</summary>
        public SWStatOverride MaxSanityStat => maxSanityStat;
        /// <summary>정신력 전환 값입니다.</summary>
        public int SanityThreshold => sanityThreshold;
        /// <summary>전투 시작 정신력 값입니다.</summary>
        public int StartSanity => startSanity;
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxSanityStat == null || maxSanityStat.Stat == null)
            {
                SWLog.LogWarning($"[PartyData] '{name}': 파티 정신력 스탯 에셋이 비어 있습니다.");
            }
        }
#endif
        #endregion // 에디터
    }
}
