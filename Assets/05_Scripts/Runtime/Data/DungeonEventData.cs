using System.Collections.Generic;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 이벤트 노드의 선택지
    /// </summary>
    [System.Serializable]
    public class DungeonEventChoice
    {
        #region 필드
        [SerializeField] private string choiceText;
        [Tooltip("선택 시 파티 정신력 변화량입니다. 골격 단계 — 효과 확장은 콘텐츠 단계에서 진행합니다.")]
        [SerializeField] private int sanityDelta;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>선택지에 표시할 문구입니다.</summary>
        public string ChoiceText => choiceText;
        /// <summary>선택 시 파티 정신력 변화량입니다.</summary>
        public int SanityDelta => sanityDelta;
        #endregion // 프로퍼티
    }

    /// <summary>
    /// 이벤트 노드 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonEvent_", menuName = "EchoesOfAsh/Data/DungeonEvent")]
    public class DungeonEventData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("선택지")]
        [Tooltip("선택지 목록입니다. 기획 기준 1~3개입니다.")]
        [SerializeField] private List<DungeonEventChoice> choices = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>이벤트의 선택지 목록입니다.</summary>
        public IReadOnlyList<DungeonEventChoice> Choices => choices;
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (choices.Count == 0 || choices.Count > 3)
            {
                SWLog.LogError($"[DungeonEventData] '{name}': 선택지는 1~3개여야 합니다. 현재 {choices.Count}개입니다.");
            }
        }
#endif // UNITY_EDITOR
        #endregion // 에디터
    }
}