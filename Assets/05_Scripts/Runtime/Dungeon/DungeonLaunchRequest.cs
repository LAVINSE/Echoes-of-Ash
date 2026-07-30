using EchoesOfAsh.Enum;
using UnityEngine;

namespace EchoesOfAsh.Dungeon
{
    /// <summary>
    /// 거점 장면에서 던전 장면으로 전환할 때 출발 방식을 전달합니다.
    /// </summary>
    public static class DungeonLaunchRequest
    {
        #region 프로퍼티
        /// <summary>대기 중인 출발 요청입니다.</summary>
        public static EDungeonLaunchMode Mode { get; private set; } = EDungeonLaunchMode.None;
        #endregion // 프로퍼티

        /// <summary>
        /// 출발 요청을 등록합니다. 던전 씬 로드 직전에 호출합니다.
        /// </summary>
        /// <param name="mode">출발 방식입니다.</param>
        public static void Request(EDungeonLaunchMode mode)
        {
            Mode = mode;
        }

        /// <summary>
        /// 대기 중인 요청을 반환하고 초기화합니다. 던전 씬 시작 시 1회 호출합니다.
        /// </summary>
        /// <returns>대기 중이던 출발 방식입니다. 요청이 없으면 None입니다.</returns>
        public static EDungeonLaunchMode Consume()
        {
            EDungeonLaunchMode consumedMode = Mode;
            Mode = EDungeonLaunchMode.None;
            return consumedMode;
        }
    }
}
