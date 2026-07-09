using EchoesOfAsh.Battle;
using EchoesOfAsh.Data;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 캐릭터 Entity
    /// </summary>
    public class CharacterEntity : BattleEntity
    {
        #region 필드
        private CharacterData characterData;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>캐릭터 데이터</summary>
        public CharacterData CharacterData => characterData;

        public override string DisplayName => characterData != null ? characterData.DisplayName : name;
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="data">캐릭터 데이터</param>
        public void Init(CharacterData data)
        {
            if (data == null)
            {
                SWLog.LogError($"[PlayerEntity] {name}: CharacterData가 null입니다");
                return;
            }

            characterData = data;
            SetupHp(data.MaxHpStat);
        }
        #endregion // 초기화
    }
}