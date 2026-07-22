using System.Collections.Generic;
using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Dungeon
{
    /// <summary>
    /// 던전 상태
    /// </summary>
    public class DungeonState
    {
        #region 필드
        private readonly int seed;
        private readonly List<CardInstance> deck = new();
        private readonly List<SanityEventData> sanityEventDatas = new();

        private int currentNodeId = -1;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>던전 시드입니다</summary>
        public int Seed => seed;

        /// <summary>던전의 덱</summary>
        public IReadOnlyList<CardInstance> Deck => deck;
        /// <summary>정신력 이벤트 데이터</summary>
        public IReadOnlyList<SanityEventData> SanityEventDatas => sanityEventDatas;

        /// <summary>현재 노드 ID</summary>
        public int CurrentNodeId => currentNodeId;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 던전 상태를 생성한다
        /// </summary>
        /// <param name="seed">던전 시드</param>
        /// <param name="startingCards">시작 덱 카드 데이터 목록</param>
        /// <param name="sanityEventDatas">정신력 이벤트 데이터</param>
        public DungeonState(int seed, IEnumerable<CardData> startingCards, IEnumerable<SanityEventData> sanityEventDatas)
        {
            this.seed = seed;

            if (startingCards != null)
            {
                foreach (var cardData in startingCards)
                {
                    if (cardData != null)
                    {
                        deck.Add(new CardInstance(cardData));
                    }
                }
            }

            if (sanityEventDatas != null)
            {
                foreach (var sanityEvent in sanityEventDatas)
                {
                    if (sanityEvent != null)
                    {
                        this.sanityEventDatas.Add(sanityEvent);
                    }
                }
            }

            if (deck.Count == 0)
            {
                SWLog.LogError("[DungeonState] 생성 경고: 시작 덱이 비어 있습니다");
            }
        }
        #endregion // 생성자

        #region 덱
        /// <summary>
        /// 덱에 카드를 추가합니다
        /// </summary>
        /// <param name="card">추가할 카드</param>
        public void AddCard(CardInstance card)
        {
            if (card == null)
            {
                SWLog.LogError("[DungeonState] AddCard 실패: 카드가 null입니다");
                return;
            }

            deck.Add(card);
        }

        /// <summary>
        /// 덱에서 카드를 제거한다
        /// </summary>
        /// <param name="card">제거할 카드</param>
        /// <returns>제거 성공 여부</returns>
        public bool RemoveCard(CardInstance card)
        {
            return deck.Remove(card);
        }
        #endregion // 덱
        
        #region 진행
        /// <summary>
        /// 현재 노드를 이동한다
        /// </summary>
        /// <param name="nodeId">이동할 노드 ID</param>
        public void MoveToNode(int nodeId)
        {
            currentNodeId = nodeId;
        }
        #endregion // 진행
    }
}