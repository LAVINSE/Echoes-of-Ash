using System;
using System.Linq;
using UnityEngine;

namespace EchoesOfAsh.Base
{
    [CreateAssetMenu(fileName = "IdentifiedObject", menuName = "EchoesOfAsh/Base/IdentifiedObject")]
    public class IdentifiedObject : ScriptableObject, ICloneable
    {
        #region 필드
        [SerializeField] private Category[] categories;
        [SerializeField] private int id;
        [SerializeField] private string codeName;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        #endregion // 필드

        #region 프로퍼티
        public int ID => id;
        public string CodeName => codeName;
        public string DisplayName => displayName;
        public virtual string Description => description;
        #endregion // 프로퍼티

        public virtual object Clone()
            => Instantiate(this);

        public bool HasCategory(Category category)
            => categories.Any(x => x.ID == category.ID);

        public bool HasCategory(string category)
            => categories.Any(x => x == category);
        
    }
}