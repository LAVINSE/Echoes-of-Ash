using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
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

        [SWGroup("설계도")]
        [Tooltip("설계도 타입일 때 해금할 제작형 카드입니다. 그 외 타입에서는 사용하지 않습니다.")]
        [SerializeField] private CardData unlockCard;

        [SWGroup("표시")]
        [SerializeField] private Sprite itemSprite;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>아이템 유형입니다.</summary>
        public EItemType ItemType => itemType;

        /// <summary>설계도가 해금하는 카드입니다. 설계도 타입이 아니면 null이어야 합니다.</summary>
        public CardData UnlockCard => unlockCard;

        /// <summary>아이콘 스프라이트입니다.</summary>
        public Sprite ItemSprite => itemSprite;

        /// <summary>
        /// 기본 자원 여부입니다. 기본 자원은 던전 패배 시에도 항상 회수됩니다.
        /// </summary>
        public bool IsBaseResource => itemType == EItemType.Resource;
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        /// <summary>
        /// 아이템 유형에 필요한 연결 데이터가 설정되었는지 검증합니다.
        /// </summary>
        private void OnValidate()
        {
            if (itemType == EItemType.BluePrint && unlockCard == null)
            {
                SWLog.LogWarning($"[ItemData] '{name}': 설계도 타입인데 해금 카드가 연결되지 않았습니다.");
            }

            if (itemType != EItemType.BluePrint && unlockCard != null)
            {
                SWLog.LogWarning($"[ItemData] '{name}': 설계도 타입이 아닌데 해금 카드가 연결되어 있습니다.");
            }
        }
#endif // UNITY_EDITOR
        #endregion // 에디터
    }
}
