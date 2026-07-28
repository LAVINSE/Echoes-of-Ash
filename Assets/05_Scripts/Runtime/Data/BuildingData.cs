using System.Collections.Generic;
using SW.Attributes;
using SW.Base;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 건물 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingData", menuName = "EchoesOfAsh/Data/BuildingData")]
    public class BuildingData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("건물")]
        [SerializeField] private List<BuildingLevelData> levelDatas = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 건물의 최대 레벨입니다.</summary>
        public int MaxLevel => levelDatas.Count;
        #endregion // 프로퍼티

        /// <summary>
        /// 지정한 레벨의 정의를 반환합니다.
        /// </summary>
        /// <param name="level">조회할 레벨입니다 (1부터 시작).</param>
        /// <returns>레벨 정의입니다. 범위 밖이면 null입니다.</returns>
        public BuildingLevelData GetLevelData(int level)
        {
            if (level < 1 || level > levelDatas.Count)
            {
                return null;
            }

            return levelDatas[level - 1];
        }

        /// <summary>
        /// 현재 레벨 기준 다음 레벨 승급 비용을 반환합니다.
        /// </summary>
        /// <param name="currentLevel">현재 레벨입니다 (0 = 미승급).</param>
        /// <returns>다음 레벨 승급 비용입니다. 최대 레벨이면 null입니다.</returns>
        public IReadOnlyList<ItemStackData> GetUpgradeCosts(int currentLevel)
        {
            BuildingLevelData nextLevelData = GetLevelData(currentLevel + 1);
            return nextLevelData?.UpgradeCosts;
        }

        /// <summary>
        /// 지정한 레벨의 효과 설명을 반환합니다.
        /// </summary>
        /// <param name="level">조회할 레벨입니다 (1부터 시작).</param>
        /// <returns>효과 설명입니다. 범위 밖이면 빈 문자열입니다.</returns>
        public string GetLevelDescription(int level)
        {
            BuildingLevelData levelData = GetLevelData(level);
            return levelData != null ? levelData.LevelDescription : string.Empty;
        }
    }
}