using System.Collections.Generic;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 던전 1개의 챕터 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonChapterData", menuName = "EchoesOfAsh/Data/DungeonChapter")]
    public class DungeonChapterData : SWScriptableObject
    {
        #region 데이터
        /// <summary>
        /// 노드 타입 하나에 연결되는 이벤트 풀
        /// </summary>
        [System.Serializable]
        public class EventNodePoolEntry
        {
            [Tooltip("이벤트 풀을 연결할 노드 타입입니다. 전투 계열(전투/엘리트/보스)은 사용할 수 없습니다.")]
            [SerializeField] private EMapNodeType nodeType;
            [Tooltip("이 노드 타입에서 표시할 이벤트 풀입니다. 1개면 고정, 복수면 무작위로 선택합니다.")]
            [SerializeField] private List<DungeonEventData> eventDatas = new();

            /// <summary>이벤트 풀을 연결할 노드 타입입니다.</summary>
            public EMapNodeType NodeType => nodeType;
            /// <summary>이 노드 타입에서 표시할 이벤트 풀입니다.</summary>
            public IReadOnlyList<DungeonEventData> EventPool => eventDatas;
        }
        #endregion // 데이터

        #region 필드
        [SWGroup("맵")]
        [Tooltip("이 챕터의 맵 생성 규칙입니다.")]
        [SerializeField] private MapConfigData mapConfigData;

        [SWGroup("적 데이터")]
        [Tooltip("일반 전투 노드에서 무작위로 선택할 풀입니다.")]
        [SerializeField] private List<EnemyEncounterData> battleEncounters = new();
        [Tooltip("엘리트 노드의 조우 풀입니다. 비어 있으면 일반 풀로 폴백합니다.")]
        [SerializeField] private List<EnemyEncounterData> eliteEncounters = new();
        [Tooltip("보스 노드의 조우 풀입니다. 비어 있으면 일반 풀로 폴백합니다.")]
        [SerializeField] private List<EnemyEncounterData> bossEncounters = new();

        [SWGroup("노드 이벤트")]
        [Tooltip("노드 타입과 이벤트 풀의 매핑 목록입니다. 미등록 타입의 노드는 통과 처리됩니다.")]
        [SerializeField] private List<EventNodePoolEntry> nodeEventPools = new();

        [SWGroup("정신력")]
        [Tooltip("이 챕터에서 발생할 수 있는 정신력 이벤트 풀입니다.")]
        [SerializeField] private List<SanityEventData> sanityEventDatas = new();

        [SWGroup("상태이상")]
        [Tooltip("이 챕터의 전투에서 유효한 상태이상 정의 목록입니다.")]
        [SerializeField] private List<StatusEffectData> statusDatas = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 챕터의 맵 생성 규칙입니다.</summary>
        public MapConfigData MapConfigData => mapConfigData;
        /// <summary>이 챕터의 정신력 이벤트 풀입니다.</summary>
        public IReadOnlyList<SanityEventData> SanityEventDatas => sanityEventDatas;
        /// <summary>이 챕터의 상태이상 정의 목록입니다.</summary>
        public IReadOnlyList<StatusEffectData> StatusDatas => statusDatas;
        #endregion // 프로퍼티

        #region 조회
        /// <summary>
        /// 노드 타입에 맞는 조우 풀에서 조우를 무작위로 선택합니다.
        /// 엘리트와 보스는 전용 풀을 우선 사용하고, 비어 있으면 일반 조우 풀로 폴백합니다.
        /// </summary>
        /// <param name="nodeType">진입한 전투 노드의 타입입니다.</param>
        /// <returns>선택한 조우 데이터입니다. 사용할 수 있는 조우가 없으면 null입니다.</returns>
        public EnemyEncounterData GetRandomEncounter(EMapNodeType nodeType)
        {
            List<EnemyEncounterData> pool = SelectEncounterPool(nodeType);

            if (pool == null || pool.Count == 0)
            {
                SWLog.LogError($"[DungeonChapterData] '{name}': {nodeType} 노드에 사용할 조우 풀이 비어 있습니다.");
                return null;
            }

            return pool[SWRandom.Range(0, pool.Count)];
        }

        /// <summary>
        /// 노드 타입에 매핑된 이벤트 풀에서 이벤트를 선택합니다. 풀 1개 = 고정, 복수 = 무작위입니다.
        /// </summary>
        /// <param name="nodeType">진입한 노드의 타입입니다.</param>
        /// <returns>선택한 이벤트 데이터입니다. 매핑이 없거나 풀이 비어 있으면 null (통과 처리)입니다.</returns>
        public DungeonEventData GetRandomEventData(EMapNodeType nodeType)
        {
            foreach (EventNodePoolEntry entry in nodeEventPools)
            {
                if (entry == null || entry.NodeType != nodeType)
                {
                    continue;
                }

                if (entry.EventPool.Count == 0)
                {
                    return null;
                }

                return entry.EventPool.Count == 1
                    ? entry.EventPool[0]
                    : entry.EventPool[SWRandom.Range(0, entry.EventPool.Count)];
            }

            return null;
        }

        /// <summary>
        /// 노드 타입에 맞는 조우 풀을 반환합니다. 엘리트와 보스 풀이 비어 있으면 일반 풀로 폴백합니다.
        /// </summary>
        /// <param name="nodeType">진입한 전투 노드의 타입입니다.</param>
        /// <returns>사용할 조우 풀입니다.</returns>
        private List<EnemyEncounterData> SelectEncounterPool(EMapNodeType nodeType)
        {
            switch (nodeType)
            {
                case EMapNodeType.Elite:
                    if (eliteEncounters.Count == 0)
                    {
                        SWLog.LogWarning($"[DungeonChapterData] '{name}': 엘리트 조우 풀이 비어 있어 일반 조우 풀로 폴백합니다.");
                        return battleEncounters;
                    }

                    return eliteEncounters;

                case EMapNodeType.Boss:
                    if (bossEncounters.Count == 0)
                    {
                        SWLog.LogWarning($"[DungeonChapterData] '{name}': 보스 조우 풀이 비어 있어 일반 조우 풀로 폴백합니다.");
                        return battleEncounters;
                    }

                    return bossEncounters;

                default:
                    return battleEncounters;
            }
        }
        #endregion // 조회

        #region 에디터
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (mapConfigData == null)
            {
                SWLog.LogWarning($"[DungeonChapterData] '{name}': 맵 생성 규칙 에셋이 비어 있습니다.");
            }

            if (battleEncounters.Count == 0)
            {
                SWLog.LogWarning($"[DungeonChapterData] '{name}': 일반 조우 풀이 비어 있습니다.");
            }

            ValidateNodeEventPools();
        }

        /// <summary>
        /// 노드 이벤트 매핑의 중복 타입과 전투 계열 타입을 검사합니다 (개정 11 결정).
        /// </summary>
        private void ValidateNodeEventPools()
        {
            List<EMapNodeType> mappedTypes = new();

            foreach (EventNodePoolEntry entry in nodeEventPools)
            {
                if (entry == null)
                {
                    continue;
                }

                bool isBattleType = entry.NodeType == EMapNodeType.Battle
                    || entry.NodeType == EMapNodeType.Elite
                    || entry.NodeType == EMapNodeType.Boss;

                if (isBattleType)
                {
                    SWLog.LogWarning($"[DungeonChapterData] '{name}': 전투 계열 타입({entry.NodeType})은 이벤트 매핑에 사용할 수 없습니다.");
                    continue;
                }

                if (mappedTypes.Contains(entry.NodeType))
                {
                    SWLog.LogWarning($"[DungeonChapterData] '{name}': 이벤트 매핑에 {entry.NodeType} 타입이 중복 등록되었습니다.");
                    continue;
                }

                mappedTypes.Add(entry.NodeType);
            }
        }
#endif
        #endregion // 에디터
    }
}