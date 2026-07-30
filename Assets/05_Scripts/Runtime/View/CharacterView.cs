using EchoesOfAsh.Battle;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.View
{
    /// <summary>
    /// 캐릭터의 전투 외형과 상태를 표시합니다.
    /// </summary>
    public class CharacterView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Color deadColor = new(0.35f, 0.35f, 0.35f, 1f);

        private CharacterEntity characterEntity;
        private Color originColor;
        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 캐릭터 렌더러의 원래 색상을 저장합니다.
        /// </summary>
        private void Awake()
        {
            if (bodyRenderer != null)
            {
                originColor = bodyRenderer.color;
            }
        }

        /// <summary>
        /// 초기화
        /// </summary>
        /// <param name="characterEntity">표시할 파티원 엔티티</param>
        public void Init(CharacterEntity characterEntity)
        {
            if (characterEntity == null)
            {
                SWLog.LogError("[CharacterView] Init 실패: 파티원 엔티티가 null입니다");
                return;
            }

            Release();

            this.characterEntity = characterEntity;

            characterEntity.OnDied += HandleDied;

            if (characterEntity.IsDead)
            {
                HandleDied(characterEntity);
            }
        }

        /// <summary>
        /// 구독 이벤트 해제
        /// </summary>
        public void Release()
        {
            if (characterEntity != null)
            {
                characterEntity.OnDied -= HandleDied;
            }

            characterEntity = null;

            if (bodyRenderer != null)
            {
                bodyRenderer.color = originColor;
            }
        }
        #endregion // 초기화

        /// <summary>
        /// 전투불능 시 표시를 회색으로 바꿉니다.
        /// </summary>
        /// <param name="entity">전투불능이 된 엔티티입니다.</param>
        private void HandleDied(BattleEntity entity)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = deadColor;
            }
        }
    }
}
