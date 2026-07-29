using EchoesOfAsh.Battle;
using EchoesOfAsh.Data;
using EchoesOfAsh.Interface;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Test
{
    /// <summary>
    /// 전투 엔티티의 피해, 회복 및 정신력 동작을 검증하는 테스트입니다.
    /// 빈 게임 오브젝트에 부착하고 적 및 캐릭터 데이터 에셋을 연결하여 전투 엔티티 기능을 검증합니다.
    /// </summary>
    public class BattleEntityTest : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("데이터")]
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private CharacterData characterData;

        private EnemyEntity enemy;
        private CharacterEntity character;

        private bool isRun = false;
        #endregion // 필드


        /// <summary>
        /// 시험용 전투 엔티티와 정신력 객체를 생성하고 이벤트 기록을 시작합니다.
        /// </summary>
        [SWButton("테스트 시작")]
        private void RunTest()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            isRun = true;

            if (enemyData != null)
            {
                enemy = new GameObject($"{enemyData.name}").AddComponent<EnemyEntity>();
                enemy.Init(enemyData);
                SubscribeEntity(enemy);
                SubscribeSanity(enemy);
            }
            else
            {
                SWLog.LogError("[BattleEntity] EnemyData가 비어 있습니다");
            }

            if (characterData != null)
            {
                character = new GameObject($"{characterData.name}").AddComponent<CharacterEntity>();
                character.Init(characterData);
                SubscribeEntity(character);
            }
            else
            {
                SWLog.LogError("[BattleEntity] CharacterData 비어 있습니다");
            }
        }

        /// <summary>
        /// 생성한 시험 객체와 기록을 초기 상태로 되돌립니다.
        /// </summary>
        [SWButton("테스트 초기화")]
        private void ResetTest()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResetCreatedEntities();
            isRun = false;
        }

        /// <summary>
        /// 객체가 제거될 때 시험용 엔티티와 정신력 객체를 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            ResetCreatedEntities();
        }


        /// <summary>
        /// 테스트에서 생성한 전투 엔티티를 정리합니다.
        /// </summary>
        private void ResetCreatedEntities()
        {
            ResetBattleEntity(ref enemy);
            ResetBattleEntity(ref character);
        }

        /// <summary>
        /// 전투 엔티티가 Unity에서 파괴된 상태인지 확인한 뒤 안전하게 초기화하고 제거합니다.
        /// </summary>
        private void ResetBattleEntity<TBattleEntity>(ref TBattleEntity battleEntity)
            where TBattleEntity : BattleEntity
        {
            if (battleEntity == null)
            {
                battleEntity = null;
                return;
            }

            GameObject battleEntityGameObject = battleEntity.gameObject;

            battleEntity.ResetEntity();
            battleEntity = null;

            if (battleEntityGameObject != null)
            {
                Destroy(battleEntityGameObject);
            }
        }

        /// <summary>
        /// 전투 엔티티 시험 조작 화면을 그립니다.
        /// </summary>
        private void OnGUI()
        {
            if (!isRun)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(20f, 20f, 460f, 600f));

            DrawCombatantControls("적 (EnemyData 경로)", enemy);
            GUILayout.Space(20f);
            DrawCombatantControls("파티원 (CharacterData 경로)", character);

            GUILayout.EndArea();
        }

        #region 테스트 UI
        /// <summary>
        /// 지정한 전투 엔티티의 상태와 조작 버튼을 그립니다.
        /// </summary>
        /// <param name="label">화면에 표시할 이름입니다.</param>
        /// <param name="battleEntity">조작할 전투 엔티티입니다.</param>
        private void DrawCombatantControls(string label, BattleEntity battleEntity)
        {
            GUILayout.Label($"=== {label} ===");

            if (battleEntity == null)
            {
                GUILayout.Label("(전투원 없음)");
                return;
            }

            string status = $"{battleEntity.DisplayName}  " +
                            $"HP {battleEntity.CurrentHp}/{battleEntity.MaxHp}  " +
                            $"방어막 {battleEntity.CurrentBlock}  " +
                            $"{(battleEntity.IsDead ? "[사망]" : "[생존]")}";

            if (battleEntity is ISanityHolder sanityHolder)
            {
                status += $"  정신력 {sanityHolder.CurrentSanity}/{sanityHolder.MaxSanity} " +
                          $"[{sanityHolder.CurrentSanityType}]";
            }

            GUILayout.Label(status);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("피해 10")) battleEntity.TakeDamage(10);
            if (GUILayout.Button("방어막 +5")) battleEntity.GainBlock(5);
            if (GUILayout.Button("방어막 초기화")) battleEntity.ResetBlock();
            if (GUILayout.Button("회복 5")) battleEntity.Heal(5);

            GUILayout.EndHorizontal();

            // SanityDamageEffect.Apply와 동일한 경로 — ISanityHolder 캐스팅 후 ChangeSanity
            if (battleEntity is ISanityHolder holder)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("정신력 -10 (SanityDamageEffect 경로)")) holder.ChangeSanity(-10);
                if (GUILayout.Button("정신력 +10")) holder.ChangeSanity(10);

                GUILayout.EndHorizontal();
            }
        }
        #endregion // 테스트 UI

        /// <summary>
        /// 전투 엔티티의 피해와 체력 변경 이벤트를 시험 기록에 연결합니다.
        /// </summary>
        /// <param name="battleEntity">구독할 전투 엔티티입니다.</param>
        private void SubscribeEntity(BattleEntity battleEntity)
        {
            string label = battleEntity.DisplayName;

            battleEntity.OnBlockChanged += block
                => SWLog.Log($"[CombatantTest] {label} OnBlockChanged: 방어막 {block}");

            battleEntity.OnHpChanged += (current, max)
                => SWLog.Log($"[CombatantTest] {label} OnHpChanged: {current}/{max}");

            battleEntity.OnDamaged += (hpLoss, original)
                => SWLog.Log($"[CombatantTest] {label} OnDamaged: 원본 {original} → 실제 HP 손실 {hpLoss}");

            battleEntity.OnDied += dead
                => SWLog.Log($"[CombatantTest] {label} OnDied ★");
        }

        /// <summary>
        /// 정신력 객체의 값과 유형 변경 이벤트를 시험 기록에 연결합니다.
        /// </summary>
        /// <param name="holder">구독할 정신력 객체입니다.</param>
        private void SubscribeSanity(ISanityHolder holder)
        {
            holder.OnSanityChanged += (current, max)
                => SWLog.Log($"[CombatantTest] 적 OnSanityChanged: {current}/{max}");

            holder.OnSanityTypeChanged += type
                => SWLog.Log($"[CombatantTest] 적 OnSanityTypeChanged: {type} ★");
        }
    }
}
