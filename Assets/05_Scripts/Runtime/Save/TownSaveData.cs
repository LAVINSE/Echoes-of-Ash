using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 아이템의 코드 이름과 보유 수량을 저장합니다.
    /// </summary>
    [Serializable]
    public class ItemCountSaveData
    {
        /// <summary>아이템 식별 코드 이름입니다.</summary>
        public string codeName;
        /// <summary>보유 수량입니다.</summary>
        public int count;
    }

    /// <summary>
    /// 건물 코드명과 현재 레벨을 보관하는 저장 항목입니다.
    /// </summary>
    [System.Serializable]
    public class BuildingLevelSaveData
    {
        /// <summary>건물 식별 코드 이름입니다.</summary>
        public string codeName;
        /// <summary>현재 레벨입니다. 0이면 아직 승급하지 않은 상태입니다.</summary>
        public int level;
    }

    /// <summary>
    /// 마을에서 계속 유지할 아이템, 건물, 캐릭터와 카드 해금 정보를 보관합니다.
    /// 버전 1에는 자원 보유량, 건물 레벨과 보유 캐릭터가 포함됩니다. 새로운 항목은 필드를 추가하고,
    /// 기존 저장 데이터와 호환되지 않는 변경이 있을 때만 버전을 올립니다.
    /// </summary>
    [Serializable]
    public class TownSaveData
    {
        /// <summary>현재 마을 저장 데이터의 형식 버전입니다.</summary>
        public const int CurrentVersion = 1;

        /// <summary>파일을 저장할 때 사용한 마을 데이터의 형식 버전입니다.</summary>
        public int version = CurrentVersion;

        /// <summary>보유 아이템 목록입니다.</summary>
        public List<ItemCountSaveData> items = new();

        /// <summary>건물별 현재 레벨 목록입니다.</summary>
        public List<BuildingLevelSaveData> buildingLevels = new();

        /// <summary>보유 캐릭터 코드명 목록입니다. 영입한 순서대로 저장합니다.</summary>
        public List<string> ownedCharacterCodeNames = new();

        /// <summary>해금된 카드 코드명 목록입니다. 처음부터 해금된 카드는 기록하지 않습니다.</summary>
        public List<string> unlockedCardCodeNames = new();
    }
}
