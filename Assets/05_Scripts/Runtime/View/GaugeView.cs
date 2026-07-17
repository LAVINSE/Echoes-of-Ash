using SW.Attributes;
using SW.Base;
using TMPro;
using UnityEngine;

namespace EchoesOfAsh.View
{
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

        private int currentValue;
        private int maxValue;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 표시 값</summary>
        public int CurrentValue => currentValue;
        /// <summary>최대 표시 값</summary>
        public int MaxValue => maxValue;
        #endregion // 프로퍼티

        /// <summary>
        /// 게이지 값을 갱신한다
        /// </summary>
        /// <param name="current"></param>
        /// <param name="max"></param>
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
        /// 색을 변경한다
        /// </summary>
        /// <param name="color">적용할 색</param>
        public void SetFillColor(Color color)
        {
            if (fillRenderer != null)
            {
                fillRenderer.color = color;
            }
        }

        /// <summary>
        /// 정신력 전환 마커를 바 해당 비율 위치에 배치한다
        /// </summary>
        /// <param name="threshold"></param>
        /// <param name="max"></param>
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
    }
}