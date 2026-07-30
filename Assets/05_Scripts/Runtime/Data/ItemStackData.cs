using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 아이템과 해당 수량을 하나의 묶음으로 보관합니다.
    /// </summary>
    [System.Serializable]
    public class ItemStackData
    {
        #region 필드
        [SerializeField] private ItemData itemData;
        [SerializeField, Min(1)] private int count;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>아이템 데이터입니다.</summary>
        public ItemData ItemData => itemData;
        /// <summary>수량입니다.</summary>
        public int Count => count;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 아이템 묶음을 생성합니다.
        /// </summary>
        /// <param name="itemData">아이템 데이터입니다.</param>
        /// <param name="count">수량입니다.</param>
        public ItemStackData(ItemData itemData, int count)
        {
            if (itemData == null)
            {
                SWLog.LogError("[ItemStack] 생성 실패: 아이템 데이터가 null입니다");
            }

            this.itemData = itemData;
            this.count = Mathf.Max(1, count);
        }
        #endregion // 생성자

        /// <summary>
        /// 수량을 더합니다. 같은 아이템의 소지 합산에 사용합니다.
        /// </summary>
        /// <param name="amount">더할 수량입니다.</param>
        public void AddCount(int amount)
        {
            count = Mathf.Max(1, count + amount);
        }
    }
}
