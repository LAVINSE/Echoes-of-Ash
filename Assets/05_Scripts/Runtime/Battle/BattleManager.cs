using System;
using System.Collections.Generic;
using EchoesOfAsh.Data;
using EchoesOfAsh.Deck;
using EchoesOfAsh.Effect;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Interface;
using EchoesOfAsh.Sanity;
using SW.Attributes;
using SW.Base;
using UnityEngine;

namespace EchoesOfAsh.Battle
{
    /// <summary>
    /// 전투 매니저
    /// </summary>
    public class BattleManager : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("데이터")]
        [SerializeField] private PartyData partyData;
        [SerializeField] private BattleBalanceData balanceData;
        [Tooltip("1인기준으로 테스트, 나중에 확장")]
        [SerializeField] private CharacterData characterData;
        [SerializeField] private List<CardData> startingCards = new();

        [SWGroup("적")]
        [SerializeField] private List<EnemyData> enemyDatas = new();

        [SWGroup("배치")]
        [SerializeField] private Transform characterRoot;
        [SerializeField] private Transform enemyRoot;

        private CharacterEntity characterEntity;

        private SanityHolder partySanityHolder;
        private DeckSystem deckSystem;
        private ApSystem apSystem;
        private EffectExecutor effectExecutor;
        private CardPlayService cardPlayService;
        private TargetResolver targetResolver;
        private TurnManager turnManager;

        private EBattleResult battleResult = EBattleResult.None;
        private bool isBattleRunning;

        private readonly List<CharacterEntity> party = new();
        private readonly List<EnemyEntity> enemies = new();
        private readonly List<EnemyAI> enemyAIs = new();
        private readonly List<ITargetable> cardTargetBuffer = new();
        private readonly List<ITargetable> enemyTargetBuffer = new();
        #endregion // 필드

        #region 프로퍼티
        /// <summary>전투 진행 여부</summary>
        public bool IsBattleRunning => isBattleRunning;
        /// <summary>전투 결과 타입</summary>
        public EBattleResult BattleResult => battleResult;

        /// <summary>파티원 엔티티)</summary>
        public CharacterEntity Character => characterEntity;
        /// <summary>적 엔티티 목록 (스폰 순서 = 행동 순서)</summary>
        public IReadOnlyList<EnemyEntity> Enemies => enemies;
        /// <summary>적 AI 목록 (적 엔티티 목록 인덱스와 일치)</summary>
        public IReadOnlyList<EnemyAI> EnemyAis => enemyAIs;

        /// <summary>파티 공유 정신력입니다.</summary>
        public ISanityHolder PartySanityHolder => partySanityHolder;
        /// <summary>덱 시스템입니다.</summary>
        public DeckSystem DeckSystem => deckSystem;
        /// <summary>AP 시스템입니다.</summary>
        public ApSystem ApSystem => apSystem;
        /// <summary>카드 사용 파이프라인</summary>
        public CardPlayService CardPlayService => cardPlayService;
        /// <summary>턴 매니저</summary>
        public TurnManager TurnManager => turnManager;

        /// <summary>전투 시작 시 호출</summary>
        public event Action OnBattleStarted;
        /// <summary>전투 종료 시 호출</summary>
        public event Action<EBattleResult> OnBattleEnded;
        #endregion // 프로퍼티

        #region 전투
        #endregion // 전투
    }
}