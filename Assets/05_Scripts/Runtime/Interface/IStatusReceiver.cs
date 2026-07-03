using EchoesOfAsh.Enum;
using UnityEngine;

namespace EchoesOfAsh.Interface
{
    public interface IStatusReceiver
    {
        /// <summary>
        /// 상태이상을 적용한다
        /// </summary>
        /// <param name="statusType">상태이상 타입</param>
        /// <param name="stack">중첩 수치</param>
        public void ApplyStatus(EStatusEffectType statusType, int stack);

        /// <summary>
        /// 해당 상태이상의 현재 중첩 수치를 반환한다
        /// </summary>
        /// <param name="statusType">상태이상 타입</param>
        /// <returns>상태이상 중첩 수치</returns>
        public int GetStatusStack(EStatusEffectType statusType);
    }
}