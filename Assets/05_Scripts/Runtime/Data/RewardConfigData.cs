using System.Collections.Generic;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 전투 승리 후 지급할 골드와 카드 보상을 설정합니다.
    /// 골드 보상 범위와 카드 보상 굴림 규칙(선택지 수·등급 가중치·발견형 등장 확률)을 소유합니다.
    /// 보상 수치와 카드 등급을 선택하며, 카드 해금 처리는 호출하는 쪽에서 담당합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "RewardConfig_", menuName = "EchoesOfAsh/Data/RewardConfig")]
    public class RewardConfigData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("골드 보상")]
        [Tooltip("일반 전투 골드 보상 최소값입니다")]
        [SerializeField, Min(0)] private int battleGoldMin = 10;
        [Tooltip("일반 전투 골드 보상 최대값입니다")]
        [SerializeField, Min(0)] private int battleGoldMax = 20;
        [Tooltip("엘리트 전투 골드 보상 최소값입니다")]
        [SerializeField, Min(0)] private int eliteGoldMin = 25;
        [Tooltip("엘리트 전투 골드 보상 최대값입니다")]
        [SerializeField, Min(0)] private int eliteGoldMax = 40;
        [Tooltip("보스 전투 골드 보상 최소값입니다")]
        [SerializeField, Min(0)] private int bossGoldMin = 50;
        [Tooltip("보스 전투 골드 보상 최대값입니다")]
        [SerializeField, Min(0)] private int bossGoldMax = 80;

        [SWGroup("카드 보상")]
        [Tooltip("한 번에 보여줄 카드 보상 수입니다.")]
        [SerializeField, Min(1)] private int choiceCount = 3;
        [Tooltip("일반 등급 등장 가중치입니다")]
        [SerializeField, Min(0f)] private float commonWeight = 70f;
        [Tooltip("희귀 등급 등장 가중치입니다")]
        [SerializeField, Min(0f)] private float rareWeight = 25f;
        [Tooltip("에픽 등급 등장 가중치입니다")]
        [SerializeField, Min(0f)] private float epicWeight = 5f;
        [Tooltip("아직 발견하지 않은 카드가 보상에 나올 확률입니다. 등장한 카드는 즉시 영구 해금됩니다.")]
        [SerializeField, Range(0f, 1f)] private float discoveryChance = 0.1f;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>카드 보상 선택지 수입니다.</summary>
        public int ChoiceCount => choiceCount;
        /// <summary>발견형 카드 등장 확률입니다.</summary>
        public float DiscoveryChance => discoveryChance;
        #endregion // 프로퍼티

        #region 굴림
        /// <summary>
        /// 노드 타입에 맞는 범위에서 골드 보상을 굴립니다.
        /// </summary>
        /// <param name="nodeType">승리한 전투 노드의 타입입니다.</param>
        /// <returns>굴린 골드량입니다.</returns>
        public int RollGold(EMapNodeType nodeType)
        {
            switch (nodeType)
            {
                case EMapNodeType.Elite:
                    return SWRandom.Range(eliteGoldMin, eliteGoldMax + 1);

                case EMapNodeType.Boss:
                    return SWRandom.Range(bossGoldMin, bossGoldMax + 1);

                default:
                    return SWRandom.Range(battleGoldMin, battleGoldMax + 1);
            }
        }

        /// <summary>
        /// 카드 보상 선택지를 굴려 결과 목록에 추가합니다. 선택지는 서로 중복되지 않습니다.
        /// 칸마다 발견형 등장 판정을 먼저 하고, 아니면 등급 가중치로 해금 풀에서 추첨합니다.
        /// 전설과 고유 등급 카드는 일반 보상에서 제외합니다.
        /// 발견형 카드는 보상에 등장했을 때 호출하는 쪽에서 해금합니다.
        /// </summary>
        /// <param name="unlockedPool">해금된 카드 풀입니다 (CardUnlockService.CollectUnlockedCards 결과).</param>
        /// <param name="discoveryCandidates">미해금 발견형 후보입니다 (CollectDiscoveryCandidates 결과).</param>
        /// <param name="resultCards">결과를 저장할 목록입니다. 기존 요소는 제거됩니다.</param>
        public void RollCardChoices(
            IReadOnlyList<CardData> unlockedPool,
            IReadOnlyList<CardData> discoveryCandidates,
            List<CardData> resultCards)
        {
            if (resultCards == null)
            {
                SWLog.LogError("[RewardConfigData] RollCardChoices 실패: 결과 목록이 없습니다.");
                return;
            }

            resultCards.Clear();

            List<CardData> commonBucket = new();
            List<CardData> rareBucket = new();
            List<CardData> epicBucket = new();
            FillRarityBuckets(unlockedPool, commonBucket, rareBucket, epicBucket);

            List<CardData> workingDiscovery = new();

            if (discoveryCandidates != null)
            {
                workingDiscovery.AddRange(discoveryCandidates);
            }

            for (int slot = 0; slot < choiceCount; slot++)
            {
                CardData picked = null;

                // 발견형 등장 판정 - 저확률로 미해금 카드가 선택지에 섞입니다
                if (workingDiscovery.Count > 0 && SWRandom.Chance(discoveryChance))
                {
                    int pickedIndex = SWRandom.Range(0, workingDiscovery.Count);
                    picked = workingDiscovery[pickedIndex];
                    workingDiscovery.RemoveAt(pickedIndex);
                }
                else
                {
                    picked = PickByRarity(commonBucket, rareBucket, epicBucket);
                }

                // 해금된 카드가 부족하면 아직 발견하지 않은 카드도 후보에 추가합니다.
                if (picked == null && workingDiscovery.Count > 0)
                {
                    int discoveryIndex = SWRandom.Range(0, workingDiscovery.Count);
                    picked = workingDiscovery[discoveryIndex];
                    workingDiscovery.RemoveAt(discoveryIndex);
                }

                if (picked == null)
                {
                    SWLog.LogWarning($"[RewardConfigData] '{name}': 카드 보상 후보가 고갈되어 {resultCards.Count}장으로 조기 종료합니다.");
                    break;
                }

                resultCards.Add(picked);
            }
        }
        #endregion // 굴림

        #region 내부
        /// <summary>
        /// 해금된 카드를 등급별 목록으로 나눕니다. 전설과 고유 등급은 제외합니다.
        /// </summary>
        /// <param name="unlockedPool">해금된 카드 풀입니다.</param>
        /// <param name="commonBucket">일반 등급 목록입니다.</param>
        /// <param name="rareBucket">희귀 등급 목록입니다.</param>
        /// <param name="epicBucket">에픽 등급 목록입니다.</param>
        private static void FillRarityBuckets(
            IReadOnlyList<CardData> unlockedPool,
            List<CardData> commonBucket,
            List<CardData> rareBucket,
            List<CardData> epicBucket)
        {
            if (unlockedPool == null)
            {
                return;
            }

            foreach (CardData cardData in unlockedPool)
            {
                if (cardData == null)
                {
                    continue;
                }

                switch (cardData.RarityType)
                {
                    case ERarityType.Common:
                        commonBucket.Add(cardData);
                        break;

                    case ERarityType.Rare:
                        rareBucket.Add(cardData);
                        break;

                    case ERarityType.Epic:
                        epicBucket.Add(cardData);
                        break;
                }
            }
        }

        /// <summary>
        /// 카드 등급을 확률에 따라 정한 뒤 해당 등급에서 카드 한 장을 뽑습니다. 같은 카드가 다시 뽑히지 않도록 후보에서 제거합니다.
        /// </summary>
        /// <param name="commonBucket">일반 등급 카드 목록입니다.</param>
        /// <param name="rareBucket">희귀 등급 카드 목록입니다.</param>
        /// <param name="epicBucket">에픽 등급 카드 목록입니다.</param>
        /// <returns>추첨된 카드입니다. 후보가 없으면 null입니다.</returns>
        private CardData PickByRarity(List<CardData> commonBucket, List<CardData> rareBucket, List<CardData> epicBucket)
        {
            float totalWeight = 0f;

            if (commonBucket.Count > 0) totalWeight += commonWeight;
            if (rareBucket.Count > 0) totalWeight += rareWeight;
            if (epicBucket.Count > 0) totalWeight += epicWeight;

            if (totalWeight <= 0f)
            {
                return null;
            }

            float picked = SWRandom.Range(0f, totalWeight);
            List<CardData> selectedBucket = null;

            if (commonBucket.Count > 0)
            {
                if (picked < commonWeight)
                {
                    selectedBucket = commonBucket;
                }

                picked -= commonWeight;
            }

            if (selectedBucket == null && rareBucket.Count > 0)
            {
                if (picked < rareWeight)
                {
                    selectedBucket = rareBucket;
                }

                picked -= rareWeight;
            }

            if (selectedBucket == null && epicBucket.Count > 0)
            {
                selectedBucket = epicBucket;
            }

            // 소수점 계산 오차가 있으면 마지막 유효 등급을 반환합니다.
            if (selectedBucket == null)
            {
                if (epicBucket.Count > 0) selectedBucket = epicBucket;
                else if (rareBucket.Count > 0) selectedBucket = rareBucket;
                else selectedBucket = commonBucket;
            }

            int pickedIndex = SWRandom.Range(0, selectedBucket.Count);
            CardData pickedCard = selectedBucket[pickedIndex];
            selectedBucket.RemoveAt(pickedIndex);
            return pickedCard;
        }
        #endregion // 내부

        #region 에디터
#if UNITY_EDITOR
        /// <summary>
        /// 골드 범위와 등급 가중치 설정을 검증합니다.
        /// </summary>
        private void OnValidate()
        {
            if (battleGoldMax < battleGoldMin || eliteGoldMax < eliteGoldMin || bossGoldMax < bossGoldMin)
            {
                SWLog.LogWarning($"[RewardConfigData] {name}: 골드 최대값이 최소값보다 작은 범위가 있습니다.");
            }

            if (commonWeight + rareWeight + epicWeight <= 0f)
            {
                SWLog.LogWarning($"[RewardConfigData] {name}: 등급 가중치 합이 0입니다 - 카드 보상이 굴려지지 않습니다.");
            }
        }
#endif // UNITY_EDITOR
        #endregion // 에디터
    }
}
