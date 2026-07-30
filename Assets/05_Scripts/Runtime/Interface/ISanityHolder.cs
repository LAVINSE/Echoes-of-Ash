using System;
using EchoesOfAsh.Enum;
using UnityEngine;

namespace EchoesOfAsh.Interface
{
    /// <summary>
    /// 정신력 값과 현재 정신력 상태를 관리하는 대상의 기능을 정의합니다.
    /// </summary>
    public interface ISanityHolder
    {
        /// <summary>현재 정신력입니다.</summary>
        public int CurrentSanity { get; }
        /// <summary>최대 정신력입니다.</summary>
        public int MaxSanity { get; }
        /// <summary>정신력 유형이 전환되는 임계값입니다.</summary>
        public int SanityThreshold { get; }
        /// <summary>현재 정신력 유형입니다.</summary>
        public ESanityType CurrentSanityType { get; }

        /// <summary>정신력 값이 변경될 때 호출됩니다.</summary>
        public event Action<int, int> OnSanityChanged;
        /// <summary>정신력 구간이 전환될 때 호출됩니다.</summary>
        public event Action<ESanityType> OnSanityTypeChanged;

        /// <summary>
        /// 정신력 증감입니다.
        /// </summary>
        /// <param name="delta">변동량입니다.</param>
        public void ChangeSanity(int delta);
    }
}
