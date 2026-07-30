using EchoesOfAsh.Enum;
using UnityEngine;

namespace EchoesOfAsh.Map
{
    /// <summary>
    /// 던전 맵을 구성하는 하나의 노드입니다.
    /// </summary>
    [System.Serializable]
    public class MapNode
    {
        #region 필드
        [SerializeField] private int identifier;
        [SerializeField] private int floor;
        [SerializeField] private int lane;
        [SerializeField] private EMapNodeType nodeType;
        [SerializeField] private Vector2 position;
        [SerializeField] private bool isMadnessOnly;
        [SerializeField] private bool isVisited;
        [SerializeField] private bool isAshConsumed;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>노드의 식별자입니다.</summary>
        public int Identifier => identifier;
        /// <summary>노드가 배치된 층입니다.</summary>
        public int Floor => floor;
        /// <summary>노드가 배치된 세로 칸입니다.</summary>
        public int Lane => lane;
        /// <summary>노드의 종류입니다.</summary>
        public EMapNodeType NodeType => nodeType;
        /// <summary>노드의 화면 좌표입니다.</summary>
        public Vector2 Position => position;
        /// <summary>광기 상태에서만 진입할 수 있는 노드인지 여부입니다.</summary>
        public bool IsMadnessOnly => isMadnessOnly;
        /// <summary>방문한 노드인지 여부입니다.</summary>
        public bool IsVisited => isVisited;
        /// <summary>잿불에 잠식된 노드인지 여부입니다.</summary>
        public bool IsAshConsumed => isAshConsumed;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 던전 맵 노드를 생성합니다.
        /// </summary>
        /// <param name="identifier">노드의 식별자입니다.</param>
        /// <param name="floor">노드가 배치된 층입니다.</param>
        /// <param name="lane">노드가 배치된 세로 칸입니다.</param>
        /// <param name="nodeType">노드의 종류입니다.</param>
        /// <param name="position">노드의 화면 좌표입니다.</param>
        public MapNode(int identifier, int floor, int lane, EMapNodeType nodeType, Vector2 position)
        {
            this.identifier = identifier;
            this.floor = floor;
            this.lane = lane;
            this.nodeType = nodeType;
            this.position = position;
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 노드 종류를 변경합니다.
        /// </summary>
        /// <param name="nodeType">변경할 노드 종류입니다.</param>
        public void SetNodeType(EMapNodeType nodeType)
        {
            this.nodeType = nodeType;
        }

        /// <summary>
        /// 노드의 화면 좌표를 설정합니다.
        /// </summary>
        /// <param name="position">설정할 화면 좌표입니다.</param>
        public void SetPosition(Vector2 position)
        {
            this.position = position;
        }

        /// <summary>
        /// 노드를 광기 상태 전용으로 표시합니다.
        /// </summary>
        public void SetMadnessOnly()
        {
            isMadnessOnly = true;
        }

        /// <summary>
        /// 노드를 방문한 상태로 변경합니다.
        /// </summary>
        public void SetVisited()
        {
            isVisited = true;
        }

        /// <summary>
        /// 노드를 잿불에 잠식된 상태로 변경합니다.
        /// </summary>
        public void SetAshConsumed()
        {
            isAshConsumed = true;
        }
        #endregion // 함수
    }
}
