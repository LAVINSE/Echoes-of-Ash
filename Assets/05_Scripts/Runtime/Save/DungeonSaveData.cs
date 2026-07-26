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
    /// 던전 1회 도전의 저장 스냅샷입니다 (P2-D1 - 전체 상태 스냅샷).
    /// 가변 런 상태만 저장하고, 정적 구성(조우 풀, 이벤트 풀, 밸런스)은 저장하지 않습니다.
    /// </summary>
    [System.Serializable]
    public class DungeonSaveData
    {
        /// <summary>저장 스키마 버전입니다. 필드 추가/변경 시 증가시키고 마이그레이션을 추가합니다.</summary>
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

        /// <summary>맵의 모든 노드입니다 (방문/잠식 상태 포함).</summary>
        public List<MapNode> mapNodes = new();
        /// <summary>맵의 모든 경로입니다.</summary>
        public List<MapEdge> mapEdges = new();

        /// <summary>던전 덱의 카드 목록입니다.</summary>
        public List<DungeonCardSaveData> deckCards = new();

        /// <summary>파티 캐릭터 코드명 목록입니다 (스키마 v2 — 편성 화면 도입)</summary>
        public List<string> partyCharacterCodeNames = new();
    }
}