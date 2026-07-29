using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 아이템 수량 저장 항목입니다. codeName 기준으로 복원합니다.
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
    /// 건물 레벨 저장 항목
    /// </summary>
    [System.Serializable]
    public class BuildingLevelSaveData
    {
        /// <summary>건물 식별 코드 이름입니다.</summary>
        public string codeName;
        /// <summary>현재 레벨입니다 (0 = 미승급).</summary>
        public int level;
    }

    /// <summary>
    /// 마을 누적 진행(기획 용어: 메타 진행)의 저장 스키마입니다. GameSaveData의 마을 구획으로 저장됩니다.
    /// v1 = 자원 보유량 + 건물 레벨 + 보유 캐릭터. 신규 항목은 필드 추가로 편입하고,
    /// 구버전 강제 폐기가 필요할 때만 버전을 인상합니다 (개정 19 규칙 - 필드 추가는 버전 유지 무해).
    /// </summary>
    [Serializable]
    public class TownSaveData
    {
        /// <summary>현재 스키마 버전입니다.</summary>
        public const int CurrentVersion = 1;

        /// <summary>저장 당시 스키마 버전입니다.</summary>
        public int version = CurrentVersion;

        /// <summary>보유 아이템 목록입니다.</summary>
        public List<ItemCountSaveData> items = new();

        /// <summary>건물 레벨 목록입니다 (P2-M6 6-1).</summary>
        public List<BuildingLevelSaveData> buildingLevels = new();

        /// <summary>보유 캐릭터 코드명 목록입니다. 등록 순서 = 영입 순서입니다 (P2-M6 6-1 - 막사 영입).</summary>
        public List<string> ownedCharacterCodeNames = new();

        /// <summary>해금된 카드 코드명 목록입니다. 발견형/제작형 공용 원장이며, 기본 해금 카드는 기록하지 않습니다 (P2-M7 7-5).</summary>
        public List<string> unlockedCardCodeNames = new();
    }
}