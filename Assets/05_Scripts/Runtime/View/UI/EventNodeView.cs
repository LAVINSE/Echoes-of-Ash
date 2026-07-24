using SW.Attributes;
using SW.Base;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 이벤트 노드 화면입니다.
    /// </summary>
    public class EventNodeView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [FormerlySerializedAs("descTExt")]
        [SerializeField] private TextMeshProUGUI descriptionText;
        #endregion // 필드
    }
}
