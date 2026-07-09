using SW.Attributes;
using SW.Base;
using SW.Stat;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    /// <summary>
    /// 캐릭터 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "Character_", menuName = "EchoesOfAsh/Data/Character")]
    public class CharacterData : SWIdentifiedObject
    {
        #region 필드
        [SWGroup("스탯")]
        [SerializeField] private SWStatOverride maxHpStat;
        [SerializeField] private bool isOptionalStat;
        [SerializeField, SWCondition("isOptionalStat", true)] private SWStatOverride[] optionalStats;

        [SWGroup("표시")]
        [SerializeField] private Sprite characterPortraitSprite;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>캐릭터 MaxHP 스탯</summary>
        public SWStatOverride MaxHpStat => maxHpStat;
        /// <summary>추가 스탯 사용 여부</summary>
        public bool IsOptionalStat => isOptionalStat;

        /// <summary>캐릭터 초상화 스프라이트</summary>
        public Sprite CharacterPortraitSprite => characterPortraitSprite;
        #endregion // 프로퍼티

        #region 에디터
#if UNITY_EDITOR
        private void OnValidate()
        {
            if(maxHpStat == null || maxHpStat.Stat == null)
            {
                SWLog.LogError($"[CharacterData] '{name}': Max_HP 스탯 에셋이 비어 있습니다.");
            }
        }
#endif // UNITY_EDITOR
        #endregion // 에디터
    }
}