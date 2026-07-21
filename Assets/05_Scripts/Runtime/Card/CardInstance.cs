using System.Collections.Generic;
using EchoesOfAsh.Data;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Card
{
    /// <summary>
    /// 카드입니다.
    /// </summary>
    public class CardInstance
    {
        #region 필드
        private int battleApCostDelta;
        private bool isUpgrade;

        private readonly CardData cardData;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>카드 데이터입니다.</summary>
        public CardData CardData => cardData;
        /// <summary>현재 카드 데이터 (강화 버전 또는 원본)입니다.</summary>
        public CardData CurrentCardData => isUpgrade && cardData.UpgradeCard != null ? cardData.UpgradeCard : cardData;

        /// <summary>행동력 비용 보정치입니다.</summary>
        public int BattleApCostDelta => battleApCostDelta;
        /// <summary>보정 적용 후 실제 행동력 비용입니다.</summary>
        public int ApCost => Mathf.Max(0, CurrentCardData.ApCost + battleApCostDelta);
        /// <summary>강화 여부입니다.</summary>
        public bool IsUpgrade => isUpgrade;
        /// <summary>정신력 반응 카드 여부입니다.</summary>
        public bool IsSanityEffect => CurrentCardData.IsSanityEffect;

        /// <summary>대상 지정 유형입니다.</summary>
        public ETargetingType TargetingType => CurrentCardData.TargetingType;

        /// <summary>현재 카드의 표시 이름입니다.</summary>
        public string DisplayName => CurrentCardData.DisplayName;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 카드 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="cardData">카드 데이터입니다.</param>
        /// <param name="isUpgrade">강화 상태인지 여부입니다.</param>
        public CardInstance(CardData cardData, bool isUpgrade = false)
        {
            if (cardData == null)
            {
                SWLog.LogError("[CardInstance] 생성 실패: CardData가 null입니다");
            }

            this.cardData = cardData;
            this.isUpgrade = isUpgrade;
        }
        #endregion // 생성자

        #region 강화
        /// <summary>
        /// 카드를 강화합니다.
        /// </summary>
        /// <returns>강화 성공 여부입니다.</returns>
        public bool TryUpgrade()
        {
            if (isUpgrade)
            {
                return false;
            }

            if (cardData == null || cardData.UpgradeCard == null)
            {
                SWLog.LogWarning($"[CardInstance] '{DisplayName}': 강화 버전 데이터가 없습니다");
                return false;
            }

            isUpgrade = true;
            return true;
        }
        #endregion // 강화

        #region 전투 상태
        /// <summary>
        /// 전투 한정 행동력 비용 보정치를 누적합니다.
        /// 음수 = 비용 감소입니다.
        /// 유물, 이벤트 효과 등에서 사용합니다.
        /// </summary>
        public void AddBattleApCostDelta(int delta)
        {
            battleApCostDelta += delta;
        }

        /// <summary>
        /// 전투 한정 행동력 비용 보정치를 초기화합니다.
        /// </summary>
        public void ResetBattleApCost()
        {
            battleApCostDelta = 0;
        }
        #endregion // 전투 상태

        #region 효과
        /// <summary>
        /// 현재 정신력 구간에 해당하는 효과 목록을 반환합니다.
        /// </summary>
        /// <param name="sanityType">현재 파티 정신력 유형입니다.</param>
        /// <returns>적용할 효과 목록입니다.</returns>
        public IReadOnlyList<EffectBlock> GetEffectBlocks(ESanityType sanityType)
            => CurrentCardData.GetEffectBlocks(sanityType);
        #endregion // 효과
    }
}
