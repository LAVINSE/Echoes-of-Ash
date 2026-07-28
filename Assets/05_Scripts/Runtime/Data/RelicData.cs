using System.Collections.Generic;
using EchoesOfAsh.Effect.Trigger;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 유물 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "Relic_", menuName = "EchoesOfAsh/Data/Relic")]
    public class RelicData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("등급")]
        [Tooltip("유물 등급입니다.")]
        [SerializeField] private ERarityType rarityType;

        [Tooltip("전용 캐릭터입니다. 비워두면 공용 유물입니다. 전용 유물은 소유 캐릭터가 파티에 있어야 발동합니다.")]
        [SerializeField] private CharacterData ownerCharacter;

        [SWGroup("트리거 효과")]
        [Tooltip("발동 시점과 정신력 조건에 따라 실행되는 효과 목록입니다. 목록 순서대로 등록됩니다.")]
        [SerializeField] private List<TriggerEffect> triggerEffects = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>유물 등급입니다.</summary>
        public ERarityType RarityType => rarityType;
        /// <summary>전용 캐릭터입니다. 공용 유물이면 null입니다.</summary>
        public CharacterData OwnerCharacter => ownerCharacter;
        /// <summary>공용 유물 여부입니다.</summary>
        public bool IsShared => ownerCharacter == null;
        /// <summary>트리거 효과 목록입니다.</summary>
        public IReadOnlyList<TriggerEffect> TriggerEffects => triggerEffects;

        /// <summary>정신력 연동 여부입니다. 정신력 구간 조건이 있는 트리거 효과가 하나라도 있으면 true입니다.</summary>
        public bool IsSanityLinked
        {
            get
            {
                foreach (TriggerEffect triggerEffect in triggerEffects)
                {
                    if (triggerEffect != null && triggerEffect.SanityCondition != ESanityCondition.None)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (triggerEffects.Count == 0)
            {
                SWLog.LogWarning($"[RelicData] '{name}': 트리거 효과 목록이 비어 있습니다.");
                return;
            }

            for (int index = 0; index < triggerEffects.Count; index++)
            {
                if (triggerEffects[index] == null || triggerEffects[index].Effects.Count == 0)
                {
                    SWLog.LogWarning($"[RelicData] '{name}': 트리거 효과 {index}번의 효과 블록이 비어 있습니다.");
                }
            }
        }
#endif // UNITY_EDITOR
        #endregion // 에디터
    }
}