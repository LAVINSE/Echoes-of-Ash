using EchoesOfAsh.View;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Town
{
    /// <summary>
    /// 마을 월드 입력의 단일 주체입니다 (CardDragController 전례 - 포인터 폴링 + OverlapPoint).
    /// 건물 콜라이더 위 호버 하이라이트와 클릭 알림만 담당하며, 클릭의 의미는 건물 뷰에 주입된 콜백이 결정합니다.
    /// 팝업이 열리는 동안은 조립 지점이 입력을 끕니다 (팝업 = 모달).
    /// </summary>
    public class TownInputController : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("판정")]
        [Tooltip("월드 좌표 변환에 사용할 카메라입니다. 미배선이면 메인 카메라를 사용합니다.")]
        [SerializeField] private Camera worldCamera;
        [Tooltip("건물 콜라이더가 속한 물리 레이어입니다.")]
        [SerializeField] private LayerMask buildingLayerMask;

        private TownBuildingView hoveredBuildingView;
        private bool isInputEnabled = true;
        #endregion // 필드

        #region 유니티 이벤트 함수
        /// <summary>
        /// 매 프레임 포인터 위치의 건물을 판정해 호버와 클릭을 처리합니다.
        /// </summary>
        private void Update()
        {
            if (!isInputEnabled)
            {
                return;
            }

            Camera pointerCamera = worldCamera != null ? worldCamera : Camera.main;

            if (pointerCamera == null)
            {
                return;
            }

            Vector2 pointerPosition = pointerCamera.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hitCollider = Physics2D.OverlapPoint(pointerPosition, buildingLayerMask);
            TownBuildingView pointedView = hitCollider != null
                ? hitCollider.GetComponentInParent<TownBuildingView>()
                : null;

            RefreshHover(pointedView);

            if (pointedView != null && Input.GetMouseButtonDown(0))
            {
                pointedView.NotifyClicked();
            }
        }

        /// <summary>
        /// 비활성화될 때 호버 상태를 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            RefreshHover(null);
        }
        #endregion // 유니티 이벤트 함수

        #region 입력
        /// <summary>
        /// 월드 입력을 켜거나 끕니다. 끌 때는 호버 상태도 정리합니다 (팝업 모달 처리용).
        /// </summary>
        /// <param name="isEnabled">입력 활성 여부입니다.</param>
        public void SetInputEnabled(bool isEnabled)
        {
            isInputEnabled = isEnabled;

            if (!isEnabled)
            {
                RefreshHover(null);
            }
        }

        /// <summary>
        /// 호버 대상 변화를 반영합니다. 이전 대상의 하이라이트를 끄고 새 대상을 켭니다.
        /// </summary>
        /// <param name="pointedView">현재 포인터가 가리키는 건물 뷰입니다. 없으면 null입니다.</param>
        private void RefreshHover(TownBuildingView pointedView)
        {
            if (hoveredBuildingView == pointedView)
            {
                return;
            }

            if (hoveredBuildingView != null)
            {
                hoveredBuildingView.SetHighlighted(false);
            }

            hoveredBuildingView = pointedView;

            if (hoveredBuildingView != null)
            {
                hoveredBuildingView.SetHighlighted(true);
            }
        }
        #endregion // 입력
    }
}