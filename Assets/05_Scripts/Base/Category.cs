using EchoesOfAsh.Base;
using UnityEngine;


namespace EchoesOfAsh.Base
{
    [CreateAssetMenu(fileName = "Category", menuName = "EchoesOfAsh/Base/Category")]
    public class Category : IdentifiedObject
    {
        public override bool Equals(object other)
            => base.Equals(other);

        public override int GetHashCode()
            => base.GetHashCode();

        public static bool operator ==(Category lhs, string rhs)
        {
            if (lhs is null)
            {
                return rhs is null;
            }

            return lhs.CodeName == rhs;
        }

        public static bool operator !=(Category lhs, string rhs)
            => !(lhs == rhs);
    }
}