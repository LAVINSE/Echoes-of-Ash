using System.Collections.Generic;
using System.Text;
using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Attributes;
using SW.Base;
using TMPro;
using UnityEngine;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 카드 유형, 비용 및 효과 설명을 화면에 표시하는 도움말 뷰입니다.
    /// </summary>
    public class CardTooltipView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private GameObject displayRoot;
        [SerializeField] private TextMeshProUGUI headerText;
        [SerializeField] private TextMeshProUGUI calmEffectText;
        [SerializeField] private GameObject madnessRoot;
        [SerializeField] private TextMeshProUGUI madnessEffectText;

        [SWGroup("배치")]
        [Tooltip("카드 절반 폭(캔버스 단위) - 카드는 폭이 균일하므로 고정값으로 보관")]
        [SerializeField] private float cardHalfWidth = 80f;
        [Tooltip("카드 가장자리와 툴팁 사이 가로 여백(캔버스 단위)")]
        [SerializeField] private float sideGap = 24f;
        [Tooltip("세로 정렬 보정 - 카드 중심 기준 위/아래 이동")]
        [SerializeField] private float verticalOffset = 0f;
        [Tooltip("화면 가장자리 최소 여백 - 이보다 밖으로 나가면 반대편으로 뒤집는다")]
        [SerializeField] private float screenPadding = 16f;
        [Tooltip("좌표 변환에 쓸 카메라 (미지정 시 Camera.main)")]
        [SerializeField] private Camera uiCamera;

        [SWGroup("정신력 구간 색상")]
        [SerializeField] private Color calmColor = new(0.35f, 0.65f, 0.95f, 1f);
        [SerializeField] private Color madnessColor = new(0.75f, 0.25f, 0.85f, 1f);
        [Tooltip("현재 구간이 아닌 쪽 텍스트 색")]
        [SerializeField] private Color dimColor = new(0.55f, 0.55f, 0.55f, 1f);

        private ISanityHolder sanityHolder;
        private CardInstance currentCard;
        private RectTransform rectTransform;

        private readonly StringBuilder stringBuilder = new();
        #endregion // 필드


        #region 초기화
        /// <summary>
        /// 툴팁의 사각 변환 참조를 저장하고 초기 표시 상태를 설정합니다.
        /// </summary>
        private void Awake()
        {
            this.rectTransform = this.transform as RectTransform;

            if (uiCamera == null)
            {
                uiCamera = Camera.main;
            }
        }

        /// <summary>
        /// 객체가 제거될 때 툴팁 상태를 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            Release();
        }

        /// <summary>
        /// 파티 정신력을 연결합니다.
        /// 구간 전환 시 강조를 실시간 갱신합니다.
        /// </summary>
        /// <param name="sanityHolder">파티 공유 정신력입니다.</param>
        public void Init(ISanityHolder sanityHolder)
        {
            this.sanityHolder = sanityHolder;

            if (sanityHolder != null)
            {
                sanityHolder.OnSanityTypeChanged += HandleSanityTypeChanged;
            }

            Hide();
        }

        /// <summary>
        /// 정신력 이벤트 구독을 해제하고 현재 카드 참조를 정리합니다.
        /// </summary>
        public void Release()
        {
            if (sanityHolder != null)
            {
                sanityHolder.OnSanityTypeChanged -= HandleSanityTypeChanged;
                sanityHolder = null;
            }

            Hide();
        }
        #endregion // 초기화

        /// <summary>
        /// 카드 툴팁을 표시합니다.
        /// </summary>
        /// <param name="card">표시할 카드입니다.</param>
        /// <param name="anchorWorldPosition">호버 카드의 월드 좌표입니다.</param>
        public void Show(CardInstance card, Vector3 anchorWorldPosition)
        {
            if (card == null)
            {
                Hide();
                return;
            }

            currentCard = card;

            if (displayRoot != null)
            {
                displayRoot.SetActive(true);
            }

            // 내용을 먼저 채워야 레이아웃 폭이 확정되어 플립 판정이 정확하다
            RefreshContent();
            RefreshHighlight();

            PlaceBesideCard(anchorWorldPosition);
        }

        /// <summary>
        /// 카드 툴팁을 숨깁니다.
        /// </summary>
        public void Hide()
        {
            currentCard = null;

            if (displayRoot != null)
            {
                displayRoot.SetActive(false);
            }
        }

        /// <summary>
        /// 카드 오른쪽에 툴팁을 배치하며, 화면을 벗어나면 왼쪽에 배치합니다.
        /// </summary>
        /// <param name="anchorWorldPosition">호버 카드의 월드 좌표(중심)입니다.</param>
        private void PlaceBesideCard(Vector3 anchorWorldPosition)
        {
            // 툴팁 폭의 절반 - 레이아웃 갱신 후 실제 폭을 읽는다 (없으면 0)
            float tooltipHalfWidth = rectTransform != null ? rectTransform.rect.width * 0.5f : 0f;

            // 카드 오른쪽 가장자리 + 여백 + 툴팁 반쪽 = 툴팁 중심 (오른쪽 배치 기준)
            float rightCenterX = anchorWorldPosition.x + cardHalfWidth + sideGap + tooltipHalfWidth;
            float leftCenterX = anchorWorldPosition.x - cardHalfWidth - sideGap - tooltipHalfWidth;

            // 오른쪽 배치 시 툴팁 오른쪽 끝의 월드 좌표
            Vector3 rightEdge = new(rightCenterX + tooltipHalfWidth, anchorWorldPosition.y, anchorWorldPosition.z);

            // 오른쪽 배치가 화면 오른쪽을 넘으면 왼쪽으로 뒤집습니다 (Slay the Spire 방식)
            bool fitsRight = !ExceedsRightEdge(rightEdge);
            float centerX = fitsRight ? rightCenterX : leftCenterX;

            transform.position = new Vector3
            (
                centerX,
                anchorWorldPosition.y + verticalOffset,
                anchorWorldPosition.z
            );
        }

        /// <summary>
        /// 주어진 월드 좌표가 화면 오른쪽 경계(여백 포함)를 넘는지 판정합니다.
        /// </summary>
        /// <param name="worldRightEdge">검사할 오른쪽 끝 월드 좌표입니다.</param>
        /// <returns>화면 밖으로 나가면 <see langword="true"/>입니다.</returns>
        private bool ExceedsRightEdge(Vector3 worldRightEdge)
        {
            if (uiCamera == null)
            {
                return false;
            }

            Vector3 screenPoint = uiCamera.WorldToScreenPoint(worldRightEdge);
            return screenPoint.x > Screen.width - screenPadding;
        }

        /// <summary>
        /// 카드 내용을 텍스트로 갱신합니다.
        /// </summary>
        private void RefreshContent()
        {
            CardData cardData = currentCard.CurrentCardData;

            if (headerText != null)
            {
                string sanityTag = currentCard.IsSanityEffect ? "  [정신력 반응]" : "";
                headerText.text = $"{currentCard.DisplayName}  AP {currentCard.ApCost}  [{GetCardTypeText(cardData.CardType)}]{sanityTag}";
            }

            if (calmEffectText != null)
            {
                string label = currentCard.IsSanityEffect ? "평정: " : "";
                calmEffectText.text = label + BuildDescription(cardData.Effects);
            }

            if (madnessRoot != null)
            {
                madnessRoot.SetActive(currentCard.IsSanityEffect);
            }

            if (madnessEffectText != null && currentCard.IsSanityEffect)
            {
                madnessEffectText.text = "광기: " + BuildDescription(cardData.SanityEffects);
            }
        }

        /// <summary>
        /// 현재 정신력 구간에 맞게 강조 색을 갱신합니다.
        /// 반응형이 아니면 평정 텍스트를 기본 강조로 유지합니다.
        /// </summary>
        private void RefreshHighlight()
        {
            if (currentCard == null)
            {
                return;
            }

            bool isMadness = sanityHolder != null && sanityHolder.CurrentSanityType == ESanityType.Madness;

            if (calmEffectText != null)
            {
                calmEffectText.color = !currentCard.IsSanityEffect || !isMadness ? calmColor : dimColor;
            }

            if (madnessEffectText != null)
            {
                madnessEffectText.color = isMadness ? madnessColor : dimColor;
            }
        }

        /// <summary>
        /// 효과 블록 설명을 줄바꿈으로 조합합니다.
        /// </summary>
        /// <param name="effects">효과 블록 목록입니다.</param>
        /// <returns>조합된 설명 문자열입니다.</returns>
        private string BuildDescription(IReadOnlyList<EffectBlock> effects)
        {
            if (effects == null || effects.Count == 0)
            {
                return "효과 없음";
            }

            stringBuilder.Clear();

            for (int index = 0; index < effects.Count; index++)
            {
                if (effects[index] == null)
                {
                    continue;
                }

                if (stringBuilder.Length > 0)
                {
                    stringBuilder.AppendLine();
                }

                stringBuilder.Append(effects[index].GetDescription());
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 카드 유형의 표시 이름을 반환합니다.
        /// </summary>
        /// <param name="cardType">카드 유형입니다.</param>
        /// <returns>표시 이름입니다.</returns>
        private static string GetCardTypeText(ECardType cardType)
        {
            switch (cardType)
            {
                case ECardType.Attack: return "공격";
                case ECardType.Defense: return "방어";
                case ECardType.Skill: return "스킬";
                case ECardType.Power: return "파워";
                case ECardType.Curse: return "저주";
                default: return cardType.ToString();
            }
        }

        /// <summary>
        /// 정신력 구간 전환 시 강조를 갱신합니다.
        /// </summary>
        /// <param name="sanityType">현재 정신력 유형입니다.</param>
        private void HandleSanityTypeChanged(ESanityType sanityType)
        {
            RefreshHighlight();
        }
    }
}
