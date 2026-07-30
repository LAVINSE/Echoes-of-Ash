using UnityEngine;

namespace EchoesOfAsh.Enum
{
    /// <summary>
    /// 카드 유형입니다.
    /// </summary>
    public enum ECardType
    {
        /// <summary>공격입니다.</summary>
        [InspectorName("공격")] Attack,
        /// <summary>방어입니다.</summary>
        [InspectorName("방어")] Defense,
        /// <summary>스킬입니다.</summary>
        [InspectorName("스킬")] Skill,
        /// <summary>파워입니다.</summary>
        [InspectorName("파워")] Power,
        /// <summary>저주입니다.</summary>
        [InspectorName("저주")] Curse,
    }

    /// <summary>
    /// 희귀도 (공통)입니다.
    /// </summary>
    public enum ERarityType
    {
        /// <summary>일반입니다.</summary>
        [InspectorName("일반")] Common,
        /// <summary>희귀입니다.</summary>
        [InspectorName("희귀")] Rare,
        /// <summary>에픽입니다.</summary>
        [InspectorName("에픽")] Epic,
        /// <summary>전설입니다.</summary>
        [InspectorName("전설")] Legend,
        /// <summary>고유입니다.</summary>
        [InspectorName("고유")] Unique
    }

    /// <summary>
    /// 정신력 유형입니다.
    /// </summary>
    public enum ESanityType
    {
        /// <summary>평정입니다.</summary>
        [InspectorName("평정")] Calm,
        /// <summary>광기입니다.</summary>
        [InspectorName("광기")] Madness
    }

    /// <summary>
    /// 아이템 유형입니다.
    /// </summary>
    public enum EItemType
    {
        /// <summary>일반 자원입니다.</summary>
        [InspectorName("일반 자원")] Resource,
        /// <summary>소모품입니다.</summary>
        [InspectorName("소모품")] Consume,
        /// <summary>재료입니다.</summary>
        [InspectorName("재료")] Material,
        /// <summary>설계도입니다.</summary>
        [InspectorName("설계도")] BluePrint,
    }

    /// <summary>
    /// 유물 발동 유형입니다.
    /// </summary>
    public enum ETriggerType
    {
        /// <summary>전투 시작 시 1회입니다.</summary>
        [InspectorName("전투 시작 시 1회")] BattleStart,
        /// <summary>매 턴 시작 시입니다.</summary>
        [InspectorName("매 턴 시작 시")] TurnStart,
        /// <summary>카드를 사용할 때마다 발동합니다.</summary>
        [InspectorName("카드 사용할 때마다")] CardPlayed,
        /// <summary>피격당할 때마다 발동합니다.</summary>
        [InspectorName("피격당할 때마다")] TakeDamage,
        /// <summary>피해를 입힐 때마다 발동합니다.</summary>
        [InspectorName("피해를 입힐 때마다")] DealDamage,
        /// <summary>전투 종료 시 1회입니다.</summary>
        [InspectorName("전투 종료 시 1회")] BattleEnd,
    }

    /// <summary>
    /// 트리거 효과의 정신력 구간 발동 조건입니다.
    /// </summary>
    public enum ESanityCondition
    {
        /// <summary>구간과 무관하게 발동합니다.</summary>
        [InspectorName("조건 없음")] None,
        /// <summary>평정 구간에만 발동합니다.</summary>
        [InspectorName("평정에만")] CalmOnly,
        /// <summary>광기 구간에만 발동합니다.</summary>
        [InspectorName("광기에만")] MadnessOnly,
    }

    /// <summary>
    /// 상태 이상 유형입니다.
    /// </summary>
    public enum EStatusEffectType
    {
        /// <summary>X입니다.</summary>
        [InspectorName("X")] None,
        /// <summary>화상입니다.</summary>
        [InspectorName("화상")] Burn,
        /// <summary>기절입니다.</summary>
        [InspectorName("기절")] Stun,
        /// <summary>출혈입니다.</summary>
        [InspectorName("출혈")] Bleed,
        /// <summary>중독입니다.</summary>
        [InspectorName("중독")] Poison,
        /// <summary>취약입니다. 받는 피해가 증가합니다.</summary>
        [InspectorName("취약")] Vulnerable,
        /// <summary>도발입니다. 적의 대상 선정을 자신에게 강제합니다.</summary>
        [InspectorName("도발")] Taunt,
    }

    /// <summary>
    /// 상태 이상의 라운드 종료 시점 중첩 감소 규칙입니다.
    /// </summary>
    public enum EStatusDecayType
    {
        /// <summary>자동 감소 없음 (효과로만 제거)입니다.</summary>
        [InspectorName("지속 (자동 감소 없음)")] None,
        /// <summary>라운드가 끝날 때마다 1씩 감소하며, 중첩은 남은 라운드 수를 뜻합니다.</summary>
        [InspectorName("라운드마다 1 감소")] TurnCountdown,
    }

    /// <summary>
    /// 카드 대상 지정 방식입니다.
    /// </summary>
    public enum ETargetingType
    {
        /// <summary>단일 (드래그 지정)입니다.</summary>
        [InspectorName("단일 (드래그 지정)")] Single,
        /// <summary>적 전체"입니다.</summary>
        [InspectorName("적 전체")] AllEnemies,
        /// <summary>무작위 적입니다.</summary>
        [InspectorName("무작위 적")] RandomEnemy,
        /// <summary>자신/아군입니다.</summary>
        [InspectorName("자신/아군")] Self,
    }

    /// <summary>
    /// 카드 해금 방식입니다.
    /// </summary>
    public enum ECardUnlockType
    {
        /// <summary>발견형 (자동 해금)입니다.</summary>
        [InspectorName("발견형 (자동 해금)")] Discovery,
        /// <summary>제작형 (설계도 해금)입니다.</summary>
        [InspectorName("제작형 (설계도 해금)")] Blueprint,
        /// <summary>몬스터 드랍형 (특정 조우 승리 드랍 한정)입니다.</summary>
        [InspectorName("몬스터 드랍형 (특정 조우 한정)")] EnemyDrop,
    }

    /// <summary>
    /// 적 종류입니다.
    /// </summary>
    public enum EEnemyType
    {
        /// <summary>일반입니다.</summary>
        [InspectorName("일반")] Normal,
        /// <summary>엘리트입니다.</summary>
        [InspectorName("엘리트")] Elite,
        /// <summary>보스입니다.</summary>
        [InspectorName("보스")] Boss,
    }

    /// <summary>
    /// 적 공격 대상 선정 규칙입니다.
    /// </summary>
    public enum EEnemyTargetRuleType
    {
        /// <summary>무작위입니다.</summary>
        [InspectorName("무작위")] Random,
        /// <summary>어그로 기반입니다.</summary>
        [InspectorName("어그로 기반")] Aggro,
        /// <summary>지정 고정입니다.</summary>
        [InspectorName("지정 고정")] Fixed,
    }

    /// <summary>
    /// 의도 표시 유형입니다.
    /// </summary>
    public enum EIntentType
    {
        /// <summary>공격입니다.</summary>
        [InspectorName("공격")] Attack,
        /// <summary>방어입니다.</summary>
        [InspectorName("방어")] Defense,
        /// <summary>버프/디버프입니다.</summary>
        [InspectorName("버프-디버프")] Buff,
        /// <summary>정신력 타격입니다.</summary>
        [InspectorName("정신력 타격")] SanityPressure,
        /// <summary>특수입니다.</summary>
        [InspectorName("특수")] Special,
    }

    /// <summary>
    /// 전투 턴 진행 단계입니다.
    /// </summary>
    public enum ETurnPhase
    {
        /// <summary>전투가 진행되지 않는 상태입니다.</summary>
        [InspectorName("없음")] None,
        /// <summary>턴 시작 처리 중입니다.</summary>
        [InspectorName("턴 시작")] TurnStart,
        /// <summary>플레이어 행동 대기입니다.</summary>
        [InspectorName("플레이어 행동")] PlayerAction,
        /// <summary>턴 종료 처리 중입니다.</summary>
        [InspectorName("턴 종료")] TurnEnd,
        /// <summary>적 행동 처리 중입니다.</summary>
        [InspectorName("적 행동")] EnemyAction,
        /// <summary>전투가 종료되었습니다.</summary>
        [InspectorName("전투 종료")] BattleEnd,
    }

    /// <summary>
    /// 전투 결과입니다.
    /// </summary>
    public enum EBattleResult
    {
        /// <summary>X입니다.</summary>
        [InspectorName("X")] None,
        /// <summary>승리입니다.</summary>
        [InspectorName("승리")] Victory,
        /// <summary>패배입니다.</summary>
        [InspectorName("패배")] Defeat,
    }

    /// <summary>
    /// 맵 노드 타입입니다.
    /// </summary>
    public enum EMapNodeType
    {
        /// <summary>전투 노드입니다.</summary>
        [InspectorName("전투")] Battle,
        /// <summary>엘리트 전투 노드입니다.</summary>
        [InspectorName("엘리트")] Elite,
        /// <summary>휴식 노드입니다.</summary>
        [InspectorName("휴식")] Rest,
        /// <summary>이벤트 노드입니다.</summary>
        [InspectorName("이벤트")] Event,
        /// <summary>상점 노드입니다.</summary>
        [InspectorName("상점")] Shop,
        /// <summary>보관 노드입니다.</summary>
        [InspectorName("보관")] Storage,
        /// <summary>보스 전투 노드입니다.</summary>
        [InspectorName("보스")] Boss,
    }

    /// <summary>
    /// 거점에서 던전 장면으로 전환할 때 사용할 출발 방식입니다.
    /// </summary>
    public enum EDungeonLaunchMode
    {

        /// <summary>요청 없음 - 던전 씬은 아무것도 하지 않습니다 (씬 단독 테스트).</summary>
        [InspectorName("요청 없음")] None,
        /// <summary>새 던전 출발 - 편성 화면을 거쳐 시작합니다.</summary>
        [InspectorName("새 던전 출발")] NewDungeon,
        /// <summary>저장된 던전 이어하기입니다.</summary>
        [InspectorName("이어하기")] Resume,
    }
}
