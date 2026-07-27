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
    /// 거점 누적 진행(기획 용어: 메타 진행)의 저장 스키마입니다. 던전 스냅샷과 별개 슬롯("hub")에 저장됩니다.
    /// v1 = 자원 보유량. 해금·시설·보유 캐릭터는 도입 시 버전 증가 + 마이그레이션으로 편입합니다 (기획서 15-5).
    /// </summary>
    [System.Serializable]
    public class HubSaveData
    {
        /// <summary>현재 스키마 버전입니다.</summary>
        public const int CurrentVersion = 1;

        /// <summary>저장 당시 스키마 버전입니다.</summary>
        public int version = CurrentVersion;

        /// <summary>보유 아이템 목록입니다.</summary>
        public List<ItemCountSaveData> items = new();
    }
}