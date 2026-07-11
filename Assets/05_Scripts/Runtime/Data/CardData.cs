using System.Collections.Generic;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 카드 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "Card_", menuName = "EchoesOfAsh/Data/Card")]
    public class CardData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("타입")]
        [SerializeField] private ECardType cardType;
        [SerializeField] private ERarityType rarityType;
        [Tooltip("활성화하면 캐릭터 전용 카드, 비활성화하면 공용 카드")]
        [SerializeField] private bool isCharacterCard;
        [SerializeField, SWCondition("isCharacterCard", true)] private CharacterData ownerCharacter;

        [SWGroup("비용 / 대상")]
        [SerializeField, Range(0, 5)] private int apCost;
        [SerializeField] private ETargetingType targetingType;

        [SWGroup("카드 효과 (일반)")]
        [SerializeReference, SWSubClassSelector(true)] private List<EffectBlock> effects = new();

        [SWGroup("카드 효과 (정신력)")]
        [SerializeField] private bool isSanityEffect;
        [SWCondition("isSanityEffect", true), SerializeReference, SWSubClassSelector(true)] private List<EffectBlock> sanityEffects = new();

        [SWGroup("카드 업그레이드")]
        [SerializeField] private bool isUpgrade;
        [SerializeField, SWCondition("isUpgrade", true)] private CardData upgradeCard;

        [SWGroup("해금 방식")]
        [SerializeField] private ECardUnlockType unlockType;

        [SWGroup("표시")]
        [SerializeField] private Sprite cardIconSprite;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>카드 유형입니다.</summary>
        public ECardType CardType => cardType;
        /// <summary>카드 희귀도입니다.</summary>
        public ERarityType RarityType => rarityType;
        /// <summary>캐릭터 전용 카드 여부입니다.</summary>
        public bool IsCharacterCard => isCharacterCard;
        /// <summary>소속 캐릭터입니다.</summary>
        public CharacterData OwnerCharacter => ownerCharacter;

        /// <summary>카드 비용입니다.</summary>
        public int ApCost => apCost;
        /// <summary>대상 지정 방식입니다.</summary>
        public ETargetingType TargetingType => targetingType;

        /// <summary>정신력 반응 여부입니다.</summary>
        public bool IsSanityEffect => isSanityEffect;

        /// <summary>카드 효과 목록입니다.</summary>
        public IReadOnlyList<EffectBlock> Effects => effects;
        /// <summary>카드 효과 (정신력) 목록입니다.</summary>
        public IReadOnlyList<EffectBlock> SanityEffects => sanityEffects;

        /// <summary>카드 강화 여부입니다.</summary>
        public bool IsUpgrade => isUpgrade;
        /// <summary>카드 강화 버전 데이터 (없으면 null)입니다.</summary>
        public CardData UpgradeCard => upgradeCard;

        /// <summary>카드 해금 방식입니다.</summary>
        public ECardUnlockType UnlockType => unlockType;

        /// <summary>카드 아이콘 스프라이트입니다.</summary>
        public Sprite CardIconSprite => cardIconSprite;
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (isSanityEffect && sanityEffects.Count == 0)
            {
                SWLog.LogError($"[CardData] '{name}': 정신력 영향을 받는 카드입니다. 효과가 비어있습니다");
            }

            if (!isSanityEffect && sanityEffects.Count > 0)
            {
                SWLog.LogError($"[CardData] '{name}': 정신력 영향을 받지 않는 카드입니다. 효과가 설정되어있습니다");
            }

            if (effects.Count == 0)
            {
                SWLog.LogError($"[CardData] '{name}': 기본 효과가 비어있습니다");
            }
        }
#endif // UNITY_EDITOR
        #endregion // 에디터

        /// <summary>
        /// 현재 정신력 구간에 해당하는 효과 목록을 반환합니다.
        /// 정신력 영향을 받지 않는 카드인 경우 기본 효과 반환
        /// </summary>
        /// <param name="sanityType">현재 파티 정신력 유형입니다.</param>
        /// <returns>적용할 효과 목록입니다.</returns>
        public IReadOnlyList<EffectBlock> GetEffectBlocks(ESanityType sanityType)
        {
            if (isSanityEffect && sanityType == ESanityType.Madness)
            {
                return sanityEffects;
            }

            return effects;
        }
    }
}
