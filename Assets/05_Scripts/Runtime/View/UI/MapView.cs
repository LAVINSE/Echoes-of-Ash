using System;
using System.Collections.Generic;
using EchoesOfAsh.Map;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 던전 맵의 노드와 경로를 생성하고 진행 상태를 표시합니다.
    /// </summary>
    public class MapView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("스크롤")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private RectTransform originRoot;

        [SWGroup("프리팹")]
        [SerializeField] private MapRoomView roomViewPrefab;
        [SerializeField] private Image edgePrefab;

        [SWGroup("배치")]
        [Tooltip("맵 콘텐츠의 좌우 여백입니다.")]
        [SerializeField, Min(0f)] private float contentPadding = 200f;
        [Tooltip("노드 사이 경로의 두께입니다.")]
        [SerializeField, Min(0f)] private float edgeThickness = 6f;

        [SWGroup("경로 색상")]
        [SerializeField] private Color edgeColor = new(0.5f, 0.47f, 0.42f, 0.6f);
        [SerializeField] private Color madnessEdgeColor = new(0.6f, 0.35f, 0.75f, 0.45f);

        private MapGraph mapGraph;
        private Action<int> nodeMoveRequestHandler;

        private readonly List<MapRoomView> roomViews = new();
        private readonly List<Image> edgeImages = new();
        #endregion // 필드

        #region 유니티 이벤트 함수
        /// <summary>
        /// 객체가 제거될 때 생성한 맵 표시 요소를 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            ClearMap();
        }
        #endregion // 유니티 이벤트 함수

        #region 초기화
        /// <summary>
        /// 맵 그래프를 기반으로 경로와 노드 표시 요소를 생성합니다.
        /// </summary>
        /// <param name="mapGraph">표시할 맵 그래프입니다.</param>
        /// <param name="nodeMoveRequestHandler">노드를 선택할 때 식별자와 함께 호출할 이동 요청 처리 함수입니다.</param>
        public void Initialize(MapGraph mapGraph, Action<int> nodeMoveRequestHandler)
        {
            if (!HasRequiredReferences(mapGraph))
            {
                SWLog.LogError("[MapView] Initialize 실패: 필수 참조가 없습니다.");
                return;
            }

            ClearMap();

            this.mapGraph = mapGraph;
            this.nodeMoveRequestHandler = nodeMoveRequestHandler;
            originRoot.anchoredPosition = new Vector2(contentPadding, 0f);

            CreateEdgeViews();
            CreateRoomViews();
            ResizeContentToMap();
        }

        /// <summary>
        /// 맵을 구성하는 데 필요한 참조가 모두 있는지 확인합니다.
        /// </summary>
        /// <param name="mapGraph">표시할 맵 그래프입니다.</param>
        /// <returns>필수 참조가 모두 있으면 true입니다.</returns>
        private bool HasRequiredReferences(MapGraph mapGraph)
        {
            return mapGraph != null
                && roomViewPrefab != null
                && edgePrefab != null
                && contentRoot != null
                && originRoot != null;
        }

        /// <summary>
        /// 생성한 노드와 경로 표시 요소를 제거하고 맵 참조를 해제합니다.
        /// </summary>
        public void ClearMap()
        {
            foreach (MapRoomView roomView in roomViews)
            {
                if (roomView != null)
                {
                    Destroy(roomView.gameObject);
                }
            }

            foreach (Image edgeImage in edgeImages)
            {
                if (edgeImage != null)
                {
                    Destroy(edgeImage.gameObject);
                }
            }

            roomViews.Clear();
            edgeImages.Clear();
            mapGraph = null;
            nodeMoveRequestHandler = null;
        }
        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 던전 맵 화면을 표시합니다.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 던전 맵 화면을 숨깁니다.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 모든 노드의 진행 상태를 갱신하고 현재 노드에 맞춰 화면을 이동합니다.
        /// </summary>
        /// <param name="currentNodeIdentifier">현재 위치한 노드의 식별자입니다. 진입 전이면 -1입니다.</param>
        /// <param name="availableNodes">현재 이동할 수 있는 노드 목록입니다.</param>
        public void RefreshNodeStates(int currentNodeIdentifier, IReadOnlyList<MapNode> availableNodes)
        {
            foreach (MapRoomView roomView in roomViews)
            {
                if (roomView == null || roomView.Node == null)
                {
                    continue;
                }

                bool isCurrentNode = roomView.Node.Identifier == currentNodeIdentifier;
                bool isAvailableNode = ContainsNodeIdentifier(availableNodes, roomView.Node.Identifier);
                roomView.RefreshState(isCurrentNode, isAvailableNode);
            }

            FocusOnNode(currentNodeIdentifier);
        }
        #endregion // 표시

        #region 구성
        /// <summary>
        /// 맵 그래프의 모든 경로 표시 요소를 생성하고 배치합니다.
        /// </summary>
        private void CreateEdgeViews()
        {
            foreach (MapEdge edge in mapGraph.Edges)
            {
                MapNode fromNode = mapGraph.GetNode(edge.FromNodeIdentifier);
                MapNode toNode = mapGraph.GetNode(edge.ToNodeIdentifier);

                if (fromNode == null || toNode == null)
                {
                    continue;
                }

                Image edgeImage = Instantiate(edgePrefab, originRoot);
                edgeImage.color = edge.IsMadnessOnly ? madnessEdgeColor : edgeColor;
                edgeImage.raycastTarget = false;

                PlaceEdgeView(edgeImage.rectTransform, fromNode.Position, toNode.Position);
                edgeImages.Add(edgeImage);
            }
        }

        /// <summary>
        /// 경로 표시 요소를 두 노드 사이의 중점에 배치하고 길이와 각도를 설정합니다.
        /// </summary>
        /// <param name="rectTransform">배치할 경로 표시 요소의 사각 트랜스폼입니다.</param>
        /// <param name="fromPosition">출발 노드의 좌표입니다.</param>
        /// <param name="toPosition">도착 노드의 좌표입니다.</param>
        private void PlaceEdgeView(RectTransform rectTransform, Vector2 fromPosition, Vector2 toPosition)
        {
            Vector2 direction = toPosition - fromPosition;

            rectTransform.anchoredPosition = fromPosition + direction * 0.5f;
            rectTransform.sizeDelta = new Vector2(direction.magnitude, edgeThickness);
            rectTransform.localRotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        /// <summary>
        /// 맵 그래프의 모든 노드 표시 요소를 생성하고 배치합니다.
        /// </summary>
        private void CreateRoomViews()
        {
            foreach (MapNode node in mapGraph.Nodes)
            {
                MapRoomView roomView = Instantiate(roomViewPrefab, originRoot);
                roomView.Initialize(node, HandleRoomSelected);
                roomViews.Add(roomView);
            }
        }

        /// <summary>
        /// 가장 오른쪽 노드의 좌표와 여백에 맞춰 콘텐츠 너비를 조정합니다.
        /// </summary>
        private void ResizeContentToMap()
        {
            float maximumHorizontalPosition = 0f;

            foreach (MapNode node in mapGraph.Nodes)
            {
                if (node.Position.x > maximumHorizontalPosition)
                {
                    maximumHorizontalPosition = node.Position.x;
                }
            }

            Vector2 contentSize = contentRoot.sizeDelta;
            contentSize.x = maximumHorizontalPosition + contentPadding * 2f;
            contentRoot.sizeDelta = contentSize;
        }
        #endregion // 구성

        #region 입력
        /// <summary>
        /// 맵 노드를 선택하면 등록된 이동 요청 처리 함수에 노드 식별자를 전달합니다.
        /// </summary>
        /// <param name="nodeIdentifier">선택한 노드의 식별자입니다.</param>
        private void HandleRoomSelected(int nodeIdentifier)
        {
            nodeMoveRequestHandler?.Invoke(nodeIdentifier);
        }
        #endregion // 입력

        #region 유틸리티
        /// <summary>
        /// 목록에 지정한 식별자의 노드가 있는지 확인합니다.
        /// </summary>
        /// <param name="nodes">확인할 노드 목록입니다.</param>
        /// <param name="nodeIdentifier">찾을 노드의 식별자입니다.</param>
        /// <returns>지정한 식별자의 노드가 있으면 true입니다.</returns>
        private static bool ContainsNodeIdentifier(IReadOnlyList<MapNode> nodes, int nodeIdentifier)
        {
            if (nodes == null)
            {
                return false;
            }

            for (int index = 0; index < nodes.Count; index++)
            {
                if (nodes[index].Identifier == nodeIdentifier)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 지정한 노드가 화면 중앙에 가깝게 보이도록 수평 스크롤 위치를 조정합니다.
        /// 노드에 진입하기 전에는 입구가 있는 화면 왼쪽으로 이동합니다.
        /// </summary>
        /// <param name="nodeIdentifier">화면 중앙에 표시할 노드의 식별자입니다.</param>
        private void FocusOnNode(int nodeIdentifier)
        {
            if (scrollRect == null || contentRoot == null)
            {
                return;
            }

            if (nodeIdentifier < 0 || mapGraph == null)
            {
                scrollRect.horizontalNormalizedPosition = 0f;
                return;
            }

            MapNode node = mapGraph.GetNode(nodeIdentifier);
            RectTransform viewport = scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.transform as RectTransform;

            if (node == null || viewport == null)
            {
                return;
            }

            float viewportWidth = viewport.rect.width;
            float contentWidth = contentRoot.sizeDelta.x;

            if (contentWidth <= viewportWidth)
            {
                return;
            }

            float targetHorizontalPosition = node.Position.x + contentPadding - viewportWidth * 0.5f;
            float scrollableWidth = contentWidth - viewportWidth;
            scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(targetHorizontalPosition / scrollableWidth);
        }
        #endregion // 유틸리티
    }
}
