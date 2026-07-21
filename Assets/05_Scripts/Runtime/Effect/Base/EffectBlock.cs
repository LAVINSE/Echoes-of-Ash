using EchoesOfAsh.Enum;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// 카드와 적 행동에서 실행되는 효과의 공통 계약을 정의합니다.
    /// </summary>
    [System.Serializable]
    public abstract class EffectBlock
    {
        /// <summary>의도 유형입니다.</summary>
        public virtual EIntentType? IntentContribution => null;

        /// <summary>
        /// 효과를 실행합니다.
        /// </summary>
        /// <param name="context">실행 컨텍스트입니다.</param>
        public abstract void Apply(EffectContext context);

        /// <summary>
        /// 효과 설명을 반환합니다.
        /// </summary>
        /// <returns>효과 텍스트입니다.</returns>
        public abstract string GetDescription();

        /// <summary>
        /// 하위 클래스 선택기를 사용할 때 인스펙터 필드에 표시할 이름을 반환합니다.
        /// </summary>
        /// <returns>효과 텍스트입니다.</returns>
        public override string ToString()
            => GetDescription();
    }
}
