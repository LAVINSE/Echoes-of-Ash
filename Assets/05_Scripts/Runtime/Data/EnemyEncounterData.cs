using System.Collections.Generic;
using SW.Attributes;
using SW.Base;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 적 조우 데이터입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyEncounter_", menuName = "EchoesOfAsh/Data/EnemyEncounter")]
    public class EnemyEncounterData : SWIdentifiedObject
    {
        #region 데이터
        /// <summary>
        /// 조우에 등장하는 적 데이터와 배치 정보를 묶는 항목입니다.
        /// </summary>
        [System.Serializable]
        public class EncounterEntry
        {
            [SerializeField] private EnemyData enemyData;
            [SerializeField] private Vector2 spawnPosition;

            /// <summary>등장할 적 데이터입니다.</summary>
            public EnemyData EnemyData => enemyData;
            /// <summary>배치 위치 (enemyRoot 기준 로컬 좌표)입니다.</summary>
            public Vector2 SpawnPosition => spawnPosition;
        }
        #endregion // 데이터

        #region 필드
        [SWGroup("구성")]
        [SerializeField] private List<EncounterEntry> entries = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>적 구성 목록입니다.</summary>
        public IReadOnlyList<EncounterEntry> Entries => entries;
        /// <summary>조우 적 수입니다.</summary>
        public int EnemyCount => entries.Count;
        #endregion // 프로퍼티
    }
}
