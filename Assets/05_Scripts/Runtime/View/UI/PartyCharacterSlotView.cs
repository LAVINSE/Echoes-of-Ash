using EchoesOfAsh.Battle;
using SW.Attributes;
using SW.Base;
using SW.Util;
using TMPro;
using UnityEngine;

namespace EchoesOfAsh.View.UI
{
    /// <summary>
    /// 파티 슬롯 View
    /// </summary>
    public class PartyCharacterSlotView : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("표시")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private UIGaugeView hpGauge;
        [SerializeField] private TextMeshProUGUI blockText;
        [SerializeField] private GameObject deadMark;

        private CharacterEntity characterEntity;
        #endregion // 필드

        #region 프로퍼티
        #endregion // 프로퍼티

        /// <summary>
        /// 초기화합니다.
        /// </summary>
        /// <param name="characterEntity">표시할 파티원 엔티티입니다.</param>
        public void Init(CharacterEntity characterEntity)
        {
            if (characterEntity == null)
            {
                SWLog.LogError("[PartyCharacterSlotView] Init 실패: 파티원 엔티티가 null입니다");
                return;
            }

            Release();

            this.characterEntity = characterEntity;

            characterEntity.OnHpChanged += HandleHpChanged;
            characterEntity.OnBlockChanged += HandleBlockChanged;
            characterEntity.OnDied += HandleDied;

            if (nameText != null)
            {
                nameText.text = characterEntity.DisplayName;
            }

            HandleHpChanged(characterEntity.CurrentHp, characterEntity.MaxHp);
            HandleBlockChanged(characterEntity.CurrentBlock);

            if (deadMark != null)
            {
                deadMark.SetActive(characterEntity.IsDead);
            }
        }

        /// <summary>
        /// 파티원 이벤트 구독을 해제합니다.
        /// </summary>
        public void Release()
        {
            if (characterEntity != null)
            {
                characterEntity.OnHpChanged -= HandleHpChanged;
                characterEntity.OnBlockChanged -= HandleBlockChanged;
                characterEntity.OnDied -= HandleDied;
            }

            characterEntity = null;
        }

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
        /// 전투불능 시 표식을 켭니다.
        /// </summary>
        /// <param name="entity">전투불능이 된 엔티티입니다.</param>
        private void HandleDied(BattleEntity entity)
        {
            if (deadMark != null)
            {
                deadMark.SetActive(true);
            }
        }
    }
}