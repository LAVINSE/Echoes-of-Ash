using EchoesOfAsh.Battle;
using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using TMPro;
using UnityEngine;

namespace EchoesOfAsh.View
{
    /// <summary>
    /// 적 표시 뷰입니다.
    /// </summary>
    public class EnemyView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private SpriteRenderer enemySprite; // TODO : 나중에 수정예정 (아직 2D animation할지 그냥 Sprite할지 모름)
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Color deadTint = new(0.25f, 0.25f, 0.25f, 1f);

        [SWGroup("게이지")]
        [SerializeField] private GaugeView hpGauge;
        [SerializeField] private GaugeView sanityGauge;
        [SerializeField] private TMP_Text blockText;

        [SWGroup("정신력 색상")]
        [SerializeField] private Color calmColor = new(0.35f, 0.65f, 0.95f, 1f);
        [SerializeField] private Color madnessColor = new(0.75f, 0.25f, 0.85f, 1f);

        [SWGroup("의도")]
        [SerializeField] private IntentView intentView;
        [SerializeField] private TMP_Text targetText;

        [SWGroup("판정")]
        [SerializeField] private Collider2D targetCollider;

        private EnemyEntity entity;
        private EnemyAI enemyAI;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>표시 중인 적 엔티티입니다.</summary>
        public EnemyEntity Entity => entity;
        #endregion // 프로퍼티

        #region 초기화
        private void OnDestroy()
        {
            Release();
        }

        /// <summary>
        /// 표시할 적 엔티티입니다.
        /// </summary>
        /// <param name="entity">연결할 적 엔티티입니다.</param>
        /// <param name="enemyAI">연결할 적 인공지능입니다.</param>
        public void Init(EnemyEntity entity, EnemyAI enemyAI)
        {
            if (entity == null)
            {
                SWLog.LogError("[EnemyView] Init 실패: 엔티티가 null입니다");
                return;
            }

            this.entity = entity;
            this.enemyAI = enemyAI;

            entity.OnDied += HandleDied;
            entity.OnHpChanged += HandleHpChanged;
            entity.OnBlockChanged += HandleBlockChanged;
            entity.OnSanityChanged += HandleSanityChanged;
            entity.OnSanityTypeChanged += HandleSanityTypeChanged;

            enemyAI.OnIntentChanged += HandleIntentChanged;
            enemyAI.OnTargetChanged += HandleTargetChanged;
         
            if (nameText != null)
            {
                nameText.text = entity.DisplayName;
            }

            HandleHpChanged(entity.CurrentHp, entity.MaxHp);
            HandleBlockChanged(entity.CurrentBlock);
            HandleSanityChanged(entity.CurrentSanity, entity.MaxSanity);
            HandleSanityTypeChanged(entity.CurrentSanityType);

            HandleIntentChanged(entity, enemyAI.NextAction);
            HandleTargetChanged(entity, enemyAI.NextTarget);
        }

        /// <summary>
        /// 적 엔티티와 인공지능의 이벤트 구독을 해제합니다.
        /// </summary>
        public void Release()
        {
            if (entity != null)
            {
                entity.OnDied -= HandleDied;
                entity.OnHpChanged -= HandleHpChanged;
                entity.OnBlockChanged -= HandleBlockChanged;
                entity.OnSanityChanged -= HandleSanityChanged;
                entity.OnSanityTypeChanged -= HandleSanityTypeChanged;
            }

            if (enemyAI != null)
            {
                enemyAI.OnIntentChanged -= HandleIntentChanged;
                enemyAI.OnTargetChanged -= HandleTargetChanged;

            }

            enemyAI = null;
            entity = null;
        }
        #endregion // 초기화

        /// <summary>
        /// HP 변경 시 게이지를 갱신합니다.
        /// </summary>
        /// <param name="current">현재 HP입니다.</param>
        /// <param name="max">최대 HP입니다.</param>
        private void HandleHpChanged(int current, int max)
        {
            if (hpGauge != null)
            {
                hpGauge.SetValue(current, max);
            }
        }

        /// <summary>
        /// 방어막이 변경되면 표시를 갱신하며, 값이 0이면 숨깁니다.
        /// </summary>
        /// <param name="block">현재 방어막입니다.</param>
        private void HandleBlockChanged(int block)
        {
            if (blockText == null)
            {
                return;
            }

            blockText.gameObject.SetActive(block > 0);
            blockText.text = block.ToString();
        }

        /// <summary>
        /// 정신력 변경 시 보조 바를 갱신합니다.
        /// </summary>
        /// <param name="current">현재 정신력입니다.</param>
        /// <param name="max">최대 정신력입니다.</param>
        private void HandleSanityChanged(int current, int max)
        {
            if (sanityGauge != null)
            {
                sanityGauge.SetValue(current, max);
            }
        }

        /// <summary>
        /// 정신력 구간 전환 시 보조 바 색을 갱신합니다.
        /// </summary>
        /// <param name="sanityType">현재 정신력 유형입니다.</param>
        private void HandleSanityTypeChanged(ESanityType sanityType)
        {
            if (sanityGauge != null)
            {
                sanityGauge.SetFillColor(sanityType == ESanityType.Madness ? madnessColor : calmColor);
            }
        }

        /// <summary>
        /// 사망 처리합니다.
        /// </summary>
        /// <param name="diedEntity">사망한 엔티티입니다.</param>
        private void HandleDied(BattleEntity diedEntity)
        {
            if (enemySprite != null)
            {
                enemySprite.color = deadTint;
            }

            if (targetCollider != null)
            {
                targetCollider.enabled = false;
            }

            if (intentView != null)
            {
                intentView.Clear();
            }
        }

        /// <summary>
        /// 의도 변경 시 의도 표시를 갱신합니다.
        /// </summary>
        private void HandleIntentChanged(EnemyEntity changedEntity, EnemyActionData action)
        {
            if (intentView == null)
            {
                return;
            }

            intentView.SetIntent(action);
        }

        /// <summary>
        /// 예고 대상 변경 시 대상 이름 표시를 갱신합니다.
        /// </summary>
        /// <param name="enemy">적 엔티티입니다.</param>
        /// <param name="target">예고된 대상입니다. 파티를 노리지 않으면 null입니다.</param>
        private void HandleTargetChanged(EnemyEntity enemy, CharacterEntity target)
        {
            if (targetText == null)
            {
                return;
            }

            targetText.gameObject.SetActive(target != null);

            if (target != null)
            {
                targetText.text = $"→ {target.DisplayName}";
            }
        }
    }
}
