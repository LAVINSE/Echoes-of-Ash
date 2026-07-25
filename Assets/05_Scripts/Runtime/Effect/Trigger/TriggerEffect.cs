using System.Collections.Generic;
using System.Text;
using EchoesOfAsh.Enum;
using SW.Attributes;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// 방동 시점과 정신력 조건에 따라 효과 블록을 실행하는 트리거
    /// </summary>
    [System.Serializable]
    public class TriggerEffect
    {
        #region 필드
        [Tooltip("효과가 발동하는 시점입니다")]
        [SerializeField] private ETriggerType triggerType;

        [Tooltip("발동에 필요한 파티 정신력 구간 조건입니다")]
        [SerializeField] private ESanityCondition sanityCondition;

        [SerializeReference, SWSubClassSelector(true)] private List<EffectBlock> effects = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>효과가 발동하는 시점입니다.</summary>
        public ETriggerType TriggerType => triggerType;
        /// <summary>발동에 필요한 파티 정신력 구간 조건입니다.</summary>
        public ESanityCondition SanityCondition => sanityCondition;
        /// <summary>발동 시 실행할 효과 블록 목록입니다.</summary>
        public IReadOnlyList<EffectBlock> Effects => effects;
        #endregion // 프로퍼티

        /// <summary>
        /// 조건과 효과를 조합한 표시용 설명을 반환합니다.
        /// </summary>
        /// <returns>조합된 설명 문자열입니다.</returns>
        public string GetDescription()
        {
            StringBuilder stringBuilder = new();

            switch (sanityCondition)
            {
                case ESanityCondition.CalmOnly:
                    stringBuilder.Append("[평정] ");
                    break;
                case ESanityCondition.MadnessOnly:
                    stringBuilder.Append("[광기] ");
                    break;
            }

            switch (triggerType)
            {
                case ETriggerType.BattleStart: stringBuilder.Append("전투 시작 시: "); break;
                case ETriggerType.TurnStart: stringBuilder.Append("턴 시작 시: "); break;
                case ETriggerType.CardPlayed: stringBuilder.Append("카드 사용 시: "); break;
                case ETriggerType.TakeDamage: stringBuilder.Append("피격 시: "); break;
                case ETriggerType.DealDamage: stringBuilder.Append("가해 시: "); break;
            }

            for (int index = 0; index < effects.Count; index++)
            {
                if (effects[index] == null)
                {
                    continue;
                }

                if (index > 0)
                {
                    stringBuilder.Append(", ");
                }

                stringBuilder.Append(effects[index].GetDescription());
            }

            return stringBuilder.ToString();
        }
    }
}