using System.Collections.Generic;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using SW.Attributes;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 적 단일 행동 데이터입니다.
    /// </summary>
    [System.Serializable]
    public class EnemyActionData
    {
        #region 필드
        [SerializeField] private string actionName;
        [SerializeReference, SWSubClassSelector(true)] private List<EffectBlock> effects = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>행동 이름입니다.</summary>
        public string ActionName => actionName;

        /// <summary>행동이 실행할 효과 목록입니다.</summary>
        public IReadOnlyList<EffectBlock> Effects => effects;
        #endregion // 프로퍼티

        /// <summary>
        /// 행동에 포함된 의도 유형 목록을 반환합니다.
        /// 의도가 없을 경우 특수 유형으로 반환합니다.
        /// </summary>
        /// <returns>의도 유형 목록입니다.</returns>
        public List<EIntentType> GetIntentTypes()
        {
            List<EIntentType> result = new();

            foreach (var effect in effects)
            {
                EIntentType? intent = effect.IntentContribution;

                if (intent.HasValue && !result.Contains(intent.Value))
                {
                    result.Add(intent.Value);
                }
            }

            if (result.Count == 0)
            {
                result.Add(EIntentType.Special);
            }

            return result;
        }

        /// <summary>
        /// 공격 의도 옆에 표시할 총 피해량을 계산합니다.
        /// 피해 블록이 없으면 0을 반환합니다.
        /// </summary>
        /// <returns>피해 블록 합산 값입니다.</returns>
        public int GetIntentDamageValue()
        {
            int total = 0;

            foreach (var effect in effects)
            {
                if (effect is DamageEffect damageEffect)
                {
                    total += damageEffect.Damage * damageEffect.Times;
                }
            }

            return total;
        }

        /// <summary>
        /// 정신력 아이콘 옆에 표시할 총 감소량을 계산합니다.
        /// </summary>
        /// <returns>정신력 감소량 합산입니다.</returns>
        public int GetIntentSanityPressureValue()
        {
            int total = 0;

            foreach (var effect in effects)
            {
                if (effect is SanityChangeEffect sanityChangeEffect && sanityChangeEffect.Delta < 0)
                {
                    total -= sanityChangeEffect.Delta;
                }
            }

            return total;
        }
    }
}
