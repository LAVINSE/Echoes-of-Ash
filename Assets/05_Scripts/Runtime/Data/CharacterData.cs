using System.Collections.Generic;
using EchoesOfAsh.Effect.Trigger;
using SW.Attributes;
using SW.Base;
using SW.Stat;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 캐릭터 데이터입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Character_", menuName = "EchoesOfAsh/Data/Character")]
    public class CharacterData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("스탯")]
        [SerializeField] private SWStatOverride maxHpStat;
        [SerializeField] private bool isOptionalStat;
        [SerializeField, SWCondition("isOptionalStat", true)] private SWStatOverride[] optionalStats;

        [SWGroup("전용 카드")]
        [Tooltip("목록에 없는 카드는 전부 공용 카드")]
        [SerializeField] private List<CardData> exclusiveCards = new();

        [SWGroup("패시브")]
        [Tooltip("전투 중 발동하는 캐릭터 고유 트리거 효과 목록입니다")]
        [SerializeField] private List<TriggerEffect> passives = new();

        [SWGroup("표시")]
        [SerializeField] private Sprite characterPortraitSprite;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>캐릭터 최대 HP 능력치입니다.</summary>
        public SWStatOverride MaxHpStat => maxHpStat;
        /// <summary>추가 능력치 사용 여부입니다.</summary>
        public bool IsOptionalStat => isOptionalStat;

        /// <summary>캐릭터 전용 카드 목록입니다. 캐릭터 전투불능 시 드로우 풀에서 제외됩니다.</summary>
        public IReadOnlyList<CardData> ExclusiveCards => exclusiveCards;

        /// <summary>캐릭터 고유 패시브 트리거 효과 목록입니다.</summary>
        public IReadOnlyList<TriggerEffect> Passives => passives;

        /// <summary>캐릭터 초상화 스프라이트입니다.</summary>
        public Sprite CharacterPortraitSprite => characterPortraitSprite;
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        /// <summary>
        /// 캐릭터 능력치와 전용 카드 설정의 필수값을 검증합니다.
        /// </summary>
        private void OnValidate()
        {
            if (maxHpStat == null || maxHpStat.Stat == null)
            {
                SWLog.LogError($"[CharacterData] '{name}': Max_HP 스탯 에셋이 비어 있습니다.");
            }

            for (int index = 0; index < exclusiveCards.Count; index++)
            {
                if (exclusiveCards[index] == null)
                {
                    SWLog.LogWarning($"[CharacterData] '{name}': 전용 카드 목록 {index}번이 비어 있습니다.");
                    continue;
                }

                if (exclusiveCards.IndexOf(exclusiveCards[index]) != index)
                {
                    SWLog.LogError($"[CharacterData] '{name}': 전용 카드 '{exclusiveCards[index].name}'가 중복 등록되었습니다.");
                }
            }
        }
#endif // UNITY_EDITOR
        #endregion // 에디터
    }
}
