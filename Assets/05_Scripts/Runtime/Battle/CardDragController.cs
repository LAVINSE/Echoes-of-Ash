using System.Collections.Generic;
using EchoesOfAsh.Enum;
using EchoesOfAsh.View;
using EchoesOfAsh.View.UI;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

using Pointer = UnityEngine.InputSystem.Pointer;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 카드 드래그 컨트롤러입니다.
    /// </summary>
    public class CardDragController : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("참조")]
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private BezierArrowsView targetingArrow;
        [SerializeField] private CardTooltipView tooltipView;
        [SerializeField] private Camera targetCamera;

        [SWGroup("판정")]
        [Tooltip("카드 선택 레이어")]
        [SerializeField] private LayerMask cardLayerMask;
        [Tooltip("적 레이어")]
        [SerializeField] private LayerMask enemyLayerMask;
        [Tooltip("카드 사용 기준선")]
        [SerializeField] private float playLineY = -2f;

        private CardView hoveredCard;
        private CardView draggedCard;
        private bool isSingleTarget;
        private Vector3 originLocalPosition;
        private Quaternion originLocalRotation;

        private ContactFilter2D cardContactFilter;
        private readonly List<Collider2D> overlapBuffer = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>드래그 진행 여부입니다.</summary>
        public bool IsDragging => draggedCard != null;
        #endregion // 프로퍼티

        #region 초기화
        private void Awake()
        {
            if (battleManager == null || targetingArrow == null)
            {
                SWLog.LogError("[CardDragController] 참조 누락");
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            cardContactFilter = new ContactFilter2D();
            cardContactFilter.SetLayerMask(cardLayerMask);
            cardContactFilter.useTriggers = true;
        }

        private void OnDisable()
        {
            SetHoveredCard(null);
            CancelDrag();
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;

            if (pointer == null)
            {
                return;
            }

            // 드래그 중 — 갱신 또는 드롭
            if (draggedCard != null)
            {
                if (!TryGetPointerWorldPosition(out Vector2 dragPosition))
                {
                    return;
                }

                if (pointer.press.wasReleasedThisFrame)
                {
                    EndDrag(dragPosition);
                    return;
                }

                UpdateDrag(dragPosition);
                return;
            }

            // 대기 중 — 호버 갱신 후 픽업 시도
            if (!CanInteract() || !TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
            {
                SetHoveredCard(null);
                return;
            }

            SetHoveredCard(FindTopmostCard(pointerWorldPosition));

            if (pointer.press.wasPressedThisFrame)
            {
                TryBeginDrag();
            }
        }
        #endregion // 초기화

        #region 드래그
        /// <summary>
        /// 호버 중인 카드가 사용 가능하면 집습니다.
        /// </summary>
        private void TryBeginDrag()
        {
            if (hoveredCard == null || !hoveredCard.IsPlayable)
            {
                return;
            }

            CardView cardView = hoveredCard;
            SetHoveredCard(null);

            draggedCard = cardView;
            isSingleTarget = cardView.CardInstance.TargetingType == ETargetingType.Single;

            Transform cardTransform = cardView.transform;
            originLocalPosition = cardTransform.localPosition;
            originLocalRotation = cardTransform.localRotation;

            cardView.SetDragging(true);

            if (isSingleTarget)
            {
                // 카드는 손패에 고정하고 화살표로 대상을 지정합니다 (STS 표준 UX)
                targetingArrow.BeginAiming(cardTransform.position);
            }
            else
            {
                cardTransform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// 드래그 상태를 갱신합니다.
        /// </summary>
        /// <param name="pointerWorldPosition">포인터의 월드 좌표입니다.</param>
        private void UpdateDrag(Vector2 pointerWorldPosition)
        {
            if (isSingleTarget)
            {
                targetingArrow.UpdateAiming(pointerWorldPosition);
                return;
            }

            Transform cardTransform = draggedCard.transform;

            cardTransform.position = new Vector3(
                pointerWorldPosition.x,
                pointerWorldPosition.y,
                cardTransform.position.z
            );
        }

        /// <summary>
        /// 드롭을 판정하고 카드 사용을 시도합니다. 실패하면 카드를 손패 위치로 되돌립니다.
        /// </summary>
        /// <param name="pointerWorldPosition">포인터의 월드 좌표입니다.</param>
        private void EndDrag(Vector2 pointerWorldPosition)
        {
            CardView cardView = draggedCard;
            draggedCard = null;

            targetingArrow.EndAiming();

            bool isPlayed = isSingleTarget
                ? TryPlayOnEnemy(cardView, pointerWorldPosition)
                : TryPlayAboveLine(cardView, pointerWorldPosition);

            // 성공 시 뷰는 OnHandChanged 재구성으로 풀에 반환됨 — 재사용 대비 소팅 레이어만 복원
            cardView.SetDragging(false);

            if (!isPlayed)
            {
                Transform cardTransform = cardView.transform;
                cardTransform.localPosition = originLocalPosition;
                cardTransform.localRotation = originLocalRotation;
            }
        }

        /// <summary>
        /// 진행 중인 드래그를 취소하고 카드를 원위치로 되돌립니다.
        /// </summary>
        private void CancelDrag()
        {
            if (draggedCard == null)
            {
                return;
            }

            CardView cardView = draggedCard;
            draggedCard = null;

            if (targetingArrow != null)
            {
                targetingArrow.EndAiming();
            }

            cardView.SetDragging(false);

            Transform cardTransform = cardView.transform;
            cardTransform.localPosition = originLocalPosition;
            cardTransform.localRotation = originLocalRotation;
        }
        #endregion // 드래그

        #region 호버
        /// <summary>
        /// 호버 대상 카드를 교체하고 강조 표시를 갱신합니다.
        /// </summary>
        /// <param name="cardView">새 호버 대상입니다. 없으면 <see langword="null"/>입니다.</param>
        private void SetHoveredCard(CardView cardView)
        {
            if (hoveredCard == cardView)
            {
                return;
            }

            if (hoveredCard != null)
            {
                hoveredCard.SetHovered(false);
            }

            hoveredCard = cardView;

            if (hoveredCard != null)
            {
                hoveredCard.SetHovered(true);
            }

            if (tooltipView != null)
            {
                if (hoveredCard != null)
                {
                    tooltipView.Show(hoveredCard.CardInstance, hoveredCard.transform.position);
                }
                else
                {
                    tooltipView.Hide();
                }
            }
        }

        /// <summary>
        /// 포인터 아래에서 가장 위에 그려진 카드를 찾습니다.
        /// 손패 뷰의 깊이 배치에서 가장 앞에 표시되는 카드를 우선합니다.
        /// </summary>
        /// <param name="pointerWorldPosition">포인터의 월드 좌표입니다.</param>
        /// <returns>최상단 카드 뷰입니다. 없으면 <see langword="null"/>입니다.</returns>
        private CardView FindTopmostCard(Vector2 pointerWorldPosition)
        {
            int hitCount = Physics2D.OverlapPoint(pointerWorldPosition, cardContactFilter, overlapBuffer);

            CardView topmostCard = null;
            float topmostZ = float.MaxValue;

            for (int index = 0; index < hitCount; index++)
            {
                CardView candidate = overlapBuffer[index].GetComponentInParent<CardView>();

                if (candidate == null || candidate.CardInstance == null)
                {
                    continue;
                }

                float candidateZ = candidate.transform.position.z;

                if (candidateZ < topmostZ)
                {
                    topmostZ = candidateZ;
                    topmostCard = candidate;
                }
            }

            return topmostCard;
        }
        #endregion // 호버

        #region 판정
        /// <summary>
        /// 카드 상호작용이 가능한 상태인지 확인합니다.
        /// </summary>
        /// <returns>상호작용 가능 여부입니다.</returns>
        private bool CanInteract()
        {
            return battleManager != null
                && battleManager.IsBattleRunning
                && battleManager.TurnManager != null
                && battleManager.TurnManager.CurrentPhase == ETurnPhase.PlayerAction;
        }

        /// <summary>
        /// 포인터 위치의 적에게 단일 대상 카드를 사용합니다.
        /// </summary>
        /// <param name="cardView">사용할 카드 뷰입니다.</param>
        /// <param name="pointerWorldPosition">포인터의 월드 좌표입니다.</param>
        /// <returns>사용 성공 여부입니다.</returns>
        private bool TryPlayOnEnemy(CardView cardView, Vector2 pointerWorldPosition)
        {
            Collider2D hit = Physics2D.OverlapPoint(pointerWorldPosition, enemyLayerMask);

            if (hit == null)
            {
                return false;
            }

            EnemyEntity enemy = hit.GetComponentInParent<EnemyEntity>();

            if (enemy == null || !enemy.IsTargetable)
            {
                return false;
            }

            return battleManager.PlayCard(cardView.CardInstance, enemy);
        }

        /// <summary>
        /// 사용 기준선 위에서 놓은 비대상 카드를 사용합니다.
        /// </summary>
        /// <param name="cardView">사용할 카드 뷰입니다.</param>
        /// <param name="pointerWorldPosition">포인터의 월드 좌표입니다.</param>
        /// <returns>사용 성공 여부입니다.</returns>
        private bool TryPlayAboveLine(CardView cardView, Vector2 pointerWorldPosition)
        {
            if (pointerWorldPosition.y < playLineY)
            {
                return false;
            }

            return battleManager.PlayCard(cardView.CardInstance);
        }
        #endregion // 판정

        #region 입력 변환
        /// <summary>
        /// 현재 포인터의 화면 좌표를 월드 좌표로 변환합니다.
        /// </summary>
        /// <param name="pointerWorldPosition">변환된 월드 좌표입니다.</param>
        /// <returns>변환 성공 여부입니다.</returns>
        private bool TryGetPointerWorldPosition(out Vector2 pointerWorldPosition)
        {
            pointerWorldPosition = default;

            Pointer pointer = Pointer.current;

            if (pointer == null || targetCamera == null)
            {
                return false;
            }

            Ray pointerRay = targetCamera.ScreenPointToRay(pointer.position.ReadValue());
            Plane gamePlane = new(Vector3.forward, Vector3.zero);

            if (!gamePlane.Raycast(pointerRay, out float distance))
            {
                return false;
            }

            pointerWorldPosition = pointerRay.GetPoint(distance);
            return true;
        }
        #endregion // 입력 변환
    }
}
