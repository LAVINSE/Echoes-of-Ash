using EchoesOfAsh.Card;
using SW.Attributes;
using SW.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 카드의 상태와 상호작용 연출을 표시하는 뷰입니다.
    /// </summary>
    public class CardView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [FormerlySerializedAs("frameImg")]
        [SerializeField] private Image frameImage;
        [FormerlySerializedAs("iconImg")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI apCostText;
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private GameObject sanityMarker;

        /// <summary>강조 연출이 적용되는 시각 요소의 루트입니다. 배치용 루트 트랜스폼에는 영향을 주지 않습니다.</summary>
        [SWGroup("강조")]
        [SerializeField] private Transform visualRoot;
        /// <summary>마우스가 카드를 가리킬 때 적용할 확대 비율입니다.</summary>
        [SerializeField, Min(1f)] private float hoverScale = 1.15f;
        /// <summary>마우스가 카드를 가리킬 때 위로 올릴 높이입니다.</summary>
        [SerializeField, Min(0f)] private float hoverRaise = 40f;

        [SWGroup("색상")]
        [SerializeField] private Color playableColor = Color.white;
        [SerializeField] private Color unplayableColor = new(0.55f, 0.55f, 0.55f, 1f);

        private CardInstance cardInstance;
        private bool isPlayable = true;
        private bool isHovered;
        private bool isDragging;
        private bool isPromoted;
        private int cachedSiblingIndex = -1;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>표시 중인 카드입니다.</summary>
        public CardInstance CardInstance => cardInstance;
        /// <summary>현재 사용 가능 표시 상태입니다.</summary>
        public bool IsPlayable => isPlayable;
        /// <summary>현재 카드가 강조되어 있는지 여부입니다.</summary>
        public bool IsHovered => isHovered;
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 비활성화될 때 카드의 강조와 표시 상태를 초기화합니다.
        /// </summary>
        private void OnDisable()
        {
            SetHovered(false);
            SetDragging(false);

            isPromoted = false;
            cachedSiblingIndex = -1;
        }

        /// <summary>
        /// 표시할 카드를 지정하고 화면을 갱신합니다.
        /// </summary>
        /// <param name="cardInstance">표시할 카드입니다.</param>
        public void Init(CardInstance cardInstance)
        {
            this.cardInstance = cardInstance;
            Refresh();
        }

        /// <summary>
        /// 현재 카드 상태로 표시를 갱신합니다.
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

            if (iconImage != null)
            {
                Sprite iconSprite = cardInstance.CurrentCardData.CardIconSprite;

                iconImage.sprite = iconSprite;
                iconImage.enabled = iconSprite != null;
            }

            if (sanityMarker != null)
            {
                sanityMarker.SetActive(cardInstance.IsSanityEffect);
            }

            ApplyPlayableTint();
        }
        #endregion // 초기화

        /// <summary>
        /// 사용 가능 여부 표시를 변경합니다.
        /// </summary>
        /// <param name="isPlayable">사용 가능 여부입니다.</param>
        public void SetPlayable(bool isPlayable)
        {
            if (this.isPlayable == isPlayable)
            {
                return;
            }

            this.isPlayable = isPlayable;
            ApplyPlayableTint();
        }

        /// <summary>
        /// 포인터가 카드 위에 있는지에 따라 강조 상태를 변경합니다.
        /// </summary>
        /// <param name="isHovered">포인터가 카드 위에 있으면 <see langword="true"/>입니다.</param>
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

            UpdatePromotion();
        }

        /// <summary>
        /// 드래그 상태를 변경합니다.
        /// 드래그 시작/종료 시 그리기 순서 승격 전환에 사용합니다.
        /// </summary>
        /// <param name="isDragging">드래그 여부입니다.</param>
        public void SetDragging(bool isDragging)
        {
            if (this.isDragging == isDragging)
            {
                return;
            }

            this.isDragging = isDragging;
            UpdatePromotion();
        }

        /// <summary>
        /// 카드의 강조와 드래그 상태에 맞춰 그리기 순서를 갱신합니다.
        /// </summary>
        private void UpdatePromotion()
        {
            bool shouldPromote = isHovered || isDragging;

            if (shouldPromote == isPromoted)
            {
                return;
            }

            isPromoted = shouldPromote;

            if (isPromoted)
            {
                cachedSiblingIndex = transform.GetSiblingIndex();
                transform.SetAsLastSibling();
                return;
            }

            if (cachedSiblingIndex >= 0)
            {
                transform.SetSiblingIndex(cachedSiblingIndex);
                cachedSiblingIndex = -1;
            }
        }

        /// <summary>
        /// 사용 가능 여부에 따른 틴트를 프레임과 아이콘에 적용합니다.
        /// </summary>
        private void ApplyPlayableTint()
        {
            Color tint = isPlayable ? playableColor : unplayableColor;

            if (frameImage != null)
            {
                frameImage.color = tint;
            }

            if (iconImage != null)
            {
                iconImage.color = tint;
            }
        }
    }
}
