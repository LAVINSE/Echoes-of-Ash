using System.Collections.Generic;
using EchoesOfAsh.Effect;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;


namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 정신력 무작위 이벤트 데이터
    /// 정신력 전환 시 확률적으로 발생
    /// 높은 확률로 부정효과 / 낮은 확률로 긍정효과
    /// </summary>
    [CreateAssetMenu(fileName = "SanityEvent", menuName = "EchoesOfAsh/Data/SanityEvent")]
    public class SanityEventData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("부정 효과")]
        [SerializeReference, SWSubClassSelector(true)] private List<EffectBlock> effects = new();

        [SWGroup("긍정 효과")]
        [SerializeField] private bool isPositiveEffect;
        [Tooltip("긍정 효과 발생 확률 0 ~ 1 => 0 ~ 100%")]
        [SerializeField, Range(0f, 1f), SWCondition("isPositiveEffect", true)] private float weight = 0f;
        [SWCondition("isPositiveEffect", true), SerializeReference, SWSubClassSelector(true)] private List<EffectBlock> positiveEffects = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>긍정 효과 이벤트 여부입니다.</summary>
        public bool IsPositiveEffect => isPositiveEffect;
        /// <summary>긍정 효과 발생 확률입니다.</summary>
        public float Weight => weight;

        /// <summary>부정 효과 목록입니다.</summary>
        public IReadOnlyList<EffectBlock> Effects => effects;
        /// <summary>긍정 효과 목록입니다.</summary>
        public IReadOnlyList<EffectBlock> PositiveEffects => positiveEffects;
        #endregion // 프로퍼티


        #region 에디터
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (effects.Count == 0)
            {
                SWLog.LogWarning($"[SanityEventData] '{name}': 효과가 비어 있습니다.");
            }
        }
#endif
        #endregion // 에디터
    }
}
