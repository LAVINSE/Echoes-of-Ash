using System;
using System.Collections.Generic;
using EchoesOfAsh.Data;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Util;

namespace EchoesOfAsh.Sanity
{
    /// <summary>
    /// 파티 정신력에 따라 정신력 이벤트를 실행합니다.
    /// 파티가 광기 상태인 채로 턴을 시작하면 던전마다 한 번 이벤트가 발생합니다.
    /// 이벤트 발생 여부는 전달받은 조회 및 기록 동작으로 던전 상태에 반영합니다.
    /// 턴 도중 광기에 진입해도 같은 턴에 회복하면 이벤트를 실행하지 않습니다.
    /// 전투 밖에서 광기에 진입하면 다음 전투의 첫 턴에 이벤트를 실행합니다.
    /// </summary>
    public class SanityEventRunner
    {
        #region 필드
        private readonly ISanityHolder partySanity;
        private readonly EffectExecutor effectExecutor;
        private readonly IReadOnlyList<SanityEventData> sanityEvents;

        /// <summary>이번 던전에서 정신력 이벤트가 이미 발생했는지 조회합니다 (던전당 1회 규칙).</summary>
        private readonly Func<bool> hasOccurred;
        /// <summary>정신력 이벤트 발생을 던전 상태에 기록합니다.</summary>
        private readonly Action markOccurred;

        /// <summary>파티원 목록입니다. 효과 대상은 판정 시점의 첫 생존자입니다.</summary>
        private readonly IReadOnlyList<ITargetable> partyMembers;
        /// <summary>효과를 받을 파티원을 잠시 담는 목록입니다.</summary>
        private readonly List<ITargetable> selfTargetBuffer = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>정신력 이벤트 발동 시 호출됩니다 (발동한 이벤트, 긍정 효과 적용 여부).</summary>
        public event Action<SanityEventData, bool> OnSanityEventTriggered;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 정신력 이벤트 실행에 필요한 정보를 설정합니다.
        /// </summary>
        /// <param name="partySanity">파티 공유 정신력입니다.</param>
        /// <param name="effectExecutor">효과 실행기입니다.</param>
        /// <param name="sanityEvents">사용할 정신력 이벤트 목록입니다.</param>
        /// <param name="partyMembers">파티원 목록입니다.</param>
        /// <param name="hasOccurred">이번 던전의 정신력 이벤트 발생 여부 조회입니다.</param>
        /// <param name="markOccurred">정신력 이벤트 발생 기록입니다.</param>
        public SanityEventRunner(ISanityHolder partySanity, EffectExecutor effectExecutor,
            IReadOnlyList<SanityEventData> sanityEvents, IReadOnlyList<ITargetable> partyMembers,
            Func<bool> hasOccurred, Action markOccurred)
        {
            if (partySanity == null || effectExecutor == null || partyMembers == null
                || hasOccurred == null || markOccurred == null)
            {
                SWLog.LogError("[SanityEventRunner] 생성 실패: 의존성 중 null이 있습니다");
            }

            this.partySanity = partySanity;
            this.effectExecutor = effectExecutor;
            this.sanityEvents = sanityEvents;
            this.partyMembers = partyMembers;
            this.hasOccurred = hasOccurred;
            this.markOccurred = markOccurred;
        }
        #endregion // 생성자

        #region 판정
        /// <summary>
        /// 턴이 시작될 때 정신력 이벤트 발생 여부를 확인합니다.
        /// 광기 구간이 아니거나 이번 던전에서 이미 발생했으면 아무 일도 하지 않습니다.
        /// </summary>
        /// <param name="turnNumber">현재 턴 번호입니다.</param>
        public void HandleTurnStartHook(int turnNumber)
        {
            if (partySanity == null || partySanity.CurrentSanityType != ESanityType.Madness)
            {
                return;
            }

            // 던전당 1회 - 이미 발생했으면 다시 발동하지 않습니다
            if (hasOccurred == null || hasOccurred())
            {
                return;
            }

            if (sanityEvents == null || sanityEvents.Count == 0)
            {
                return;
            }

            // 설정된 목록에서 이벤트 하나를 무작위로 선택합니다.
            SanityEventData sanityEvent = sanityEvents[SWRandom.Range(0, sanityEvents.Count)];

            if (sanityEvent == null)
            {
                SWLog.LogError("[SanityEventRunner] 이벤트 풀에 null 항목이 있습니다");
                return;
            }

            ExecuteEvent(sanityEvent, turnNumber);
        }

        /// <summary>
        /// 이벤트 내부 분기를 판정하고 해당 효과 블록을 실행합니다.
        /// 긍정 이벤트면 weight 확률로 긍정 효과, 그 외는 부정 효과입니다 (다키스트 던전의 기인/붕괴 분기 대응).
        /// </summary>
        /// <param name="sanityEvent">발동한 이벤트입니다.</param>
        /// <param name="turnNumber">현재 턴 번호입니다.</param>
        private void ExecuteEvent(SanityEventData sanityEvent, int turnNumber)
        {
            bool isPositive = sanityEvent.IsPositiveEffect && SWRandom.Chance(sanityEvent.Weight);

            IReadOnlyList<EffectBlock> effectBlocks = isPositive
                ? sanityEvent.PositiveEffects
                : sanityEvent.Effects;

            if (effectBlocks == null || effectBlocks.Count == 0)
            {
                SWLog.LogError($"[SanityEventRunner] '{sanityEvent.DisplayName}': 실행할 효과가 비어 있습니다");
                return;
            }

            ITargetable eventTarget = GetFirstAliveMember();

            if (eventTarget == null)
            {
                return;
            }

            // 발생 확정 기록 - 효과가 정신력을 다시 바꿔도 재발동하지 않습니다 (던전당 1회)
            markOccurred?.Invoke();

            SWLog.Log($"[SanityEventRunner] 턴 {turnNumber} 정신력 이벤트 발동: '{sanityEvent.DisplayName}' ({(isPositive ? "긍정" : "부정")})");

            selfTargetBuffer.Clear();
            selfTargetBuffer.Add(eventTarget);

            effectExecutor.Execute(effectBlocks, eventTarget, selfTargetBuffer);
            OnSanityEventTriggered?.Invoke(sanityEvent, isPositive);
        }

        /// <summary>
        /// 정신력 이벤트를 실행할 첫 번째 생존 파티원을 반환합니다. 모두 사망했으면 null을 반환합니다.
        /// </summary>
        /// <returns>첫 생존 파티원입니다.</returns>
        private ITargetable GetFirstAliveMember()
        {
            if (partyMembers == null)
            {
                return null;
            }

            foreach (ITargetable member in partyMembers)
            {
                if (member != null && member.IsTargetable)
                {
                    return member;
                }
            }

            return null;
        }
        #endregion // 판정
    }
}
