using SW.Attributes;
using SW.Base;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 맵 생성 규칙을 관리하는 데이터입니다.
    /// </summary>
    [CreateAssetMenu(fileName = "MapConfig", menuName = "EchoesOfAsh/Data/MapConfig")]
    public class MapConfigData : SWScriptableObject
    {
        #region 필드
        [SWGroup("구조")]
        [Tooltip("보스 층을 제외한 층 수입니다.")]
        [SerializeField, Min(3)] private int floorCount = 12;
        [Tooltip("각 층에 배치할 수 있는 세로 칸 수입니다.")]
        [SerializeField, Min(2)] private int laneCount = 3;
        [Tooltip("생성할 무작위 경로 수입니다. 값이 클수록 그래프가 촘촘해집니다.")]
        [SerializeField, Min(2)] private int pathCount = 5;

        [SWGroup("노드 타입 가중치")]
        [Tooltip("전투 노드의 선택 가중치입니다.")]
        [SerializeField, Min(0)] private int battleWeight = 10;
        [Tooltip("엘리트 노드의 선택 가중치입니다.")]
        [SerializeField, Min(0)] private int eliteWeight = 2;
        [Tooltip("휴식 노드의 선택 가중치입니다.")]
        [SerializeField, Min(0)] private int restWeight = 2;
        [Tooltip("이벤트 노드의 선택 가중치입니다.")]
        [SerializeField, Min(0)] private int eventWeight = 3;
        [Tooltip("상점 노드의 선택 가중치입니다.")]
        [SerializeField, Min(0)] private int shopWeight = 1;
        [Tooltip("보관 노드의 선택 가중치입니다.")]
        [SerializeField, Min(0)] private int storageWeight = 1;
        [Tooltip("엘리트 노드가 등장할 수 있는 최소 층입니다.")]
        [SerializeField, Min(1)] private int eliteMinFloor = 3;

        [SWGroup("정신력 경로")]
        [Tooltip("연결되지 않은 인접 경로가 광기 전용 경로로 생성될 확률입니다.")]
        [SerializeField, Range(0f, 1f)] private float madnessEdgeChance = 0.15f;

        [SWGroup("잿불 침식")]
        [Tooltip("잿불 침식이 한 층 전진하는 데 필요한 방 이동 횟수입니다. 0이면 비활성화됩니다.")]
        [SerializeField, Min(0)] private int ashAdvanceInterval = 2;

        [SWGroup("배치")]
        [Tooltip("층 사이의 가로 간격입니다.")]
        [SerializeField, Min(50f)] private float floorSpacing = 250f;
        [Tooltip("세로 칸 사이의 간격입니다.")]
        [SerializeField, Min(50f)] private float laneSpacing = 220f;
        [Tooltip("시드에 따라 노드 좌표에 적용할 최대 무작위 편차입니다.")]
        [SerializeField, Min(0f)] private float positionOffset = 40f;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>보스 층을 제외한 층 수입니다.</summary>
        public int FloorCount => floorCount;
        /// <summary>각 층에 배치할 세로 칸 수입니다.</summary>
        public int LaneCount => laneCount;
        /// <summary>생성할 무작위 경로 수입니다.</summary>
        public int PathCount => pathCount;

        /// <summary>전투 노드의 선택 가중치입니다.</summary>
        public int BattleWeight => battleWeight;
        /// <summary>엘리트 노드의 선택 가중치입니다.</summary>
        public int EliteWeight => eliteWeight;
        /// <summary>휴식 노드의 선택 가중치입니다.</summary>
        public int RestWeight => restWeight;
        /// <summary>이벤트 노드의 선택 가중치입니다.</summary>
        public int EventWeight => eventWeight;
        /// <summary>상점 노드의 선택 가중치입니다.</summary>
        public int ShopWeight => shopWeight;
        /// <summary>보관 노드의 선택 가중치입니다.</summary>
        public int StorageWeight => storageWeight;
        /// <summary>엘리트 노드가 등장할 수 있는 최소 층입니다.</summary>
        public int EliteMinFloor => eliteMinFloor;

        /// <summary>광기 전용 경로가 생성될 확률입니다.</summary>
        public float MadnessEdgeChance => madnessEdgeChance;
        /// <summary>잿불 침식이 전진하는 데 필요한 방 이동 횟수입니다.</summary>
        public int AshAdvanceInterval => ashAdvanceInterval;

        /// <summary>층 사이의 가로 간격입니다.</summary>
        public float FloorSpacing => floorSpacing;
        /// <summary>세로 칸 사이의 간격입니다.</summary>
        public float LaneSpacing => laneSpacing;
        /// <summary>노드 좌표에 적용할 최대 무작위 편차입니다.</summary>
        public float PositionOffset => positionOffset;
        #endregion // 프로퍼티
    }
}
