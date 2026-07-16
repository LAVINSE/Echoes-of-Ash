using System.Collections.Generic;
using EchoesOfAsh.Battle;
using EchoesOfAsh.Card;
using EchoesOfAsh.Deck;
using SW.Attributes;
using SW.Base;
using SW.Pooling;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.View
{
    /// <summary>
    /// 손패 뷰
    /// </summary>
    public class HandView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("카드")]
        [SerializeField] private CardView cardViewPrefab;
        [SerializeField] private Transform cardRoot;

        [SWGroup("배치 연출")]
        [Tooltip("카드 사이 가로 간격입니다.")]
        [SerializeField, Min(0f)] private float cardSpacing = 1.6f;
        [Tooltip("부채꼴 중앙이 솟는 높이입니다.")]
        [SerializeField, Min(0f)] private float arcHeight = 0.3f;
        [Tooltip("가장자리 카드의 최대 기울기(도)입니다.")]
        [SerializeField, Min(0f)] private float maxTiltAngle = 8f;
        [Tooltip("겹침 정렬용 카드별 Z 간격입니다. 오른쪽 카드가 위에 그려집니다.")]
        [SerializeField, Min(0f)] private float depthStep = 0.01f;

        private bool isInit;

        private DeckSystem deckSystem;
        private CardPlayService cardPlayService;
        private ApSystem apSystem;
        private SWPool pool;

        private readonly List<CardView> cardViews = new();
        #endregion // 필드

        #region 프로퍼티
        public IReadOnlyList<CardView> CardViews => cardViews;
        #endregion // 프로퍼티

        #region 초기화
        private void Awake()
        {
            if (cardRoot == null)
            {
                cardRoot = this.transform;
            }
        }

        private void OnDestroy()
        {
            Release();
        }

        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="deckSystem">덱 시스템</param>
        /// <param name="cardPlayService">카드 사용 파이프라인</param>
        /// <param name="apSystem">AP 시스템</param>
        public void Init(DeckSystem deckSystem, CardPlayService cardPlayService, ApSystem apSystem)
        {
            if (deckSystem == null || cardPlayService == null || apSystem == null)
            {
                SWLog.LogError("[HandView] Init 실패: 의존성 중 null이 있습니다");
                return;
            }

            if (cardViewPrefab == null)
            {
                SWLog.LogError("[HandView] Init 실패: cardViewPrefab이 비어 있습니다");
                return;
            }

            Release();

            pool = SWPool.Instance;
            pool.Prewarm(cardViewPrefab.gameObject, deckSystem.MaxHandSize);

            if (pool == null)
            {
                SWLog.LogError("[HandView] Init 실패: 씬에 SWPool이 없습니다");
                return;
            }

            this.deckSystem = deckSystem;
            this.cardPlayService = cardPlayService;
            this.apSystem = apSystem;

            deckSystem.OnHandChanged += RebuildHand;
            apSystem.OnApChanged += HandleApChanged;

            isInit = true;
            RebuildHand();
        }

        /// <summary>
        /// 구독을 해제하고 표시 중인 카드를 전부 풀에 반환한다
        /// </summary>
        public void Release()
        {
            if (deckSystem != null)
            {
                deckSystem.OnHandChanged -= RebuildHand;
            }

            if (apSystem != null)
            {
                apSystem.OnApChanged -= HandleApChanged;
            }

            ReturnAllCardViews();

            deckSystem = null;
            cardPlayService = null;
            apSystem = null;
            pool = null;

            isInit = false;
        }
        #endregion // 초기화

        #region 풀링
        /// <summary>
        /// SWPool에서 카드 뷰를 가져온다
        /// </summary>
        /// <returns>카드 뷰</returns>
        private CardView GetCardView()
        {
            CardView cardView = pool.Spawn<CardView>(cardViewPrefab.gameObject, parent: cardRoot);

            if (cardView == null)
            {
                SWLog.LogError("[HandView] 카드 뷰 스폰 실패: 프리팹에 CardView가 없습니다");
            }

            return cardView;
        }

        /// <summary>
        /// 카드 뷰를 SWPool에 반환한다
        /// </summary>
        /// <param name="cardView">카드 뷰</param>
        private void ReturnCardView(CardView cardView)
        {
            if (cardView == null)
            {
                return;
            }

            if (pool != null)
            {
                pool.Release(cardView.gameObject);
                return;
            }

            Destroy(cardView.gameObject);
        }

        private void ReturnAllCardViews()
        {
            foreach (CardView cardView in cardViews)
            {
                ReturnCardView(cardView);
            }

            cardViews.Clear();
        }
        #endregion // 풀링

        /// <summary>
        /// 손패 전체의 사용 가능 표시를 갱신한다
        /// </summary>
        public void RefreshPlayable()
        {
            if (!isInit)
            {
                return;
            }

            foreach (CardView cardView in cardViews)
            {
                cardView.SetPlayable(cardPlayService.CanPlay(cardView.CardInstance));
            }
        }

        /// <summary>
        /// 현재 손패에 맞춰 카드 뷰를 다시 구성하고 배치한다
        /// </summary>
        private void RebuildHand()
        {
            if (!isInit)
            {
                return;
            }

            IReadOnlyList<CardInstance> hand = deckSystem.Hand;

            while (cardViews.Count < hand.Count)
            {
                CardView cardView = GetCardView();

                if (cardView == null)
                {
                    return;
                }

                cardViews.Add(cardView);
            }

            while (cardViews.Count > hand.Count)
            {
                int lastIndex = cardViews.Count - 1;
                ReturnCardView(cardViews[lastIndex]);
                cardViews.RemoveAt(lastIndex);
            }

            for (int i = 0; i < hand.Count; i++)
            {
                cardViews[i].Init(hand[i]);
                LayoutCard(cardViews[i].transform, i, hand.Count);
            }

            RefreshPlayable();
        }

        /// <summary>
        /// AP 변경 시 사용 가능 표시를 갱신한다
        /// </summary>
        /// <param name="currentAp">현재 AP</param>
        private void HandleApChanged(int currentAp)
        {
            RefreshPlayable();
        }

        /// <summary>
        /// 카드를 부채꼴 형태로 배치한다
        /// </summary>
        /// <param name="cardTransform">배치할 카드 위치</param>
        /// <param name="i">손패 내 인덱스</param>
        /// <param name="count">손패 수</param>
        private void LayoutCard(Transform cardTransform, int i, int count)
        {
            float offset = i - (count - 1) * 0.5f;

            float normalized = count > 1 ? offset / ((count - 1) * 0.5f) : 0f;

            float x = offset * cardSpacing;
            float y = arcHeight * (1f - normalized * normalized);
            float z = -depthStep * i;

            cardTransform.localPosition = new Vector3(x, y, z);
            cardTransform.localRotation = Quaternion.Euler(0f, 0f, -normalized * maxTiltAngle);
        }
    }
}