using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 카드 타입
    /// </summary>
    public enum ECardType
    {
        [InspectorName("공격")] Attack,
        [InspectorName("방어")] Defense,
        [InspectorName("스킬")] Skill,
        [InspectorName("파워")] Power,
        [InspectorName("저주")] Curse,
    }

    /// <summary>
    /// 희귀도 (공통)
    /// </summary>
    public enum ERarityType
    {
        [InspectorName("일반")] Common,
        [InspectorName("희귀")] Rare,
        [InspectorName("에픽")] Epic,
        [InspectorName("전설")] Legend,
        [InspectorName("고유")] Unique
    }

    /// <summary>
    /// 정신력 타입
    /// </summary>
    public enum ESanityType
    {
        [InspectorName("평정")] Calm,
        [InspectorName("광기")] Madness
    }
}