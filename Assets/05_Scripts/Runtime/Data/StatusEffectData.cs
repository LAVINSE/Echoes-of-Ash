using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 상태 이상 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "Status_", menuName = "EchoesOfAsh/Data/StatusEffect")]
    public class StatusEffectData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("타입")]
        [SerializeField] private EStatusEffectType statusEffectType;

        [SWGroup("버프/디버프")]
        [SerializeField] private bool isDebuff = true;

        [SWGroup("상태이상 감소 규칙")]
        [SerializeField] private EStatusDecayType decayType = EStatusDecayType.TurnCountdown;

        [SWGroup("배율")]
        [Tooltip("받는 피해 배율")]
        [SerializeField, Min(0f)] private float damageTakenMultiplier = 1f;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>상태이상 타입</summary>
        public EStatusEffectType StatusEffectType => statusEffectType;
        /// <summary>디버프 여부입니다.</summary>
        public bool IsDebuff => isDebuff;
        /// <summary>라운드 종료 시점의 중첩 감소 규칙입니다.</summary>
        public EStatusDecayType DecayType => decayType;
        /// <summary>활성 상태일 때 받는 피해 배율입니다.</summary>
        public float DamageTakenMultiplier => damageTakenMultiplier;
        #endregion // 프로퍼티

#if UNITY_EDITOR
        /// <summary>
        /// 상태 이상 유형이 실제 처리 규칙과 연결될 수 있는지 검증합니다.
        /// </summary>
        private void OnValidate()
        {
            if (statusEffectType == EStatusEffectType.None)
            {
                SWLog.LogWarning($"[StatusEffectData] {name}: 상태 이상 유형이 None입니다. 로직 매핑 키를 지정하세요");
            }
        }
#endif
    }
}
