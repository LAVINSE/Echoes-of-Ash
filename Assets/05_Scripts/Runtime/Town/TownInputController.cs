using EchoesOfAsh.View;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Town
{
    /// <summary>
    /// 마우스 위치의 건물을 찾고 강조 표시와 클릭 입력을 처리합니다.
    /// 마우스가 건물 위에 있을 때 강조 표시하고 클릭된 건물에 입력을 전달합니다.
    /// 다른 화면이 열려 있으면 외부에서 입력을 끌 수 있습니다.
    /// </summary>
    public class TownInputController : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("판정")]
        [Tooltip("화면 좌표를 게임 공간의 좌표로 바꿀 때 사용할 카메라입니다. 지정하지 않으면 메인 카메라를 사용합니다.")]
        [SerializeField] private Camera worldCamera;
        [Tooltip("건물 콜라이더가 속한 물리 레이어입니다.")]
        [SerializeField] private LayerMask buildingLayerMask;

        private TownBuildingView hoveredBuildingView;
        private bool isInputEnabled = true;
        #endregion // 필드

        #region 유니티 이벤트 함수
        /// <summary>
        /// 매 프레임 마우스 아래의 건물을 찾아 강조 표시와 클릭을 처리합니다.
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
        /// 비활성화될 때 건물 강조 표시를 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            RefreshHover(null);
        }
        #endregion // 유니티 이벤트 함수

        #region 입력
        /// <summary>
        /// 마을 건물 입력을 켜거나 끕니다. 입력을 끄면 건물 강조 표시도 해제합니다.
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
        /// 마우스가 가리키는 건물이 바뀌면 이전 강조를 끄고 새 건물을 강조합니다.
        /// </summary>
        /// <param name="pointedView">현재 마우스가 가리키는 건물 화면입니다. 없으면 <see langword="null"/>입니다.</param>
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
