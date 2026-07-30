using System.Collections.Generic;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Stat;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 적 데이터입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy_", menuName = "EchoesOfAsh/Data/Enemy")]
    public class EnemyData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("타입")]
        [Tooltip("등장 구간")]
        [SerializeField] private Vector2Int spawnRange;
        [SerializeField] private EEnemyType enemyType = EEnemyType.Normal;

        [SWGroup("스탯")]
        [SerializeField] private SWStatOverride maxHpStat;
        [SerializeField] private SWStatOverride maxSanityStat;
        [SerializeField] private bool isOptionalStat;
        [SerializeField, SWCondition("isOptionalStat", true)] private SWStatOverride[] optionalStats;

        [SWGroup("정신력 전환")]
        [Tooltip("정신력 전환 값")]
        [SerializeField, Min(0)] private int sanityThreshold;
        [Tooltip("전투 시작 시 정신력 값")]
        [SerializeField, Min(0)] private int startSanity;

        [SWGroup("대상 선정 규칙")]
        [SerializeField] private EEnemyTargetRuleType targetRuleType = EEnemyTargetRuleType.Random;

        [SWGroup("행동 (일반)")]
        [SerializeField] private List<EnemyActionData> actions = new();

        [SWGroup("행동 (정신력)")]
        [SerializeField] private bool isSanityAction;
        [SerializeField, SWCondition("isSanityAction", true)] private List<EnemyActionData> sanityActions = new();

        [SWGroup("HP 페이즈 전환")]
        [SerializeField] private List<EnemyPhasePatternData> phasePatterns = new();

        [SWGroup("표시")]
        [SerializeField] private Sprite enemyPortraitSprite;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>등장 구간입니다.</summary>
        public Vector2Int SpawnRange => spawnRange;
        /// <summary>적 유형입니다.</summary>
        public EEnemyType EnemyType => enemyType;

        /// <summary>적 최대 HP 능력치입니다.</summary>
        public SWStatOverride MaxHpStat => maxHpStat;
        /// <summary>적 최대 정신력 능력치입니다.</summary>
        public SWStatOverride MaxSanityStat => maxSanityStat;
        /// <summary>추가 능력치 사용 여부입니다.</summary>
        public bool IsOptionalStat => isOptionalStat;

        /// <summary>정신력 전환 값입니다.</summary>
        public int SanityThreshold => sanityThreshold;
        /// <summary>전투 시작 정신력 값입니다.</summary>
        public int StartSanity => startSanity;

        /// <summary>대상 선정 규칙입니다.</summary>
        public EEnemyTargetRuleType TargetRuleType => targetRuleType;

        /// <summary>정신력 반응 여부입니다.</summary>
        public bool IsSanityAction => isSanityAction;

        /// <summary>행동 (일반) 목록입니다.</summary>
        public IReadOnlyList<EnemyActionData> Actions => actions;
        /// <summary>행동 (정신력) 목록입니다.</summary>
        public IReadOnlyList<EnemyActionData> SanityActions => sanityActions;

        /// <summary>체력 단계 전환 목록입니다.</summary>
        public IReadOnlyList<EnemyPhasePatternData> PhasePatterns => phasePatterns;

        /// <summary>적 초상화 스프라이트입니다.</summary>
        public Sprite EnemyPortraitSprite => enemyPortraitSprite;
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        /// <summary>
        /// 적 능력치와 행동 패턴의 필수값을 검증합니다.
        /// </summary>
        private void OnValidate()
        {
            if (maxHpStat == null || maxHpStat.Stat == null)
            {
                SWLog.LogError($"[EnemyData] '{name}': HP 스탯 에셋이 비어 있습니다.");
            }

            if (maxSanityStat == null || maxSanityStat.Stat == null)
            {
                SWLog.LogError($"[EnemyData] '{name}': 정신력 스탯 에셋이 비어 있습니다.");
            }

            if (actions.Count == 0)
            {
                SWLog.LogError($"[EnemyData] '{name}': 행동 패턴이 비어 있습니다.");
            }

            foreach (EnemyActionData action in actions)
            {
                if (action.Effects.Count == 0)
                {
                    SWLog.LogError($"[EnemyData] '{name}': 행동 '{action.ActionName}'의 효과가 비어 있습니다.");
                }
            }
        }
#endif // UNITY_EDITOR
        #endregion // 에디터
    }
}
