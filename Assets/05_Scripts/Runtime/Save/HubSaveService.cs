namespace EchoesOfAsh.Save
{
    /// <summary>
    /// 거점 누적 진행(기획 용어: 메타 진행) 구획의 접근 창구입니다.
    /// 파일 입출력은 GameSaveService에 위임하며, 이 클래스는 거점 구획의 조작만 담당합니다.
    /// </summary>
    public static class HubSaveService
    {
        #region 프로퍼티
        /// <summary>현재 프로필의 거점 데이터입니다.</summary>
        public static HubSaveData Current => GameSaveService.Current.hub;
        #endregion // 프로퍼티

        #region 저장
        /// <summary>
        /// 거점 데이터를 포함한 프로필 저장을 수행합니다.
        /// </summary>
        /// <returns>저장 성공 여부입니다.</returns>
        public static bool Save()
        {
            return GameSaveService.Save();
        }
        #endregion // 저장

        #region 자원
        /// <summary>
        /// 아이템을 보유량에 더합니다. 저장은 호출자가 일괄 수행합니다.
        /// </summary>
        /// <param name="codeName">아이템 코드 이름입니다.</param>
        /// <param name="count">더할 수량입니다.</param>
        public static void AddItem(string codeName, int count)
        {
            if (string.IsNullOrEmpty(codeName) || count <= 0)
            {
                return;
            }

            HubSaveData hubData = Current;

            foreach (ItemCountSaveData item in hubData.items)
            {
                if (item.codeName == codeName)
                {
                    item.count += count;
                    return;
                }
            }

            hubData.items.Add(new ItemCountSaveData { codeName = codeName, count = count });
        }

        /// <summary>
        /// 아이템 보유량을 반환합니다.
        /// </summary>
        /// <param name="codeName">아이템 코드 이름입니다.</param>
        /// <returns>보유 수량입니다. 없으면 0입니다.</returns>
        public static int GetItemCount(string codeName)
        {
            foreach (ItemCountSaveData item in Current.items)
            {
                if (item.codeName == codeName)
                {
                    return item.count;
                }
            }

            return 0;
        }
        #endregion // 자원
    }
}