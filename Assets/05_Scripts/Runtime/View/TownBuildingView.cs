using System;
using EchoesOfAsh.Data;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.View
{
    /// <summary>
    /// 장면에 배치된 마을 건물을 표시하고 마우스가 가리킬 때 강조합니다.
    /// 각 건물 화면은 자신이 나타내는 건물 데이터를 직접 참조합니다.
    /// 클릭 여부는 <see cref="EchoesOfAsh.Town.TownInputController"/>가 확인하며, 이 클래스는 전달받은 클릭 동작을 실행합니다.
    /// </summary>
    public class TownBuildingView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("데이터")]
        [Tooltip("이 배치가 나타내는 건물 데이터입니다. TownConfigData의 건물 목록에 등록된 데이터여야 합니다.")]
        [SerializeField] private BuildingData buildingData;

        [SWGroup("표시")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("마우스가 건물을 가리킬 때 적용할 강조 색입니다.")]
        [SerializeField] private Color highlightColor = new(1.15f, 1.15f, 1.15f, 1f);

        private Color originColor = Color.white;
        private bool isOriginColorSaved;
        private Action onClicked;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이 배치가 나타내는 건물 데이터입니다.</summary>
        public BuildingData BuildingData => buildingData;
        #endregion // 프로퍼티

        #region 유니티 이벤트 함수
        /// <summary>
        /// 강조 표시를 해제할 때 복원할 원래 색상을 저장합니다.
        /// </summary>
        private void Awake()
        {
            SaveOriginColor();
        }
        #endregion // 유니티 이벤트 함수

        #region 초기화
        /// <summary>
        /// 건물이 클릭될 때 실행할 동작을 연결합니다.
        /// </summary>
        /// <param name="onClicked">건물이 클릭될 때 호출됩니다.</param>
        public void Init(Action onClicked)
        {
            if (onClicked == null)
            {
                SWLog.LogError("[TownBuildingView] Init 실패: 클릭 동작이 없습니다.");
                return;
            }

            Release();
            this.onClicked = onClicked;
        }

        /// <summary>
        /// 클릭 동작을 해제하고 강조 표시를 되돌립니다.
        /// </summary>
        public void Release()
        {
            onClicked = null;
            SetHighlighted(false);
        }

        /// <summary>
        /// 원래 색을 아직 저장하지 않았으면 저장합니다.
        /// </summary>
        private void SaveOriginColor()
        {
            if (isOriginColorSaved || spriteRenderer == null)
            {
                return;
            }

            originColor = spriteRenderer.color;
            isOriginColorSaved = true;
        }
        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 건물의 강조 표시를 켜거나 끕니다.
        /// </summary>
        /// <param name="isHighlighted">하이라이트 여부입니다.</param>
        public void SetHighlighted(bool isHighlighted)
        {
            if (spriteRenderer == null)
            {
                return;
            }

            SaveOriginColor();
            spriteRenderer.color = isHighlighted ? originColor * highlightColor : originColor;
        }
        #endregion // 표시

        /// <summary>
        /// 건물이 클릭되었을 때 연결된 동작을 실행합니다.
        /// </summary>
        public void NotifyClicked()
        {
            onClicked?.Invoke();
        }
    }
}
