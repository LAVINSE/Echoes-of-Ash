using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Attributes;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    [System.Serializable]
    [SWAddTypeMenu("유틸리티/상태이상 부여")]
    public class StatusEffect : EffectBlock
    {
        #region 필드
        [SerializeField] private EStatusEffectType statusType;
        [SerializeField, Min(1)] private int stack = 1;
        #endregion // 필드

        #region 프로퍼티
        public override EIntentType? IntentContribution => EIntentType.Buff;
        #endregion // 프로퍼티

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

        public override string GetDescription()
        {
            return $"{statusType} {stack} 부여";
        }
    }

}
