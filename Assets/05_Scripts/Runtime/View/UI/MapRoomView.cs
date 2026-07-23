using System;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Map;
using SW.Attributes;
using SW.Base;
using SW.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 하나의 맵 노드 상태를 표시하고 노드 선택 입력을 전달합니다.
    /// </summary>
    public class MapRoomView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private GameObject highlightRoot;

        [SWGroup("상태 색상")]
        [SerializeField] private Color normalColor = new(0.35f, 0.33f, 0.30f, 1f);
        [SerializeField] private Color visitedColor = new(0.22f, 0.21f, 0.19f, 1f);
        [SerializeField] private Color currentColor = new(0.85f, 0.75f, 0.45f, 1f);
        [SerializeField] private Color ashConsumedColor = new(0.15f, 0.14f, 0.13f, 0.6f);
        [SerializeField] private Color madnessLabelColor = new(0.75f, 0.45f, 0.9f, 1f);

        private MapNode node;
        private Action<int> nodeSelectionHandler;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 표시하는 맵 노드입니다.</summary>
        public MapNode Node => node;
        #endregion // 프로퍼티

        #region 유니티 이벤트 함수
        /// <summary>
        /// 버튼 선택 이벤트를 구독합니다.
        /// </summary>
        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleButtonClicked);
            }
        }

        /// <summary>
        /// 버튼 선택 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleButtonClicked);
            }
        }
        #endregion // 유니티 이벤트 함수

        #region 초기화
        /// <summary>
        /// 표시할 맵 노드와 노드 선택 처리 함수를 설정하고 화면 좌표에 배치합니다.
        /// </summary>
        /// <param name="node">표시할 맵 노드입니다.</param>
        /// <param name="nodeSelectionHandler">노드를 선택할 때 식별자와 함께 호출할 처리 함수입니다.</param>
        public void Initialize(MapNode node, Action<int> nodeSelectionHandler)
        {
            this.node = node;
            this.nodeSelectionHandler = nodeSelectionHandler;

            if (node == null)
            {
                SWLog.LogError("[MapRoomView] Initialize 실패: 표시할 맵 노드가 없습니다.");
                return;
            }

            RectTransform rectTransform = transform as RectTransform;

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = node.Position;
            }

            if (labelText != null)
            {
                string madnessOnlyMark = node.IsMadnessOnly ? " †" : string.Empty;
                labelText.text = GetNodeTypeDisplayName(node.NodeType) + madnessOnlyMark;
                labelText.color = node.IsMadnessOnly ? madnessLabelColor : Color.white;
            }

            RefreshState(false, false);
        }
        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 맵 노드의 진행 상태와 이동 가능 여부를 화면에 반영합니다.
        /// 잿불 잠식, 현재 위치, 방문 완료, 기본 상태 순서로 색상을 결정합니다.
        /// </summary>
        /// <param name="isCurrentNode">현재 위치한 노드인지 여부입니다.</param>
        /// <param name="isAvailableNode">이동할 수 있는 노드인지 여부입니다.</param>
        public void RefreshState(bool isCurrentNode, bool isAvailableNode)
        {
            if (node == null)
            {
                return;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = GetBackgroundColor(isCurrentNode);
            }

            if (highlightRoot != null)
            {
                highlightRoot.SetActive(isAvailableNode);
            }

            if (button != null)
            {
                button.interactable = isAvailableNode;
            }
        }

        /// <summary>
        /// 맵 노드의 진행 상태에 해당하는 배경 색상을 반환합니다.
        /// </summary>
        /// <param name="isCurrentNode">현재 위치한 노드인지 여부입니다.</param>
        /// <returns>맵 노드에 적용할 배경 색상입니다.</returns>
        private Color GetBackgroundColor(bool isCurrentNode)
        {
            if (node.IsAshConsumed)
            {
                return ashConsumedColor;
            }

            if (isCurrentNode)
            {
                return currentColor;
            }

            return node.IsVisited ? visitedColor : normalColor;
        }

        /// <summary>
        /// 버튼 선택 시 현재 맵 노드의 식별자를 전달합니다.
        /// </summary>
        private void HandleButtonClicked()
        {
            if (node != null)
            {
                nodeSelectionHandler?.Invoke(node.Identifier);
            }
        }

        /// <summary>
        /// 맵 노드 타입의 한글 표시 이름을 반환합니다.
        /// </summary>
        /// <param name="nodeType">표시 이름을 조회할 맵 노드 타입입니다.</param>
        /// <returns>맵 노드 타입의 한글 표시 이름입니다.</returns>
        private static string GetNodeTypeDisplayName(EMapNodeType nodeType)
        {
            switch (nodeType)
            {
                case EMapNodeType.Battle:
                    return "전투";
                case EMapNodeType.Elite:
                    return "엘리트";
                case EMapNodeType.Rest:
                    return "휴식";
                case EMapNodeType.Event:
                    return "이벤트";
                case EMapNodeType.Shop:
                    return "상점";
                case EMapNodeType.Storage:
                    return "보관";
                case EMapNodeType.Boss:
                    return "보스";
                default:
                    return nodeType.ToString();
            }
        }
        #endregion // 표시
    }
}
