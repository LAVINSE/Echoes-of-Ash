using EchoesOfAsh.Enum;
using UnityEngine;

namespace EchoesOfAsh.Interface
{
    /// <summary>
    /// 상태 이상을 적용하고 현재 중첩을 조회할 수 있는 대상의 기능을 정의합니다.
    /// </summary>
    public interface IStatusReceiver
    {
        /// <summary>
        /// 상태 이상을 적용합니다.
        /// </summary>
        /// <param name="statusType">상태 이상 유형입니다.</param>
        /// <param name="stack">중첩 수치입니다.</param>
        public void ApplyStatus(EStatusEffectType statusType, int stack);

        /// <summary>
        /// 해당 상태 이상의 현재 중첩 수치를 반환합니다.
        /// </summary>
        /// <param name="statusType">상태 이상 유형입니다.</param>
        /// <returns>상태 이상 중첩 수치입니다.</returns>
        public int GetStatusStack(EStatusEffectType statusType);
    }
}
