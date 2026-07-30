using System.Collections.Generic;
using System.Text;
using EchoesOfAsh.Data;
using EchoesOfAsh.Dungeon;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Save;
using EchoesOfAsh.View;
using EchoesOfAsh.View.UI;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoesOfAsh.Town
{
    /// <summary>
    /// Town 씬의 조립 지점입니다. 건물 승급, 막사 영입, 던전 출발을 담당합니다 (DungeonManager와 씬 기준 명명 대칭).
    /// 마을 상태의 진실 원본은 TownSaveData, 마을 구성의 진실 원본은 TownConfigData이며, 이 클래스는 둘을 잇는 판정만 소유합니다.
    /// 표현은 하이브리드입니다 - 건물/배경 = 월드 스프라이트 (씬 배치), 팝업/HUD = Canvas.
    /// 잠정 규칙: 팝업 뷰는 아트 시점 도입 - 건물 클릭은 로그만 남기고, 승급/영입 검증은 임시 테스트 버튼이 담당합니다.
    /// </summary>
    public class TownManager : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("데이터")]
        [Tooltip("마을 구성 데이터입니다. 건물·막사 영입·기본 캐릭터 목록을 소유합니다.")]
        [SerializeField] private TownConfigData townConfigData;
        [Tooltip("자원 요약의 아이템 이름 표시에 사용합니다. 미배선이면 코드명으로 표시합니다.")]
        [SerializeField] private SWIODatabase itemDatabase;

        [SWGroup("씬")]
        [Tooltip("던전 출발 시 로드할 씬 이름입니다.")]
        [SerializeField] private string dungeonSceneName = "Dungeon";

        [SWGroup("뷰")]
        [Tooltip("씬에 배치된 건물 뷰 목록입니다. 각 뷰가 자신의 건물 데이터를 참조합니다.")]
        [SerializeField] private List<TownBuildingView> buildingViews = new();
        [SerializeField] private TownInputController inputController;
        [SerializeField] private TownHUDView hudView;

        [SWGroup("임시 테스트")]
        [Tooltip("설계도 해금 테스트용 아이템입니다. 봉인된 서고 팝업(아트 시점) 도입 시 제거합니다.")]
        [SerializeField] private ItemData testBlueprintItem;

        private readonly StringBuilder stringBuilder = new();
        #endregion // 필드

        #region 유니티 이벤트 함수
        /// <summary>
        /// 씬 시작 시 구성을 검증하고, 기본 캐릭터를 보장한 뒤 뷰를 배선합니다.
        /// </summary>
        private void Start()
        {
            if (townConfigData == null)
            {
                SWLog.LogError("[TownManager] 시작 실패: 마을 구성 데이터가 없습니다.");
                return;
            }

            EnsureStarterCharacters();
            InitViews();
        }

        /// <summary>
        /// 객체가 제거될 때 건물 뷰의 콜백 연결을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            foreach (TownBuildingView buildingView in buildingViews)
            {
                if (buildingView != null)
                {
                    buildingView.Release();
                }
            }
        }
        #endregion // 유니티 이벤트 함수

        #region 초기화
        /// <summary>
        /// 보유 캐릭터가 하나도 없으면 기본 캐릭터를 영입하고 저장합니다 (최초 실행·구저장 폴백).
        /// </summary>
        private void EnsureStarterCharacters()
        {
            if (TownSaveService.Current.ownedCharacterCodeNames.Count > 0)
            {
                return;
            }

            int addedCount = 0;

            foreach (CharacterData starterCharacter in townConfigData.StarterCharacters)
            {
                if (starterCharacter == null)
                {
                    continue;
                }

                TownSaveService.AddCharacter(starterCharacter.CodeName);
                addedCount++;
            }

            if (addedCount == 0)
            {
                SWLog.LogWarning("[TownManager] 기본 캐릭터가 비어 있습니다 - 막사 영입 전까지 편성 후보가 없습니다.");
                return;
            }

            TownSaveService.Save();
            SWLog.Log($"[TownManager] 기본 캐릭터 {addedCount}명을 영입했습니다.");
        }

        /// <summary>
        /// 건물 뷰에 클릭 콜백을 주입하고 HUD를 배선합니다. 미배선분은 통과합니다 (미배선 통과 원칙).
        /// 구성에 등록되지 않은 건물 데이터를 참조하는 뷰는 경고 후 동작합니다.
        /// </summary>
        private void InitViews()
        {
            foreach (TownBuildingView buildingView in buildingViews)
            {
                if (buildingView == null || buildingView.BuildingData == null)
                {
                    SWLog.LogWarning("[TownManager] 건물 뷰가 없거나 건물 데이터가 비어 있어 건너뜁니다.");
                    continue;
                }

                if (!townConfigData.HasBuilding(buildingView.BuildingData))
                {
                    SWLog.LogWarning($"[TownManager] '{buildingView.BuildingData.DisplayName}'은(는) "
                        + "마을 구성 데이터에 등록되지 않은 건물입니다 - 등록을 권장합니다.");
                }

                TownBuildingView capturedView = buildingView;
                buildingView.Init(() => HandleBuildingClicked(capturedView));
            }

            if (hudView != null)
            {
                hudView.Show(EnterDungeon, ResumeDungeon);
                RefreshHud();
            }
            else
            {
                SWLog.Log("[TownManager] HUD 미배선: 인스펙터 버튼으로 조작합니다.");
            }
        }
        #endregion // 초기화

        #region 건물 클릭
        /// <summary>
        /// 건물 클릭을 처리합니다. 팝업 도입 전이므로 현재 상태 로그만 남깁니다 (임시 조치 - 팝업 도입 시 팝업 열기로 대체).
        /// </summary>
        /// <param name="buildingView">클릭된 건물 뷰입니다.</param>
        private void HandleBuildingClicked(TownBuildingView buildingView)
        {
            BuildingData building = buildingView.BuildingData;
            int currentLevel = TownSaveService.GetBuildingLevel(building.CodeName);
            bool isBarracks = townConfigData.IsBarracks(building);

            SWLog.Log($"[TownManager] 건물 클릭: {building.DisplayName} (Lv {currentLevel} / {building.MaxLevel}"
                + $"{(isBarracks ? ", 막사" : string.Empty)}) - 팝업은 아트 시점 도입 예정입니다.");
        }
        #endregion // 건물 클릭

        #region 건물
        /// <summary>
        /// 건물을 한 단계 승급합니다. 비용을 검사한 뒤 일괄 차감하고 즉시 저장합니다.
        /// 팝업 도입 시 팝업의 승급 요청이 이 함수를 호출합니다.
        /// </summary>
        /// <param name="building">승급할 건물 정의입니다.</param>
        /// <returns>승급에 성공했으면 true입니다.</returns>
        public bool TryUpgradeBuilding(BuildingData building)
        {
            if (building == null)
            {
                SWLog.LogWarning("[TownManager] 건물 승급 무시: 건물 정의가 없습니다.");
                return false;
            }

            int currentLevel = TownSaveService.GetBuildingLevel(building.CodeName);
            IReadOnlyList<ItemStackData> upgradeCosts = building.GetUpgradeCosts(currentLevel);

            if (upgradeCosts == null)
            {
                SWLog.Log($"[TownManager] 건물 승급 무시: {building.DisplayName}은(는) 이미 최대 레벨입니다.");
                return false;
            }

            if (!TownSaveService.TryConsumeItems(upgradeCosts))
            {
                SWLog.Log($"[TownManager] 건물 승급 실패: {building.DisplayName} 비용이 부족합니다"
                    + $" (필요: {BuildCostText(upgradeCosts)}).");
                return false;
            }

            TownSaveService.SetBuildingLevel(building.CodeName, currentLevel + 1);
            TownSaveService.Save();

            SWLog.Log($"[TownManager] 건물을 승급했습니다: {building.DisplayName} Lv {currentLevel + 1}");
            RefreshHud();
            return true;
        }
        #endregion // 건물

        #region 막사
        /// <summary>
        /// 지정한 슬롯의 캐릭터를 영입합니다. 비용을 검사한 뒤 일괄 차감하고 즉시 저장합니다.
        /// 팝업 도입 시 팝업의 영입 요청이 이 함수를 호출합니다.
        /// </summary>
        /// <param name="offerIndex">영입 목록 인덱스입니다 (마을 구성 데이터의 목록 순서).</param>
        /// <returns>영입에 성공했으면 true입니다.</returns>
        public bool TryRecruit(int offerIndex)
        {
            IReadOnlyList<CharacterRecruitData> characterRecruits = townConfigData.CharacterRecruits;

            if (offerIndex < 0 || offerIndex >= characterRecruits.Count
                || characterRecruits[offerIndex]?.CharacterData == null)
            {
                SWLog.LogWarning($"[TownManager] 영입 무시: 인덱스 {offerIndex}가 유효하지 않습니다.");
                return false;
            }

            CharacterRecruitData recruitOffer = characterRecruits[offerIndex];
            CharacterData characterData = recruitOffer.CharacterData;

            if (TownSaveService.HasCharacter(characterData.CodeName))
            {
                SWLog.Log($"[TownManager] 영입 무시: {characterData.DisplayName}은(는) 이미 보유한 캐릭터입니다.");
                return false;
            }

            if (!TownSaveService.TryConsumeItems(recruitOffer.Costs))
            {
                SWLog.Log($"[TownManager] 영입 실패: {characterData.DisplayName} 비용이 부족합니다"
                    + $" (필요: {BuildCostText(recruitOffer.Costs)}).");
                return false;
            }

            TownSaveService.AddCharacter(characterData.CodeName);
            TownSaveService.Save();

            SWLog.Log($"[TownManager] 캐릭터를 영입했습니다: {characterData.DisplayName}");
            RefreshHud();
            return true;
        }
        #endregion // 막사

        #region 임시 테스트
        /// <summary>
        /// 마을 구성의 첫 번째 건물 승급을 시도합니다 (임시 조치 - 팝업 도입 전 저장/차감 경로 검증용, 도입 시 제거).
        /// </summary>
        [SWButton("테스트: 첫 건물 승급")]
        public void TestUpgradeFirstBuilding()
        {
            if (townConfigData == null || townConfigData.Buildings.Count == 0)
            {
                SWLog.LogWarning("[TownManager] 테스트 승급 무시: 마을 구성의 건물 목록이 비어 있습니다.");
                return;
            }

            TryUpgradeBuilding(townConfigData.Buildings[0]);
        }

        /// <summary>
        /// 마을 구성의 첫 번째 영입 항목의 영입을 시도합니다 (임시 조치 - 팝업 도입 전 저장/차감 경로 검증용, 도입 시 제거).
        /// </summary>
        [SWButton("테스트: 첫 항목 영입")]
        public void TestRecruitFirstOffer()
        {
            if (townConfigData == null)
            {
                SWLog.LogWarning("[TownManager] 테스트 영입 무시: 마을 구성 데이터가 없습니다.");
                return;
            }

            TryRecruit(0);
        }
        #endregion // 임시 테스트

        #region 던전 출발
        /// <summary>
        /// 새 던전 출발을 요청하고 던전 씬으로 전환합니다. 편성/시작은 던전 씬의 DungeonManager가 담당합니다.
        /// </summary>
        [SWButton("던전 출발")]
        public void EnterDungeon()
        {
            DungeonLaunchRequest.Request(EDungeonLaunchMode.NewDungeon);
            SceneManager.LoadScene(dungeonSceneName);
        }

        /// <summary>
        /// 저장된 던전 스냅샷의 이어하기를 요청하고 던전 씬으로 전환합니다.
        /// </summary>
        [SWButton("던전 이어하기")]
        public void ResumeDungeon()
        {
            if (!DungeonSaveService.HasSave())
            {
                SWLog.LogWarning("[TownManager] ResumeDungeon 무시: 진행 중인 던전 스냅샷이 없습니다.");
                return;
            }

            DungeonLaunchRequest.Request(EDungeonLaunchMode.Resume);
            SceneManager.LoadScene(dungeonSceneName);
        }
        #endregion // 던전 출발

        #region 화면 갱신
        /// <summary>
        /// HUD 표시 내용을 갱신합니다.
        /// </summary>
        private void RefreshHud()
        {
            if (hudView == null)
            {
                return;
            }

            hudView.Refresh(BuildResourceSummary(), DungeonSaveService.HasSave());
        }

        /// <summary>
        /// 보유 아이템의 요약 문구를 구성합니다. 데이터베이스 미배선이면 코드명으로 표시합니다.
        /// </summary>
        /// <returns>자원 요약 문구입니다.</returns>
        private string BuildResourceSummary()
        {
            IReadOnlyList<ItemCountSaveData> items = TownSaveService.Current.items;

            if (items.Count == 0)
            {
                return "보유 자원 없음";
            }

            stringBuilder.Clear();

            foreach (ItemCountSaveData item in items)
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.AppendLine();
                }

                stringBuilder.Append($"{ResolveItemName(item.codeName)} x{item.count}");
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 비용 목록의 표시 문구를 구성합니다.
        /// </summary>
        /// <param name="costs">비용 목록입니다.</param>
        /// <returns>비용 문구입니다. 비어 있으면 "비용 없음"입니다.</returns>
        private string BuildCostText(IReadOnlyList<ItemStackData> costs)
        {
            if (costs == null || costs.Count == 0)
            {
                return "비용 없음";
            }

            stringBuilder.Clear();

            foreach (ItemStackData cost in costs)
            {
                if (cost?.ItemData == null)
                {
                    continue;
                }

                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append(", ");
                }

                stringBuilder.Append($"{cost.ItemData.DisplayName} x{cost.Count}");
            }

            return stringBuilder.Length > 0 ? stringBuilder.ToString() : "비용 없음";
        }

        /// <summary>
        /// 아이템 코드명을 표시 이름으로 변환합니다.
        /// </summary>
        /// <param name="codeName">아이템 코드 이름입니다.</param>
        /// <returns>표시 이름입니다. 찾지 못하면 코드명입니다.</returns>
        private string ResolveItemName(string codeName)
        {
            if (itemDatabase == null)
            {
                return codeName;
            }

            ItemData itemData = itemDatabase.GetDataByCodeName<ItemData>(codeName);
            return itemData != null ? itemData.DisplayName : codeName;
        }
        #endregion // 화면 갱신

        /// <summary>
        /// 테스트용 설계도 해금을 실행합니다. 봉인된 서고 팝업 도입 시 제거합니다.
        /// </summary>
        [SWButton("테스트 설계도 해금")]
        private void TestUnlockByBlueprint()
        {
            if (CardUnlockService.TryUnlockByBlueprint(testBlueprintItem))
            {
                RefreshHud();
            }
        }
    }
}