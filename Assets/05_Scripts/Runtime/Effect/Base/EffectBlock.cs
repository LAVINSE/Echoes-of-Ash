using UnityEngine;

namespace EchoesOfAsh.Effect
{
    [System.Serializable]
    public abstract class EffectBlock
    {
        /// <summary>
        /// 효과를 실행한다
        /// </summary>
        /// <param name="context">실행 컨텍스트</param>
        public abstract void Apply(EffectContext context);

        /// <summary>
        /// 효과 설명을 반환한다
        /// </summary>
        /// <returns>효과 텍스트</returns>
        public abstract string GetDescription();

        /// <summary>
        /// SWSubClassSelector(true) 사용 시 인스펙터 필드 라벨로 표시
        /// </summary>
        /// <returns>효과 텍스트</returns>
        public override string ToString()
            => GetDescription();
    }
}