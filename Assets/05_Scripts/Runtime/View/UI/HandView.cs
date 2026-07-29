using System.Collections.Generic;
using EchoesOfAsh.Battle;
using EchoesOfAsh.Card;
using EchoesOfAsh.Deck;
using SW.Attributes;
using SW.Base;
using SW.Pooling;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 손패의 카드 표시와 배치를 관리하는 뷰입니다.
    /// </summary>
    public class HandView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("카드")]
        [SerializeField] private CardView cardViewPrefab;
        [SerializeField] private Transform cardRoot;

        [SWGroup("배치 연출")]
        [Tooltip("카드 사이 가로 간격(캔버스 단위)입니다.")]
        [SerializeField, Min(0f)] private float cardSpacing = 180f;
        [Tooltip("부채꼴 중앙이 솟는 높이(캔버스 단위)입니다.")]
        [SerializeField, Min(0f)] private float arcHeight = 30f;
        [Tooltip("가장자리 카드의 최대 기울기(도)입니다.")]
        [SerializeField, Min(0f)] private float maxTiltAngle = 8f;
        [Tooltip("카드별 Z 간격 - 그리기와 무관, 픽업 판정의 최상단 선정 전용입니다.")]
        [SerializeField, Min(0f)] private float depthStep = 0.01f;

        private bool isInitialized;

        private DeckSystem deckSystem;
        private CardPlayService cardPlayService;
        private ApSystem apSystem;
        private SWPool pool;

        private readonly List<CardView> cardViews = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 표시 중인 카드 뷰 목록입니다.</summary>
        public IReadOnlyList<CardView> CardViews => cardViews;
        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 손패 카드가 배치될 부모 참조를 확인합니다.
        /// </summary>
        private void Awake()
        {
            if (cardRoot == null)
            {
                cardRoot = this.transform;
            }
        }

        /// <summary>
        /// 객체가 제거될 때 카드 뷰와 이벤트 구독을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            Release();
        }

        /// <summary>
        /// 초기화합니다.
        /// </summary>
        /// <param name="deckSystem">덱 시스템입니다.</param>
        /// <param name="cardPlayService">카드 사용 파이프라인입니다.</param>
        /// <param name="apSystem">AP 시스템입니다.</param>
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

            if (pool == null)
            {
                SWLog.LogError("[HandView] Init 실패: 씬에 SWPool이 없습니다");
                return;
            }

            pool.Prewarm(cardViewPrefab.gameObject, deckSystem.MaxHandSize);

            this.deckSystem = deckSystem;
            this.cardPlayService = cardPlayService;
            this.apSystem = apSystem;

            deckSystem.OnHandChanged += RebuildHand;
            apSystem.OnApChanged += HandleApChanged;

            isInitialized = true;
            RebuildHand();
        }

        /// <summary>
        /// 구독을 해제하고 표시 중인 카드를 전부 풀에 반환합니다.
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

            isInitialized = false;
        }
        #endregion // 초기화

        #region 풀링
        /// <summary>
        /// SWPool에서 카드 뷰를 가져옵니다.
        /// </summary>
        /// <returns>카드 뷰입니다.</returns>
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
        /// 카드 뷰를 SWPool에 반환합니다.
        /// </summary>
        /// <param name="cardView">카드 뷰입니다.</param>
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

        /// <summary>
        /// 현재 표시 중인 모든 카드 뷰를 반환하고 목록을 비웁니다.
        /// </summary>
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
        /// 손패 전체의 사용 가능 표시를 갱신합니다.
        /// </summary>
        public void RefreshPlayable()
        {
            if (!isInitialized)
            {
                return;
            }

            foreach (CardView cardView in cardViews)
            {
                cardView.SetPlayable(cardPlayService.CanPlay(cardView.CardInstance));
            }
        }

        /// <summary>
        /// 현재 손패에 맞춰 카드 뷰를 다시 구성하고 배치합니다.
        /// </summary>
        private void RebuildHand()
        {
            if (!isInitialized)
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

            for (int index = 0; index < hand.Count; index++)
            {
                cardViews[index].Init(hand[index]);
                LayoutCard(cardViews[index].transform, index, hand.Count);

                cardViews[index].transform.SetSiblingIndex(index);
            }

            RefreshPlayable();
        }

        /// <summary>
        /// AP 변경 시 사용 가능 표시를 갱신합니다.
        /// </summary>
        /// <param name="currentAp">현재 AP입니다.</param>
        private void HandleApChanged(int currentAp)
        {
            RefreshPlayable();
        }

        /// <summary>
        /// 카드를 부채꼴 형태로 배치합니다.
        /// </summary>
        /// <param name="cardTransform">배치할 카드 위치입니다.</param>
        /// <param name="index">손패 내 인덱스입니다.</param>
        /// <param name="count">손패 수입니다.</param>
        private void LayoutCard(Transform cardTransform, int index, int count)
        {
            float offset = index - (count - 1) * 0.5f;

            float normalizedPosition = count > 1 ? offset / ((count - 1) * 0.5f) : 0f;

            float horizontalPosition = offset * cardSpacing;
            float verticalPosition = arcHeight * (1f - normalizedPosition * normalizedPosition);
            float depthPosition = -depthStep * index;

            cardTransform.localPosition = new Vector3(horizontalPosition, verticalPosition, depthPosition);
            cardTransform.localRotation = Quaternion.Euler(0f, 0f, -normalizedPosition * maxTiltAngle);
        }
    }
}
