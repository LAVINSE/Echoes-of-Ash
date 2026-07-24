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
        private PartyData partyData;
        private readonly int seed;
        private readonly List<CardInstance> deck = new();
        private readonly List<SanityEventData> sanityEventDatas = new();
        private readonly List<CharacterData> characterDatas = new();
        private int currentNodeIdentifier = -1;
        private int carriedSanity = -1;
        private int moveCount;
        private int ashConsumedFloor = -1;
        private bool isCurrentNodeResolved = true;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 던전의 맵 그래프입니다.</summary>
        public MapGraph MapGraph => mapGraph;
        /// <summary>파티 공유 속성 데이터입니다</summary>
        public PartyData PartyData => partyData;
        /// <summary>던전 생성에 사용한 시드입니다.</summary>
        public int Seed => seed;
        /// <summary>던전에서 사용하는 덱입니다.</summary>
        public IReadOnlyList<CardInstance> Deck => deck;
        /// <summary>던전에서 발생할 수 있는 정신력 이벤트 데이터 목록입니다.</summary>
        public IReadOnlyList<SanityEventData> SanityEventDatas => sanityEventDatas;
        /// <summary>파티를 구성하는 캐릭터 데이터 목록입니다</summary>
        public IReadOnlyList<CharacterData> CharacterDatas => characterDatas;
        /// <summary>현재 위치한 노드의 식별자입니다. 노드에 진입하지 않았으면 -1입니다.</summary>
        public int CurrentNodeIdentifier => currentNodeIdentifier;
        /// <summary>전투 사이에 이월하는 파티 정신력입니다. 기록이 없으면 -1입니다.</summary>
        public int CarriedSanity => carriedSanity;
        /// <summary>이월된 정신력 기록이 있는지 여부입니다.</summary>
        public bool HasCarriedSanity => carriedSanity >= 0;
        /// <summary>던전에서 노드를 이동한 누적 횟수입니다.</summary>
        public int MoveCount => moveCount;
        /// <summary>잿불에 잠식된 마지막 층입니다. 잠식된 층이 없으면 -1입니다.</summary>
        public int AshConsumedFloor => ashConsumedFloor;
        /// <summary>현재 노드의 진입 처리가 완료되었는지 여부입니다. 미완료 상태로 복원되면 진입 처리를 다시 실행합니다.</summary>
        public bool IsCurrentNodeResolved => isCurrentNodeResolved;
        #endregion // 프로퍼티

        #region 생성자
        /// <summary>
        /// 던전 상태를 생성하고 파티 구성, 시작 덱, 정신력 이벤트 데이터를 복사합니다.
        /// </summary>
        /// <param name="seed">던전 생성에 사용한 시드입니다.</param>
        /// <param name="partyData">파티 공유 속성 데이터입니다.</param>
        /// <param name="characterDatas">파티를 구성할 캐릭터 데이터 목록입니다.</param>
        /// <param name="startingCards">시작 덱을 구성할 카드 데이터 목록입니다.</param>
        /// <param name="sanityEventDatas">던전에서 사용할 정신력 이벤트 데이터 목록입니다.</param>
        public DungeonState(
            int seed,
            PartyData partyData,
            IEnumerable<CharacterData> characterDatas,
            IEnumerable<CardData> startingCards,
            IEnumerable<SanityEventData> sanityEventDatas)
        {
            this.seed = seed;
            this.partyData = partyData;

            if (partyData == null)
            {
                SWLog.LogError("[DungeonState] 생성 실패: PartyData가 없습니다.");
            }

            if (characterDatas != null)
            {
                foreach (CharacterData characterData in characterDatas)
                {
                    if (characterData != null)
                    {
                        this.characterDatas.Add(characterData);
                    }
                }
            }

            if (this.characterDatas.Count == 0)
            {
                SWLog.LogError("[DungeonState] 생성 실패: 파티 캐릭터가 없습니다.");
            }
            
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
            moveCount = 0;
            ashConsumedFloor = -1;
        }

        /// <summary>
        /// 현재 위치한 노드를 변경합니다.
        /// </summary>
        /// <param name="nodeIdentifier">이동한 노드의 식별자입니다.</param>
        public void SetCurrentNode(int nodeIdentifier)
        {
            currentNodeIdentifier = nodeIdentifier;
            isCurrentNodeResolved = false;
        }

        /// <summary>
        /// 현재 노드의 진입 처리를 완료 상태로 기록합니다.
        /// </summary>
        public void SetCurrentNodeResolved()
        {
            isCurrentNodeResolved = true;
        }

        /// <summary>
        /// 전투 사이에 이월할 파티 정신력을 기록합니다.
        /// </summary>
        /// <param name="sanity">기록할 정신력 값입니다. 0 미만이면 0으로 보정합니다.</param>
        public void SetCarriedSanity(int sanity)
        {
            carriedSanity = sanity < 0 ? 0 : sanity;
        }

        /// <summary>
        /// 노드 이동 누적 횟수를 1 증가시키고 증가한 값을 반환합니다.
        /// </summary>
        /// <returns>증가한 이동 누적 횟수입니다.</returns>
        public int IncrementMoveCount()
        {
            return ++moveCount;
        }

        /// <summary>
        /// 잿불에 잠식된 마지막 층을 기록합니다.
        /// </summary>
        /// <param name="floor">잠식된 마지막 층입니다.</param>
        public void SetAshConsumedFloor(int floor)
        {
            ashConsumedFloor = floor;
        }

        /// <summary>
        /// 저장 데이터로 던전 진행 상태를 복원합니다. SetMapGraph 이후에 호출해야 합니다.
        /// </summary>
        /// <param name="currentNodeIdentifier">복원할 현재 노드 식별자입니다.</param>
        /// <param name="isCurrentNodeResolved">현재 노드의 진입 처리 완료 여부입니다.</param>
        /// <param name="carriedSanity">이월 정신력입니다.</param>
        /// <param name="moveCount">노드 이동 누적 횟수입니다.</param>
        /// <param name="ashConsumedFloor">잿불에 잠식된 마지막 층입니다.</param>
        public void RestoreProgress(int currentNodeIdentifier, bool isCurrentNodeResolved,
            int carriedSanity, int moveCount, int ashConsumedFloor)
        {
            this.currentNodeIdentifier = currentNodeIdentifier;
            this.isCurrentNodeResolved = isCurrentNodeResolved;
            this.carriedSanity = carriedSanity < 0 ? 0 : carriedSanity;
            this.moveCount = moveCount < 0 ? 0 : moveCount;
            this.ashConsumedFloor = ashConsumedFloor;
        }
        #endregion // 함수
    }
}
