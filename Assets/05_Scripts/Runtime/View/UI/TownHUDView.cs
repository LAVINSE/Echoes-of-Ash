using System;
using SW.Attributes;
using SW.Base;
using SW.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 마을 화면 부착 HUD입니다 (BattleHUDView 전례). 자원 요약과 던전 출발/이어하기 버튼을 표시합니다.
    /// 건물 상호작용은 월드 계층(TownBuildingView) 소관이며, 이 뷰는 화면 고정 요소만 담당합니다.
    /// </summary>
    public class TownHUDView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("자원")]
        [SerializeField] private TextMeshProUGUI resourceText;

        [SWGroup("던전 출발")]
        [SerializeField] private Button enterDungeonButton;
        [Tooltip("던전 스냅샷이 있을 때만 표시되는 이어하기 버튼입니다.")]
        [SerializeField] private Button resumeDungeonButton;

        private Action onEnterDungeon;
        private Action onResumeDungeon;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 버튼의 클릭 처리를 연결합니다.
        /// </summary>
        private void Awake()
        {
            if (enterDungeonButton != null)
            {
                enterDungeonButton.onClick.AddListener(() => onEnterDungeon?.Invoke());
            }

            if (resumeDungeonButton != null)
            {
                resumeDungeonButton.onClick.AddListener(() => onResumeDungeon?.Invoke());
            }
        }
        #endregion // 초기화

        /// <summary>
        /// 콜백을 연결합니다. 표시 내용은 Refresh로 갱신합니다.
        /// </summary>
        /// <param name="onEnterDungeon">던전 출발 요청 시 호출됩니다.</param>
        /// <param name="onResumeDungeon">던전 이어하기 요청 시 호출됩니다.</param>
        public void Show(Action onEnterDungeon, Action onResumeDungeon)
        {
            if (onEnterDungeon == null)
            {
                SWLog.LogError("[TownHudView] Show 실패: 출발 콜백이 없습니다.");
                return;
            }

            this.onEnterDungeon = onEnterDungeon;
            this.onResumeDungeon = onResumeDungeon;
        }

        /// <summary>
        /// 콜백 연결을 해제합니다.
        /// </summary>
        public void Hide()
        {
            onEnterDungeon = null;
            onResumeDungeon = null;
        }

        /// <summary>
        /// 표시 내용을 갱신합니다.
        /// </summary>
        /// <param name="resourceSummary">자원 요약 문구입니다.</param>
        /// <param name="hasDungeonSave">진행 중인 던전 스냅샷이 있는지 여부입니다.</param>
        public void Refresh(string resourceSummary, bool hasDungeonSave)
        {
            if (resourceText != null)
            {
                resourceText.text = resourceSummary;
            }

            if (resumeDungeonButton != null)
            {
                resumeDungeonButton.gameObject.SetActive(hasDungeonSave);
            }
        }
    }
}