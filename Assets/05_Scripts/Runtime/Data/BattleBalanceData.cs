using SW.Attributes;
using SW.Base;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 전투 규칙 밸런스 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "BattleBalance", menuName = "EchoesOfAsh/Data/BattleBalance")]
    public class BattleBalanceData : SWScriptableObject
    {
        #region 필드
        [SWGroup("턴 / 드로우")]
        [SerializeField, Min(1)] private int drawPerTurn = 5;
        [SerializeField, Min(1)] private int maxHandSize = 10;

        [SWGroup("정신력 이벤트")]
        [SerializeField] private SanityEventData sanityEvent;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>턴당 드로우 수</summary>
        public int DrawPerTurn => drawPerTurn;
        /// <summary>최대 손패 수</summary>
        public int MaxHandSize => maxHandSize;

        /// <summary>정신력 이벤트</summary>
        public SanityEventData SanityEvent => sanityEvent;
        #endregion // 프로퍼티
    }
}