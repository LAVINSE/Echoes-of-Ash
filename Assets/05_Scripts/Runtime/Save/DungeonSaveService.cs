using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using EchoesOfAsh.Dungeon;
using SW.Util;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 진행 중인 던전 데이터를 저장하고 불러오며 삭제합니다.
    /// 실제 파일 처리는 GameSaveService에 맡기고 마을 저장 데이터는 그대로 유지합니다.
    /// 저장 버전이 다르면 이전 던전 데이터는 사용하지 않습니다.
    /// </summary>
    public static class DungeonSaveService
    {
        #region 상수
        /// <summary>현재 던전 저장 형식의 버전입니다.</summary>
        public const int CurrentVersion = 1;
        #endregion // 상수

        #region 함수
        /// <summary>
        /// 진행 중인 던전 저장 데이터가 있는지 확인합니다.
        /// </summary>
        /// <returns>저장 데이터가 있으면 true입니다.</returns>
        public static bool HasSave()
        {
            return GameSaveService.Current.hasDungeon;
        }

        /// <summary>
        /// 진행 중인 던전 저장 데이터를 삭제합니다. 마을 저장 데이터는 유지합니다.
        /// </summary>
        public static void DeleteSave()
        {
            GameSaveData gameData = GameSaveService.Current;
            gameData.hasDungeon = false;
            gameData.dungeon = new DungeonSaveData();

            GameSaveService.Save();
        }

        /// <summary>
        /// 현재 던전 상태를 저장합니다.
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
                hasMadnessEventOccurred = dungeonState.HasMadnessEventOccurred,
                gold = dungeonState.Gold,
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

            // 아직 마을로 옮기지 않은 소지 아이템을 기록합니다.
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
            
            // 유물 효과 실행 순서를 유지하도록 현재 목록 순서대로 기록합니다.
            foreach (RelicData relic in dungeonState.Relics)
            {
                if (relic != null)
                {
                    saveData.relicCodeNames.Add(relic.CodeName);
                }
            }

            GameSaveData gameData = GameSaveService.Current;
            gameData.dungeon = saveData;
            gameData.hasDungeon = true;

            return GameSaveService.Save();
        }

        /// <summary>
        /// 던전 저장 데이터를 읽어옵니다. 저장 버전이 다르면 사용하지 않습니다.
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

            if (gameData.dungeon.version != CurrentVersion)
            {
                SWLog.LogWarning($"[DungeonSaveService] 던전 저장 버전({gameData.dungeon.version})이 현재 버전({CurrentVersion})과 다릅니다 - 스냅샷을 폐기합니다.");
                DeleteSave();
                return null;
            }

            return gameData.dungeon;
        }
        #endregion // 함수
    }
}
