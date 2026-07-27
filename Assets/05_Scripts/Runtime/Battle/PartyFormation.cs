using System.Collections.Generic;
using System.Linq;
using SW.Attributes;
using SW.Base;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    public class PartyFormation : SWMonoBehaviour
    {
        #region 데이터
        [System.Serializable]
        public class FormationData
        {
            public int partyCount = 1;
            public List<Vector3> posList = new();

            public FormationData(int partyCount, List<Vector3> posList)
            {
                this.partyCount = partyCount;
                this.posList = posList;
            }
        }
        #endregion // 데이터

        #region 필드
        [SWGroup("데이터")]
        [SerializeField] private List<FormationData> partyFormationDatas = new();

        [SWGroup("포메이션 설정")]
        [SerializeField, Range(1, 3)] private int partyCount = 1;
        [SerializeField] private Transform firstFormation;
        [SerializeField] private Transform secondFormation;
        [SerializeField] private Transform ThirdFormation;
        #endregion // 필드

        #region 프로퍼티
        public IReadOnlyList<FormationData> PartyFormationDatas => partyFormationDatas;
        #endregion // 프로퍼티

        #region 유틸리티
        [SWButton("포메이션 설정")]
        private void SetFormation()
        {
            List<Vector3> posList = new();

            switch (partyCount)
            {
                case 1:
                    posList.Add(firstFormation.position);
                    break;
                case 2:
                    posList.Add(firstFormation.position);
                    posList.Add(secondFormation.position);
                    break;
                case 3:
                    posList.Add(firstFormation.position);
                    posList.Add(secondFormation.position);
                    posList.Add(ThirdFormation.position);
                    break;
                default:
                    break;
            }

            FormationData data = new(partyCount, posList);

            var formationData = partyFormationDatas.Find(x => x.partyCount == partyCount);

            if (formationData == null)
            {
                partyFormationDatas.Add(data);
            }
            else
            {
                formationData.posList = posList;
            }

            partyFormationDatas.OrderByDescending(x => x.partyCount);
        }

        [SWButton("포메이션 보기")]
        private void ViewFormation()
        {

        }
        #endregion // 유틸리티
    }
}