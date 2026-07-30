using System.Collections.Generic;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 건물의 레벨별 비용과 효과 설명을 보관합니다.
    /// </summary>
    [System.Serializable]
    public class BuildingLevelData
    {
        #region 필드
        [Tooltip("레벨로 승급할 때 소모되는 비용")]
        [SerializeField] private List<ItemStackData> upgradeCosts = new();
        [Tooltip("레벨의 효과 설명")]
        [SerializeField, TextArea] private string levelDescription;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 레벨로 승급할 때 소모하는 비용 목록입니다.</summary>
        public IReadOnlyList<ItemStackData> UpgradeCosts => upgradeCosts;
        /// <summary>이 레벨의 효과 설명입니다.</summary>
        public string LevelDescription => levelDescription;
        #endregion // 프로퍼티
    }
}
