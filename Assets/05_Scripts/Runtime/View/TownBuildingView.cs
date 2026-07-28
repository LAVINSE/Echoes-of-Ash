using System;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.View
{
    /// <summary>
    /// 마을 건물 VIEW
    /// </summary>
    public class TownBuildingView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private SpriteRenderer buildingSprite;
        [Tooltip("호버 시 곱해지는 하이라이트 색입니다.")]
        [SerializeField] private Color highlightColor = new(1.15f, 1.15f, 1.15f, 1f);

        private Color originColor = Color.white;
        private bool isOriginColorSaved;
        private Action onClicked;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 원래 색을 1회 저장합니다 (Init/Release 순서 오염 방지 - CharacterView 투명 버그 교훈, 개정 19).
        /// </summary>
        private void Awake()
        {
            SaveOriginColor();
        }

        /// <summary>
        /// 클릭 콜백을 연결합니다.
        /// </summary>
        /// <param name="onClicked">건물이 클릭될 때 호출됩니다.</param>
        public void Init(Action onClicked)
        {
            if (onClicked == null)
            {
                SWLog.LogError("[TownBuildingView] Init 실패: 클릭 콜백이 없습니다.");
                return;
            }

            Release();
            this.onClicked = onClicked;
        }

        /// <summary>
        /// 콜백 연결을 해제하고 하이라이트를 되돌립니다.
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
            if (isOriginColorSaved || buildingSprite == null)
            {
                return;
            }

            originColor = buildingSprite.color;
            isOriginColorSaved = true;
        }
        #endregion // 초기화

        /// <summary>
        /// 호버 하이라이트 표시를 전환합니다.
        /// </summary>
        /// <param name="isHighlighted">하이라이트 여부입니다.</param>
        public void SetHighlighted(bool isHighlighted)
        {
            if (buildingSprite == null)
            {
                return;
            }

            SaveOriginColor();
            buildingSprite.color = isHighlighted ? originColor * highlightColor : originColor;
        }

        /// <summary>
        /// 입력 주체가 클릭을 판정했을 때 호출합니다. 주입된 콜백을 실행합니다.
        /// </summary>
        public void NotifyClicked()
        {
            onClicked?.Invoke();
        }
    }
}