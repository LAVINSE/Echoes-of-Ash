using System.Collections.Generic;
using EchoesOfAsh.Data;
using SW.Util;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 마을에서 유지할 아이템, 건물과 캐릭터 정보를 변경합니다.
    /// 실제 파일 저장은 GameSaveService에 맡기며, 변경 후 저장 시점은 호출하는 쪽에서 결정합니다.
    /// </summary>
    public static class TownSaveService
    {
        #region 프로퍼티
        /// <summary>현재 프로필의 마을 데이터입니다.</summary>
        public static TownSaveData Current => GameSaveService.Current.Town;
        #endregion // 프로퍼티

        #region 저장
        /// <summary>
        /// 마을 데이터를 포함한 프로필 저장을 수행합니다.
        /// </summary>
        /// <returns>저장 성공 여부입니다.</returns>
        public static bool Save()
        {
            return GameSaveService.Save();
        }
        #endregion // 저장

        #region 자원
        /// <summary>
        /// 아이템을 보유량에 더합니다. 저장은 호출자가 일괄 수행합니다.
        /// </summary>
        /// <param name="codeName">아이템 코드 이름입니다.</param>
        /// <param name="count">더할 수량입니다.</param>
        public static void AddItem(string codeName, int count)
        {
            if (string.IsNullOrEmpty(codeName) || count <= 0)
            {
                return;
            }

            TownSaveData townData = Current;

            foreach (ItemCountSaveData item in townData.items)
            {
                if (item.codeName == codeName)
                {
                    item.count += count;
                    return;
                }
            }

            townData.items.Add(new ItemCountSaveData { codeName = codeName, count = count });
        }

        /// <summary>
        /// 아이템 보유량을 반환합니다.
        /// </summary>
        /// <param name="codeName">아이템 코드 이름입니다.</param>
        /// <returns>보유 수량입니다. 없으면 0입니다.</returns>
        public static int GetItemCount(string codeName)
        {
            foreach (ItemCountSaveData item in Current.items)
            {
                if (item.codeName == codeName)
                {
                    return item.count;
                }
            }

            return 0;
        }

        /// <summary>
        /// 비용 목록을 전부 지불할 수 있는지 확인합니다. 같은 아이템의 중복 항목은 합산해 판정합니다.
        /// </summary>
        /// <param name="costs">확인할 비용 목록입니다.</param>
        /// <returns>전부 지불할 수 있으면 true입니다. 비어 있으면 true입니다.</returns>
        public static bool HasItems(IReadOnlyList<ItemStackData> costs)
        {
            if (costs == null || costs.Count == 0)
            {
                return true;
            }

            Dictionary<string, int> requiredCounts = new();

            foreach (ItemStackData cost in costs)
            {
                if (cost?.ItemData == null)
                {
                    continue;
                }

                string codeName = cost.ItemData.CodeName;
                requiredCounts.TryGetValue(codeName, out int requiredCount);
                requiredCounts[codeName] = requiredCount + cost.Count;
            }

            foreach (KeyValuePair<string, int> required in requiredCounts)
            {
                if (GetItemCount(required.Key) < required.Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 비용 목록을 검사한 뒤 일괄 차감합니다. 하나라도 부족하면 아무것도 차감하지 않습니다 (부분 차감 방지).
        /// 저장은 호출자가 일괄 수행합니다.
        /// </summary>
        /// <param name="costs">차감할 비용 목록입니다.</param>
        /// <returns>차감에 성공했으면 true입니다.</returns>
        public static bool TryConsumeItems(IReadOnlyList<ItemStackData> costs)
        {
            if (!HasItems(costs))
            {
                return false;
            }

            if (costs == null)
            {
                return true;
            }

            foreach (ItemStackData cost in costs)
            {
                if (cost?.ItemData == null)
                {
                    continue;
                }

                RemoveItem(cost.ItemData.CodeName, cost.Count);
            }

            return true;
        }

        /// <summary>
        /// 아이템 보유량을 차감합니다. 0 이하가 되면 항목을 제거합니다.
        /// </summary>
        /// <param name="codeName">아이템 코드 이름입니다.</param>
        /// <param name="count">차감할 수량입니다.</param>
        private static void RemoveItem(string codeName, int count)
        {
            List<ItemCountSaveData> items = Current.items;

            for (int index = 0; index < items.Count; index++)
            {
                if (items[index].codeName != codeName)
                {
                    continue;
                }

                items[index].count -= count;

                if (items[index].count <= 0)
                {
                    items.RemoveAt(index);
                }

                return;
            }

            SWLog.LogWarning($"[TownSaveService] RemoveItem: 보유하지 않은 아이템입니다 - {codeName}");
        }
        #endregion // 자원

        #region 건물
        /// <summary>
        /// 건물의 현재 레벨을 반환합니다.
        /// </summary>
        /// <param name="codeName">건물 코드 이름입니다.</param>
        /// <returns>현재 레벨입니다. 기록이 없으면 0입니다 (미승급).</returns>
        public static int GetBuildingLevel(string codeName)
        {
            foreach (BuildingLevelSaveData building in Current.buildingLevels)
            {
                if (building.codeName == codeName)
                {
                    return building.level;
                }
            }

            return 0;
        }

        /// <summary>
        /// 건물의 현재 레벨을 설정합니다. 저장은 호출자가 일괄 수행합니다.
        /// </summary>
        /// <param name="codeName">건물 코드 이름입니다.</param>
        /// <param name="level">설정할 레벨입니다.</param>
        public static void SetBuildingLevel(string codeName, int level)
        {
            if (string.IsNullOrEmpty(codeName) || level < 0)
            {
                return;
            }

            TownSaveData townData = Current;

            foreach (BuildingLevelSaveData building in townData.buildingLevels)
            {
                if (building.codeName == codeName)
                {
                    building.level = level;
                    return;
                }
            }

            townData.buildingLevels.Add(new BuildingLevelSaveData { codeName = codeName, level = level });
        }
        #endregion // 건물

        #region 캐릭터
        /// <summary>
        /// 캐릭터를 보유하고 있는지 확인합니다.
        /// </summary>
        /// <param name="codeName">캐릭터 코드 이름입니다.</param>
        /// <returns>보유하고 있으면 true입니다.</returns>
        public static bool HasCharacter(string codeName)
        {
            return Current.ownedCharacterCodeNames.Contains(codeName);
        }

        /// <summary>
        /// 캐릭터를 보유 목록에 영입한 순서대로 추가하며, 이미 있는 캐릭터는 무시합니다.
        /// 저장은 호출자가 일괄 수행합니다.
        /// </summary>
        /// <param name="codeName">캐릭터 코드 이름입니다.</param>
        public static void AddCharacter(string codeName)
        {
            if (string.IsNullOrEmpty(codeName) || HasCharacter(codeName))
            {
                return;
            }

            Current.ownedCharacterCodeNames.Add(codeName);
        }
        #endregion // 캐릭터
    }
}
