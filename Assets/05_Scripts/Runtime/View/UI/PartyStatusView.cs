using EchoesOfAsh.Battle;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using SW.Attributes;
using SW.Base;
using SW.Util;
using TMPro;
using UnityEngine;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 파티원의 HP, 방어막 및 공유 정신력을 표시하는 상태 뷰입니다.
    /// </summary>
    public class PartyStatusView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private UIGaugeView hpGauge;
        [SerializeField] private TextMeshProUGUI blockText;

        [SWGroup("정신력")] 
        [SerializeField] private UIGaugeView sanityGauge;
        [SerializeField] private TextMeshProUGUI sanityTypeText;

        [SWGroup("정신력 색상")]
        [SerializeField] private Color calmColor = new(0.35f, 0.65f, 0.95f, 1f);
        [SerializeField] private Color madnessColor = new(0.75f, 0.25f, 0.85f, 1f);

        private CharacterEntity character;
        private ISanityHolder sanityHolder;
        #endregion // 필드


        #region 초기화
        /// <summary>
        /// 초기화합니다.
        /// </summary>
        /// <param name="character">표시할 파티원 엔티티입니다.</param>
        /// <param name="sanityHolder">파티원 정신력입니다.</param>
        public void Init(CharacterEntity character, ISanityHolder sanityHolder)
        {
            if (character == null || sanityHolder == null)
            {
                SWLog.LogError("[PartyStatusView] Init 실패: 의존성 중 null이 있습니다");
                return;
            }

            Release();

            this.character = character;
            this.sanityHolder = sanityHolder;

            character.OnHpChanged += HandleHpChanged;
            character.OnBlockChanged += HandleBlockChanged;

            sanityHolder.OnSanityChanged += HandleSanityChanged;
            sanityHolder.OnSanityTypeChanged += HandleSanityTypeChanged;

            HandleHpChanged(character.CurrentHp, character.MaxHp);
            HandleBlockChanged(character.CurrentBlock);
            HandleSanityChanged(sanityHolder.CurrentSanity, sanityHolder.MaxSanity);
            HandleSanityTypeChanged(sanityHolder.CurrentSanityType);

            if (sanityGauge != null)
            {
                sanityGauge.SetSanityMarker(sanityHolder.SanityThreshold, sanityHolder.MaxSanity);
            }
        }

        /// <summary>
        /// 파티원과 정신력 이벤트 구독을 해제합니다.
        /// </summary>
        public void Release()
        {
            if (character != null)
            {
                character.OnHpChanged -= HandleHpChanged;
                character.OnBlockChanged -= HandleBlockChanged;
            }

            if (sanityHolder != null)
            {
                sanityHolder.OnSanityChanged -= HandleSanityChanged;
                sanityHolder.OnSanityTypeChanged -= HandleSanityTypeChanged;
            }

            character = null;
            sanityHolder = null;

        }
        #endregion // 초기화

        /// <summary>
        /// HP 변경 시 게이지를 갱신합니다.
        /// </summary>
        /// <param name="current">현재 HP입니다.</param>
        /// <param name="max">최대 HP입니다.</param>
        private void HandleHpChanged(int current, int max)
        {
            if (hpGauge != null)
            {
                hpGauge.SetValue(current, max);
            }
        }

        /// <summary>
        /// 방어막이 변경되면 표시를 갱신하며, 값이 0이면 숨깁니다.
        /// </summary>
        /// <param name="block">현재 방어막입니다.</param>
        private void HandleBlockChanged(int block)
        {
            if (blockText == null)
            {
                return;
            }

            blockText.gameObject.SetActive(block > 0);
            blockText.text = block.ToString();
        }

        /// <summary>
        /// 정신력 변경 시 게이지를 갱신합니다.
        /// </summary>
        /// <param name="current">현재 정신력입니다.</param>
        /// <param name="max">최대 정신력입니다.</param>
        private void HandleSanityChanged(int current, int max)
        {
            if (sanityGauge != null)
            {
                sanityGauge.SetValue(current, max);
            }
        }

        /// <summary>
        /// 정신력 구간 전환 시 게이지 색과 구간 라벨을 갱신합니다.
        /// </summary>
        /// <param name="sanityType">현재 정신력 유형입니다.</param>
        private void HandleSanityTypeChanged(ESanityType sanityType)
        {
            bool isMadness = sanityType == ESanityType.Madness;
            Color sectionColor = isMadness ? madnessColor : calmColor;

            if (sanityGauge != null)
            {
                sanityGauge.SetFillColor(sectionColor);
            }

            if (sanityTypeText != null)
            {
                sanityTypeText.text = isMadness ? "[광기]" : "[평정]";
                sanityTypeText.color = sectionColor;
            }
        }
    }
}
