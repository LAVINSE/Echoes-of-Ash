using System.Collections.Generic;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 캐릭터 영입 데이터
    /// </summary>
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