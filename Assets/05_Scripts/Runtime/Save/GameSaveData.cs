using UnityEngine;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 프로필 1칸의 저장
    /// </summary>
    [System.Serializable]
    public class GameSaveData
    {
        /// <summary>현재 루트 스키마 버전입니다.</summary>
        public const int CurrentVersion = 1;

        /// <summary>저장 당시 루트 스키마 버전입니다.</summary>
        public int version = CurrentVersion;

        /// <summary>거점 누적 진행 구획입니다. 영구 보존됩니다.</summary>
        public TownSaveData Town = new();

        /// <summary>
        /// 진행 중인 던전 스냅샷이 있는지 여부입니다.
        /// JsonUtility는 중첩 클래스의 null을 보존하지 못하므로 유무는 이 깃발이 판정합니다.
        /// </summary>
        public bool hasDungeon;

        /// <summary>던전 스냅샷 구획입니다. hasDungeon이 false면 내용은 무시합니다.</summary>
        public DungeonSaveData dungeon = new();
    }
}