using System.Collections.Generic;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 카드 대상 지정 방식에 따라 실제 대상 목록을 구성한다
    /// </summary>
    public class TargetResolver
    {
        #region 필드
        private readonly List<EnemyEntity> targetableBuffer = new();
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        /// <summary>
        /// 대상 지정 방식에 따라 대상 목록을 구성한다
        /// </summary>
        /// <param name="targetingType">대상 지정 방식</param>
        /// <param name="caster">시전자</param>
        /// <param name="target">다인 댕상 카드에서 드래그로 지정한 대상</param>
        /// <param name="enemies">적 목록</param>
        /// <param name="results">결과를 담을 목록</param>
        /// <returns>구성 성공 여부</returns>
        public bool Resolve(ETargetingType targetingType, ITargetable caster, ITargetable target,
         IReadOnlyList<EnemyEntity> enemies, List<ITargetable> results)
        {
            if (results == null)
            {
                SWLog.LogError("[TargetingResolver] Resolve 실패: 결과 목록이 null입니다");
                return false;
            }

            results.Clear();

            switch (targetingType)
            {
                case ETargetingType.Single:
                    return ResolveSingle(target, results);
                case ETargetingType.AllEnemies:
                    return ResolveAllEnemies(enemies, results);
                case ETargetingType.RandomEnemy:
                    return ResolveRandomEnemy(enemies, results);
                case ETargetingType.Self:
                    return ResolveSelf(caster, results);
                default:
                    SWLog.LogError($"[TargetingResolver] Resolve 실패: 지원하지 않는 대상 지정 방식({targetingType})입니다");
                    return false;
            }
        }

        #region 타입별 구성
        /// <summary>
        /// 단일 대상 (드래그 지정) 목록을 구성
        /// </summary>
        /// <param name="target">지정된 대상</param>
        /// <param name="results">결과 목록</param>
        /// <returns>구성 성공 여부</returns>
        private bool ResolveSingle(ITargetable target, List<ITargetable> results)
        {
            if (target == null || !target.IsTargetable)
            {
                SWLog.LogError("[TargetResolver] 단일 대상 구성 실패: 지정 대상이 없거나 대상 지정이 불가능합니다");
                return false;
            }

            results.Add(target);
            return true;
        }

        /// <summary>
        /// 적 전체 대상 목록을 구성
        /// </summary>
        /// <param name="enemies">적 목록</param>
        /// <param name="results">결과 목록</param>
        /// <returns>구성 성공 여부</returns>
        private bool ResolveAllEnemies(IReadOnlyList<EnemyEntity> enemies, List<ITargetable> results)
        {
            CollectTargetable(enemies);

            if (targetableBuffer.Count == 0)
            {
                SWLog.LogError("[TargetingResolver] 적 전체 구성 실패: 대상 지정 가능한 적이 없습니다");
                return false;
            }

            foreach (var enemy in targetableBuffer)
            {
                results.Add(enemy);
            }

            return true;
        }

        /// <summary>
        /// 무작위 적 대상 목록을 구성한다
        /// </summary>
        /// <param name="enemies">적 목록</param>
        /// <param name="results">결과 목록</param>
        /// <returns>구성 성공 여부</returns>
        private bool ResolveRandomEnemy(IReadOnlyList<EnemyEntity> enemies, List<ITargetable> results)
        {
            CollectTargetable(enemies);

            if (targetableBuffer.Count == 0)
            {
                SWLog.LogError("[TargetingResolver] 무작위 적 구성 실패: 대상 지정 가능한 적이 없습니다");
                return false;
            }

            results.Add(SWRandom.Pick(targetableBuffer));
            return true;
        }

        /// <summary>
        /// 자신/아군 대상 목록을 구성한다
        /// </summary>
        /// <param name="caster">시전자</param>
        /// <param name="results">결과 목록</param>
        /// <returns>구성 성공 여부</returns>
        private bool ResolveSelf(ITargetable caster, List<ITargetable> results)
        {

            if (caster == null)
            {
                SWLog.LogError("[TargetingResolver] 자신 대상 구성 실패: 시전자가 null입니다");
                return false;
            }

            results.Add(caster);
            return true;
        }
        #endregion // 타입별 구성

        /// <summary>
        /// 대상 지정 가능한 적만 버퍼에 추가
        /// </summary>
        /// <param name="enemies"></param>
        private void CollectTargetable(IReadOnlyList<EnemyEntity> enemies)
        {
            targetableBuffer.Clear();

            if (enemies == null)
            {
                return;
            }
            
            foreach(var enemy in enemies)
            {
                if(enemy != null && enemy.IsTargetable)
                {
                    targetableBuffer.Add(enemy);
                }
            }
        }
    }
}