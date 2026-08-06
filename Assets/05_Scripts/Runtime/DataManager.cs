using SW.Attributes;
using SW.Data;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    public class DataManager : SWSingleton<DataManager>
    {
        #region 상수
        public const string LoginCountKey = "EchoesOfAsh.LoginCountKey";
        #endregion // 상수

        #region 필드
        [SWGroup("데이터 객체")]
        [SerializeField] private SpriteData spriteData;
        #endregion // 필드

        #region 프로퍼티
        public SpriteData SpriteData => spriteData;
        #endregion // 프로퍼티

        #region 데이터 필드
        private readonly SWEncrypt<int> loginCount = new(LoginCountKey, 0);
        #endregion // 데이터 필드

        #region 데이터 프로퍼티
        public int LoginCount
        {
            get => loginCount.Value;
            set => loginCount.Set(value);
        }
        #endregion // 데이터 프로퍼티
    }
}