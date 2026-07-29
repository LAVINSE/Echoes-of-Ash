using System.Collections.Generic;
using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using TMPro;
using UnityEngine;

namespace EchoesOfAsh.View
{
    /// <summary>
    /// 적이 다음 행동에서 수행할 의도와 수치를 표시하는 뷰입니다.
    /// </summary>
    public class IntentView : SWMonoBehaviour
    {
        #region 데이터
        [System.Serializable]
        private class IntentSlot
        {
            [SerializeField] private GameObject root;
            [SerializeField] private SpriteRenderer iconRenderer;
            [SerializeField] private TMP_Text valueText;

            /// <summary>
            /// 슬롯을 표시합니다.
            /// </summary>
            /// <param name="iconSprite">아이콘 스프라이트입니다.</param>
            /// <param name="value">값 문자열, 없으면 빈 문자열입니다.</param>
            public void Show(Sprite iconSprite, string value)
            {
                if (root != null)
                {
                    root.SetActive(true);
                }

                if (iconRenderer != null)
                {
                    iconRenderer.sprite = iconSprite;
                }

                if (valueText != null)
                {
                    valueText.text = value;
                }
            }

            /// <summary>
            /// 슬롯을 숨깁니다.
            /// </summary>
            public void Hide()
            {
                if (root != null)
                {
                    root.SetActive(false);
                }
            }
        }

        [System.Serializable]
        private class IntentStyle
        {
            [SerializeField] private EIntentType intentType;
            [SerializeField] private Sprite iconSprite;

            /// <summary>의도 유형입니다.</summary>
            public EIntentType IntentType => intentType;
            /// <summary>아이콘 스프라이트입니다.</summary>
            public Sprite IconSprite => iconSprite;
        }
        #endregion // 데이터

        #region 필드
        [SWGroup("슬롯")]
        [Tooltip("의도 유형 개수만큼 미리 배치")]
        [SerializeField] private List<IntentSlot> slots = new();
        [Tooltip("의도 유형별 아이콘")]
        [SerializeField] private List<IntentStyle> styles = new();
        #endregion // 필드

        /// <summary>
        /// 행동의 의도를 표시합니다.
        /// </summary>
        /// <param name="actionData">표시할 행동입니다.</param>
        public void SetIntent(EnemyActionData actionData)
        {
            if (actionData == null)
            {
                Clear();
                return;
            }

            List<EIntentType> intentTypes = actionData.GetIntentTypes();

            for (int index = 0; index < slots.Count; index++)
            {
                if (index >= intentTypes.Count)
                {
                    slots[index].Hide();
                    continue;
                }

                EIntentType intentType = intentTypes[index];
                IntentStyle style = FindStyle(intentType);

                slots[index].Show(style != null ? style.IconSprite : null, GetValueText(actionData, intentType));
            }

            if (intentTypes.Count > slots.Count)
            {
                SWLog.LogError($"[IntentView] 의도 {intentTypes.Count}개 중 {slots.Count}개만 표시합니다 - 슬롯 부족");
            }
        }

        /// <summary>
        /// 모든 의도 표시를 숨깁니다.
        /// </summary>
        public void Clear()
        {
            foreach (IntentSlot slot in slots)
            {
                slot.Hide();
            }
        }

        /// <summary>
        /// 의도 유형에 맞는 스타일을 찾습니다.
        /// </summary>
        /// <param name="intentType">의도 유형입니다.</param>
        /// <returns>스타일입니다.</returns>
        private IntentStyle FindStyle(EIntentType intentType)
        {
            foreach (var style in styles)
            {
                if (style.IntentType == intentType)
                {
                    return style;
                }
            }

            return null;
        }

        /// <summary>
        /// 의도 유형 앞에 표시할 수치 문자열을 반환합니다.
        /// </summary>
        /// <param name="actionData">행동입니다.</param>
        /// <param name="intentType">의도 유형입니다.</param>
        /// <returns>수치 문자열입니다.</returns>
        private string GetValueText(EnemyActionData actionData, EIntentType intentType)
        {
            switch (intentType)
            {
                case EIntentType.Attack:
                    return actionData.GetIntentDamageValue().ToString();
                case EIntentType.SanityPressure:
                    return actionData.GetIntentSanityPressureValue().ToString();
                default:
                    return string.Empty;
            }
        }
    }
}
