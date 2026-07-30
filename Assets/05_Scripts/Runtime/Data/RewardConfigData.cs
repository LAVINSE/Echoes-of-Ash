using System.Collections.Generic;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 전투 승리 보상 구성 데이터입니다 (기획서 14-6).
    /// 골드 보상 범위와 카드 보상 굴림 규칙(선택지 수·등급 가중치·발견형 등장 확률)을 소유합니다.
    /// 굴림 로직은 SO가 소유하고 (DropTableData 전례), 해금 확정과 풀 수집은 호출자(조립 지점) 소관입니다.
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
        [Tooltip("카드 보상 선택지 수입니다 (기획서 14-6 - 3장 중 1택)")]
        [SerializeField, Min(1)] private int choiceCount = 3;
        [Tooltip("일반 등급 등장 가중치입니다")]
        [SerializeField, Min(0f)] private float commonWeight = 70f;
        [Tooltip("희귀 등급 등장 가중치입니다")]
        [SerializeField, Min(0f)] private float rareWeight = 25f;
        [Tooltip("에픽 등급 등장 가중치입니다")]
        [SerializeField, Min(0f)] private float epicWeight = 5f;
        [Tooltip("선택지 한 칸에 미해금 발견형 카드가 섞여 등장할 확률입니다 (기획서 5-3 - 등장 = 즉시 영구 해금)")]
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
        /// 잠정 규칙: 전설/고유 등급은 보상 풀에서 제외합니다 (확장 예약분 - 기획서 5-1).
        /// 발견형 카드의 해금 확정은 호출자 소관입니다 (원장과 소비처 분리 - CardUnlockService 전례).
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

                // 해금 풀이 고갈되면 발견형 후보로 폴백합니다 (첫 런 풀 공백 완충)
                if (picked == null && workingDiscovery.Count > 0)
                {
                    int fallbackIndex = SWRandom.Range(0, workingDiscovery.Count);
                    picked = workingDiscovery[fallbackIndex];
                    workingDiscovery.RemoveAt(fallbackIndex);
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
        /// 해금 풀을 등급별 작업 목록으로 분류합니다. 전설/고유 등급은 제외합니다 (잠정 규칙).
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
        /// 비어 있지 않은 등급 버킷을 가중치로 추첨한 뒤 버킷 안에서 균등 추첨합니다. 뽑힌 카드는 버킷에서 제거됩니다.
        /// </summary>
        /// <param name="commonBucket">일반 등급 목록입니다.</param>
        /// <param name="rareBucket">희귀 등급 목록입니다.</param>
        /// <param name="epicBucket">에픽 등급 목록입니다.</param>
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

            // 부동소수 오차 보정 - 마지막 유효 버킷 폴백
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