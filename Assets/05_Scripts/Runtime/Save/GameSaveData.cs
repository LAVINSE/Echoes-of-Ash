using UnityEngine;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 프로필 한 칸의 마을 및 던전 진행 데이터를 보관합니다.
    /// </summary>
    [System.Serializable]
    public class GameSaveData
    {
        /// <summary>현재 전체 저장 데이터의 형식 버전입니다.</summary>
        public const int CurrentVersion = 1;

        /// <summary>파일을 저장할 때 사용한 형식 버전입니다.</summary>
        public int version = CurrentVersion;

        /// <summary>마을에서 계속 유지할 진행 데이터입니다.</summary>
        public TownSaveData Town = new();

        /// <summary>
        /// 진행 중인 던전 저장 데이터가 있는지 여부입니다.
        /// 진행 중인 던전이 없는 상태를 구분하기 위해 별도 값을 사용합니다.
        /// </summary>
        public bool hasDungeon;

        /// <summary>진행 중인 던전 데이터입니다. hasDungeon이 false면 사용하지 않습니다.</summary>
        public DungeonSaveData dungeon = new();
    }
}
