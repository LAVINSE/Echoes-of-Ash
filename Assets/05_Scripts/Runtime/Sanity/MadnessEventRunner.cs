using System;
using System.Collections.Generic;
using EchoesOfAsh.Data;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Sanity
{
    /// <summary>
    /// 정신력 광기 이벤트 러너
    /// </summary>
    public class MadnessEventRunner
    {
        #region 필드
        private readonly ISanityHolder partySanity;
        private readonly EffectExecutor effectExecutor;
        private readonly BattleBalanceData balanceData;
        private readonly IReadOnlyList<SanityEventData> sanityEvents;

        private readonly ITargetable caster;
        private readonly IReadOnlyList<ITargetable> selfTargets;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>광기 이벤트 발동 시 호출 (발동한 이벤트, 긍정 효과 적용 여부)</summary>
        public event Action<SanityEventData, bool> OnMadnessEventTriggered;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 광기 이벤트 러너를 생성합니다
        /// </summary>
        /// <param name="partySanity">파티 공유 정신력</param>
        /// <param name="effectExecutor">효과 실행기</param>
        /// <param name="balanceData">전투 규칙 데이터</param>
        /// <param name="sanityEvents">광기 이벤트 풀</param>
        /// <param name="caster">효과 시전/대상이 될 파티원</param>
        public MadnessEventRunner(ISanityHolder partySanity, EffectExecutor effectExecutor, BattleBalanceData balanceData,
            IReadOnlyList<SanityEventData> sanityEvents, ITargetable caster)
        {
            if (partySanity == null || effectExecutor == null || balanceData == null || caster == null)
            {
                SWLog.LogError("[MadnessEventRunner] 생성 실패: 의존성 중 null이 있습니다");
            }

            this.partySanity = partySanity;
            this.effectExecutor = effectExecutor;
            this.balanceData = balanceData;
            this.sanityEvents = sanityEvents;

            this.caster = caster;
            this.selfTargets = new[] { caster };
        }
        #endregion // 생성자

        #region 판정
        /// <summary>
        /// 턴 시작 훅 처리 - TurnManager.OnTurnStartHook에 구독됩니다
        /// 광기 구간이 아니거나 판정에 실패하면 아무 일도 하지 않습니다
        /// </summary>
        /// <param name="turnNumber">현재 턴 번호</param>
        public void HandleTurnStartHook(int turnNumber)
        {
            if (partySanity == null || partySanity.CurrentSanityType != ESanityType.Madness)
            {
                return;
            }

            if (sanityEvents == null || sanityEvents.Count == 0)
            {
                return;
            }

            // 발동 판정 - SAN이 낮을수록 확률 증가 (곡선은 Balance 소유)
            float chance = balanceData != null
                ? balanceData.GetMadnessEventChance(partySanity.CurrentSanity, partySanity.SanityThreshold)
                : 0f;

            if (!SWRandom.Chance(chance))
            {
                return;
            }

            // 풀에서 무작위 1건 선택 (SWRandom 일원화 - 시드 결정성 유지)
            SanityEventData sanityEvent = sanityEvents[SWRandom.Range(0, sanityEvents.Count)];

            if (sanityEvent == null)
            {
                SWLog.LogError("[MadnessEventRunner] 이벤트 풀에 null 항목이 있습니다");
                return;
            }

            ExecuteEvent(sanityEvent, turnNumber);
        }

        /// <summary>
        /// 이벤트 내부 분기를 판정하고 해당 효과 블록을 실행합니다
        /// 긍정 이벤트면 weight 확률로 긍정 효과, 그 외는 부정 효과
        /// </summary>
        /// <param name="sanityEvent">발동한 이벤트</param>
        /// <param name="turnNumber">현재 턴 번호</param>
        private void ExecuteEvent(SanityEventData sanityEvent, int turnNumber)
        {
            bool isPositive = sanityEvent.IsPositiveEffect && SWRandom.Chance(sanityEvent.Weight);

            IReadOnlyList<EffectBlock> effectBlocks = isPositive
                ? sanityEvent.PositiveEffects
                : sanityEvent.Effects;

            if (effectBlocks == null || effectBlocks.Count == 0)
            {
                SWLog.LogError($"[MadnessEventRunner] '{sanityEvent.DisplayName}': 실행할 효과가 비어 있습니다");
                return;
            }

            SWLog.Log($"[MadnessEventRunner] 턴 {turnNumber} 광기 이벤트 발동: '{sanityEvent.DisplayName}' ({(isPositive ? "긍정" : "부정")})");

            effectExecutor.Execute(effectBlocks, caster, selfTargets);
            OnMadnessEventTriggered?.Invoke(sanityEvent, isPositive);
        }
        #endregion // 판정
    }
}