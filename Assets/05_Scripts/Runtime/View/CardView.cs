using EchoesOfAsh.Card;
using SW.Attributes;
using SW.Base;
using SW.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace EchoesOfAsh.View
{
    /// <summary>
    /// 카드 표시 뷰
    /// </summary>
    public class CardView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private SpriteRenderer frameRenderer;
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text apCostText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private GameObject sanityMarker;

        [SWGroup("정렬")]
        /// <summary>카드 단위 정렬용 소팅 그룹입니다. (루트에 부착)</summary>
        [SerializeField] private SortingGroup sortingGroup;

        [SWGroup("강조")]
        /// <summary>강조 연출이 적용될 시각 요소 루트입니다. (루트 트랜스폼은 배치 소유 — 건드리지 않음)</summary>
        [SerializeField] private Transform visualRoot;
        /// <summary>호버 시 확대 배율입니다.</summary>
        [SerializeField, Min(1f)] private float hoverScale = 1.15f;
        /// <summary>호버 시 위로 올라가는 높이입니다.</summary>
        [SerializeField, Min(0f)] private float hoverRaise = 0.35f;
        /// <summary>호버 시 이웃 카드 위로 올라갈 소팅 오더입니다.</summary>
        [SerializeField, Min(0)] private int hoverSortingOrder = 1;

        [SWGroup("색상")]
        [SerializeField] private Color playableColor = Color.white;
        [SerializeField] private Color unplayableColor = new(0.55f, 0.55f, 0.55f, 1f);

        private CardInstance cardInstance;
        private bool isPlayable = true;
        private bool isHovered;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>표시 중인 카드</summary>
        public CardInstance CardInstance => cardInstance;
        /// <summary>현재 사용 가능 표시 상태</summary>
        public bool IsPlayable => isPlayable;
        /// <summary>현재 호버 강조 상태입니다.</summary>
        public bool IsHovered => isHovered;
        #endregion // 프로퍼티

        #region 초기화
        private void OnDisable()
        {
            SetHovered(false);
        }
        
        /// <summary>
        /// 표시할 카드를 지정하고 화면을 갱신한다
        /// </summary>
        /// <param name="cardInstance">표시할 카드</param>
        public void Init(CardInstance cardInstance)
        {
            this.cardInstance = cardInstance;
            Refresh();
        }

        /// <summary>
        /// 현재 카드 상태로 표시를 갱신힌다
        /// </summary>
        public void Refresh()
        {
            if (cardInstance == null)
            {
                return;
            }

            if (nameText != null)
            {
                nameText.text = cardInstance.DisplayName;
            }

            if (apCostText != null)
            {
                apCostText.text = cardInstance.ApCost.ToString();
            }

            if (typeText != null)
            {
                typeText.text = cardInstance.CurrentCardData.CardType.ToString();
            }

            if (iconRenderer != null)
            {
                iconRenderer.sprite = cardInstance.CurrentCardData.CardIconSprite;
            }

            if (sanityMarker != null)
            {
                sanityMarker.SetActive(cardInstance.IsSanityEffect);
            }

            ApplyPlayableTint();
        }
        #endregion // 초기화

        /// <summary>
        /// 사용 가능 여부 표시를 변경한다
        /// </summary>
        /// <param name="isPlayable">사용 가능 여부</param>
        public void SetPlayable(bool isPlayable)
        {
            if (this.isPlayable == isPlayable)
            {
                return;
            }

            this.isPlayable = isPlayable;
            ApplyPlayableTint();
        }

        public void SetHovered(bool isHovered)
        {
            if (this.isHovered == isHovered)
            {
                return;
            }

            this.isHovered = isHovered;

            if (visualRoot != null)
            {
                visualRoot.localScale = isHovered
                    ? Vector3.one * hoverScale
                    : Vector3.one;

                visualRoot.localPosition = isHovered
                    ? new Vector3(0f, hoverRaise, 0f)
                    : Vector3.zero;
            }

            if (sortingGroup != null)
            {
                sortingGroup.sortingOrder = isHovered ? hoverSortingOrder : 0;
            }
        }

        /// <summary>
        /// 카드의 모든 렌더러의 소팅 레이어를 변경한다
        /// 드래그 시작/종료 시 전환에 사용
        /// </summary>
        /// <param name="sortingLayerName">적용할 소팅 레이어 이름</param>
        public void SetSortingLayer(string sortingLayerName)
        {
            if (sortingGroup == null)
            {
                SWLog.LogError($"[CardView] {name}: SortingGroup이 연결되지 않았습니다");
                return;
            }

            sortingGroup.sortingLayerName = sortingLayerName;
        }

        /// <summary>
        /// 사용 가능 여부에 따른 틴트를 프레임과 아이콘에 적용한다
        /// </summary>
        private void ApplyPlayableTint()
        {
            Color tint = isPlayable ? playableColor : unplayableColor;

            if (frameRenderer != null)
            {
                frameRenderer.color = tint;
            }

            if (iconRenderer != null)
            {
                iconRenderer.color = tint;
            }
        }
    }
}