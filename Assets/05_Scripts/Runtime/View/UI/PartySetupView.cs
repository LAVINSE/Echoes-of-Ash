using System;
using System.Collections.Generic;
using System.Text;
using EchoesOfAsh.Data;
using SW.Attributes;
using SW.Base;
using SW.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 파티 편성 화면
    /// </summary>
    public class PartySetupView : SWMonoBehaviour
    {
        #region 데이터 
        [System.Serializable]
        private class CharacterSlot
        {
            public Button button;
            public TextMeshProUGUI label;
            public GameObject selectedMark;
        }
        #endregion // 데이터

        #region 필드
        [SWGroup("패널")]
        [SerializeField] private GameObject panelRoot;

        [SWGroup("슬롯")]
        [Tooltip("보유 캐릭터 표시 슬롯")]
        [SerializeField] private List<CharacterSlot> characterSlots = new();

        [SWGroup("캐릭터 정보")]
        [SerializeField] private TextMeshProUGUI infoNameText;
        [SerializeField] private TextMeshProUGUI infoPassiveText;
        [SerializeField] private TextMeshProUGUI infoCardsText;

        [SWGroup("시작 덱")]
        [SerializeField] private TextMeshProUGUI startingDeckText;

        [SWGroup("확정")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private TextMeshProUGUI selectionCountText;

        [SWGroup("규칙")]
        [Tooltip("파티 최대 인원입니다")]
        [SerializeField, Min(1)] private int maxPartySize = 3;

        private IReadOnlyList<CharacterData> candidates;
        private Action<List<CharacterData>> onConfirm;

        private readonly List<CharacterData> selectedCharacters = new();
        private readonly StringBuilder stringBuilder = new();
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        #region 초기화
        private void Awake()
        {
            for (int index = 0; index < characterSlots.Count; index++)
            {
                int slotIndex = index;
                CharacterSlot slot = characterSlots[index];

                if (slot?.button != null)
                {
                    slot.button.onClick.AddListener(() => HandleCandidateClicked(slotIndex));
                }
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirmClicked);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 편성 화면을 표시합니다.
        /// </summary>
        /// <param name="candidates">보유 캐릭터 목록입니다.</param>
        /// <param name="startingCards">시작 덱 미리보기에 표시할 카드 목록입니다.</param>
        /// <param name="onConfirm">파티 확정 시 선택 목록과 함께 호출됩니다.</param>
        public void Show(IReadOnlyList<CharacterData> candidates, IReadOnlyList<CardData> startingCards,
            Action<List<CharacterData>> onConfirm)
        {
            if (candidates == null || candidates.Count == 0 || onConfirm == null)
            {
                SWLog.LogError("[PartySetupView] Show 실패: 후보 목록 또는 콜백이 없습니다");
                return;
            }

            this.candidates = candidates;
            this.onConfirm = onConfirm;
            selectedCharacters.Clear();

            for (int index = 0; index < characterSlots.Count; index++)
            {
                CharacterSlot slot = characterSlots[index];

                if (slot?.button == null)
                {
                    continue;
                }

                bool isUsed = index < candidates.Count && candidates[index] != null;
                slot.button.gameObject.SetActive(isUsed);

                if (isUsed && slot.label != null)
                {
                    slot.label.text = candidates[index].DisplayName;
                }
            }

            RefreshStartingDeck(startingCards);
            ShowCharacterInfo(null);
            RefreshSelection();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        /// <summary>
        /// 편성 화면을 숨깁니다.
        /// </summary>
        public void Hide()
        {
            candidates = null;
            onConfirm = null;
            selectedCharacters.Clear();

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
        #endregion // 표시

        #region 선택
        /// <summary>
        /// 후보 클릭 시 선택을 토글하고 정보를 표시합니다.
        /// </summary>
        /// <param name="slotIndex">클릭한 슬롯 인덱스입니다.</param>
        private void HandleCandidateClicked(int slotIndex)
        {
            if (candidates == null || slotIndex < 0 || slotIndex >= candidates.Count)
            {
                return;
            }

            CharacterData characterData = candidates[slotIndex];

            if (characterData == null)
            {
                return;
            }

            if (selectedCharacters.Contains(characterData))
            {
                selectedCharacters.Remove(characterData);
            }
            else if (selectedCharacters.Count < maxPartySize)
            {
                selectedCharacters.Add(characterData);
            }
            else
            {
                SWLog.Log($"[PartySetupView] 파티는 최대 {maxPartySize}인입니다");
            }

            ShowCharacterInfo(characterData);
            RefreshSelection();
        }

        /// <summary>
        /// 확정 버튼 클릭 시 선택 목록을 사본으로 전달합니다.
        /// </summary>
        private void HandleConfirmClicked()
        {
            if (selectedCharacters.Count == 0 || selectedCharacters.Count > maxPartySize)
            {
                return;
            }

            onConfirm?.Invoke(new List<CharacterData>(selectedCharacters));
        }

        /// <summary>
        /// 선택 표식, 인원 표시 및 확정 버튼 활성을 갱신합니다.
        /// </summary>
        private void RefreshSelection()
        {
            for (int index = 0; index < characterSlots.Count; index++)
            {
                CharacterSlot slot = characterSlots[index];

                if (slot?.selectedMark == null)
                {
                    continue;
                }

                bool isSelected = candidates != null
                    && index < candidates.Count
                    && candidates[index] != null
                    && selectedCharacters.Contains(candidates[index]);

                slot.selectedMark.SetActive(isSelected);
            }

            if (selectionCountText != null)
            {
                selectionCountText.text = $"{selectedCharacters.Count}/{maxPartySize}";
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = selectedCharacters.Count > 0;
            }
        }
        #endregion // 선택

        #region 정보
        /// <summary>
        /// 캐릭터의 패시브와 전용 카드 정보를 표시합니다.
        /// </summary>
        /// <param name="characterData">표시할 캐릭터입니다. null이면 비웁니다.</param>
        private void ShowCharacterInfo(CharacterData characterData)
        {
            if (infoNameText != null)
            {
                infoNameText.text = characterData != null ? characterData.DisplayName : string.Empty;
            }

            if (infoPassiveText != null)
            {
                infoPassiveText.text = characterData != null
                    ? BuildPassiveDescription(characterData)
                    : string.Empty;
            }

            if (infoCardsText != null)
            {
                infoCardsText.text = characterData != null
                    ? BuildExclusiveCardNames(characterData)
                    : string.Empty;
            }
        }

        /// <summary>
        /// 패시브 설명을 줄바꿈으로 조합합니다.
        /// </summary>
        /// <param name="characterData">대상 캐릭터입니다.</param>
        /// <returns>조합된 설명입니다.</returns>
        private string BuildPassiveDescription(CharacterData characterData)
        {
            if (characterData.Passives.Count == 0)
            {
                return "패시브 없음";
            }

            stringBuilder.Clear();

            foreach (var passive in characterData.Passives)
            {
                if (passive == null)
                {
                    continue;
                }

                if (stringBuilder.Length > 0)
                {
                    stringBuilder.AppendLine();
                }

                stringBuilder.Append(passive.GetDescription());
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 전용 카드 이름 목록을 조합합니다.
        /// </summary>
        /// <param name="characterData">대상 캐릭터입니다.</param>
        /// <returns>조합된 카드 이름입니다.</returns>
        private string BuildExclusiveCardNames(CharacterData characterData)
        {
            if (characterData.ExclusiveCards.Count == 0)
            {
                return "전용 카드 없음";
            }

            stringBuilder.Clear();
            stringBuilder.Append("전용 카드: ");

            for (int index = 0; index < characterData.ExclusiveCards.Count; index++)
            {
                CardData cardData = characterData.ExclusiveCards[index];

                if (cardData == null)
                {
                    continue;
                }

                if (index > 0)
                {
                    stringBuilder.Append(", ");
                }

                stringBuilder.Append(cardData.DisplayName);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 시작 덱 미리보기를 갱신합니다 (데이터 정의 기반 — 기획서 4-5).
        /// </summary>
        /// <param name="startingCards">시작 덱 카드 목록입니다.</param>
        private void RefreshStartingDeck(IReadOnlyList<CardData> startingCards)
        {
            if (startingDeckText == null)
            {
                return;
            }

            if (startingCards == null || startingCards.Count == 0)
            {
                startingDeckText.text = string.Empty;
                return;
            }

            stringBuilder.Clear();
            stringBuilder.Append($"시작 덱 ({startingCards.Count}장): ");

            for (int index = 0; index < startingCards.Count; index++)
            {
                if (startingCards[index] == null)
                {
                    continue;
                }

                if (index > 0)
                {
                    stringBuilder.Append(", ");
                }

                stringBuilder.Append(startingCards[index].DisplayName);
            }

            startingDeckText.text = stringBuilder.ToString();
        }
        #endregion // 정보
    }
}