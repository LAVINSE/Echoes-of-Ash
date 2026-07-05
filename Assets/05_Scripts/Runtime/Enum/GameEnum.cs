using UnityEngine;

namespace EchoesOfAsh.Enum
{
    /// <summary>
    /// 카드 타입
    /// </summary>
    public enum ECardType
    {
        /// <summary>공격</summary>
        [InspectorName("공격")] Attack,
        /// <summary>방어</summary>
        [InspectorName("방어")] Defense,
        /// <summary>스킬</summary>
        [InspectorName("스킬")] Skill,
        /// <summary>파워</summary>
        [InspectorName("파워")] Power,
        /// <summary>저주</summary>
        [InspectorName("저주")] Curse,
    }

    /// <summary>
    /// 희귀도 (공통)
    /// </summary>
    public enum ERarityType
    {
        /// <summary>일반</summary>
        [InspectorName("일반")] Common,
        /// <summary>희귀</summary>
        [InspectorName("희귀")] Rare,
        /// <summary>에픽</summary>
        [InspectorName("에픽")] Epic,
        /// <summary>전설</summary>
        [InspectorName("전설")] Legend,
        /// <summary>고유</summary>
        [InspectorName("고유")] Unique
    }

    /// <summary>
    /// 정신력 타입
    /// </summary>
    public enum ESanityType
    {
        /// <summary>평정</summary>
        [InspectorName("평정")] Calm,
        /// <summary>광기</summary>
        [InspectorName("광기")] Madness
    }

    /// <summary>
    /// 아이템 타입
    /// </summary>
    public enum EItemType
    {
        /// <summary>일반 자원</summary>
        [InspectorName("일반 자원")] Resource,
        /// <summary>소모품</summary>
        [InspectorName("소모품")] Consume,
        /// <summary>재료</summary>
        [InspectorName("재료")] Material,
        /// <summary>설계도</summary>
        [InspectorName("설계도")] BluePrint,
    }

    /// <summary>
    /// 유물 발동 타입
    /// </summary>
    public enum ERelicTriggerType
    {
        /// <summary>패시브</summary>
        [InspectorName("패시브")] Passive,
        /// <summary>전투 시작 시 1회</summary>
        [InspectorName("전투 시작 시 1회")] OnBattleStart,
        /// <summary>매 턴 시작 시</summary>
        [InspectorName("매 턴 시작 시")] TurnStart,
        /// <summary>카드 사용할 때마다</summary>
        [InspectorName("카드 사용할 때마다")] OnCardPlay,
        /// <summary>피격당할 때마다</summary>
        [InspectorName("피격당할 때마다")] OnTakeDamage,
        /// <summary>피해를 입힐 때마다</summary>
        [InspectorName("피해를 입힐 때마다")] OnDealDamage,
        /// <summary>전투 종료 시 1회</summary>
        [InspectorName("전투 종료 시 1회")] OnBattleEnd,
    }

    /// <summary>
    /// 상태이상 타입
    /// </summary>
    public enum EStatusEffectType
    {
        /// <summary>X</summary>
        [InspectorName("X")] None,
        /// <summary>화상</summary>
        [InspectorName("화상")] Burn,
        /// <summary>기절</summary>
        [InspectorName("기절")] Stun,
        /// <summary>출혈</summary>
        [InspectorName("출혈")] Bleed,
        /// <summary>중독</summary>
        [InspectorName("중독")] Poison,
    }

    /// <summary>
    /// 카드 대상 지정 방식
    /// </summary>
    public enum ETargetingType
    {
        /// <summary>단일 (드래그 지정)</summary>
        [InspectorName("단일 (드래그 지정)")] Single,
        /// <summary>적 전체"</summary>
        [InspectorName("적 전체")] AllEnemies,
        /// <summary>무작위 적</summary>
        [InspectorName("무작위 적")] RandomEnemy,
        /// <summary>자신/아군</summary>
        [InspectorName("자신/아군")] Self,
    }

    /// <summary>
    /// 카드 해금 방식
    /// </summary>
    public enum ECardUnlockType
    {
        /// <summary>발견형 (자동 해금)</summary>
        [InspectorName("발견형 (자동 해금)")] Discovery,
        /// <summary>제작형 (설계도 해금)</summary>
        [InspectorName("제작형 (설계도 해금)")] Blueprint,
    }

    /// <summary>
    /// 적 공격 대상 선정 규칙
    /// </summary>
    public enum EEnemyTargetRuleType
    {
        /// <summary>무작위</summary>
        [InspectorName("무작위")] Random,
        /// <summary>어그로 기반</summary>
        [InspectorName("어그로 기반")] Aggro,
        /// <summary>지정 고정</summary>
        [InspectorName("지정 고정")] Fixed,
    }

    /// <summary>
    /// 의도 표시 타입
    /// </summary>
    public enum EIntentType
    {
        /// <summary>공격</summary>
        [InspectorName("공격")] Attack,
        /// <summary>방어</summary>
        [InspectorName("방어")] Defense,
        /// <summary>버프/디버프</summary>
        [InspectorName("버프/디버프")] Buff,
        /// <summary>정신력 타격</summary>
        [InspectorName("정신력 타격")] SanityPressure,
        /// <summary>특수</summary>
        [InspectorName("특수")] Special,
    }
}