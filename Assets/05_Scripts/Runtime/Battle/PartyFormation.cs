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
    /// 편집 화면의 표식 위치를 저장하며, 전투에서는 저장된 좌표만 사용합니다.
    /// </summary>
    public class PartyFormation : SWMonoBehaviour
    {
        #region 데이터
        /// <summary>
        /// 한 가지 파티 인원수에 대한 배치 좌표 목록입니다. 좌표는 파티원 순서대로 저장됩니다.
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
        /// 파티원의 생성 위치를 반환합니다. 위치가 설정되지 않았으면 원점을 반환합니다.
        /// </summary>
        /// <param name="partyCount">파티 인원수입니다.</param>
        /// <param name="memberIndex">파티원 순번입니다. 파티 목록의 순서와 같습니다.</param>
        /// <returns>캐릭터 부모 객체를 기준으로 한 생성 좌표입니다.</returns>
        public Vector3 GetSpawnPosition(int partyCount, int memberIndex)
        {
            FormationData formationData = FindFormationData(partyCount);

            if (formationData == null)
            {
                SWLog.LogWarning($"[PartyFormation] {partyCount}인 배치 데이터가 없어 원점에 배치합니다.");
                return Vector3.zero;
            }

            if (memberIndex < 0 || memberIndex >= formationData.positions.Count)
            {
                SWLog.LogWarning($"[PartyFormation] {partyCount}인 배치에 {memberIndex + 1}번째 좌표가 없어 원점에 배치합니다.");
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
        /// 현재 표식 위치를 지정한 인원수의 배치 좌표로 저장합니다. 같은 인원수의 데이터가 있으면 갱신합니다.
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
        /// 지정한 인원수의 저장된 배치 좌표를 표식에 적용해 편집 화면에서 확인할 수 있게 합니다.
        /// 표식을 옮긴 뒤 "포메이션 설정"을 누르면 변경한 위치가 저장됩니다.
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
        /// 부모 객체를 기준으로 한 표식 좌표를 목록에 추가합니다. 표식이 없으면 실패합니다.
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
