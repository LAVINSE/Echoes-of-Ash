using UnityEngine;

namespace EchoesOfAsh.Map
{
    /// <summary>
    /// 두 맵 노드를 연결하는 경로입니다.
    /// </summary>
    [System.Serializable]
    public class MapEdge
    {
        #region 필드
        [SerializeField] private int fromNodeIdentifier;
        [SerializeField] private int toNodeIdentifier;
        [SerializeField] private bool isMadnessOnly;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>출발 노드의 식별자입니다.</summary>
        public int FromNodeIdentifier => fromNodeIdentifier;
        /// <summary>도착 노드의 식별자입니다.</summary>
        public int ToNodeIdentifier => toNodeIdentifier;
        /// <summary>광기 상태에서만 통행할 수 있는 경로인지 여부입니다.</summary>
        public bool IsMadnessOnly => isMadnessOnly;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 두 맵 노드를 연결하는 경로를 생성합니다.
        /// </summary>
        /// <param name="fromNodeIdentifier">출발 노드의 식별자입니다.</param>
        /// <param name="toNodeIdentifier">도착 노드의 식별자입니다.</param>
        /// <param name="isMadnessOnly">광기 상태에서만 통행할 수 있는지 여부입니다.</param>
        public MapEdge(int fromNodeIdentifier, int toNodeIdentifier, bool isMadnessOnly)
        {
            this.fromNodeIdentifier = fromNodeIdentifier;
            this.toNodeIdentifier = toNodeIdentifier;
            this.isMadnessOnly = isMadnessOnly;
        }
        #endregion // 생성자
    }
}
