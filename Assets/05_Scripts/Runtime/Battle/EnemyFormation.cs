using System.Collections.Generic;
using EchoesOfAsh.Data;
using EchoesOfAsh.View;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 적 조우 배치 편집 도구입니다.
    /// 조우 데이터의 적을 씬에 생성해 위치를 조정한 뒤, 조정한 위치를 조우 데이터에 저장합니다.
    /// </summary>
    public class EnemyFormation : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("데이터")]
        [Tooltip("배치를 편집할 적 조우 데이터입니다")]
        [SerializeField] private EnemyEncounterData enemyEncounterData;
        [Tooltip("배치 확인용으로 생성할 적 뷰 프리팹입니다")]
        [SerializeField] private EnemyView enemyViewPrefab;

#if UNITY_EDITOR
        [SWGroup("배치")]
        [Tooltip("적 생성 버튼으로 생성된 적 뷰 목록입니다. 순서 = 조우 항목 순서")]
        [SerializeField] private List<EnemyView> spawnedEnemyViews = new();
#endif // UNITY_EDITOR
        #endregion // 필드

#if UNITY_EDITOR
        #region 유틸리티
        /// <summary>
        /// 조우 데이터의 적을 저장된 배치 위치에 생성합니다. 이미 생성된 적은 제거하고 다시 생성합니다.
        /// 씬에서 위치를 옮긴 뒤 "적 위치 지정"을 누르면 조우 데이터에 저장됩니다.
        /// </summary>
        [SWButton("적 생성")]
        private void SpawnEnemies()
        {
            if (enemyEncounterData == null || enemyViewPrefab == null)
            {
                SWLog.LogError("[EnemyFormation] 적 생성 실패: 조우 데이터 또는 적 뷰 프리팹이 비어 있습니다.");
                return;
            }

            if (enemyEncounterData.EnemyCount == 0)
            {
                SWLog.LogWarning($"[EnemyFormation] '{enemyEncounterData.name}'에 적 항목이 없습니다.");
                return;
            }

            ClearSpawnedEnemies();

            foreach (EnemyEncounterData.EncounterEntry entry in enemyEncounterData.Entries)
            {
                if (entry == null || entry.EnemyData == null)
                {
                    SWLog.LogError($"[EnemyFormation] 적 생성 실패: '{enemyEncounterData.name}'에 적이 비어 있는 항목이 있습니다.");
                    ClearSpawnedEnemies();
                    return;
                }

                EnemyView enemyView = Instantiate(enemyViewPrefab, transform);
                enemyView.name = entry.EnemyData.name;
                enemyView.transform.localPosition = entry.SpawnPosition;

                spawnedEnemyViews.Add(enemyView);
            }

            SWLog.Log($"[EnemyFormation] '{enemyEncounterData.name}'의 적 {spawnedEnemyViews.Count}체를 생성했습니다.");
        }

        /// <summary>
        /// 생성된 적의 현재 위치를 조우 데이터의 배치 위치로 저장하고 에셋에 기록합니다.
        /// </summary>
        [SWButton("적 위치 지정")]
        private void SaveSpawnPositions()
        {
            if (enemyEncounterData == null)
            {
                SWLog.LogError("[EnemyFormation] 위치 저장 실패: 조우 데이터가 비어 있습니다.");
                return;
            }

            IReadOnlyList<EnemyEncounterData.EncounterEntry> entries = enemyEncounterData.Entries;

            if (spawnedEnemyViews.Count != entries.Count)
            {
                SWLog.LogError($"[EnemyFormation] 위치 저장 실패: 생성된 적 수({spawnedEnemyViews.Count})와 조우 항목 수({entries.Count})가 다릅니다 - 적 생성을 다시 해주세요.");
                return;
            }

            // 일부만 저장되는 일이 없도록 쓰기 전에 전체를 먼저 검증합니다.
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index] == null || spawnedEnemyViews[index] == null)
                {
                    SWLog.LogError($"[EnemyFormation] 위치 저장 실패: {index + 1}번째 항목 또는 생성된 적이 없습니다 - 적 생성을 다시 해주세요.");
                    return;
                }
            }

            for (int index = 0; index < entries.Count; index++)
            {
                entries[index].SetSpawnPosition(spawnedEnemyViews[index].transform.localPosition);
            }

            UnityEditor.EditorUtility.SetDirty(enemyEncounterData);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(enemyEncounterData);

            SWLog.Log($"[EnemyFormation] '{enemyEncounterData.name}'에 적 {entries.Count}체의 배치 위치를 저장했습니다.");
        }

        /// <summary>
        /// 생성된 적 뷰를 전부 제거합니다.
        /// </summary>
        private void ClearSpawnedEnemies()
        {
            foreach (EnemyView enemyView in spawnedEnemyViews)
            {
                if (enemyView != null)
                {
                    DestroyImmediate(enemyView.gameObject);
                }
            }

            spawnedEnemyViews.Clear();
        }
        #endregion // 유틸리티
#endif // UNITY_EDITOR
    }
}