using System;
using EchoesOfAsh.Enum;
using UnityEngine;

namespace EchoesOfAsh.Interface
{
    public interface ISanityHolder
    {
        public int CurrentSanity { get; }
        public int MaxSanity { get; }
        public int SanityThreshold { get; }
        public ESanityType CurrentSanityType { get; }

        public event Action<int, int> OnSanityChanged;
        /// <summary>정신력 구간이 전환될 때 호출됩니다.</summary>
        public event Action<ESanityType> OnSanityTypeChanged;

        /// <summary>
        /// 정신력 증감
        /// </summary>
        /// <param name="delta">변동량입니다.</param>
        public void ChangeSanity(int delta);
    }
}
