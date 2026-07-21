using System.Collections.Generic;
using SW.Attributes;
using SW.Base;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif // UNITY_EDITOR

namespace EchoesOfAsh.View
{
    /// <summary>
    /// 대상 지정 화살표 뷰입니다.
    /// 시작 지점에서 지정 지점까지 베지어 곡선 화살표를 표시합니다.
    /// 입력을 직접 읽지 않으며, 드래그 핸들러가 BeginAiming / UpdateAiming / EndAiming으로 구동합니다.
    /// </summary>
    public class BezierArrowsView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("프리팹")]
        /// <summary>화살촉 프리팹입니다.</summary>
        [SerializeField] private GameObject arrowHeadPrefab;
        /// <summary>화살표 몸통 마디 프리팹입니다.</summary>
        [SerializeField] private GameObject arrowNodePrefab;

        [SWGroup("화살표")]
        /// <summary>몸통 마디 개수입니다.</summary>
        [SerializeField, Min(1)] private int arrowNodeCount = 12;
        /// <summary>곡선이 위로 솟는 높이입니다.</summary>
        [SerializeField, Min(0f)] private float curveHeight = 2f;
        /// <summary>화살표 전체 크기 배율입니다.</summary>
        [SerializeField, Min(0f)] private float scaleFactor = 1f;

        [SWGroup("디버그")]
        [Tooltip("에디터 전용 — 시작점을 이 오브젝트 위치로 두고 포인터를 따라가는 셀프 조준 테스트")]
        [SerializeField] private bool isSelfAimingTest;

        private Vector2 aimOrigin;
        private bool isAiming;
        private bool wasSelfAimingTest;

        // 생성 직후 노드는 활성 상태이므로, Awake의 첫 숨김 처리가 통과되도록 true로 시작합니다.
        private bool isArrowVisible = true;

        private readonly List<Transform> arrowNodes = new();
        private readonly List<Vector3> originalNodeScales = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>대상 지정 화살표가 활성화되어 있는지 여부입니다.</summary>
        public bool IsAiming => isAiming;
        #endregion // 프로퍼티

        #region 생명주기
        private void Awake()
        {
            CreateArrowNodes();
            SetArrowVisible(false);
        }

        private void OnDisable()
        {
            EndAiming();
        }
        #endregion // 생명주기

        #region 공개 메서드
        /// <summary>
        /// 대상 지정을 시작하고 화살표를 표시합니다.
        /// </summary>
        /// <param name="origin">화살표 시작 지점(월드 좌표)입니다. 보통 드래그 중인 카드 위치입니다.</param>
        public void BeginAiming(Vector2 origin)
        {
            aimOrigin = origin;
            isAiming = true;

            SetArrowVisible(true);
            UpdateArrow(origin);
        }

        /// <summary>
        /// 화살표 끝점을 갱신합니다. 조준 중이 아니면 무시됩니다.
        /// </summary>
        /// <param name="pointerWorldPosition">포인터의 월드 좌표입니다.</param>
        public void UpdateAiming(Vector2 pointerWorldPosition)
        {
            if (!isAiming)
            {
                return;
            }

            UpdateArrow(pointerWorldPosition);
        }

        /// <summary>
        /// 드래그 중인 카드의 움직임에 맞춰 화살표 시작 지점을 변경합니다.
        /// </summary>
        /// <param name="origin">새 시작 지점(월드 좌표)입니다.</param>
        public void SetOrigin(Vector2 origin)
        {
            aimOrigin = origin;
        }

        /// <summary>
        /// 대상 지정을 종료하고 화살표를 숨깁니다.
        /// </summary>
        public void EndAiming()
        {
            isAiming = false;
            SetArrowVisible(false);
        }
        #endregion // 공개 메서드

        #region 초기화
        /// <summary>
        /// 화살표 몸통과 화살촉을 생성합니다.
        /// </summary>
        private void CreateArrowNodes()
        {
            for (int index = 0; index < arrowNodeCount; index++)
            {
                CreateArrowObject(arrowNodePrefab);
            }

            CreateArrowObject(arrowHeadPrefab);
        }

        /// <summary>
        /// 화살표 오브젝트를 생성하고 초기 크기를 저장합니다.
        /// </summary>
        /// <param name="prefab">생성할 화살표 요소 프리팹입니다.</param>
        private void CreateArrowObject(GameObject prefab)
        {
            GameObject arrowObject = Instantiate(prefab, transform);
            Transform arrowTransform = arrowObject.transform;

            arrowNodes.Add(arrowTransform);
            originalNodeScales.Add(arrowTransform.localScale);
        }
        #endregion // 초기화

        #region 화살표 갱신
        /// <summary>
        /// 시작 지점에서 끝점을 향하도록 베지어 화살표를 갱신합니다.
        /// </summary>
        /// <param name="endPoint">화살표 끝점(월드 좌표)입니다.</param>
        private void UpdateArrow(Vector2 endPoint)
        {
            Vector2 startPoint = aimOrigin;

            Vector2 firstControlPoint =
                startPoint + Vector2.up * curveHeight;

            Vector2 secondControlPoint =
                Vector2.Lerp(startPoint, endPoint, 0.65f) +
                Vector2.up * curveHeight;

            int lastNodeIndex = arrowNodes.Count - 1;

            for (int index = 0; index < arrowNodes.Count; index++)
            {
                float normalizedPosition = (float)index / lastNodeIndex;

                float curvePosition =
                    Mathf.Log(normalizedPosition + 1f, 2f);

                Vector2 nodePosition = CalculateBezierPoint(
                    startPoint,
                    firstControlPoint,
                    secondControlPoint,
                    endPoint,
                    curvePosition
                );

                Transform arrowNode = arrowNodes[index];

                arrowNode.position = new Vector3(
                    nodePosition.x,
                    nodePosition.y,
                    transform.position.z
                );

                UpdateNodeScale(arrowNode, index);

                if (index > 0)
                {
                    UpdateNodeRotation(index);
                }
            }

            if (arrowNodes.Count >= 2)
            {
                arrowNodes[0].rotation = arrowNodes[1].rotation;
            }
        }

        /// <summary>
        /// 3차 베지어 곡선의 지정 위치를 계산합니다.
        /// </summary>
        private Vector2 CalculateBezierPoint(
            Vector2 startPoint,
            Vector2 firstControlPoint,
            Vector2 secondControlPoint,
            Vector2 endPoint,
            float curvePosition)
        {
            float remainingPosition = 1f - curvePosition;

            return
                Mathf.Pow(remainingPosition, 3f) * startPoint +
                3f * Mathf.Pow(remainingPosition, 2f) *
                curvePosition * firstControlPoint +
                3f * remainingPosition *
                Mathf.Pow(curvePosition, 2f) * secondControlPoint +
                Mathf.Pow(curvePosition, 3f) * endPoint;
        }

        /// <summary>
        /// 화살표 요소를 곡선 진행 방향으로 회전시킵니다.
        /// </summary>
        /// <param name="nodeIndex">회전시킬 요소의 인덱스입니다.</param>
        private void UpdateNodeRotation(int nodeIndex)
        {
            Vector2 previousPosition = arrowNodes[nodeIndex - 1].position;
            Vector2 currentPosition = arrowNodes[nodeIndex].position;
            Vector2 direction = currentPosition - previousPosition;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            float angle = Vector2.SignedAngle(Vector2.up, direction);

            arrowNodes[nodeIndex].rotation =
                Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// 화살촉에 가까울수록 화살표 요소를 크게 표시합니다.
        /// </summary>
        /// <param name="arrowNode">크기를 조정할 요소입니다.</param>
        /// <param name="nodeIndex">요소의 인덱스입니다.</param>
        private void UpdateNodeScale(Transform arrowNode, int nodeIndex)
        {
            float nodeScale =
                scaleFactor *
                (1f - 0.03f * (arrowNodes.Count - 1 - nodeIndex));

            nodeScale = Mathf.Max(0f, nodeScale);

            arrowNode.localScale =
                originalNodeScales[nodeIndex] * nodeScale;
        }
        #endregion // 화살표 갱신

        #region 표시
        /// <summary>
        /// 생성된 모든 화살표 요소의 표시 여부를 변경합니다. 같은 상태면 순회하지 않습니다.
        /// </summary>
        /// <param name="isVisible">표시 여부입니다.</param>
        private void SetArrowVisible(bool isVisible)
        {
            if (isArrowVisible == isVisible)
            {
                return;
            }

            isArrowVisible = isVisible;

            foreach (Transform arrowNode in arrowNodes)
            {
                arrowNode.gameObject.SetActive(isVisible);
            }
        }
        #endregion // 표시

        #region 에디터 디버그
#if UNITY_EDITOR
        private void Update()
        {
            if (!isSelfAimingTest)
            {
                // 테스트를 '끈 순간'에만 정리 — 외부(드래그 컨트롤러) 조준은 건드리지 않는다
                if (wasSelfAimingTest)
                {
                    wasSelfAimingTest = false;
                    EndAiming();
                }

                return;
            }

            wasSelfAimingTest = true;

            if (!isAiming)
            {
                BeginAiming(transform.position);
            }

            if (TryGetPointerWorldPosition(out Vector2 pointerWorldPosition))
            {
                UpdateAiming(pointerWorldPosition);
            }
        }

        /// <summary>
        /// 자체 조준 테스트를 위해 현재 포인터의 화면 좌표를 월드 좌표로 변환합니다.
        /// </summary>
        /// <param name="pointerWorldPosition">변환된 월드 좌표입니다.</param>
        /// <returns>변환 성공 여부입니다.</returns>
        private bool TryGetPointerWorldPosition(out Vector2 pointerWorldPosition)
        {
            pointerWorldPosition = default;

            Pointer pointer = Pointer.current;
            Camera mainCamera = Camera.main;

            if (pointer == null || mainCamera == null)
            {
                return false;
            }

            Ray pointerRay = mainCamera.ScreenPointToRay(pointer.position.ReadValue());
            Plane gamePlane = new(Vector3.forward, transform.position);

            if (!gamePlane.Raycast(pointerRay, out float distance))
            {
                return false;
            }

            pointerWorldPosition = pointerRay.GetPoint(distance);
            return true;
        }
#endif // UNITY_EDITOR
        #endregion // 에디터 디버그
    }
}
