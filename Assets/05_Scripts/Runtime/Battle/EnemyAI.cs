using System;
using System.Collections.Generic;
using EchoesOfAsh.Data;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 적 인공지능입니다.
    /// </summary>
    public class EnemyAI
    {
        #region 필드
        private IReadOnlyList<EnemyActionData> currentPatterns;
        private EnemyActionData nextAction;

        private readonly EnemyEntity entity;
        private readonly List<CharacterEntity> targetableBuffer = new();

        private CharacterEntity nextTarget;
        private readonly IReadOnlyList<CharacterEntity> party;

        private readonly AggroSystem aggroSystem;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>적 엔티티입니다.</summary>
        public EnemyEntity Entity => entity;
        /// <summary>다음 턴에 실행할 행동입니다.</summary>
        public EnemyActionData NextAction => nextAction;

        /// <summary>의도 변경 시 호출됩니다.</summary>
        public event Action<EnemyEntity, EnemyActionData> OnIntentChanged;

        /// <summary>예고된 다음 행동의 대상입니다. 파티를 노리지 않는 행동이면 null입니다.</summary>
        public CharacterEntity NextTarget => nextTarget;
        /// <summary>예고 대상 변경 시 호출됩니다.</summary>
        public event Action<EnemyEntity, CharacterEntity> OnTargetChanged;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 적 인공지능를 생성합니다.
        /// </summary>
        /// <param name="entity">제어할 적 엔티티입니다.</param>
        public EnemyAI(EnemyEntity entity, IReadOnlyList<CharacterEntity> party, AggroSystem aggroSystem)
        {
            if (entity == null || entity.EnemyData == null)
            {
                SWLog.LogError("[EnemyAI] 생성 실패: 적 엔티티 또는 EnemyData가 null입니다");
                return;
            }

            this.entity = entity;
            this.party = party;
            this.aggroSystem = aggroSystem;

            EvaluatePattern();
            DecideNextAction();
            PickNextTarget();
        }
        #endregion // 생성자

        #region 다음 턴 준비
        /// <summary>
        /// 다음 턴을 준비합니다.
        /// </summary>
        public void PrepareNextTurn()
        {
            if (entity == null || entity.IsDead)
            {
                return;
            }

            EvaluatePattern();
            DecideNextAction();
            PickNextTarget();
        }
        #endregion // 다음 턴 준비

        #region 행동 실행
        /// <summary>
        /// 예고된 행동을 실행합니다.
        /// </summary>
        /// <returns>실행 성공 여부입니다.</returns>
        public bool PlayAction(EffectExecutor effectExecutor, IReadOnlyList<ITargetable> targets)
        {
            if (effectExecutor == null)
            {
                SWLog.LogError("[EnemyAI] Action 실패: EffectExecutor가 null입니다");
                return false;
            }

            if (entity == null || entity.IsDead)
            {
                return false;
            }

            if (nextAction == null)
            {
                SWLog.LogError($"[EnemyAI] Action 실패: '{entity.DisplayName}' 예고된 행동이 없습니다");
                return false;
            }

            effectExecutor.Execute(nextAction.Effects, entity, targets);

            entity.ActionIndex++;
            return true;
        }
        #endregion // 행동 실행

        #region 대상 선정
        /// <summary>
        /// 실행할 행동의 대상 목록을 구성합니다.
        /// 예고 대상이 생존해 있으면 그대로 사용하고, 아니면 실행 시점에 다시 선정합니다.
        /// </summary>
        /// <param name="results">선정 결과입니다.</param>
        /// <returns>선정 성공 여부입니다.</returns>
        public bool SelectTargets(List<ITargetable> results)
        {
            if (results == null)
            {
                SWLog.LogError("[EnemyAI] SelectTargets 실패: 결과 목록이 null입니다");
                return false;
            }

            results.Clear();

            CharacterEntity target = FindTauntTarget() ?? nextTarget;

            // 예고 대상이 없거나(비공격 행동) 전투불능이면 실행 시점에 선정합니다
            if (target == null || !target.IsTargetable)
            {
                target = PickTargetByRule();
            }

            if (target == null)
            {
                SWLog.LogError($"[EnemyAI] '{entity.DisplayName}' 대상 선정 실패: 대상 지정 가능한 파티원이 없습니다");
                return false;
            }

            results.Add(target);
            return true;
        }

        /// <summary>
        /// 예고된 행동이 파티를 노리면(공격·정신력 타격) 대상을 미리 확정하고 알립니다.
        /// </summary>
        private void PickNextTarget()
        {
            CharacterEntity previousTarget = nextTarget;
            nextTarget = null;

            if (nextAction != null
                && (nextAction.GetIntentDamageValue() > 0 || nextAction.GetIntentSanityPressureValue() > 0))
            {
                nextTarget = PickTargetByRule();
            }

            if (previousTarget != nextTarget)
            {
                OnTargetChanged?.Invoke(entity, nextTarget);
            }
        }

        /// <summary>
        /// 대상 선정 규칙에 따라 파티에서 대상 한 명을 선정합니다.
        /// </summary>
        /// <returns>선정한 파티원입니다. 없으면 null입니다.</returns>
        private CharacterEntity PickTargetByRule()
        {
            CharacterEntity tauntTarget = FindTauntTarget();

            if(tauntTarget != null)
            {
                return tauntTarget;
            }

            targetableBuffer.Clear();

            if (party != null)
            {
                foreach (CharacterEntity member in party)
                {
                    if (member != null && member.IsTargetable)
                    {
                        targetableBuffer.Add(member);
                    }
                }
            }

            if (targetableBuffer.Count == 0)
            {
                return null;
            }

            switch (entity.EnemyData.TargetRuleType)
            {
                case EEnemyTargetRuleType.Random:
                    return SWRandom.Pick(targetableBuffer);
                case EEnemyTargetRuleType.Aggro:
                    CharacterEntity topAggro = aggroSystem?.PickTopAggro(targetableBuffer);
                    return topAggro != null ? topAggro : SWRandom.Pick(targetableBuffer);
                case EEnemyTargetRuleType.Fixed:
                    return targetableBuffer[0];
                default:
                    SWLog.LogError($"[EnemyAI] 대상 선정 실패: 지원하지 않는 규칙({entity.EnemyData.TargetRuleType})입니다");
                    return null;
            }
        }

        /// <summary>
        /// 대상 지정 가능한 파티원 중 도발 상태인 첫 번째를 반환한다.
        /// </summary>
        /// <returns>도발 중인 파티원. 없으면 null</returns>
        private CharacterEntity FindTauntTarget()
        {
            if (party == null)
            {
                return null;
            }

            foreach (CharacterEntity character in party)
            {
                if (character != null && character.IsTargetable && character.GetStatusStack(EStatusEffectType.Taunt) > 0)
                {
                    return character;
                }
            }

            return null;
        }

        /// <summary>
        /// 도발 상태가 바뀌면 다음 공격 대상을 다시 선택합니다.
        /// 도발자로의 교체만 수행하며 무작위 재추첨은 하지 않습니다.
        /// </summary>
        public void RefreshTauntPreview()
        {
            if (entity == null || entity.IsDead || nextAction == null)
            {
                return;
            }

            // 파티를 노리는 행동만 예고 대상을 가집니다 (PickNextTarget과 동일 조건)
            if (nextAction.GetIntentDamageValue() <= 0 && nextAction.GetIntentSanityPressureValue() <= 0)
            {
                return;
            }

            CharacterEntity tauntTarget = FindTauntTarget();

            if (tauntTarget == null || tauntTarget == nextTarget)
            {
                return;
            }

            nextTarget = tauntTarget;
            OnTargetChanged?.Invoke(entity, tauntTarget);
        }
        #endregion // 대상 선정

        #region 패턴
        /// <summary>
        /// 현재 상태에 맞는 패턴을 평가합니다.
        /// </summary>
        private void EvaluatePattern()
        {
            IReadOnlyList<EnemyActionData> selectedPatterns = SelectPatterns();

            if (ReferenceEquals(selectedPatterns, currentPatterns))
            {
                return;
            }

            currentPatterns = selectedPatterns;
            entity.ActionIndex = 0;
        }

        /// <summary>
        /// 우선순위에 따라 사용할 패턴을 선택합니다.
        /// </summary>
        /// <returns>선택한 행동 패턴입니다.</returns>
        private IReadOnlyList<EnemyActionData> SelectPatterns()
        {
            EnemyData data = entity.EnemyData;

            // 1순위: 정신력
            if (entity.CurrentSanityType == ESanityType.Madness && data.IsSanityAction && data.SanityActions.Count > 0)
            {
                return data.SanityActions;
            }

            // 2순위 HP 페이즈 패턴
            float hpRatio = entity.MaxHp > 0 ? (float)entity.CurrentHp / entity.MaxHp : 0f;
            EnemyPhasePatternData phasePatternData = null;

            foreach (EnemyPhasePatternData phase in data.PhasePatterns)
            {
                if (phase == null || phase.ActionPatterns.Count == 0)
                {
                    continue;
                }

                if (hpRatio > phase.HpThresholdRatio)
                {
                    continue;
                }

                if (phasePatternData == null || phase.HpThresholdRatio < phasePatternData.HpThresholdRatio)
                {
                    phasePatternData = phase;
                }
            }

            if (phasePatternData != null)
            {
                return phasePatternData.ActionPatterns;
            }

            // 기본 패턴
            return data.Actions;
        }

        /// <summary>
        /// 현재 패턴에서 다음 행동(의도)을 선정합니다.
        /// </summary>
        private void DecideNextAction()
        {
            if (currentPatterns == null || currentPatterns.Count == 0)
            {
                SWLog.LogError($"[EnemyAI] 의도 선정 실패: '{entity.DisplayName}' 행동 패턴이 비어 있습니다");
                nextAction = null;
                return;
            }

            nextAction = currentPatterns[entity.ActionIndex % currentPatterns.Count];
            OnIntentChanged?.Invoke(entity, nextAction);
        }
        #endregion // 패턴
    }
}
