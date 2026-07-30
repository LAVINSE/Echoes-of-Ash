using System.Collections.Generic;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 영입할 캐릭터와 필요한 비용을 보관합니다.
    /// </summary>
    [System.Serializable]
    public class CharacterRecruitData
    {
        #region 필드
        [Tooltip("영입할 캐릭터")]
        [SerializeField] private CharacterData characterData;
        [Tooltip("영입할 비용 - 비우면 무료 영입")]
        [SerializeField] private List<ItemStackData> costs = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>영입할 캐릭터 데이터입니다.</summary>
        public CharacterData CharacterData => characterData;
        /// <summary>영입 비용 목록입니다.</summary>
        public IReadOnlyList<ItemStackData> Costs => costs;
        #endregion // 프로퍼티
    }
}
