using System.Collections.Generic;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    [CreateAssetMenu(fileName = "Drop_", menuName = "EchoesOfAsh/Data/DropTable")]
    public class DropTableData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("드랍 확률")]
        [Tooltip("드랍 뽑기 횟수 최소값입니다")]
        [SerializeField, Min(0)] private int rollCountMin = 1;
        [Tooltip("드랍 뽑기 횟수 최대값입니다")]
        [SerializeField, Min(0)] private int rollCountMax = 1;
        [Tooltip("드랍되지 않을 가중치 - 0이면 반드시 드랍")]
        [SerializeField, Min(0f)] private float noDropWeight = 0f;

        [SWGroup("드랍 정보")]
        [Tooltip("가중치 추첨 후보 목록입니다.)")]
        [SerializeField] private List<DropEntryData> entries = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>드랍 항목 목록입니다.</summary>
        public IReadOnlyList<DropEntryData> Entries => entries;
        #endregion // 프로퍼티

        /// <summary>
        /// 드랍 뽑기를 진행해 목록에 추가한다
        /// </summary>
        /// <param name="results">결과 목록</param>
        public void Roll(List<ItemStackData> results)
        {
            if (results == null)
            {
                SWLog.LogError("[DropTableData] Roll 실패: 결과 목록이 null입니다");
                return;
            }

            float totalWeight = GetTotalWeight();

            if (totalWeight <= 0f)
            {
                return;
            }

            int rollCount = SWRandom.Range(rollCountMin, rollCountMax + 1);

            for (int roll = 0; roll < rollCount; roll++)
            {
                DropEntryData pickedEntry = PickEntryByWeight(totalWeight);

                if (pickedEntry == null)
                {
                    continue;
                }

                int count = SWRandom.Range(pickedEntry.MinCount, pickedEntry.MaxCount + 1);
                results.Add(new ItemStackData(pickedEntry.ItemData, count));
            }
        }

        /// <summary>
        /// 꽝 가중치를 포함한 전체 가중치 합을 반환합니다.
        /// </summary>
        /// <returns>전체 가중치 합입니다.</returns>
        private float GetTotalWeight()
        {
            float totalWeight = noDropWeight;

            foreach (DropEntryData entry in entries)
            {
                if (entry != null && entry.ItemData != null)
                {
                    totalWeight += entry.Weight;
                }
            }

            return totalWeight;
        }

        /// <summary>
        /// 가중치 비례로 항목 하나를 추첨합니다. 순회 순서 = 판정 순서 (결정성).
        /// </summary>
        /// <param name="totalWeight">꽝 포함 전체 가중치 합입니다.</param>
        /// <returns>추첨된 항목입니다. 꽝이면 null입니다.</returns>
        private DropEntryData PickEntryByWeight(float totalWeight)
        {
            float picked = SWRandom.Range(0f, totalWeight);

            if (picked < noDropWeight)
            {
                return null;
            }

            picked -= noDropWeight;

            foreach (DropEntryData entry in entries)
            {
                if (entry == null || entry.ItemData == null)
                {
                    continue;
                }

                if (picked < entry.Weight)
                {
                    return entry;
                }

                picked -= entry.Weight;
            }

            // 부동소수 오차로 경계를 넘긴 경우 - 마지막 유효 항목으로 보정
            for (int index = entries.Count - 1; index >= 0; index--)
            {
                if (entries[index] != null && entries[index].ItemData != null)
                {
                    return entries[index];
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rollCountMax < rollCountMin)
            {
                Debug.LogWarning($"[DropTableData] {name}: 굴림 횟수 최대값이 최소값보다 작습니다", this);
            }

            foreach (DropEntryData entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }

                if (entry.ItemData == null)
                {
                    Debug.LogWarning($"[DropTableData] {name}: 아이템이 비어 있는 드랍 항목이 있습니다", this);
                }

                if (entry.MaxCount < entry.MinCount)
                {
                    Debug.LogWarning($"[DropTableData] {name}: 수량 최대값이 최소값보다 작은 항목이 있습니다", this);
                }
            }
        }
#endif
    }
}