using EchoesOfAsh.Card;
using EchoesOfAsh.Dungeon;
using SW.Data;
using SW.Util;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 던전 스냅샷의 저장, 로드, 마이그레이션을 담당합니다.
    /// SWSaveDataManager의 "dungeon" 슬롯을 사용합니다.
    /// </summary>
    public static class DungeonSaveService
    {
        #region 상수
        /// <summary>던전 스냅샷 저장 슬롯 이름입니다. 메타 저장(해금/거점)은 별도 슬롯으로 분리 예정입니다.</summary>
        public const string SaveSlot = "dungeon";
        /// <summary>현재 저장 스키마 버전입니다.</summary>
        public const int CurrentVersion = 1;
        #endregion // 상수

        #region 함수
        /// <summary>
        /// 던전 저장 파일이 있는지 확인합니다.
        /// </summary>
        /// <returns>저장 파일이 있으면 true입니다.</returns>
        public static bool HasSave()
        {
            return SWSaveDataManager.HasSave(SaveSlot);
        }

        /// <summary>
        /// 던전 저장 파일을 삭제합니다. 던전 종료 시 호출합니다.
        /// </summary>
        public static void DeleteSave()
        {
            SWSaveDataManager.Delete(SaveSlot);
        }

        /// <summary>
        /// 던전 상태를 스냅샷으로 저장합니다.
        /// </summary>
        /// <param name="dungeonState">저장할 던전 상태입니다.</param>
        /// <returns>저장에 성공했으면 true입니다.</returns>
        public static bool Save(DungeonState dungeonState)
        {
            if (dungeonState == null || dungeonState.MapGraph == null)
            {
                SWLog.LogError("[DungeonSaveService] Save 실패: 던전 상태 또는 맵이 없습니다.");
                return false;
            }

            DungeonSaveData saveData = new()
            {
                version = CurrentVersion,
                seed = dungeonState.Seed,
                currentNodeIdentifier = dungeonState.CurrentNodeIdentifier,
                isCurrentNodeResolved = dungeonState.IsCurrentNodeResolved,
                carriedSanity = dungeonState.CarriedSanity,
                moveCount = dungeonState.MoveCount,
                ashConsumedFloor = dungeonState.AshConsumedFloor,
            };

            saveData.mapNodes.AddRange(dungeonState.MapGraph.Nodes);
            saveData.mapEdges.AddRange(dungeonState.MapGraph.Edges);

            foreach (CardInstance card in dungeonState.Deck)
            {
                if (card == null || card.CardData == null)
                {
                    continue;
                }

                saveData.deckCards.Add(new DungeonCardSaveData
                {
                    cardCodeName = card.CardData.CodeName,
                    isUpgrade = card.IsUpgrade,
                });
            }

            SWSaveDataManager.SetData(saveData);
            return SWSaveDataManager.SaveAll(null, SaveSlot, false, true, false);
        }

        /// <summary>
        /// 던전 스냅샷을 로드하고 마이그레이션을 적용합니다.
        /// </summary>
        /// <returns>로드한 저장 데이터입니다. 실패하면 null입니다.</returns>
        public static DungeonSaveData Load()
        {
            if (!HasSave())
            {
                SWLog.LogWarning("[DungeonSaveService] Load 실패: 저장 파일이 없습니다.");
                return null;
            }

            SWSaveDataManager.SetData(new DungeonSaveData());

            bool isLoaded = false;
            SWSaveDataManager.LoadAll(success => isLoaded = success, SaveSlot, false);

            if (!isLoaded)
            {
                SWLog.LogError("[DungeonSaveService] Load 실패: 저장 파일을 읽지 못했습니다.");
                return null;
            }

            DungeonSaveData saveData = SWSaveDataManager.GetData<DungeonSaveData>();

            if (saveData == null || !Migrate(saveData))
            {
                return null;
            }

            return saveData;
        }

        /// <summary>
        /// 저장 데이터를 현재 스키마 버전으로 순차 마이그레이션합니다.
        /// </summary>
        /// <param name="saveData">마이그레이션할 저장 데이터입니다.</param>
        /// <returns>현재 버전으로 변환했으면 true입니다.</returns>
        private static bool Migrate(DungeonSaveData saveData)
        {
            if (saveData.version == CurrentVersion)
            {
                return true;
            }

            if (saveData.version > CurrentVersion)
            {
                SWLog.LogError($"[DungeonSaveService] 마이그레이션 실패: 저장 버전 {saveData.version}이 현재 버전 {CurrentVersion}보다 높습니다.");
                return false;
            }

            // 버전별 순차 마이그레이션 - v1이 초판이므로 현재 케이스 없음
            // 예: if (saveData.version == 1) { 신규 필드 기본값 채움; saveData.version = 2; }

            SWLog.LogError($"[DungeonSaveService] 마이그레이션 실패: 지원하지 않는 저장 버전 {saveData.version}입니다.");
            return false;
        }
        #endregion // 함수
    }
}