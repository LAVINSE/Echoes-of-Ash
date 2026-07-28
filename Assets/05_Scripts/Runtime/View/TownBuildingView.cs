using System;
using EchoesOfAsh.Data;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.View
{
    /// <summary>
    /// 마을 건물의 월드 표시입니다. 씬에 수동 배치되며(위치 = 씬 소유), 콜라이더 클릭과 호버 하이라이트만 담당합니다.
    /// 어느 건물인지의 배치-데이터 연결도 씬이 소유합니다 - 이 뷰가 자신의 건물 데이터를 참조합니다 (TownConfigData는 씬 오브젝트를 참조할 수 없음).
    /// 클릭 판정은 TownInputController(단일 입력 주체)가 수행하고, 이 뷰는 알림을 받아 주입된 콜백을 호출합니다.
    /// </summary>
    public class TownBuildingView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("데이터")]
        [Tooltip("이 배치가 나타내는 건물 데이터입니다. TownConfigData의 건물 목록에 등록된 데이터여야 합니다.")]
        [SerializeField] private BuildingData buildingData;

        [SWGroup("표시")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("호버 시 곱해지는 하이라이트 색입니다.")]
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
        /// 원래 색을 1회 저장합니다 (Init/Release 순서 오염 방지 - CharacterView 투명 버그 교훈, 개정 19).
        /// </summary>
        private void Awake()
        {
            SaveOriginColor();
        }
        #endregion // 유니티 이벤트 함수

        #region 초기화
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
        /// 호버 하이라이트 표시를 전환합니다.
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
        /// 입력 주체가 클릭을 판정했을 때 호출합니다. 주입된 콜백을 실행합니다.
        /// </summary>
        public void NotifyClicked()
        {
            onClicked?.Invoke();
        }
    }
}