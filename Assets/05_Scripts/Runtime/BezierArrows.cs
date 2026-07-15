using System.Collections.Generic;
using SW.Attributes;
using SW.Base;
using UnityEngine;
using UnityEngine.InputSystem;

public class BezierArrows : SWMonoBehaviour
{
    #region 필드
    [SWGroup("프리팹")]
    [SerializeField] private GameObject arrowHeadPrefab;
    [SerializeField] private GameObject arrowNodePrefab;

    [SWGroup("화살표")]
    [SerializeField, Min(1)] private int arrowNodeCount = 12;
    [SerializeField, Min(0f)] private float curveHeight = 2f;
    [SerializeField, Min(0f)] private float scaleFactor = 1f;
    [SerializeField] private bool showOnAwake;

    [SWGroup("카메라")]
    [SerializeField] private Camera targetCamera;

    private readonly List<Transform> arrowNodes = new();
    private readonly List<Vector3> originalNodeScales = new();

    private bool isAiming;
    #endregion // 필드

    #region 프로퍼티
    /// <summary>
    /// 현재 대상 지정 화살표가 활성화되어 있는지 반환합니다.
    /// </summary>
    public bool IsAiming => this.isAiming;
    #endregion // 프로퍼티

    #region 생명주기
    private void Awake()
    {
        this.InitializeCamera();
        this.CreateArrowNodes();

        if (this.showOnAwake)
        {
            this.BeginAiming();
        }
        else
        {
            this.EndAiming();
        }
    }

    private void Update()
    {
        if (!this.isAiming)
        {
            return;
        }

        if (!this.TryGetPointerWorldPosition(
                out Vector2 pointerWorldPosition))
        {
            this.SetArrowVisible(false);
            return;
        }

        this.SetArrowVisible(true);
        this.UpdateArrow(pointerWorldPosition);
    }

    private void OnDisable()
    {
        this.EndAiming();
    }
    #endregion // 생명주기

    #region 공개 메서드
    /// <summary>
    /// 카드의 대상 지정을 시작하고 화살표를 표시합니다.
    /// </summary>
    public void BeginAiming()
    {
        this.isAiming = true;
        this.SetArrowVisible(true);
    }

    /// <summary>
    /// 카드의 대상 지정을 종료하고 화살표를 숨깁니다.
    /// </summary>
    public void EndAiming()
    {
        this.isAiming = false;
        this.SetArrowVisible(false);
    }
    #endregion // 공개 메서드

    #region 초기화
    /// <summary>
    /// 사용할 카메라를 초기화합니다.
    /// </summary>
    private void InitializeCamera()
    {
        if (this.targetCamera == null)
        {
            this.targetCamera = Camera.main;
        }
    }

    /// <summary>
    /// 화살표 몸통과 화살촉을 생성합니다.
    /// </summary>
    private void CreateArrowNodes()
    {
        for (int index = 0; index < this.arrowNodeCount; index++)
        {
            this.CreateArrowObject(this.arrowNodePrefab);
        }

        this.CreateArrowObject(this.arrowHeadPrefab);
    }

    /// <summary>
    /// 화살표 오브젝트를 생성하고 초기 크기를 저장합니다.
    /// </summary>
    private void CreateArrowObject(GameObject prefab)
    {
        GameObject arrowObject = Instantiate(
            prefab,
            this.transform
        );

        Transform arrowTransform = arrowObject.transform;

        this.arrowNodes.Add(arrowTransform);
        this.originalNodeScales.Add(arrowTransform.localScale);
    }
    #endregion // 초기화

    #region 화살표 갱신
    /// <summary>
    /// 포인터 위치를 향하도록 베지어 화살표를 갱신합니다.
    /// </summary>
    private void UpdateArrow(Vector2 pointerWorldPosition)
    {
        Vector2 startPoint = this.transform.position;
        Vector2 endPoint = pointerWorldPosition;

        Vector2 firstControlPoint =
            startPoint + Vector2.up * this.curveHeight;

        Vector2 secondControlPoint =
            Vector2.Lerp(startPoint, endPoint, 0.65f) +
            Vector2.up * this.curveHeight;

        int lastNodeIndex = this.arrowNodes.Count - 1;

        for (int index = 0; index < this.arrowNodes.Count; index++)
        {
            float normalizedPosition =
                (float)index / lastNodeIndex;

            float curvePosition =
                Mathf.Log(normalizedPosition + 1f, 2f);

            Vector2 nodePosition = this.CalculateBezierPoint(
                startPoint,
                firstControlPoint,
                secondControlPoint,
                endPoint,
                curvePosition
            );

            Transform arrowNode = this.arrowNodes[index];

            arrowNode.position = new Vector3(
                nodePosition.x,
                nodePosition.y,
                this.transform.position.z
            );

            this.UpdateNodeScale(arrowNode, index);

            if (index > 0)
            {
                this.UpdateNodeRotation(index);
            }
        }

        if (this.arrowNodes.Count >= 2)
        {
            this.arrowNodes[0].rotation =
                this.arrowNodes[1].rotation;
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
    private void UpdateNodeRotation(int nodeIndex)
    {
        Vector2 previousPosition =
            this.arrowNodes[nodeIndex - 1].position;

        Vector2 currentPosition =
            this.arrowNodes[nodeIndex].position;

        Vector2 direction =
            currentPosition - previousPosition;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        float angle =
            Vector2.SignedAngle(Vector2.up, direction);

        this.arrowNodes[nodeIndex].rotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// 화살촉에 가까울수록 화살표 요소를 크게 표시합니다.
    /// </summary>
    private void UpdateNodeScale(
        Transform arrowNode,
        int nodeIndex)
    {
        float nodeScale =
            this.scaleFactor *
            (1f - 0.03f * (this.arrowNodes.Count - 1 - nodeIndex));

        nodeScale = Mathf.Max(0f, nodeScale);

        arrowNode.localScale =
            this.originalNodeScales[nodeIndex] * nodeScale;
    }
    #endregion // 화살표 갱신

    #region 입력
    /// <summary>
    /// 현재 포인터의 화면 좌표를 월드 좌표로 변환합니다.
    /// </summary>
    private bool TryGetPointerWorldPosition(
        out Vector2 pointerWorldPosition)
    {
        pointerWorldPosition = default;

        Pointer pointer = Pointer.current;

        if (pointer == null || this.targetCamera == null)
        {
            return false;
        }

        Vector2 pointerScreenPosition =
            pointer.position.ReadValue();

        Ray pointerRay =
            this.targetCamera.ScreenPointToRay(
                pointerScreenPosition
            );

        Plane gamePlane = new(
            Vector3.forward,
            this.transform.position
        );

        if (!gamePlane.Raycast(pointerRay, out float distance))
        {
            return false;
        }

        Vector3 worldPosition =
            pointerRay.GetPoint(distance);

        pointerWorldPosition = worldPosition;
        return true;
    }
    #endregion // 입력

    #region 표시
    /// <summary>
    /// 생성된 모든 화살표 요소의 표시 여부를 변경합니다.
    /// </summary>
    private void SetArrowVisible(bool isVisible)
    {
        foreach (Transform arrowNode in this.arrowNodes)
        {
            arrowNode.gameObject.SetActive(isVisible);
        }
    }
    #endregion // 표시
}