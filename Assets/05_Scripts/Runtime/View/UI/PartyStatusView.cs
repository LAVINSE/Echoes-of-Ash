using System.Collections.Generic;
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
        [SWGroup("파티 슬롯")]
        [Tooltip("파티원 표시 슬롯입니다. 3개 사전 배치, 인원수만큼 활성화합니다")]
        [SerializeField] private List<PartyCharacterSlotView> characterSlotViews = new();

        [SWGroup("정신력")]
        [SerializeField] private UIGaugeView sanityGauge;
        [SerializeField] private TextMeshProUGUI sanityTypeText;

        [SWGroup("정신력 색상")]
        [SerializeField] private Color calmColor = new(0.35f, 0.65f, 0.95f, 1f);
        [SerializeField] private Color madnessColor = new(0.75f, 0.25f, 0.85f, 1f);

        private ISanityHolder sanityHolder;
        #endregion // 필드


        #region 초기화
        /// <summary>
        /// 초기화합니다. 파티 인원수만큼 슬롯을 활성화합니다.
        /// </summary>
        /// <param name="partyCharacters">표시할 파티원 목록입니다 (스폰 순서 고정).</param>
        /// <param name="sanityHolder">파티 공유 정신력입니다.</param>
        public void Init(IReadOnlyList<CharacterEntity> partyCharacters, ISanityHolder sanityHolder)
        {
            if (partyCharacters == null || partyCharacters.Count == 0 || sanityHolder == null)
            {
                SWLog.LogError("[PartyStatusView] Init 실패: 의존성 중 null이 있습니다");
                return;
            }

            Release();

            this.sanityHolder = sanityHolder;

            for (int index = 0; index < characterSlotViews.Count; index++)
            {
                PartyCharacterSlotView slot = characterSlotViews[index];

                if (slot == null)
                {
                    continue;
                }

                bool isUsed = index < partyCharacters.Count;
                slot.gameObject.SetActive(isUsed);

                if (isUsed)
                {
                    slot.Init(partyCharacters[index]);
                }
            }

            sanityHolder.OnSanityChanged += HandleSanityChanged;
            sanityHolder.OnSanityTypeChanged += HandleSanityTypeChanged;

            HandleSanityChanged(sanityHolder.CurrentSanity, sanityHolder.MaxSanity);
            HandleSanityTypeChanged(sanityHolder.CurrentSanityType);

            if (sanityGauge != null)
            {
                sanityGauge.SetSanityMarker(sanityHolder.SanityThreshold, sanityHolder.MaxSanity);
            }
        }

        /// <summary>
        /// 슬롯과 정신력 이벤트 구독을 해제합니다.
        /// </summary>
        public void Release()
        {
            foreach (PartyCharacterSlotView slot in characterSlotViews)
            {
                if (slot != null)
                {
                    slot.Release();
                }
            }

            if (sanityHolder != null)
            {
                sanityHolder.OnSanityChanged -= HandleSanityChanged;
                sanityHolder.OnSanityTypeChanged -= HandleSanityTypeChanged;
            }

            sanityHolder = null;
        }
        #endregion // 초기화

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
