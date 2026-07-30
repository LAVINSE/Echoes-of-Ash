using System.Collections.Generic;
using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using SW.Base;
using SW.Util;

namespace EchoesOfAsh.Dungeon
{
    /// <summary>
    /// 상점의 판매 목록과 카드 및 유물 구매를 관리합니다.
    /// 화면 표시와 저장은 DungeonManager가 담당합니다.
    /// 완료하지 않은 상점에 다시 들어오면 판매 목록을 새로 만듭니다.
    /// </summary>
    public class ShopService
    {
        #region 데이터
        /// <summary>
        /// 상점 카드 판매 항목입니다.
        /// </summary>
        public class CardOffer
        {
            /// <summary>판매 카드 데이터입니다.</summary>
            public CardData Card { get; }
            /// <summary>판매 가격입니다.</summary>
            public int Price { get; }
            /// <summary>판매 완료 여부입니다.</summary>
            public bool IsSold { get; private set; }

            /// <summary>
            /// 카드 판매 항목을 생성합니다.
            /// </summary>
            /// <param name="card">판매할 카드 데이터입니다.</param>
            /// <param name="price">판매 가격입니다.</param>
            public CardOffer(CardData card, int price)
            {
                Card = card;
                Price = price;
            }

            /// <summary>
            /// 항목을 판매 완료 상태로 표시합니다.
            /// </summary>
            public void MarkSold()
            {
                IsSold = true;
            }
        }

        /// <summary>
        /// 상점 유물 판매 항목입니다.
        /// </summary>
        public class RelicOffer
        {
            /// <summary>판매 유물 데이터입니다.</summary>
            public RelicData Relic { get; }
            /// <summary>판매 가격입니다.</summary>
            public int Price { get; }
            /// <summary>판매 완료 여부입니다.</summary>
            public bool IsSold { get; private set; }

            /// <summary>
            /// 유물 판매 항목을 생성합니다.
            /// </summary>
            /// <param name="relic">판매할 유물 데이터입니다.</param>
            /// <param name="price">판매 가격입니다.</param>
            public RelicOffer(RelicData relic, int price)
            {
                Relic = relic;
                Price = price;
            }

            /// <summary>
            /// 항목을 판매 완료 상태로 표시합니다.
            /// </summary>
            public void MarkSold()
            {
                IsSold = true;
            }
        }
        #endregion // 데이터

        #region 필드
        private readonly ShopConfigData shopConfig;
        private readonly DungeonState dungeonState;
        private readonly List<CardOffer> cardOffers = new();
        private readonly List<RelicOffer> relicOffers = new();
        private int removeCost;
        private bool isRemoveUsed;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>카드 판매 항목 목록입니다.</summary>
        public IReadOnlyList<CardOffer> CardOffers => cardOffers;
        /// <summary>유물 판매 항목 목록입니다.</summary>
        public IReadOnlyList<RelicOffer> RelicOffers => relicOffers;
        /// <summary>카드 제거 비용입니다.</summary>
        public int RemoveCost => removeCost;
        /// <summary>이번 상점에서 카드 제거를 이미 사용했는지 여부입니다.</summary>
        public bool IsRemoveUsed => isRemoveUsed;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 상점 서비스를 생성합니다.
        /// </summary>
        /// <param name="shopConfig">상점 구성 데이터입니다.</param>
        /// <param name="dungeonState">골드와 덱, 유물을 소유한 던전 상태입니다.</param>
        public ShopService(ShopConfigData shopConfig, DungeonState dungeonState)
        {
            this.shopConfig = shopConfig;
            this.dungeonState = dungeonState;

            if (shopConfig == null)
            {
                SWLog.LogError("[ShopService] 생성 실패: 상점 구성 데이터가 없습니다.");
            }

            if (dungeonState == null)
            {
                SWLog.LogError("[ShopService] 생성 실패: 던전 상태가 없습니다.");
            }
        }
        #endregion // 생성자

        #region 재고
        /// <summary>
        /// 상점 재고를 굴립니다. 카드는 해금 풀에서, 유물은 미보유 유물에서 각각 중복 없이 추첨합니다.
        /// 상점에서는 이미 해금된 카드만 판매합니다.
        /// </summary>
        /// <param name="unlockedCardPool">해금된 카드 풀입니다 (CardUnlockService.CollectUnlockedCards 결과).</param>
        /// <param name="relicDatabase">전체 유물 데이터베이스입니다.</param>
        public void RollStock(IReadOnlyList<CardData> unlockedCardPool, SWIODatabase relicDatabase)
        {
            if (shopConfig == null || dungeonState == null)
            {
                SWLog.LogError("[ShopService] RollStock 실패: 필수 참조가 없습니다.");
                return;
            }

            RollCardStock(unlockedCardPool);
            RollRelicStock(relicDatabase);

            removeCost = shopConfig.RollRemoveCost();
            isRemoveUsed = false;
        }

        /// <summary>
        /// 카드 판매 슬롯을 굴립니다.
        /// </summary>
        /// <param name="unlockedCardPool">해금된 카드 풀입니다.</param>
        private void RollCardStock(IReadOnlyList<CardData> unlockedCardPool)
        {
            cardOffers.Clear();

            if (unlockedCardPool == null || unlockedCardPool.Count == 0)
            {
                SWLog.LogWarning("[ShopService] 카드 재고 굴림 건너뜀: 해금된 카드 풀이 비어 있습니다.");
                return;
            }

            List<CardData> workingPool = new(unlockedCardPool);

            for (int slot = 0; slot < shopConfig.CardSlotCount && workingPool.Count > 0; slot++)
            {
                int pickedIndex = SWRandom.Range(0, workingPool.Count);
                CardData pickedCard = workingPool[pickedIndex];
                workingPool.RemoveAt(pickedIndex);

                cardOffers.Add(new CardOffer(pickedCard, shopConfig.RollCardPrice(pickedCard.RarityType)));
            }
        }

        /// <summary>
        /// 판매할 유물을 선택합니다. 이미 보유한 유물은 제외합니다.
        /// </summary>
        /// <param name="relicDatabase">전체 유물 데이터베이스입니다.</param>
        private void RollRelicStock(SWIODatabase relicDatabase)
        {
            relicOffers.Clear();

            if (relicDatabase == null)
            {
                SWLog.LogWarning("[ShopService] 유물 재고 굴림 건너뜀: 유물 데이터베이스가 없습니다.");
                return;
            }

            List<RelicData> workingPool = new();

            foreach (SWIdentifiedObject data in relicDatabase.Datas)
            {
                RelicData relicData = data as RelicData;

                if (relicData == null || dungeonState.HasRelic(relicData))
                {
                    continue;
                }

                workingPool.Add(relicData);
            }

            for (int slot = 0; slot < shopConfig.RelicSlotCount && workingPool.Count > 0; slot++)
            {
                int pickedIndex = SWRandom.Range(0, workingPool.Count);
                RelicData pickedRelic = workingPool[pickedIndex];
                workingPool.RemoveAt(pickedIndex);

                relicOffers.Add(new RelicOffer(pickedRelic, shopConfig.RollRelicPrice(pickedRelic.RarityType)));
            }
        }
        #endregion // 재고

        #region 거래
        /// <summary>
        /// 카드 판매 항목을 구매합니다. 골드를 차감하고 던전 덱에 새 카드를 추가합니다.
        /// </summary>
        /// <param name="offerIndex">구매할 카드 판매 항목의 순번입니다.</param>
        /// <returns>구매에 성공했으면 true입니다.</returns>
        public bool TryPurchaseCard(int offerIndex)
        {
            if (offerIndex < 0 || offerIndex >= cardOffers.Count)
            {
                SWLog.LogError($"[ShopService] TryPurchaseCard 실패: 잘못된 항목 순번입니다 - {offerIndex}");
                return false;
            }

            CardOffer offer = cardOffers[offerIndex];

            if (offer.IsSold)
            {
                SWLog.LogWarning($"[ShopService] TryPurchaseCard 무시: 이미 판매된 항목입니다 - {offer.Card.DisplayName}");
                return false;
            }

            if (!dungeonState.TrySpendGold(offer.Price))
            {
                SWLog.LogWarning($"[ShopService] TryPurchaseCard 실패: 골드가 부족합니다 - 필요 {offer.Price}, 보유 {dungeonState.Gold}");
                return false;
            }

            dungeonState.AddCard(new CardInstance(offer.Card));
            offer.MarkSold();

            SWLog.Log($"[ShopService] 카드를 구매했습니다: {offer.Card.DisplayName} (-{offer.Price} 골드, 잔여 {dungeonState.Gold})");
            return true;
        }

        /// <summary>
        /// 유물 판매 항목을 구매합니다. 골드를 차감하고 던전 상태에 유물을 획득시킵니다.
        /// </summary>
        /// <param name="offerIndex">구매할 유물 판매 항목의 순번입니다.</param>
        /// <returns>구매에 성공했으면 true입니다.</returns>
        public bool TryPurchaseRelic(int offerIndex)
        {
            if (offerIndex < 0 || offerIndex >= relicOffers.Count)
            {
                SWLog.LogError($"[ShopService] TryPurchaseRelic 실패: 잘못된 항목 순번입니다 - {offerIndex}");
                return false;
            }

            RelicOffer offer = relicOffers[offerIndex];

            if (offer.IsSold)
            {
                SWLog.LogWarning($"[ShopService] TryPurchaseRelic 무시: 이미 판매된 항목입니다 - {offer.Relic.DisplayName}");
                return false;
            }

            if (!dungeonState.TrySpendGold(offer.Price))
            {
                SWLog.LogWarning($"[ShopService] TryPurchaseRelic 실패: 골드가 부족합니다 - 필요 {offer.Price}, 보유 {dungeonState.Gold}");
                return false;
            }

            // 재고 굴림에서 보유 유물을 제외했으므로 실패할 수 없지만, 실패 시 골드를 환불합니다 (방어)
            if (!dungeonState.AddRelic(offer.Relic))
            {
                dungeonState.AddGold(offer.Price);
                SWLog.LogWarning($"[ShopService] TryPurchaseRelic 실패: 유물 획득에 실패해 골드를 환불합니다 - {offer.Relic.DisplayName}");
                return false;
            }

            offer.MarkSold();

            SWLog.Log($"[ShopService] 유물을 구매했습니다: {offer.Relic.DisplayName} (-{offer.Price} 골드, 잔여 {dungeonState.Gold})");
            return true;
        }

        /// <summary>
        /// 골드를 사용해 던전 덱에서 카드 한 장을 제거합니다. 상점마다 한 번만 사용할 수 있습니다.
        /// </summary>
        /// <param name="card">제거할 카드 인스턴스입니다.</param>
        /// <returns>제거에 성공했으면 true입니다.</returns>
        public bool TryRemoveCard(CardInstance card)
        {
            if (card == null)
            {
                SWLog.LogError("[ShopService] TryRemoveCard 실패: 카드가 없습니다.");
                return false;
            }

            if (isRemoveUsed)
            {
                SWLog.LogWarning("[ShopService] TryRemoveCard 무시: 이번 상점에서 카드 제거를 이미 사용했습니다.");
                return false;
            }

            if (!dungeonState.TrySpendGold(removeCost))
            {
                SWLog.LogWarning($"[ShopService] TryRemoveCard 실패: 골드가 부족합니다 - 필요 {removeCost}, 보유 {dungeonState.Gold}");
                return false;
            }

            if (!dungeonState.RemoveCard(card))
            {
                dungeonState.AddGold(removeCost);
                SWLog.LogWarning("[ShopService] TryRemoveCard 실패: 덱에 없는 카드라 골드를 환불합니다.");
                return false;
            }

            isRemoveUsed = true;

            SWLog.Log($"[ShopService] 카드를 제거했습니다: {card.CardData.DisplayName} (-{removeCost} 골드, 잔여 {dungeonState.Gold})");
            return true;
        }
        #endregion // 거래
    }
}
