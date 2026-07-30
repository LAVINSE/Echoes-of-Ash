using System;
using System.Collections.Generic;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 적 조우 데이터입니다.
    /// 아이템 드랍 테이블과 몬스터 드랍형 카드를 함께 소유합니다 - 드랍의 소유 단위 = 조우 (P2-M7 7-4).
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

        [SWGroup("드랍")]
        [Tooltip("이 조우 승리 시 굴릴 드랍 테이블입니다. 비우면 드랍 없음")]
        [SerializeField] private DropTableData dropTable;
        [Tooltip("이 조우 승리 시 드랍될 수 있는 몬스터 드랍형 카드입니다. 비우면 카드 드랍 없음")]
        [SerializeField] private CardData dropCard;
        [Tooltip("카드 드랍 확률입니다 (조우당 1회 굴림)")]
        [SerializeField, Range(0f, 1f)] private float dropCardChance;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>적 구성 목록입니다.</summary>
        public IReadOnlyList<EncounterEntry> Entries => entries;
        /// <summary>조우 적 수입니다.</summary>
        public int EnemyCount => entries.Count;

        /// <summary>승리 시 굴릴 드랍 테이블입니다. 없으면 null입니다.</summary>
        public DropTableData DropTable => dropTable;
        /// <summary>승리 시 드랍될 수 있는 몬스터 드랍형 카드입니다. 없으면 null입니다.</summary>
        public CardData DropCard => dropCard;
        /// <summary>카드 드랍 확률입니다.</summary>
        public float DropCardChance => dropCardChance;
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        /// <summary>
        /// 드랍 카드의 해금 방식 정합을 검증합니다 (ItemData.unlockCard 검사 전례).
        /// </summary>
        private void OnValidate()
        {
            if (dropCard != null && dropCard.UnlockType != ECardUnlockType.EnemyDrop)
            {
                SWLog.LogWarning($"[EnemyEncounterData] '{name}': 드랍 카드가 몬스터 드랍형이 아닙니다 - {dropCard.name} (unlockType을 확인하세요)");
            }

            if (dropCard != null && dropCardChance <= 0f)
            {
                SWLog.LogWarning($"[EnemyEncounterData] '{name}': 드랍 카드가 연결되어 있지만 드랍 확률이 0입니다.");
            }
        }
#endif // UNITY_EDITOR
        #endregion // 에디터
    }
}