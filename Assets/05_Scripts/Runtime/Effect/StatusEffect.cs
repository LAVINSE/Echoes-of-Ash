using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Attributes;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// 대상에게 지정한 상태 이상 중첩을 적용하는 효과입니다.
    /// </summary>
    [System.Serializable]
    [SWAddTypeMenu("유틸리티/상태이상 부여")]
    public class StatusEffect : EffectBlock
    {
        #region 필드
        [SerializeField] private EStatusEffectType statusType;
        [SerializeField, Min(1)] private int stack = 1;
        #endregion // 필드

        #region 프로퍼티
        /// <inheritdoc />
        public override EIntentType? IntentContribution => EIntentType.Buff;
        #endregion // 프로퍼티

        /// <inheritdoc />
        public override void Apply(EffectContext context)
        {
            foreach(var target in context.Targets)
            {
                if(target is IStatusReceiver receiver)
                {
                    receiver.ApplyStatus(statusType, stack);
                }
            }
        }

        /// <inheritdoc />
        public override string GetDescription()
        {
            return $"{statusType} {stack} 부여";
        }
    }

}
