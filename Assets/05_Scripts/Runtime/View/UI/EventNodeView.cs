using SW.Attributes;
using SW.Base;
using TMPro;
using UnityEngine;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 이벤트 노드의 View UI
    /// </summary>
    public class EventNodeView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descTExt;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티
    }
}