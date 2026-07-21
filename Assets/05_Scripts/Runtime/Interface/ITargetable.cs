using UnityEngine;

namespace EchoesOfAsh.Interface
{
    /// <summary>
    /// 전투 효과의 대상으로 선택될 수 있는 객체의 계약을 정의합니다.
    /// </summary>
    public interface ITargetable
    {
        /// <summary>표시용 이름입니다.</summary>
        public string DisplayName { get; }
        /// <summary>대상이 유효한지 (사망/전투불능 여부 등)입니다.</summary>
        public bool IsTargetable { get; }
    }
}
