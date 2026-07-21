using SW.Attributes;
using SW.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 사용자 인터페이스에서 현재 값과 정신력 전환 지점을 표시하는 게이지 뷰입니다.
    /// </summary>
    public class UIGaugeView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [FormerlySerializedAs("fillImg")]
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI valueText;

        [SWGroup("정신력 전환")]
        [SerializeField] private RectTransform sanityMarkerRect;

#if UNITY_EDITOR
        [SWGroup("테스트")]
        /// <summary>슬라이더 조작 시 게이지를 즉시 갱신할지 여부입니다.</summary>
        [SerializeField] private bool testLiveUpdate;
        [SerializeField, Range(0f, 1f)] private float testFillRatio = 1f;
        [SerializeField, Range(0f, 1f)] private float testThresholdRatio = 0.3f;
        [SerializeField, Min(1)] private int testMaxValue = 100;
#endif

        [SWGroup("보간 연출")]
        [Tooltip("초당 비율 변화량 - 0이면 보간 없이 즉시 반영")]
        [SerializeField, Min(0f)] private float lerpSpeed = 2f;

        private int currentValue;
        private int maxValue;

        private float targetRatio;
        private float displayedRatio;
        private bool hasValue;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 표시 값입니다.</summary>
        public int CurrentValue => currentValue;
        /// <summary>최대 표시 값입니다.</summary>
        public int MaxValue => maxValue;
        #endregion // 프로퍼티

        private void Update()
        {
            if (Mathf.Approximately(displayedRatio, targetRatio))
            {
                return;
            }

            displayedRatio = Mathf.MoveTowards(displayedRatio, targetRatio, lerpSpeed * Time.deltaTime);
            ApplyFill();
        }

        /// <summary>
        /// 게이지 값을 갱신합니다.
        /// </summary>
        /// <param name="current">현재 값입니다.</param>
        /// <param name="max">최대 값입니다.</param>
        public void SetValue(int current, int max)
        {
            currentValue = Mathf.Max(0, current);
            maxValue = Mathf.Max(0, max);

            targetRatio = maxValue > 0 ? Mathf.Clamp01((float)currentValue / maxValue) : 0f;

            // 첫 값이거나 보간이 꺼져 있으면 즉시 반영한다
            if (!hasValue || lerpSpeed <= 0f)
            {
                hasValue = true;
                displayedRatio = targetRatio;
                ApplyFill();
            }

            if (valueText != null)
            {
                valueText.text = $"{currentValue}/{maxValue}";
            }
        }

        /// <summary>
        /// 표시 비율을 채움 이미지에 적용한다
        /// </summary>
        private void ApplyFill()
        {
            if (fillImage != null)
            {
                fillImage.fillAmount = displayedRatio;
            }
        }

        /// <summary>
        /// 색을 변경한다
        /// </summary>
        /// <param name="color">적용할 색</param>
        public void SetFillColor(Color color)
        {
            if (fillImage != null)
            {
                fillImage.color = color;
            }
        }

        /// <summary>
        /// 정신력 전환 마커를 바 해당 비율 위치에 배치한다
        /// </summary>
        /// <param name="threshold">전환 임계값</param>
        /// <param name="max">최대 값</param>
        public void SetSanityMarker(int threshold, int max)
        {
            if (sanityMarkerRect == null)
            {
                return;
            }

            if (threshold <= 0 || max <= 0)
            {
                sanityMarkerRect.gameObject.SetActive(false);
                return;
            }

            float ratio = Mathf.Clamp01((float)threshold / max);

            sanityMarkerRect.gameObject.SetActive(true);

            // 앵커를 비율 위치로 이동 — 바 폭이 바뀌어도 항상 같은 비율 지점 유지 (barWidth 세트 불필요)
            sanityMarkerRect.anchorMin = new Vector2(ratio, sanityMarkerRect.anchorMin.y);
            sanityMarkerRect.anchorMax = new Vector2(ratio, sanityMarkerRect.anchorMax.y);
            sanityMarkerRect.anchoredPosition = new Vector2(0f, sanityMarkerRect.anchoredPosition.y);
        }

        #region 에디터
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!testLiveUpdate)
            {
                return;
            }

            // OnValidate 내 UI 직접 갱신은 SendMessage 경고 유발 — 지연 적용 + fake null 가드 (5-3 체계 동일)
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                    return;
                }

                SetValue(Mathf.RoundToInt(testFillRatio * testMaxValue), testMaxValue);
                SetSanityMarker(Mathf.RoundToInt(testThresholdRatio * testMaxValue), testMaxValue);
            };
        }
#endif
        #endregion // 에디터
    }
}
