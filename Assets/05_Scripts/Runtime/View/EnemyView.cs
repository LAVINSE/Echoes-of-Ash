using EchoesOfAsh.Battle;
using SW.Attributes;
using SW.Base;
using SW.Util;
using TMPro;
using UnityEngine;

namespace EchoesOfAsh.View
{
    /// <summary>
    /// 적 표시 뷰
    /// </summary>
    public class EnemyView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private SpriteRenderer enemySprite; // TODO : 나중에 수정예정 (아직 2D animation할지 그냥 Sprite할지 모름)
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Color deadTint = new(0.25f, 0.25f, 0.25f, 1f);

        [SWGroup("판정")]
        [SerializeField] private Collider2D targetCollider;

        private EnemyEntity entity;
        #endregion // 필드

        #region 프로퍼티
        public EnemyEntity Entity => entity;
        #endregion // 프로퍼티

        #region 초기화
        private void OnDestroy()
        {
            if (entity != null)
            {
                entity.OnDied -= HandleDied;
            }
        }

        /// <summary>
        /// 표시할 적 엔티티
        /// </summary>
        /// <param name="entity">연결할 적 엔티티</param>
        public void Init(EnemyEntity entity)
        {
            if (entity == null)
            {
                SWLog.LogError("[EnemyView] Init 실패: 엔티티가 null입니다");
                return;
            }

            this.entity = entity;
            entity.OnDied += HandleDied;

            if (nameText != null)
            {
                nameText.text = entity.DisplayName;
            }
        }
        #endregion // 초기화

        /// <summary>
        /// 사망 처리
        /// </summary>
        /// <param name="diedEntity">사망한 엔티티</param>
        private void HandleDied(BattleEntity diedEntity)
        {
            if (enemySprite != null)
            {
                enemySprite.color = deadTint;
            }
            
            if(targetCollider != null)
            {
                targetCollider.enabled = false;
            }
        }
    }
}