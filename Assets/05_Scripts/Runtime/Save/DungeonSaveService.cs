using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using EchoesOfAsh.Dungeon;
using SW.Util;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 던전 스냅샷 구획의 기록, 판독, 소멸을 담당합니다.
    /// 파일 입출력은 GameSaveService에 위임합니다 - 던전 소멸은 파일 삭제가 아니라 깃발 하강이며 거점 구획은 보존됩니다.
    /// 잠정 규칙: 개발 중에는 마이그레이션 없이 버전 불일치 = 폐기합니다 (데이터 보존 시작 시점에 계층 복원 — 기획서 15-5).
    /// </summary>
    public static class DungeonSaveService
    {
        #region 상수
        /// <summary>현재 저장 스키마 버전입니다. 구버전 스냅샷을 강제 폐기하고 싶을 때만 증가시킵니다.</summary>
        public const int CURRENT_VERSION = 1;
        #endregion // 상수

        #region 함수
        /// <summary>
        /// 진행 중인 던전 스냅샷이 있는지 확인합니다.
        /// </summary>
        /// <returns>스냅샷이 있으면 true입니다.</returns>
        public static bool HasSave()
        {
            return GameSaveService.Current.hasDungeon;
        }

        /// <summary>
        /// 던전 스냅샷을 소멸시킵니다. 던전 종료 시 호출합니다. 거점 구획은 보존됩니다.
        /// </summary>
        public static void DeleteSave()
        {
            GameSaveData gameData = GameSaveService.Current;
            gameData.hasDungeon = false;
            gameData.dungeon = new DungeonSaveData();

            GameSaveService.Save();
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
                version = CURRENT_VERSION,
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

            // 파티 구성 기록 (편성 화면 도입분)
            foreach (CharacterData characterData in dungeonState.CharacterDatas)
            {
                if (characterData != null)
                {
                    saveData.partyCharacterCodeNames.Add(characterData.CodeName);
                }
            }

            // 소지 드랍 기록 (P2-M6 - 회수 판정 전까지의 임시 보유분)
            foreach (ItemStackData stack in dungeonState.CarriedItems)
            {
                if (stack == null || stack.ItemData == null)
                {
                    continue;
                }

                saveData.carriedItems.Add(new ItemCountSaveData
                {
                    codeName = stack.ItemData.CodeName,
                    count = stack.Count,
                });
            }

            GameSaveData gameData = GameSaveService.Current;
            gameData.dungeon = saveData;
            gameData.hasDungeon = true;

            return GameSaveService.Save();
        }

        /// <summary>
        /// 던전 스냅샷을 읽어옵니다. 버전이 다르면 폐기합니다 (개발 중 잠정 규칙 - 마이그레이션 없음).
        /// </summary>
        /// <returns>읽어온 저장 데이터입니다. 실패하면 null입니다.</returns>
        public static DungeonSaveData Load()
        {
            GameSaveData gameData = GameSaveService.Current;

            if (!gameData.hasDungeon || gameData.dungeon == null)
            {
                SWLog.LogWarning("[DungeonSaveService] Load 실패: 진행 중인 던전 스냅샷이 없습니다.");
                return null;
            }

            if (gameData.dungeon.version != CURRENT_VERSION)
            {
                SWLog.LogWarning($"[DungeonSaveService] 던전 저장 버전({gameData.dungeon.version})이 현재 버전({CURRENT_VERSION})과 다릅니다 - 스냅샷을 폐기합니다.");
                DeleteSave();
                return null;
            }

            return gameData.dungeon;
        }
        #endregion // 함수
    }
}