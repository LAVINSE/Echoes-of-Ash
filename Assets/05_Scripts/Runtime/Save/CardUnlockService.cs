using System.Collections.Generic;
using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using SW.Base;
using SW.Util;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 카드 해금 상태를 조회하고 새로 해금된 카드를 저장합니다.
    /// 보상과 상점에서 어떤 카드를 보여줄지는 각 시스템에서 결정합니다.
    /// 적 처치로 얻는 카드는 도감에 등록된 뒤에도 일반 보상이나 상점에는 나오지 않습니다.
    /// </summary>
    public static class CardUnlockService
    {
        #region 조회
        /// <summary>
        /// 카드가 해금(도감 발견)되어 있는지 확인합니다. 기본 해금 카드는 저장 기록 없이도 해금 상태입니다.
        /// </summary>
        /// <param name="cardData">확인할 카드 데이터입니다.</param>
        /// <returns>해금되어 있으면 true입니다.</returns>
        public static bool IsUnlocked(CardData cardData)
        {
            if (cardData == null)
            {
                return false;
            }

            if (cardData.IsDefaultUnlocked)
            {
                return true;
            }

            return TownSaveService.Current.unlockedCardCodeNames.Contains(cardData.CodeName);
        }

        /// <summary>
        /// 일반 보상과 상점에 나올 수 있는 해금 카드를 결과 목록에 담습니다.
        /// 강화 카드, 저주 카드와 적 처치로만 얻는 카드는 제외합니다.
        /// </summary>
        /// <param name="cardDatabase">전체 카드 데이터베이스입니다.</param>
        /// <param name="resultCards">결과를 저장할 목록입니다. 기존 요소는 제거됩니다.</param>
        public static void CollectUnlockedCards(SWIODatabase cardDatabase, List<CardData> resultCards)
        {
            if (!ValidateCollectArguments(cardDatabase, resultCards, nameof(CollectUnlockedCards)))
            {
                return;
            }

            resultCards.Clear();

            foreach (SWIdentifiedObject data in cardDatabase.Datas)
            {
                CardData cardData = data as CardData;

                if (!IsPoolEligible(cardData))
                {
                    continue;
                }

                if (IsUnlocked(cardData))
                {
                    resultCards.Add(cardData);
                }
            }
        }

        /// <summary>
        /// 아직 발견하지 않은 발견형 카드를 결과 목록에 추가합니다.
        /// 설계도가 필요한 카드와 적 처치로 얻는 카드는 발견 후보에서 제외합니다.
        /// </summary>
        /// <param name="cardDatabase">전체 카드 데이터베이스입니다.</param>
        /// <param name="resultCards">결과를 저장할 목록입니다. 기존 요소는 제거됩니다.</param>
        public static void CollectDiscoveryCandidates(SWIODatabase cardDatabase, List<CardData> resultCards)
        {
            if (!ValidateCollectArguments(cardDatabase, resultCards, nameof(CollectDiscoveryCandidates)))
            {
                return;
            }

            resultCards.Clear();

            foreach (SWIdentifiedObject data in cardDatabase.Datas)
            {
                CardData cardData = data as CardData;

                if (!IsPoolEligible(cardData))
                {
                    continue;
                }

                if (cardData.UnlockType == ECardUnlockType.Discovery && !IsUnlocked(cardData))
                {
                    resultCards.Add(cardData);
                }
            }
        }
        #endregion // 조회

        #region 해금
        /// <summary>
        /// 발견형 카드를 영구 해금하고 즉시 저장합니다.
        /// </summary>
        /// <param name="cardData">해금할 카드 데이터입니다.</param>
        /// <returns>신규 해금에 성공했으면 true입니다. 이미 해금된 카드는 무시하고 false를 반환합니다.</returns>
        public static bool TryUnlockByDiscovery(CardData cardData)
        {
            if (cardData == null)
            {
                SWLog.LogError("[CardUnlockService] TryUnlockByDiscovery 실패: 카드 데이터가 없습니다.");
                return false;
            }

            if (cardData.UnlockType != ECardUnlockType.Discovery)
            {
                SWLog.LogWarning($"[CardUnlockService] TryUnlockByDiscovery 무시: 발견형 카드가 아닙니다 - {cardData.CodeName} (제작형은 설계도, 몬스터 드랍형은 처치 드랍 경로로만 해금됩니다)");
                return false;
            }

            if (IsUnlocked(cardData))
            {
                return false;
            }

            RegisterUnlock(cardData.CodeName);
            TownSaveService.Save();

            SWLog.Log($"[CardUnlockService] 발견형 카드를 해금했습니다: {cardData.CodeName}");
            return true;
        }

        /// <summary>
        /// 설계도 한 개를 사용해 연결된 제작형 카드를 해금하고 즉시 저장합니다.
        /// 이미 해금된 카드의 설계도는 소모하지 않습니다.
        /// </summary>
        /// <param name="blueprintItem">소모할 설계도 아이템입니다.</param>
        /// <returns>해금에 성공했으면 true입니다.</returns>
        public static bool TryUnlockByBlueprint(ItemData blueprintItem)
        {
            if (blueprintItem == null)
            {
                SWLog.LogError("[CardUnlockService] TryUnlockByBlueprint 실패: 설계도 아이템이 없습니다.");
                return false;
            }

            if (blueprintItem.ItemType != EItemType.BluePrint)
            {
                SWLog.LogWarning($"[CardUnlockService] TryUnlockByBlueprint 무시: 설계도 타입이 아닙니다 - {blueprintItem.CodeName}");
                return false;
            }

            CardData unlockCard = blueprintItem.UnlockCard;

            if (unlockCard == null)
            {
                SWLog.LogError($"[CardUnlockService] TryUnlockByBlueprint 실패: 설계도에 해금 카드가 연결되지 않았습니다 - {blueprintItem.CodeName}");
                return false;
            }

            if (unlockCard.UnlockType != ECardUnlockType.Blueprint)
            {
                SWLog.LogError($"[CardUnlockService] TryUnlockByBlueprint 실패: 연결된 카드가 제작형이 아닙니다 - {unlockCard.CodeName} (데이터 오류)");
                return false;
            }

            if (IsUnlocked(unlockCard))
            {
                SWLog.LogWarning($"[CardUnlockService] TryUnlockByBlueprint 무시: 이미 해금된 카드입니다 - {unlockCard.CodeName} (설계도를 소모하지 않습니다)");
                return false;
            }

            List<ItemStackData> costs = new() { new ItemStackData(blueprintItem, 1) };

            if (!TownSaveService.TryConsumeItems(costs))
            {
                SWLog.LogWarning($"[CardUnlockService] TryUnlockByBlueprint 실패: 설계도가 부족합니다 - {blueprintItem.CodeName}");
                return false;
            }

            RegisterUnlock(unlockCard.CodeName);
            TownSaveService.Save();

            SWLog.Log($"[CardUnlockService] 설계도로 카드를 해금했습니다: {blueprintItem.CodeName} -> {unlockCard.CodeName}");
            return true;
        }

        /// <summary>
        /// 적 처치로 얻은 카드를 도감에 등록하고 즉시 저장합니다.
        /// 등록된 뒤에도 일반 보상과 상점에는 나오지 않습니다.
        /// </summary>
        /// <param name="cardData">발견 처리할 카드 데이터입니다.</param>
        /// <returns>신규 발견에 성공했으면 true입니다. 이미 발견된 카드는 무시하고 false를 반환합니다.</returns>
        public static bool TryUnlockByEnemyDrop(CardData cardData)
        {
            if (cardData == null)
            {
                SWLog.LogError("[CardUnlockService] TryUnlockByEnemyDrop 실패: 카드 데이터가 없습니다.");
                return false;
            }

            if (cardData.UnlockType != ECardUnlockType.EnemyDrop)
            {
                SWLog.LogWarning($"[CardUnlockService] TryUnlockByEnemyDrop 무시: 몬스터 드랍형 카드가 아닙니다 - {cardData.CodeName}");
                return false;
            }

            if (IsUnlocked(cardData))
            {
                return false;
            }

            RegisterUnlock(cardData.CodeName);
            TownSaveService.Save();

            SWLog.Log($"[CardUnlockService] 몬스터 드랍형 카드를 도감에 발견 처리했습니다: {cardData.CodeName}");
            return true;
        }
        #endregion // 해금

        #region 내부
        /// <summary>
        /// 해금된 카드의 코드명을 저장 목록에 추가합니다. 파일 저장은 호출하는 쪽에서 수행합니다.
        /// </summary>
        /// <param name="codeName">기록할 카드 코드 이름입니다.</param>
        private static void RegisterUnlock(string codeName)
        {
            List<string> unlockedCodeNames = TownSaveService.Current.unlockedCardCodeNames;

            if (!unlockedCodeNames.Contains(codeName))
            {
                unlockedCodeNames.Add(codeName);
            }
        }

        /// <summary>
        /// 카드가 일반 보상과 상점에 나올 수 있는지 확인합니다.
        /// 강화 카드, 저주 카드와 몬스터 드랍 카드는 일반 보상 및 상점 후보에서 제외합니다.
        /// </summary>
        /// <param name="cardData">확인할 카드 데이터입니다.</param>
        /// <returns>일반 보상과 상점에 나올 수 있으면 true입니다.</returns>
        private static bool IsPoolEligible(CardData cardData)
        {
            if (cardData == null)
            {
                return false;
            }

            return !cardData.IsUpgrade
                && cardData.CardType != ECardType.Curse
                && cardData.UnlockType != ECardUnlockType.EnemyDrop;
        }

        /// <summary>
        /// 수집 함수의 인자를 검증합니다.
        /// </summary>
        /// <param name="cardDatabase">카드 데이터베이스입니다.</param>
        /// <param name="resultCards">결과 목록입니다.</param>
        /// <param name="callerName">호출한 함수 이름입니다.</param>
        /// <returns>인자가 유효하면 true입니다.</returns>
        private static bool ValidateCollectArguments(SWIODatabase cardDatabase, List<CardData> resultCards, string callerName)
        {
            if (cardDatabase == null)
            {
                SWLog.LogError($"[CardUnlockService] {callerName} 실패: 카드 데이터베이스가 없습니다.");
                return false;
            }

            if (resultCards == null)
            {
                SWLog.LogError($"[CardUnlockService] {callerName} 실패: 결과 목록이 없습니다.");
                return false;
            }

            return true;
        }
        #endregion // 내부
    }
}
