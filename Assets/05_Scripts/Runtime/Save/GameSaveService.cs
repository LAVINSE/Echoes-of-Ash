using SW.Data;
using SW.Util;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 프로필 저장 파일의 로드/저장 단일 진입점입니다. SWSaveDataManager를 직접 호출하는 유일한 클래스입니다.
    /// 슬롯 = 프로필 (저장 칸 본래 의미). 구획 접근은 HubSaveService / DungeonSaveService를 경유합니다.
    /// 잠정 규칙: 개발 중에는 저장 데이터를 보존하지 않으므로 마이그레이션 없이 버전 불일치 = 폐기합니다 (데이터 보존 시작 시점에 계층 복원 — 기획서 15-5).
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
                // 정적 단일 데이터 구조이므로 호출 직전 타입을 등록합니다 (P2-D7 — 이 클래스 내부로 국소화)
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
        /// 현재 프로필의 저장 데이터를 파일에 씁니다. 클라우드 백업은 잠정 제외입니다 (출시 준비 시 일괄 결정).
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