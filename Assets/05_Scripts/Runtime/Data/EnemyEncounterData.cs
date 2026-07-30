using System;
using System.Collections.Generic;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 적 조우 데이터입니다.
    /// 아이템 드랍 테이블과 몬스터 드랍형 카드 드랍을 함께 소유합니다 - 드랍의 소유 단위 = 조우 (P2-M7 7-4).
    /// 카드 드랍은 가중치 추첨입니다 (DropTableData 미러 - 꽝 가중치 + 후보 목록, 조우당 1회 굴림·최대 1장).
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyEncounter_", menuName = "EchoesOfAsh/Data/EnemyEncounter")]
    public class EnemyEncounterData : SWIdentifiedObject
    {
        #region 데이터
        /// <summary>
        /// 조우에 등장하는 적 데이터와 배치 정보를 묶는 항목입니다.
        /// </summary>
        [System.Serializable]
        public class EncounterEntry
        {
            [SerializeField] private EnemyData enemyData;
            [SerializeField] private Vector2 spawnPosition;

            /// <summary>등장할 적 데이터입니다.</summary>
            public EnemyData EnemyData => enemyData;
            /// <summary>배치 위치 (enemyRoot 기준 로컬 좌표)입니다.</summary>
            public Vector2 SpawnPosition => spawnPosition;
        }

        /// <summary>
        /// 몬스터 드랍형 카드의 가중치 추첨 항목입니다 (DropEntryData 대응물 - 카드는 수량이 없어 가중치만 소유).
        /// </summary>
        [System.Serializable]
        public class CardDropEntry
        {
            [Tooltip("드랍될 수 있는 몬스터 드랍형 카드입니다")]
            [SerializeField] private CardData cardData;
            [Tooltip("추첨 가중치")]
            [SerializeField, Min(0f)] private float weight = 1f;

            /// <summary>드랍될 카드 데이터입니다.</summary>
            public CardData CardData => cardData;
            /// <summary>추첨 가중치입니다.</summary>
            public float Weight => weight;
        }
        #endregion // 데이터

        #region 필드
        [SWGroup("구성")]
        [SerializeField] private List<EncounterEntry> entries = new();

        [SWGroup("드랍")]
        [Tooltip("이 조우 승리 시 굴릴 드랍 테이블입니다. 비우면 드랍 없음")]
        [SerializeField] private DropTableData dropTable;

        [SWGroup("카드 드랍")]
        [Tooltip("카드가 드랍되지 않을 가중치 - 0이면 반드시 드랍 (후보가 있을 때)")]
        [SerializeField, Min(0f)] private float noCardDropWeight = 1f;
        [Tooltip("몬스터 드랍형 카드 가중치 추첨 후보 목록입니다 (조우당 1회 굴림, 최대 1장 드랍)")]
        [SerializeField] private List<CardDropEntry> cardDropEntries = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>적 구성 목록입니다.</summary>
        public IReadOnlyList<EncounterEntry> Entries => entries;
        /// <summary>조우 적 수입니다.</summary>
        public int EnemyCount => entries.Count;

        /// <summary>승리 시 굴릴 드랍 테이블입니다. 없으면 null입니다.</summary>
        public DropTableData DropTable => dropTable;
        /// <summary>몬스터 드랍형 카드 추첨 후보 목록입니다.</summary>
        public IReadOnlyList<CardDropEntry> CardDropEntries => cardDropEntries;
        #endregion // 프로퍼티

        #region 조회
        /// <summary>
        /// 이 조우가 지정한 층에 등장할 수 있는지 확인합니다 (SpawnRange 결합 - 구성 적 전원의 등장 구간이 층을 포함해야 합니다).
        /// 잠정 규칙: 등장 구간이 (0, 0)인 적은 미설정 = 무제한으로 취급합니다.
        /// </summary>
        /// <param name="floor">확인할 층입니다 (0 = 입구층).</param>
        /// <returns>등장할 수 있으면 true입니다.</returns>
        public bool IsSpawnableAtFloor(int floor)
        {
            foreach (EncounterEntry entry in entries)
            {
                EnemyData enemyData = entry != null ? entry.EnemyData : null;

                if (enemyData == null)
                {
                    continue;
                }

                Vector2Int spawnRange = enemyData.SpawnRange;

                // (0, 0) = 미설정 - 등장 구간 제한 없음 (잠정 규칙)
                if (spawnRange.x <= 0 && spawnRange.y <= 0)
                {
                    continue;
                }

                if (floor < spawnRange.x || floor > spawnRange.y)
                {
                    return false;
                }
            }

            return true;
        }
        #endregion // 조회

        #region 굴림
        /// <summary>
        /// 몬스터 드랍형 카드를 가중치로 추첨합니다 (조우당 1회 굴림 - DropTableData 전례, 순회 순서 = 판정 순서).
        /// </summary>
        /// <returns>추첨된 카드입니다. 후보가 없거나 꽝이면 null입니다.</returns>
        public CardData RollDropCard()
        {
            float totalWeight = GetCardDropTotalWeight();

            if (totalWeight <= 0f)
            {
                return null;
            }

            float picked = SWRandom.Range(0f, totalWeight);

            if (picked < noCardDropWeight)
            {
                return null;
            }

            picked -= noCardDropWeight;

            foreach (CardDropEntry entry in cardDropEntries)
            {
                if (entry == null || entry.CardData == null)
                {
                    continue;
                }

                if (picked < entry.Weight)
                {
                    return entry.CardData;
                }

                picked -= entry.Weight;
            }

            // 부동소수 오차로 경계를 넘긴 경우 - 마지막 유효 항목으로 보정 (DropTableData 전례)
            for (int index = cardDropEntries.Count - 1; index >= 0; index--)
            {
                if (cardDropEntries[index] != null && cardDropEntries[index].CardData != null)
                {
                    return cardDropEntries[index].CardData;
                }
            }

            return null;
        }

        /// <summary>
        /// 꽝 가중치를 포함한 카드 드랍 전체 가중치 합을 반환합니다. 유효 후보가 없으면 0입니다 (굴림 자체를 생략).
        /// </summary>
        /// <returns>전체 가중치 합입니다.</returns>
        private float GetCardDropTotalWeight()
        {
            float totalWeight = 0f;

            foreach (CardDropEntry entry in cardDropEntries)
            {
                if (entry != null && entry.CardData != null)
                {
                    totalWeight += entry.Weight;
                }
            }

            // 후보가 하나도 없으면 꽝 가중치도 의미가 없습니다 - 카드 드랍 없는 조우
            if (totalWeight <= 0f)
            {
                return 0f;
            }

            return totalWeight + noCardDropWeight;
        }
        #endregion // 굴림

        #region 에디터
#if UNITY_EDITOR
        /// <summary>
        /// 카드 드랍 후보의 해금 방식 정합과 가중치 설정을 검증합니다 (ItemData.unlockCard 검사 전례).
        /// </summary>
        private void OnValidate()
        {
            foreach (CardDropEntry entry in cardDropEntries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.CardData == null)
                {
                    SWLog.LogWarning($"[EnemyEncounterData] '{name}': 카드가 비어 있는 카드 드랍 항목이 있습니다.");
                    continue;
                }

                if (entry.CardData.UnlockType != ECardUnlockType.EnemyDrop)
                {
                    SWLog.LogWarning($"[EnemyEncounterData] '{name}': 드랍 후보가 몬스터 드랍형이 아닙니다 - {entry.CardData.name} (unlockType을 확인하세요)");
                }

                if (entry.Weight <= 0f)
                {
                    SWLog.LogWarning($"[EnemyEncounterData] '{name}': 가중치가 0인 카드 드랍 항목이 있습니다 - {entry.CardData.name} (추첨되지 않습니다)");
                }
            }
        }
#endif // UNITY_EDITOR
        #endregion // 에디터
    }
}