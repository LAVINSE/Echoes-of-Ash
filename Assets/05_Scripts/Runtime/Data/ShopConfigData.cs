using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 상점의 판매 수량, 카드 등급별 가격과 카드 제거 비용을 설정합니다.
    /// 소모품 판매 기능은 아직 포함하지 않습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopConfig_", menuName = "EchoesOfAsh/Data/ShopConfig")]
    public class ShopConfigData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("슬롯")]
        [Tooltip("한 번에 판매할 카드 수입니다.")]
        [SerializeField, Min(0)] private int cardSlotCount = 4;
        [Tooltip("한 번에 판매할 유물 수입니다.")]
        [SerializeField, Min(0)] private int relicSlotCount = 2;

        [SWGroup("카드 가격")]
        [Tooltip("일반 카드 가격 최소값입니다")]
        [SerializeField, Min(0)] private int cardCommonPriceMin = 40;
        [Tooltip("일반 카드 가격 최대값입니다")]
        [SerializeField, Min(0)] private int cardCommonPriceMax = 60;
        [Tooltip("희귀 카드 가격 최소값입니다")]
        [SerializeField, Min(0)] private int cardRarePriceMin = 80;
        [Tooltip("희귀 카드 가격 최대값입니다")]
        [SerializeField, Min(0)] private int cardRarePriceMax = 120;
        [Tooltip("에픽 카드 가격 최소값입니다.")]
        [SerializeField, Min(0)] private int cardEpicPriceMin = 150;
        [Tooltip("에픽 카드 가격 최대값입니다.")]
        [SerializeField, Min(0)] private int cardEpicPriceMax = 200;

        [SWGroup("유물 가격")]
        [Tooltip("일반 유물 가격 최소값입니다")]
        [SerializeField, Min(0)] private int relicCommonPriceMin = 100;
        [Tooltip("일반 유물 가격 최대값입니다")]
        [SerializeField, Min(0)] private int relicCommonPriceMax = 150;
        [Tooltip("희귀 유물 가격 최소값입니다")]
        [SerializeField, Min(0)] private int relicRarePriceMin = 200;
        [Tooltip("희귀 유물 가격 최대값입니다")]
        [SerializeField, Min(0)] private int relicRarePriceMax = 250;
        [Tooltip("에픽 유물 가격 최소값입니다.")]
        [SerializeField, Min(0)] private int relicEpicPriceMin = 300;
        [Tooltip("에픽 유물 가격 최대값입니다.")]
        [SerializeField, Min(0)] private int relicEpicPriceMax = 350;

        [SWGroup("카드 제거")]
        [Tooltip("카드 제거 비용 최소값입니다.")]
        [SerializeField, Min(0)] private int removeCostMin = 75;
        [Tooltip("카드 제거 비용 최대값입니다.")]
        [SerializeField, Min(0)] private int removeCostMax = 100;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>카드 판매 슬롯 수입니다.</summary>
        public int CardSlotCount => cardSlotCount;
        /// <summary>유물 판매 슬롯 수입니다.</summary>
        public int RelicSlotCount => relicSlotCount;
        #endregion // 프로퍼티

        #region 굴림
        /// <summary>
        /// 카드 등급에 맞는 범위에서 판매 가격을 굴립니다.
        /// 전설과 고유 등급 카드는 에픽 등급의 가격 범위를 사용합니다.
        /// </summary>
        /// <param name="rarityType">카드 등급입니다.</param>
        /// <returns>굴린 가격입니다.</returns>
        public int RollCardPrice(ERarityType rarityType)
        {
            switch (rarityType)
            {
                case ERarityType.Common:
                    return SWRandom.Range(cardCommonPriceMin, cardCommonPriceMax + 1);

                case ERarityType.Rare:
                    return SWRandom.Range(cardRarePriceMin, cardRarePriceMax + 1);

                default:
                    return SWRandom.Range(cardEpicPriceMin, cardEpicPriceMax + 1);
            }
        }

        /// <summary>
        /// 유물 등급에 맞는 범위에서 판매 가격을 굴립니다.
        /// 전설과 고유 등급 유물은 에픽 등급의 가격 범위를 사용합니다.
        /// </summary>
        /// <param name="rarityType">유물 등급입니다.</param>
        /// <returns>굴린 가격입니다.</returns>
        public int RollRelicPrice(ERarityType rarityType)
        {
            switch (rarityType)
            {
                case ERarityType.Common:
                    return SWRandom.Range(relicCommonPriceMin, relicCommonPriceMax + 1);

                case ERarityType.Rare:
                    return SWRandom.Range(relicRarePriceMin, relicRarePriceMax + 1);

                default:
                    return SWRandom.Range(relicEpicPriceMin, relicEpicPriceMax + 1);
            }
        }

        /// <summary>
        /// 카드 제거 비용을 굴립니다.
        /// </summary>
        /// <returns>굴린 제거 비용입니다.</returns>
        public int RollRemoveCost()
        {
            return SWRandom.Range(removeCostMin, removeCostMax + 1);
        }
        #endregion // 굴림

        #region 에디터
#if UNITY_EDITOR
        /// <summary>
        /// 가격 범위 설정을 검증합니다.
        /// </summary>
        private void OnValidate()
        {
            bool hasInvalidCardRange = cardCommonPriceMax < cardCommonPriceMin
                || cardRarePriceMax < cardRarePriceMin
                || cardEpicPriceMax < cardEpicPriceMin;
            bool hasInvalidRelicRange = relicCommonPriceMax < relicCommonPriceMin
                || relicRarePriceMax < relicRarePriceMin
                || relicEpicPriceMax < relicEpicPriceMin;

            if (hasInvalidCardRange || hasInvalidRelicRange || removeCostMax < removeCostMin)
            {
                SWLog.LogWarning($"[ShopConfigData] {name}: 가격 최대값이 최소값보다 작은 범위가 있습니다.");
            }
        }
#endif // UNITY_EDITOR
        #endregion // 에디터
    }
}
