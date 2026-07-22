using System;
using System.Collections.Generic;
using EchoesOfAsh.Battle;
using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Dungeon
{
    /// <summary>
    /// 던전 매니저
    /// </summary>
    public class DungeonManager : SWMonoBehaviour
    {
        #region 타입
        /// <summary>
        /// 던전 진행 상태
        /// </summary>
        public enum EDungeonPhase
        {
            None,
            Battle,
            Ended,
        }
        #endregion // 타입

        #region 필드
        [SWGroup("참조")]
        [SerializeField] private BattleManager battleManager;

        [SWGroup("던전 구성 - 임시")]
        [Tooltip("0이면 무작위 시드 생성")]
        [SerializeField] private int dungeonSeed;
        [SerializeField] private List<CardData> startingCards = new(); // 대체
        [SerializeField] private List<SanityEventData> sanityEventDatas = new(); // 대체
        [SerializeField] private List<EnemyEncounterData> enemyEncounterDatas = new(); // 대체

        private DungeonState dungeonState;
        private EDungeonPhase currentPhase = EDungeonPhase.None;
        private int enemyEncounterIndex;
        private bool isSubscribed;
        #endregion // 필드

        #region 프로퍼티
        /// <summary>현재 던전 상태입니다. 던전 미진행 시 null</summary>
        public DungeonState DungeonState => dungeonState;
        /// <summary>현재 던전 진행 상태입니다.</summary>
        public EDungeonPhase CurrentPhase => currentPhase;

        /// <summary>던전 시작 시 호출됩니다.</summary>
        public event Action OnDungeonStarted;
        /// <summary>던전 종료 시 호출됩니다. (승리 여부)</summary>
        public event Action<bool> OnDungeonEnded;
        #endregion // 프로퍼티

        #region 초기화
        private void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>
        /// 전투 종료 이벤트를 구독합니다 (중복 구독 방지)
        /// </summary>
        private void Subscribe()
        {
            if (isSubscribed || battleManager == null)
            {
                return;
            }

            battleManager.OnBattleEnded += HandleBattleEnded;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || battleManager == null)
            {
                return;
            }

            battleManager.OnBattleEnded -= HandleBattleEnded;
            isSubscribed = false;
        }
        #endregion // 초기화

        #region 던전
        /// <summary>
        /// 던전을 시작합니다
        /// 시드 적용(D3 일원화) → 던전 상태 생성 → 첫 조우 진입
        /// </summary>
        [SWButton("던전 시작")]
        public void StartDungeon()
        {
            if (currentPhase == EDungeonPhase.Battle)
            {
                SWLog.LogWarning("[DungeonManager] StartDungeon 무시: 던전이 이미 진행 중입니다");
                return;
            }

            if (battleManager == null)
            {
                SWLog.LogError("[DungeonManager] StartDungeon 실패: BattleManager 참조가 없습니다");
                return;
            }

            if (enemyEncounterDatas.Count == 0)
            {
                SWLog.LogError("[DungeonManager] StartDungeon 실패: 조우 목록이 비어 있습니다");
                return;
            }

            // 시드 확정 - 0이면 무작위 생성 후 기록 (재현용)
            int seed = dungeonSeed != 0 ? dungeonSeed : Environment.TickCount;
            SWRandom.SetSeed(seed);

            dungeonState = new DungeonState(seed, startingCards, sanityEventDatas);
            enemyEncounterIndex = 0;

            Subscribe();

            SWLog.Log($"[DungeonManager] --- 던전 시작 (시드: {seed}, 조우 {enemyEncounterDatas.Count}개) ---");
            OnDungeonStarted?.Invoke();

            StartNextBattle();
        }

        /// <summary>
        /// 다음 조우 전투를 시작합니다. 남은 조우가 없으면 던전 승리로 종료합니다
        /// P2-M1에서 맵 화면 복귀 → 노드 선택으로 대체될 지점
        /// </summary>
        private void StartNextBattle()
        {
            if (enemyEncounterIndex >= enemyEncounterDatas.Count)
            {
                EndDungeon(true);
                return;
            }

            EnemyEncounterData encounter = enemyEncounterDatas[enemyEncounterIndex];

            if (!battleManager.StartBattle(dungeonState, encounter))
            {
                SWLog.LogError($"[DungeonManager] 전투 시작 실패 (조우 {enemyEncounterIndex}) - 던전을 종료합니다");
                EndDungeon(false);
                return;
            }

            currentPhase = EDungeonPhase.Battle;
        }

        /// <summary>
        /// 던전을 종료합니다
        /// 던전 상태는 결과 화면 참조를 위해 유지 - 다음 StartDungeon에서 교체된다
        /// </summary>
        /// <param name="isVictory">승리 여부</param>
        private void EndDungeon(bool isVictory)
        {
            currentPhase = EDungeonPhase.Ended;

            SWLog.Log($"[DungeonManager] --- 던전 종료: {(isVictory ? "승리" : "패배")} ---");
            OnDungeonEnded?.Invoke(isVictory);
        }
        #endregion // 던전

        /// <summary>
        /// 전투 종료 시 던전 흐름을 진행한다
        /// 승리 = 다음 조우 / 패배 = 던전 종료
        /// </summary>
        /// <param name="battleResult">전투 결과</param>
        private void HandleBattleEnded(EBattleResult battleResult)
        {
            if (currentPhase != EDungeonPhase.Battle)
            {
                return;
            }

            if (battleResult != EBattleResult.Victory)
            {
                EndDungeon(false);
                return;
            }

            enemyEncounterIndex++;
            StartNextBattle();
        }
    }
}