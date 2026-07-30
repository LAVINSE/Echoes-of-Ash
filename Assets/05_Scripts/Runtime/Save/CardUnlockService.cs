using System.Collections.Generic;
using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using SW.Base;
using SW.Util;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 카드 해금 원장의 접근 창구입니다 (기획서 5-3 - 발견형/제작형 이원화 + 몬스터 드랍형).
    /// 해금 기록은 마을 구획(TownSaveData)에 영구 누적되며, 해금 확정 즉시 파일에 저장합니다 (발견형 즉시 영구 계약 - 기획서 13-1).
    /// 해금 풀 추첨(등장 확률·굴림)은 보상/상점 로직 소관입니다 - 이 클래스는 해금 상태와 후보 조회만 담당합니다.
    /// 몬스터 드랍형은 해금(도감 발견) 후에도 보상/상점 풀에 등장하지 않습니다 - 획득 경로는 해당 적 처치 드랍뿐입니다.
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
        /// 해금된 카드를 결과 목록에 수집합니다. 보상/상점 추첨의 원천 풀입니다.
        /// 잠정 규칙: 강화 카드, 저주 카드, 몬스터 드랍형 카드는 풀에서 제외합니다
        /// (강화 = 휴식/수련 경로, 저주 = 적이 심는 카드, 몬스터 드랍형 = 특정 적 처치 드랍 한정).
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
        /// 미해금 발견형 카드를 결과 목록에 수집합니다. 보상 풀에 낮은 확률로 섞이는 등장 후보입니다 (기획서 5-3).
        /// 제작형 카드는 해금 전까지 어디에도 등장하지 않고, 몬스터 드랍형 카드는 드랍 경로만 가지므로 후보에서 제외됩니다.
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
        /// 발견형 카드를 해금하고 즉시 저장합니다. 미해금 카드가 보상 풀에 등장하는 순간 호출합니다 (기획서 5-3 - 등장 = 즉시 영구 확정).
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
        /// 설계도를 1개 소모해 연결된 제작형 카드를 해금하고 즉시 저장합니다 (봉인된 서고 - 기획서 6-3).
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
        /// 몬스터 드랍형 카드를 도감 발견 처리하고 즉시 저장합니다. 해당 카드가 적 처치 드랍으로 등장하는 순간 호출합니다 (P2-M7 7-4 보강).
        /// 발견 처리 후에도 보상/상점 풀에는 등장하지 않습니다 - 획득 경로는 해당 적 처치 드랍뿐입니다.
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
        /// 해금 코드명을 원장에 기록합니다. 저장은 호출자가 수행합니다.
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
        /// 카드가 해금 풀 대상인지 확인합니다.
        /// 강화 카드, 저주 카드, 몬스터 드랍형 카드는 풀 대상이 아닙니다 (잠정 규칙 - 몬스터 드랍형 = 특정 적 처치 드랍 한정).
        /// </summary>
        /// <param name="cardData">확인할 카드 데이터입니다.</param>
        /// <returns>풀 대상이면 true입니다.</returns>
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