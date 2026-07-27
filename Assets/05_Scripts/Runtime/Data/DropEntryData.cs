using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 드랍 항목
    /// </summary>
    [System.Serializable]
    public class DropEntryData
    {
        #region 필드
        [SerializeField] private ItemData itemData;
        [Tooltip("가중치")]
        [SerializeField, Min(0f)] private float weight = 1f;
        [Tooltip("드랍 수량 최소값")]
        [SerializeField, Min(1)] private int minCount = 1;
        [Tooltip("드랍 수량 최대값")]
        [SerializeField, Min(1)] private int maxCount = 1;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>드랍할 아이템 데이터입니다.</summary>
        public ItemData ItemData => itemData;
        /// <summary>추첨 가중치입니다.</summary>
        public float Weight => weight;
        /// <summary>드랍 수량 최소값입니다.</summary>
        public int MinCount => minCount;
        /// <summary>드랍 수량 최대값입니다.</summary>
        public int MaxCount => maxCount;
        #endregion // 프로퍼티
    }
}