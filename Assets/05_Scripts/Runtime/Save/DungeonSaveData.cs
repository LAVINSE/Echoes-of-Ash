using System.Collections.Generic;
using EchoesOfAsh.Map;
using UnityEngine;

namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 덱 카드 하나의 저장 데이터입니다.
    /// </summary>
    [System.Serializable]
    public class DungeonCardSaveData
    {
        /// <summary>원본 카드 데이터의 코드명입니다.</summary>
        public string cardCodeName;
        /// <summary>강화 상태 여부입니다.</summary>
        public bool isUpgrade;
    }

    /// <summary>
    /// 진행 중인 던전의 변경 가능한 상태를 보관합니다.
    /// 지도 구성, 조우 목록과 밸런스처럼 에셋에서 다시 읽을 수 있는 정보는 저장하지 않습니다.
    /// </summary>
    [System.Serializable]
    public class DungeonSaveData
    {
        /// <summary>던전 저장 데이터의 형식 버전입니다.</summary>
        public int version;

        /// <summary>던전 생성에 사용한 시드입니다 (생성 기록/재현용 - 재개 시 난수 연속성은 보장하지 않습니다).</summary>
        public int seed;
        /// <summary>현재 위치한 노드의 식별자입니다.</summary>
        public int currentNodeIdentifier;
        /// <summary>현재 노드의 진입 처리가 완료되었는지 여부입니다. 미완료면 복원 시 진입 처리를 다시 실행합니다.</summary>
        public bool isCurrentNodeResolved;
        /// <summary>전투 사이에 이월하는 파티 정신력입니다.</summary>
        public int carriedSanity;
        /// <summary>노드 이동 누적 횟수입니다.</summary>
        public int moveCount;
        /// <summary>잿불에 잠식된 마지막 층입니다.</summary>
        public int ashConsumedFloor;
        /// <summary>이번 던전에서 광기 이벤트가 발생했는지 여부입니다. 이전 저장 데이터에서는 발생하지 않은 상태로 읽습니다.</summary>
        public bool hasMadnessEventOccurred;
        /// <summary>던전 중 보유한 골드입니다.</summary>
        public int gold;

        /// <summary>맵의 모든 노드입니다 (방문/잠식 상태 포함).</summary>
        public List<MapNode> mapNodes = new();
        /// <summary>맵의 모든 경로입니다.</summary>
        public List<MapEdge> mapEdges = new();

        /// <summary>던전 덱의 카드 목록입니다.</summary>
        public List<DungeonCardSaveData> deckCards = new();

        /// <summary>파티 캐릭터 코드명 목록입니다 (편성 화면 도입분).</summary>
        public List<string> partyCharacterCodeNames = new();

        /// <summary>던전 중 소지한 아이템 목록입니다. 코드명으로 원본 데이터를 찾습니다.</summary>
        public List<ItemCountSaveData> carriedItems = new();
        /// <summary>던전 중 획득한 유물 코드명 목록입니다. 유물 효과는 목록 순서대로 실행됩니다.</summary>
        public List<string> relicCodeNames = new();
    }
}
