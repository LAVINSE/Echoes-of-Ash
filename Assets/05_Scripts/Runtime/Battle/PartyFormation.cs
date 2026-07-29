using System.Collections.Generic;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 편성 인원수별 파티 배치 좌표를 보관합니다.
    /// 에디터에서 표식 Transform 위치를 데이터로 구워두고, 전투 스폰은 데이터만 읽습니다 (표식은 런타임 미사용).
    /// </summary>
    public class PartyFormation : SWMonoBehaviour
    {
        #region 데이터
        /// <summary>
        /// 인원수 하나에 대한 배치 좌표 목록입니다. 좌표 순서 = 파티 순서입니다.
        /// </summary>
        [System.Serializable]
        public class FormationData
        {
            /// <summary>이 배치 데이터를 사용하는 파티 인원수입니다.</summary>
            public int partyCount = 1;
            /// <summary>파티 순서에 대응하는 배치 좌표 목록입니다.</summary>
            [FormerlySerializedAs("posList")]
            public List<Vector3> positions = new();

            /// <summary>
            /// 파티 배치 데이터를 생성합니다.
            /// </summary>
            /// <param name="partyCount">파티 인원수입니다.</param>
            /// <param name="positions">파티 순서에 대응하는 배치 좌표 목록입니다.</param>
            public FormationData(int partyCount, List<Vector3> positions)
            {
                this.partyCount = partyCount;
                this.positions = positions;
            }
        }
        #endregion // 데이터

        #region 필드
        [SWGroup("데이터")]
        [Tooltip("인원수별 배치 좌표입니다. 좌표 순서 = 파티 순서 (characterRoot 기준 로컬)")]
        [SerializeField] private List<FormationData> partyFormationDatas = new();

        [SWGroup("포메이션 설정")]
        [Tooltip("설정/보기 버튼이 다룰 인원수입니다")]
        [SerializeField, Range(1, 3)] private int partyCount = 1;
        [SerializeField] private Transform firstFormation;
        [SerializeField] private Transform secondFormation;
        [SerializeField] private Transform thirdFormation;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>인원수별 배치 좌표 목록입니다.</summary>
        public IReadOnlyList<FormationData> PartyFormationDatas => partyFormationDatas;
        #endregion // 프로퍼티

        #region 조회
        /// <summary>
        /// 파티원의 스폰 좌표를 반환합니다. 데이터 미비 시 원점 폴백입니다 (미배선 통과 원칙).
        /// </summary>
        /// <param name="partyCount">파티 인원수입니다.</param>
        /// <param name="memberIndex">파티원 순번입니다 (스폰 순서 = 파티 순서).</param>
        /// <returns>characterRoot 기준 로컬 스폰 좌표입니다.</returns>
        public Vector3 GetSpawnPosition(int partyCount, int memberIndex)
        {
            FormationData formationData = FindFormationData(partyCount);

            if (formationData == null)
            {
                SWLog.LogWarning($"[PartyFormation] {partyCount}인 배치 데이터가 없습니다 - 원점 배치로 폴백합니다");
                return Vector3.zero;
            }

            if (memberIndex < 0 || memberIndex >= formationData.positions.Count)
            {
                SWLog.LogWarning($"[PartyFormation] {partyCount}인 배치에 {memberIndex + 1}번째 좌표가 없습니다 - 원점 배치로 폴백합니다");
                return Vector3.zero;
            }

            return formationData.positions[memberIndex];
        }

        /// <summary>
        /// 인원수에 해당하는 배치 데이터를 찾습니다.
        /// </summary>
        /// <param name="partyCount">파티 인원수입니다.</param>
        /// <returns>배치 데이터입니다. 없으면 null입니다.</returns>
        private FormationData FindFormationData(int partyCount)
        {
            foreach (FormationData formationData in partyFormationDatas)
            {
                if (formationData != null && formationData.partyCount == partyCount)
                {
                    return formationData;
                }
            }

            return null;
        }
        #endregion // 조회

        #region 유틸리티
        /// <summary>
        /// 현재 표식 Transform 위치를 지정한 인원수의 배치 데이터로 굽습니다. 같은 인원수 데이터가 있으면 갱신합니다.
        /// </summary>
        [SWButton("포메이션 설정")]
        private void SetFormation()
        {
            List<Vector3> positions = new();

            switch (partyCount)
            {
                case 1:
                    if (!TryAddMarkerPosition(positions, firstFormation)) return;
                    break;
                case 2:
                    if (!TryAddMarkerPosition(positions, firstFormation)) return;
                    if (!TryAddMarkerPosition(positions, secondFormation)) return;
                    break;
                case 3:
                    if (!TryAddMarkerPosition(positions, firstFormation)) return;
                    if (!TryAddMarkerPosition(positions, secondFormation)) return;
                    if (!TryAddMarkerPosition(positions, thirdFormation)) return;
                    break;
                default:
                    return;
            }

            FormationData formationData = FindFormationData(partyCount);

            if (formationData == null)
            {
                partyFormationDatas.Add(new FormationData(partyCount, positions));
            }
            else
            {
                formationData.positions = positions;
            }

            partyFormationDatas.Sort((left, right) => left.partyCount.CompareTo(right.partyCount));

            SWLog.Log($"[PartyFormation] {partyCount}인 배치를 저장했습니다.");
        }

        /// <summary>
        /// 지정한 인원수의 저장된 배치 좌표를 표식 Transform에 되돌려 씬에서 확인할 수 있게 합니다.
        /// 표식을 옮긴 뒤 "포메이션 설정"으로 다시 구우면 편집이 완성됩니다.
        /// </summary>
        [SWButton("포메이션 보기")]
        private void ViewFormation()
        {
            FormationData formationData = FindFormationData(partyCount);

            if (formationData == null)
            {
                SWLog.LogWarning($"[PartyFormation] {partyCount}인 배치 데이터가 없습니다 - 먼저 저장해 주세요.");
                return;
            }

            Transform[] markers = { firstFormation, secondFormation, thirdFormation };

            for (int index = 0; index < formationData.positions.Count && index < markers.Length; index++)
            {
                if (markers[index] != null)
                {
                    markers[index].localPosition = formationData.positions[index];
                }
            }

            SWLog.Log($"[PartyFormation] {partyCount}인 배치를 표식에 표시했습니다.");
        }

        /// <summary>
        /// 표식 Transform의 로컬 좌표를 목록에 추가합니다. 표식이 비어 있으면 실패합니다.
        /// </summary>
        /// <param name="positions">좌표를 담을 목록입니다.</param>
        /// <param name="marker">읽을 표식 Transform입니다.</param>
        /// <returns>추가에 성공했으면 true입니다.</returns>
        private bool TryAddMarkerPosition(List<Vector3> positions, Transform marker)
        {
            if (marker == null)
            {
                SWLog.LogError("[PartyFormation] 포메이션 설정 실패: 표식 Transform이 비어 있습니다.");
                return false;
            }

            positions.Add(marker.localPosition);
            return true;
        }
        #endregion // 유틸리티
    }
}
