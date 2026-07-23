using System.Collections.Generic;
using EchoesOfAsh.Card;
using EchoesOfAsh.Data;
using EchoesOfAsh.Map;
using SW.Util;

namespace EchoesOfAsh.Dungeon
{
    /// <summary>
    /// 던전 진행에 필요한 맵, 덱, 정신력 이벤트 상태를 관리합니다.
    /// </summary>
    public class DungeonState
    {
        #region 필드
        private MapGraph mapGraph;
        private readonly int seed;
        private readonly List<CardInstance> deck = new();
        private readonly List<SanityEventData> sanityEventDatas = new();
        private int currentNodeIdentifier = -1;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 던전의 맵 그래프입니다.</summary>
        public MapGraph MapGraph => mapGraph;
        /// <summary>던전 생성에 사용한 시드입니다.</summary>
        public int Seed => seed;
        /// <summary>던전에서 사용하는 덱입니다.</summary>
        public IReadOnlyList<CardInstance> Deck => deck;
        /// <summary>던전에서 발생할 수 있는 정신력 이벤트 데이터 목록입니다.</summary>
        public IReadOnlyList<SanityEventData> SanityEventDatas => sanityEventDatas;
        /// <summary>현재 위치한 노드의 식별자입니다. 노드에 진입하지 않았으면 -1입니다.</summary>
        public int CurrentNodeIdentifier => currentNodeIdentifier;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 던전 상태를 생성하고 시작 덱과 정신력 이벤트 데이터를 복사합니다.
        /// </summary>
        /// <param name="seed">던전 생성에 사용한 시드입니다.</param>
        /// <param name="startingCards">시작 덱을 구성할 카드 데이터 목록입니다.</param>
        /// <param name="sanityEventDatas">던전에서 사용할 정신력 이벤트 데이터 목록입니다.</param>
        public DungeonState(
            int seed,
            IEnumerable<CardData> startingCards,
            IEnumerable<SanityEventData> sanityEventDatas)
        {
            this.seed = seed;

            if (startingCards != null)
            {
                foreach (CardData cardData in startingCards)
                {
                    if (cardData != null)
                    {
                        deck.Add(new CardInstance(cardData));
                    }
                }
            }

            if (sanityEventDatas != null)
            {
                foreach (SanityEventData sanityEventData in sanityEventDatas)
                {
                    if (sanityEventData != null)
                    {
                        this.sanityEventDatas.Add(sanityEventData);
                    }
                }
            }

            if (deck.Count == 0)
            {
                SWLog.LogWarning("[DungeonState] 생성 경고: 시작 덱이 비어 있습니다.");
            }
        }
        #endregion // 생성자

        #region 함수
        /// <summary>
        /// 던전 덱에 카드를 추가합니다.
        /// </summary>
        /// <param name="card">추가할 카드입니다.</param>
        public void AddCard(CardInstance card)
        {
            if (card == null)
            {
                SWLog.LogError("[DungeonState] AddCard 실패: 카드가 없습니다.");
                return;
            }

            deck.Add(card);
        }

        /// <summary>
        /// 던전 덱에서 카드를 제거합니다.
        /// </summary>
        /// <param name="card">제거할 카드입니다.</param>
        /// <returns>카드를 제거했으면 true입니다.</returns>
        public bool RemoveCard(CardInstance card)
        {
            return deck.Remove(card);
        }

        /// <summary>
        /// 현재 던전에서 사용할 맵 그래프를 설정합니다.
        /// </summary>
        /// <param name="mapGraph">설정할 맵 그래프입니다.</param>
        public void SetMapGraph(MapGraph mapGraph)
        {
            if (mapGraph == null)
            {
                SWLog.LogError("[DungeonState] SetMapGraph 실패: 맵 그래프가 없습니다.");
                return;
            }

            this.mapGraph = mapGraph;
            currentNodeIdentifier = -1;
        }

        /// <summary>
        /// 현재 위치한 노드를 변경합니다.
        /// </summary>
        /// <param name="nodeIdentifier">이동한 노드의 식별자입니다.</param>
        public void SetCurrentNode(int nodeIdentifier)
        {
            currentNodeIdentifier = nodeIdentifier;
        }
        #endregion // 함수
    }
}
