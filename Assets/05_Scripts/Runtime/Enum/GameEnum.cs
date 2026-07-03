using UnityEngine;

namespace EchoesOfAsh.Enum
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

    /// <summary>
    /// 아이템 타입
    /// </summary>
    public enum EItemType
    {
        [InspectorName("일반 자원")] Resource,
        [InspectorName("소모품")] Consume,
        [InspectorName("재료")] Material,
        [InspectorName("설계도")] BluePrint,
    }

    /// <summary>
    /// 유물 발동 타입
    /// </summary>
    public enum ERelicTriggerType
    {
        [InspectorName("패시브")] Passive,
        [InspectorName("전투 시작 시 1회")] OnBattleStart,
        [InspectorName("매 턴 시작 시")] TurnStart,
        [InspectorName("카드 사용할 때마다")] OnCardPlay,
        [InspectorName("피격당할 때마다")] OnTakeDamage,
        [InspectorName("피해를 입힐 때마다")] OnDealDamage,
        [InspectorName("전투 종료 시 1회")] OnBattleEnd,
    }

    /// <summary>
    /// 상태이상 타입
    /// </summary>
    public enum EStatusEffectType
    {
        [InspectorName("X")] None,
        [InspectorName("화상")] Burn,
        [InspectorName("기절")] Stun,
        [InspectorName("출혈")] Bleed,
        [InspectorName("중독")] Poison,
    }

    /// <summary>
    /// 카드 대상 지정 방식
    /// </summary>
    public enum ETargetingType
    {
        [InspectorName("단일 (드래그 지정)")] Single,
        [InspectorName("적 전체")] AllEnemies,
        [InspectorName("무작위 적")] RandomEnemy,
        [InspectorName("자신/아군")] Self,
    }

    /// <summary>
    /// 적 공격 대상 선정 규칙
    /// </summary>
    public enum EEnemyTargetRuleType
    {
        [InspectorName("무작위")] Random,
        [InspectorName("어그로 기반")] Aggro,
        [InspectorName("지정 고정")] Fixed,
    }

    /// <summary>
    /// 의도 표시 타입
    /// </summary>
    public enum EIntentType
    {
        [InspectorName("공격")] Attack,
        [InspectorName("방어")] Defense,
        [InspectorName("버프/디버프")] Buff,
        [InspectorName("정신력")] SanityPressure,
        [InspectorName("특수")] Special,
    }
}