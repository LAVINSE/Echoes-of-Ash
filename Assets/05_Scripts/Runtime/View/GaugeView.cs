using SW.Attributes;
using SW.Base;
using TMPro;
using UnityEngine;

namespace EchoesOfAsh.View
{
    /// <summary>
    /// 현재 값과 최댓값 및 정신력 전환 지점을 표시하는 게이지 뷰입니다.
    /// </summary>
    public class GaugeView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private Transform fillRoot;
        [SerializeField] private SpriteRenderer fillRenderer;
        [SerializeField] private TMP_Text valueText;
        [Tooltip("바 전체 폭")]
        [SerializeField] private float barWidth;

        [SWGroup("정신력 전환 마커")]
        [SerializeField] private Transform sanityMarker;

#if UNITY_EDITOR
        [SWGroup("테스트")]
        /// <summary>슬라이더 조작 시 게이지를 즉시 갱신할지 여부입니다.</summary>
        [SerializeField] private bool testLiveUpdate;
        [SerializeField, Range(0f, 1f)] private float testFillRatio = 1f;
        [SerializeField, Range(0f, 1f)] private float testThresholdRatio = 0.3f;
        [SerializeField, Min(1)] private int testMaxValue = 100;
#endif

        private int currentValue;
        private int maxValue;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 표시 값입니다.</summary>
        public int CurrentValue => currentValue;
        /// <summary>최대 표시 값입니다.</summary>
        public int MaxValue => maxValue;
        #endregion // 프로퍼티

        /// <summary>
        /// 게이지 값을 갱신합니다.
        /// </summary>
        /// <param name="current">현재 값입니다.</param>
        /// <param name="max">최대 값입니다.</param>
        public void SetValue(int current, int max)
        {
            currentValue = Mathf.Max(0, current);
            maxValue = Mathf.Max(0, max);

            if (fillRoot != null)
            {
                float ratio = maxValue > 0 ? Mathf.Clamp01((float)currentValue / maxValue) : 0f;

                Vector3 scale = fillRoot.localScale;
                scale.x = ratio;
                fillRoot.localScale = scale;
            }

            if (valueText != null)
            {
                valueText.text = $"{currentValue}/{maxValue}";
            }
        }

        /// <summary>
        /// 색을 변경합니다.
        /// </summary>
        /// <param name="color">적용할 색입니다.</param>
        public void SetFillColor(Color color)
        {
            if (fillRenderer != null)
            {
                fillRenderer.color = color;
            }
        }

        /// <summary>
        /// 정신력 전환 마커를 바 해당 비율 위치에 배치합니다.
        /// </summary>
        /// <param name="threshold">정신력 전환 임계값입니다.</param>
        /// <param name="max">최대 값입니다.</param>
        public void SetSanityMarker(int threshold, int max)
        {
            if (sanityMarker == null)
            {
                return;
            }

            if (threshold <= 0 || max <= 0)
            {
                sanityMarker.gameObject.SetActive(false);
                return;
            }

            float ratio = Mathf.Clamp01((float)threshold / max);

            Vector3 markerLocalPosition = sanityMarker.localPosition;
            markerLocalPosition.x = barWidth * ratio;
            sanityMarker.localPosition = markerLocalPosition;

            sanityMarker.gameObject.SetActive(true);
        }


        #region 테스트
#if UNITY_EDITOR
        /// <summary>
        /// 인스펙터 값이 변경되면 테스트 슬라이더 상태를 게이지에 반영합니다.
        /// 렌더러/TMP 갱신은 OnValidate 내 직접 호출이 안전하지 않아 delayCall로 지연 적용합니다.
        /// </summary>
        private void OnValidate()
        {
            if (!testLiveUpdate)
            {
                return;
            }

            UnityEditor.EditorApplication.delayCall += () =>
            {
                // 도중에 파괴/프리팹 닫힘 등으로 무효해진 경우 무시
                if (this == null)
                {
                    return;
                }

                ApplyTestState();
            };
        }

        /// <summary>
        /// 테스트 슬라이더 비율을 값으로 환산하여 게이지에 적용합니다.
        /// </summary>
        private void ApplyTestState()
        {
            SetValue(Mathf.RoundToInt(testFillRatio * testMaxValue), testMaxValue);
            SetSanityMarker(Mathf.RoundToInt(testThresholdRatio * testMaxValue), testMaxValue);
        }

        /// <summary>
        /// 자동 갱신이 꺼진 상태에서 테스트 값을 게이지에 한 번 적용합니다.
        /// </summary>
        [SWButton("테스트: 현재 슬라이더 값 적용")]
        private void ApplyTest()
        {
            ApplyTestState();
        }

        /// <summary>
        /// 막대 너비를 기준으로 채움 영역의 크기와 위치를 자동 정렬합니다.
        /// 채움 영역의 왼쪽 가장자리를 채움 루트의 원점에 맞춥니다.
        /// </summary>
        [SWButton("테스트: Fill 자동 정렬 (barWidth 기준)")]
        private void AlignTestFill()
        {
            if (fillRoot == null || fillRenderer == null || fillRenderer.sprite == null)
            {
                return;
            }

            Transform fill = fillRenderer.transform;

            // 스프라이트 원본 폭 기준으로 barWidth에 맞는 스케일 계산
            float spriteWidth = fillRenderer.sprite.bounds.size.x;
            Vector3 fillScale = fill.localScale;
            fillScale.x = barWidth / spriteWidth;
            fill.localScale = fillScale;

            // 왼쪽 가장자리를 원점에 정렬
            Vector3 fillPosition = fill.localPosition;
            fillPosition.x = barWidth * 0.5f;
            fill.localPosition = fillPosition;

            fillRoot.localScale = Vector3.one;
        }
#endif
        #endregion // 테스트

    }
}
