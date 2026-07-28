using System.Collections.Generic;
using SW.Attributes;
using SW.Base;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 마을 구성 데이터
    /// </summary>
    /// <remarks>
    /// 마을에 존재하는 건물, 막사 영입 항목, 기본 캐릭터를 에셋 하나로 소유합니다 (MapConfigData 전례 - 구성 = SO 외부화).
    /// 씬 오브젝트는 참조할 수 없으므로 건물의 배치-데이터 연결은 씬의 TownBuildingView가 소유합니다.
    /// </remarks>
    [CreateAssetMenu(fileName = "TownConfigData", menuName = "EchoesOfAsh/Data/TownConfigData")]
    public class TownConfigData : SWScriptableObject
    {
        #region 필드
        [SWGroup("건물")]
        [Tooltip("마을에 존재하는 건물 목록입니다. 씬 배치 뷰의 데이터 검증에 사용합니다.")]
        [SerializeField] private List<BuildingData> buildings = new();
        [Tooltip("막사로 취급할 건물입니다. 팝업 도입 시 영입 목록 표시 판정에 사용합니다.")]
        [SerializeField] private BuildingData barracksBuilding;

        [SWGroup("막사")]
        [Tooltip("막사에서 영입할 수 있는 캐릭터 항목입니다. 목록 순서 = 팝업 슬롯 순서입니다.")]
        [SerializeField] private List<CharacterRecruitData> characterRecruits = new();
        [Tooltip("보유 캐릭터가 하나도 없을 때(최초 실행) 자동으로 영입되는 기본 캐릭터입니다.")]
        [SerializeField] private List<CharacterData> starterCharacters = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>마을에 존재하는 건물 목록입니다.</summary>
        public IReadOnlyList<BuildingData> Buildings => buildings;
        /// <summary>막사로 취급할 건물입니다.</summary>
        public BuildingData BarracksBuilding => barracksBuilding;
        /// <summary>막사 영입 항목 목록입니다.</summary>
        public IReadOnlyList<CharacterRecruitData> CharacterRecruits => characterRecruits;
        /// <summary>최초 실행 시 자동으로 영입되는 기본 캐릭터 목록입니다.</summary>
        public IReadOnlyList<CharacterData> StarterCharacters => starterCharacters;
        #endregion // 프로퍼티

        /// <summary>
        /// 지정한 건물이 이 마을 구성에 등록되어 있는지 확인합니다.
        /// </summary>
        /// <param name="buildingData">확인할 건물 데이터입니다.</param>
        /// <returns>등록되어 있으면 true입니다.</returns>
        public bool HasBuilding(BuildingData buildingData)
        {
            return buildingData != null && buildings.Contains(buildingData);
        }

        /// <summary>
        /// 지정한 건물이 막사인지 확인합니다.
        /// </summary>
        /// <param name="buildingData">확인할 건물 데이터입니다.</param>
        /// <returns>막사면 true입니다.</returns>
        public bool IsBarracks(BuildingData buildingData)
        {
            return buildingData != null && buildingData == barracksBuilding;
        }
    }
}