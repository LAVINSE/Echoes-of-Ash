using System;
using System.Collections.Generic;
using EchoesOfAsh.Data;
using SW.Attributes;
using SW.Base;
using SW.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 선택지형 노드 화면(휴식/이벤트/보관 골격)의 제목, 설명 및 선택지를 표시합니다.
    /// 어떤 노드의 화면인지는 조립 지점(DungeonManager)이 데이터로 결정합니다.
    /// </summary>
    public class NodeScreenView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [SWGroup("선택지")]
        [Tooltip("선택지 버튼 슬롯입니다. 기획 최대치인 3개를 사전 배치합니다.")]
        [SerializeField] private List<Button> choiceButtons = new();
        [Tooltip("선택지 버튼과 같은 순서의 문구 텍스트입니다.")]
        [SerializeField] private List<TextMeshProUGUI> choiceTexts = new();

        private Action<int> onChoiceSelected;
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 선택지 버튼의 클릭 처리를 연결합니다.
        /// </summary>
        private void Awake()
        {
            for (int index = 0; index < choiceButtons.Count; index++)
            {
                int choiceIndex = index;
                choiceButtons[index].onClick.AddListener(() => HandleChoiceClicked(choiceIndex));
            }
        }
        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 이벤트 내용을 표시하고 선택 콜백을 연결합니다.
        /// </summary>
        /// <param name="eventData">표시할 이벤트 데이터입니다.</param>
        /// <param name="onChoiceSelected">선택지 인덱스를 전달할 콜백입니다.</param>
        public void Show(DungeonEventData eventData, Action<int> onChoiceSelected)
        {
            if (eventData == null || eventData.Choices.Count == 0)
            {
                SWLog.LogError("[NodeScreenView] Show 실패: 이벤트 데이터가 없거나 선택지가 비어 있습니다.");
                return;
            }

            this.onChoiceSelected = onChoiceSelected;

            if (titleText != null)
            {
                titleText.text = eventData.DisplayName;
            }

            if (descriptionText != null)
            {
                descriptionText.text = eventData.Description;
            }

            for (int index = 0; index < choiceButtons.Count; index++)
            {
                bool isUsed = index < eventData.Choices.Count;
                choiceButtons[index].gameObject.SetActive(isUsed);

                if (isUsed && index < choiceTexts.Count && choiceTexts[index] != null)
                {
                    choiceTexts[index].text = eventData.Choices[index].ChoiceText;
                }
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        /// <summary>
        /// 화면을 숨기고 콜백 연결을 해제합니다.
        /// </summary>
        public void Hide()
        {
            onChoiceSelected = null;

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
        #endregion // 표시

        #region 핸들러
        /// <summary>
        /// 선택지 클릭을 콜백으로 전달합니다.
        /// </summary>
        /// <param name="choiceIndex">클릭한 선택지 인덱스입니다.</param>
        private void HandleChoiceClicked(int choiceIndex)
        {
            onChoiceSelected?.Invoke(choiceIndex);
        }
        #endregion // 핸들러
    }
}