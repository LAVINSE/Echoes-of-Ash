using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 드랍과 회수의 대상이 되는 아이템 데이터입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "EchoesOfAsh/Data/Item")]
    public class ItemData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("타입")]
        [SerializeField] private EItemType itemType;

        [SWGroup("표시")]
        [SerializeField] private Sprite itemSprite;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>아이템 유형입니다.</summary>
        public EItemType ItemType => itemType;
        /// <summary>아이콘 스프라이트입니다.</summary>
        public Sprite ItemSprite => itemSprite;

        /// <summary>
        /// 기본 자원 여부입니다. 기본 자원은 던전 패배 시에도 항상 회수됩니다
        /// </summary>
        public bool IsBaseResource => itemType == EItemType.Resource;
        #endregion // 프로퍼티
    }

}