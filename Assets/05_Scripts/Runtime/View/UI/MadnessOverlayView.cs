using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Attributes;
using SW.Base;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 파티 정신력 구간에 따라 화면 광기 효과를 표시합니다.
    /// </summary>
    public class MadnessOverlayView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [Tooltip("풀 스크린 이미지")]
        [SerializeField] private Image overlayImage;

        [SWGroup("연출")]
        [Tooltip("광기 구간 틴트 색 - 알파는 최대 알파로 별도 제어")]
        [SerializeField] private Color madnessTint = new(0.45f, 0.1f, 0.5f, 1f);
        [Tooltip("광기 구간에서 도달할 최대 알파")]
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.2f;
        [Tooltip("초당 알파 변화량")]
        [SerializeField, Min(0.01f)] private float fadeSpeed = 1.5f;

        private ISanityHolder sanityHolder;
        private float targetAlpha;
        private float currentAlpha;
        #endregion // 필드

        #region 초기화
        private void Awake()
        {
            if (overlayImage != null)
            {
                // 풀스크린 이미지가 턴 종료 버튼 등 UI 레이캐스트를 가로채지 않도록 강제
                overlayImage.raycastTarget = false;
            }
        }

        private void OnDestroy()
        {
            Release();
        }

        private void Update()
        {
            if (Mathf.Approximately(currentAlpha, targetAlpha))
            {
                return;
            }

            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            ApplyAlpha();
        }

        /// <summary>
        /// 파티 정신력 보유자를 연결하고 현재 상태를 즉시 반영합니다.
        /// </summary>
        /// <param name="sanityHolder">파티 공유 정신력 보유자입니다.</param>
        public void Init(ISanityHolder sanityHolder)
        {
            Release();

            this.sanityHolder = sanityHolder;

            if (sanityHolder != null)
            {
                sanityHolder.OnSanityTypeChanged += HandleSanityTypeChanged;

                // 초기 1회 직접 갱신 + 즉시 반영 (전투 시작 시 페이드 없이 현재 구간 상태로 시작)
                HandleSanityTypeChanged(sanityHolder.CurrentSanityType);
                currentAlpha = targetAlpha;
                ApplyAlpha();
            }
        }

        /// <summary>
        /// 연결된 정신력 이벤트를 해제하고 화면 효과를 초기화합니다.
        /// </summary>
        public void Release()
        {
            if (sanityHolder != null)
            {
                sanityHolder.OnSanityTypeChanged -= HandleSanityTypeChanged;
                sanityHolder = null;
            }

            targetAlpha = 0f;
            currentAlpha = 0f;
            ApplyAlpha();
        }
        #endregion // 초기화

        /// <summary>
        /// 정신력 구간 전환 시 목표 알파를 갱신합니다.
        /// </summary>
        /// <param name="sanityType">현재 정신력 유형입니다.</param>
        private void HandleSanityTypeChanged(ESanityType sanityType)
        {
            targetAlpha = sanityType == ESanityType.Madness ? maxAlpha : 0f;
        }

        /// <summary>
        /// 현재 알파를 오버레이에 적용합니다.
        /// </summary>
        private void ApplyAlpha()
        {
            if (overlayImage == null)
            {
                return;
            }

            Color color = madnessTint;
            color.a = currentAlpha;
            overlayImage.color = color;

            // 완전 투명이면 렌더 비용 절약을 위해 꺼둔다
            overlayImage.enabled = currentAlpha > 0f;
        }
    }
}
