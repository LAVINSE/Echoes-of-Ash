using SW.Data;
using SW.Util;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 프로필 저장 파일을 읽고 쓰는 공통 서비스입니다.
    /// 마을과 던전 데이터는 각각 TownSaveService와 DungeonSaveService를 통해 변경합니다.
    /// 저장 버전이 다르면 이전 데이터를 사용하지 않습니다.
    /// </summary>
    public static class GameSaveService
    {
        #region 필드
        private static GameSaveData cached;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 프로필의 저장 데이터입니다. 최초 접근 시 파일에서 로드합니다.</summary>
        public static GameSaveData Current => cached ?? Load();

        /// <summary>현재 선택된 프로필(슬롯) 이름입니다.</summary>
        public static string CurrentProfile => SWSaveDataManager.CurrentSlot;
        #endregion // 프로퍼티

        #region 프로필
        /// <summary>
        /// 프로필(저장 칸)을 전환하고 해당 프로필을 로드합니다. 프로필 선택 화면 도입 시 사용합니다.
        /// </summary>
        /// <param name="profileSlot">전환할 프로필 슬롯 이름입니다.</param>
        public static void SelectProfile(string profileSlot)
        {
            SWSaveDataManager.SetSlot(profileSlot);
            cached = null;
            Load();

            SWLog.Log($"[GameSaveService] 프로필을 전환했습니다: {profileSlot}");
        }
        #endregion // 프로필

        #region 로드 - 저장
        /// <summary>
        /// 현재 프로필의 저장 파일을 로드합니다. 파일이 없거나 버전이 다르면 새 데이터로 시작합니다.
        /// </summary>
        /// <returns>로드된 저장 데이터입니다.</returns>
        public static GameSaveData Load()
        {
            GameSaveData gameData = new();

            if (SWSaveDataManager.HasSave())
            {
                // 저장하기 직전에 현재 저장 데이터 형식을 등록합니다.
                SWSaveDataManager.SetData(gameData);
                SWSaveDataManager.LoadAll(null, null, cloudFirst: false);

                GameSaveData loaded = SWSaveDataManager.GetData<GameSaveData>();

                if (loaded == null || loaded.version != GameSaveData.CurrentVersion)
                {
                    SWLog.LogWarning("[GameSaveService] 저장 버전이 다르거나 읽기에 실패했습니다 - 새 데이터로 시작합니다");
                }
                else
                {
                    gameData = loaded;
                }
            }

            cached = gameData;
            return gameData;
        }

        /// <summary>
        /// 현재 프로필의 저장 데이터를 파일에 씁니다. 클라우드 저장은 사용하지 않습니다.
        /// </summary>
        /// <returns>저장 성공 여부입니다.</returns>
        public static bool Save()
        {
            cached ??= new GameSaveData();

            SWSaveDataManager.SetData(cached);
            return SWSaveDataManager.SaveAll(null, null, prettyPrint: false, createBackup: true, backupToCloud: false);
        }
        #endregion // 로드 - 저장
    }
}
