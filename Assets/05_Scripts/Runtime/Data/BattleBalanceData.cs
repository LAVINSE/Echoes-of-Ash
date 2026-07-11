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

        [SWGroup("AP")]
        [Tooltip("턴 시작 시 지급하는 AP")]
        [SerializeField, Min(0)] private int apPerTurn = 3;
        [Tooltip("턴 종료 시 다음 턴으로 이월할 수 있는 행동력의 최댓값")]
        [SerializeField, Min(0)] private int apCarryOverMax = 2; 

        [SWGroup("정신력 이벤트")]
        [SerializeField] private SanityEventData sanityEvent;
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
    }
}
