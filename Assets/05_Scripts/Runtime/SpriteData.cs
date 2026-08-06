using System.Collections.Generic;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using UnityEngine;

namespace EchoesOfAsh.Data
{
    [CreateAssetMenu(fileName = "SpriteData", menuName = "EchoesOfAsh/Data/SpriteData")]
    public class SpriteData : SWScriptableObject
    {
        #region 데이터
        [System.Serializable]
        public class CardSpriteData
        {
            [SerializeField] private ECardType cardType;
            [SerializeField] private Sprite cardSprite;

            public ECardType CardType => cardType;
            public Sprite CardSprite => cardSprite;
        }
        #endregion // 데이터

        #region 필드
        [SWGroup("카드 스프라이트")]
        [SerializeField] private List<CardSpriteData> cardSpriteDatas = new();
        #endregion // 필드

        #region 프로퍼티
        public IReadOnlyList<CardSpriteData> CardSpriteDatas => cardSpriteDatas;
        #endregion // 프로퍼티

        public Sprite GetCardSprite(ECardType cardType)
        {
            for (int i = 0; i < cardSpriteDatas.Count; i++)
            {
                if (cardSpriteDatas[i].CardType == cardType)
                {
                    return cardSpriteDatas[i].CardSprite;
                }
            }
            
            return null;
        }
    }
}