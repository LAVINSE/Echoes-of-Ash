using System;
using System.Collections.Generic;
using EchoesOfAsh.Interface;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Effect
{
    /// <summary>
    /// EffectContext 조립 + 효과 블록 목록 순회 실행
    /// </summary>
    public class EffectExecutor
    {
        #region 필드
        private static readonly IReadOnlyList<ITargetable> emptyTargets = Array.Empty<ITargetable>();

        private readonly ISanityHolder partySanity;
        private readonly Action<int> drawRequest;
        private readonly Action<int> discardRequest;
        private readonly Action<int> apChangeRequest;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 효과 실행기를 생성합니다.
        /// </summary>
        /// <param name="partySanity">파티 공유 정신력입니다.</param>
        /// <param name="drawRequest">카드 드로우 요청입니다.</param>
        /// <param name="discardRequest">카드 버림 요청입니다.</param>
        /// <param name="apChangeRequest">행동력 변화 요청입니다.</param>
        public EffectExecutor(ISanityHolder partySanity, Action<int> drawRequest, Action<int> discardRequest, Action<int> apChangeRequest)
        {
            if (partySanity == null)
            {
                SWLog.LogError("[EffectExecutor] 생성 실패: 파티 정신력이 null입니다");
            }

            this.partySanity = partySanity;
            this.drawRequest = drawRequest;
            this.discardRequest = discardRequest;
            this.apChangeRequest = apChangeRequest;
        }
        #endregion // 생성자
        /// <summary>
        /// 효과 블록 목록을 순서대로 실행합니다.
        /// </summary>
        /// <param name="effectBlocks">실행할 효과 블록 목록입니다.</param>
        /// <param name="caster">시전자입니다.</param>
        /// <param name="targets">효과 대상 목록입니다.</param>
        public void Execute(IReadOnlyList<EffectBlock> effectBlocks, ITargetable caster, IReadOnlyList<ITargetable> targets)
        {
            if (effectBlocks == null || effectBlocks.Count == 0)
            {
                SWLog.LogError("[EffectExecutor] Execute 실패: 효과 블록이 비어 있습니다");
                return;
            }

            EffectContext context = new()
            {
                Caster = caster,
                CasterDamageable = caster as IDamageable,
                PartySanity = partySanity,
                Targets = targets ?? emptyTargets,
                DrawRequest = drawRequest,
                DiscardRequest = discardRequest,
                ApChangeRequest = apChangeRequest
            };

            foreach(var effectBlock in effectBlocks)
            {
                if (effectBlock == null)
                {
                    SWLog.LogWarning("[EffectExecutor] null 효과 블록을 건너뜁니다");
                    continue;
                }

                effectBlock.Apply(context);
            }
        }
    }
}
