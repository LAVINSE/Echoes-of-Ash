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
    /// 카드 데이터입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Card_", menuName = "EchoesOfAsh/Data/Card")]
    public class CardData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("타입")]
        [SerializeField] private ECardType cardType;
        [SerializeField] private ERarityType rarityType;

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
        [Tooltip("처음부터 해금된 카드인지 여부입니다. 해금 풀 조회 시 저장 기록 없이도 포함됩니다.")]
        [SerializeField] private bool isDefaultUnlocked;

        [SWGroup("표시")]
        [SerializeField] private Sprite cardIconSprite;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>카드 유형입니다.</summary>
        public ECardType CardType => cardType;
        /// <summary>카드 희귀도입니다.</summary>
        public ERarityType RarityType => rarityType;

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
        /// <summary>카드 강화 버전 데이터입니다. 없으면 <see langword="null"/>입니다.</summary>
        public CardData UpgradeCard => upgradeCard;

        /// <summary>카드 해금 방식입니다.</summary>
        public ECardUnlockType UnlockType => unlockType;
        /// <summary>기본 해금 여부입니다. true면 저장 기록 없이도 해금 풀에 포함됩니다.</summary>
        public bool IsDefaultUnlocked => isDefaultUnlocked;

        /// <summary>카드 아이콘 스프라이트입니다.</summary>
        public Sprite CardIconSprite => cardIconSprite;
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        /// <summary>
        /// 카드 효과와 정신력 분기 설정이 서로 일치하는지 검증합니다.
        /// </summary>
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
        /// 정신력의 영향을 받지 않는 카드인 경우 기본 효과를 반환합니다.
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
