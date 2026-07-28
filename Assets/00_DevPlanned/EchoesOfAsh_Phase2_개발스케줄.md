# Echoes of Ash — Phase 2 개발 스케줄

> 기준 문서: 기획서 v3.2 (16장 Phase 2 체크리스트) + Phase 1 개발스케줄 개정 11 · 작성일: 2026-07-22 (개정 21 — 2026-07-29, P2-M7 7-4 챕터 SO 신설 + 정신력 이벤트 DD 방식 개정 + SanityEventRunner 개명)
> 목표: **런 1회 완주 + 거점 순환 + 파티가 가능한 상태** — 기간 잠정 12주 (기획 2~4개월 범위)

---

## 1. 착수 전제 (Phase 1 → Phase 2 인계)

### 인계 상태

- **Phase 1 코드 완료** (M0~M6-1) — 전투 1사이클·정신력·뷰/입력 전부 동작. 6-2~6-5(콘텐츠 데이터·밸런스·검증)는 리소스 작업 시점 이월 (구성안 확정 보관)
- **정신력 시스템 유지 확정** (코어 디자인 결정 — Phase 1 개정 11). 6-5는 **밸런스 게이트** — 수치·이벤트 효과 변경은 전부 에셋 교체로 대응, 코드 무수정
- **하이브리드 렌더링 확정** (D5 개정 7): 전장 = 월드 / 화면 부착 UI = Canvas(Screen Space - Camera). Phase 2 신규 화면(맵·거점·상점·편성)은 **전부 Canvas** — 결정 불필요 → **마을만 의도적 이탈 (개정 20): 건물/배경 = 월드 스프라이트 + 팝업/HUD = Canvas (P2-M6 6-1 완료 기록 참조)**
- **아트 부담 억제 유지**: Phase 2도 플레이스홀더 — 기능적 아트는 리소스 작업 시점

### Phase 1이 남긴 임시 조치 = P2-M0의 작업 목록

| 임시 조치 | 대체 |
|-----------|------|
| `BattleManager.startingCards` 인스펙터 주입 | 던전 상태(DungeonState)의 덱 주입 — ✅ 해소 (P2-M0) |
| `BattleManager.sanityEvents` 인스펙터 풀 | 던전 상태 경유 주입 — ✅ 해소 (P2-M0) |
| `BattleManager.characterData` 단수 필드 | 파티 구성 목록 (P2-M4에서 3인 확장) — ✅ 해소 (개정 14 — DungeonState 경유) |
| `BattleManager.enemyEncounterData` 단일 지정 | 맵 노드 → 조우 참조 (P2-M1) — ✅ 해소 (P2-M1) |
| `EEnemyTargetRuleType.Aggro` 무작위 폴백 | 어그로 실구현 — ✅ 해소 (P2-M5, 개정 18) = **Phase 1 임시 조치 전량 해소 완료** |

---

## 2. 마일스톤 로드맵 (P2-M0 ~ P2-M8, 잠정 12주)

> 순서는 의존성 순. **밸런스 게이트(6-2~6-5)는 리소스 작업과 병행** — P2-M0~M3(메타 계층)은 게이트와 독립, P2-M4부터는 게이트 이후 권장 (강제 아님 — 정신력 유지 확정으로 완화).

### P2-M0 — 런 상태 + 흐름 골격 (1주) ✅ 완료 (개정 3)

| # | 산출물 | 책임 |
|---|--------|------|
| 0-1 | `Dungeon/DungeonState.cs` | 던전 1회 도전(런)의 가변 상태 단일 소유 (순수 클래스): 덱 목록(CardInstance), 파티 구성, 자원, 던전 시드, 현재 노드 위치. 저장 스키마의 원본 |
| 0-2 | `Dungeon/DungeonManager.cs` | 던전 시작/종료 + **Dungeon 씬 내 화면 상태 머신** (맵 ⇄ 전투 ⇄ 노드 화면 — P2-D6) + 거점⇄던전 씬 전환. 조립 지점 — BattleManager에 던전 상태를 주입하는 유일한 주체 |
| 0-3 | `BattleManager` 임시 조치 대체 | `StartBattle(DungeonState, EnemyEncounterData)` 형태로 주입 경로 전환 — 인스펙터 필드 4종 제거 (위 표) |

**DoD:** 런 시작 → 전투 진입 → 승리 → 런 상태(덱·자원) 반영 → 다음 전투 진입이 코드 경로로 완주된다.

#### P2-M0 완료 기록 (2026-07-23)

- **산출물:** `Dungeon/DungeonState.cs` + `Dungeon/DungeonManager.cs` (P2-D6 — Dungeon 씬 부착, 조우 순차 진행 최소 골격) + `BattleManager` 주입 경로 전환 (`StartBattle(DungeonState, EnemyEncounterData)`) + `BattleTest` 자체 DungeonState 구성 경로 (모듈 단독 테스트 유지)
- **임시 조치의 상승 이동:** BattleManager 인스펙터 4종 제거(`startingCards`/`sanityEvents`/`enemyEncounterData` — `characterData`는 계획대로 P2-M4까지 잔존) → **DungeonManager 인스펙터로 이동** (`startingCards` = 편성 화면 대체 예정 / `sanityEventDatas` = 던전 구성 데이터 대체 예정 / `enemyEncounterDatas` 순차 목록 = 맵 노드 그래프 대체 예정 — P2-M1)
- **던전 수명 덱 확립:** CardInstance가 전투마다 재생성되지 않고 던전 1회 동안 유지 — 15-5(강화·상태 유지)의 토대. 전투 한정 상태는 기존 `EndBattle → ResetDeckSystem → ResetBattleApCost` 경로가 정리
- **주입 순서 계약:** `StartBattle`은 주입 보관 → `ValidateData`(필드 검사) → `ResetBattle` 순 — `ResetBattle`은 `dungeonState`/`currentEncounter`를 지우지 않는다 (던전 소유는 DungeonManager)
- **시드 규칙:** `StartDungeon`에서 1회 `SWRandom.SetSeed` (D3 일원화). 0 = 무작위 생성 후 기록 (재현용)
- **명명 정리 (적용 개정):** DungeonState 프로퍼티 `SanityEventDatas`, DungeonManager 필드 `enemyEncounterDatas`/`sanityEventDatas`/`enemyEncounterIndex`
- **씬 정리 노트:** Dungeon.unity의 BattleManager 프리팹 오버라이드에 제거 필드 잔여 값 존재 — 미사용 오버라이드 정리 필요 (무해)

### P2-M1 — 맵 / 런 루프 (1.5주) ✅ 완료 (개정 11 — 실플레이 통합 검증은 리소스 연결 시점 이월)

| # | 산출물 | 책임 |
|---|--------|------|
| 1-1 | `Map/MapGenerator.cs` | 시드 기반 층×레인 그래프 생성 (`SWRandom` — 던전 시드): 경로 랜덤 워크 → 간선 병합 → 타입 배정 규칙 → **광기 간선 승격 + 격자 좌표 산출** (P2-D2) |
| 1-2 | 노드 타입 데이터 | 전투 / 엘리트 / 휴식(SAN +30) / 이벤트 / 상점 / **보관** / 보스 — 노드 → 콘텐츠 참조 (전투 노드 = `EnemyEncounterData` 조우 풀 + `EnemyData.SpawnRange` 결합) |
| 1-3 | 맵 화면 (Canvas) | **던전 도면식 표현** (P2-D2): 방·복도 렌더 + 시드 기반 좌표 지터 + 가로 심부 진행 + 잿불 침식/광기 간선 표시 + 이동 가능 방 선택 — 플레이스홀더(사각 방+통로) |
| 1-5 | DungeonManager 맵 통합 | ~~SWStackStateMachine 채택~~ → **철회 (구현 검증 후):** 상태 2개·전이 선형·로직이 컨텍스트 메서드에 있어 상태 클래스가 빈 껍데기화 — 간접층만 추가. **enum 화면 상태 유지** + 맵 이동 API(`GetAvailableNodes`/`MoveToNode`/`OnNodeEntered`) 통합. **재채택 기준 기록:** ① 노드 화면이 실제 중첩 스택을 요구할 때(이벤트발 전투 → 이벤트 복귀 등) ② 상태별 매 프레임 Tick 로직이 생길 때 |
| 1-6 | 잿불 침식 + 광기 간선 + **파티 SAN 던전 지속화** | 이동 카운터 → 잠식 층 판정 → 방 상태 반영 (속도 = Balance 외부화, 0 = 비활성) / **선행 발견(1-5): 광기 간선·휴식 SAN 회복·침식이 전부 "던전 지속 SAN"을 전제 — 현재는 전투마다 SanityHolder 신규 생성.** `DungeonState`에 SAN 이월 보관 → `BattleManager` 시작 시 주입·종료 시 기록 → 광기 복도 통행 판정·휴식 회복·의도적 광기 진입 수단을 그 위에 설계 |
| 1-4 | 휴식·이벤트·보관 노드 기초 | 휴식 = SAN 회복 / 이벤트 = 데이터 기반 선택지 골격 / 보관 = 전송 UI 골격 (드랍 연동은 P2-M6) |

**DoD:** 맵 생성 → 노드 선택 → 전투/휴식 → 맵 복귀 → 보스 노드 도달까지 런 루프 완주.

#### 1-1·1-2 완료 기록 (2026-07-23)

- **산출물:** `Map/MapGraph.cs` (MapNode·MapEdge·MapGraph — 순수 데이터, `[Serializable]` 저장 스키마 원본) + `Map/MapGenerator.cs` (순수 클래스) + `Data/MapConfigData.cs` (구조·가중치·광기 간선·침식 간격·도면 배치 전부 외부화) + `Test/MapGeneratorTest.cs` + `EMapNodeType` → `GameEnum.cs` 병합
- **생성 5단계:** 경로 랜덤 워크(모든 방이 경로의 산물 — 고아 방·경로 단절 원리상 없음) → 타입 배정(첫 층 전투·보스 직전 휴식 고정, 가중치 무작위, 엘리트 최소 층, 보관 1개 보장 — 결정성 위해 순회 순서 고정 변환) → 광기 간선 승격(미연결 인접 후보 확률) → 보스 연결(마지막 층 전체) → 좌표 산출(가로 진행 + 시드 지터)
- **광기 전용 방 = 자동 판정:** 진입 복도가 전부 광기 복도인 방에 표식 — 별도 배정 규칙 불필요
- **이동 판정 계약:** `GetNextNodes(from, buffer, includeMadness)` — 논할당 버퍼, 잠식 방 자동 제외 (침식 규칙은 `SetAshConsumed`만 호출), 광기 복도 포함 여부는 호출자(진행 규칙) 결정
- **불변식 검증 내장:** 테스트가 일반 복도만으로 입구→보스 BFS 도달성 검증 — 광기 간선은 지름길이지 필수 경로가 아님을 매 생성마다 확인
- **명명 정리 (적용 개정):** `Identifier` 전체 표기, `GenerateMapGraph`, `PositionOffset`, 단계 함수명 서술형 통일

#### 1-3 완료 기록 (2026-07-24)

- **산출물:** `View/UI/MapView.cs` + `View/UI/MapRoomView.cs` + MapRoom·복도 프리팹 + `DungeonManager` 뷰 배선(시작 `Initialize`+`Show`+`RefreshNodeStates` / 전투 진입 `Hide` / 복귀 `Show`+갱신 / 종료 `Hide`)
- **던전 도면식 표현 (P2-D2 이행):** 방(버튼+타입 라벨+상태 색) + 복도(중점·회전·길이 배치 Image, 광기 복도 색 구분) + 수평 ScrollRect + 현재 방 자동 중앙 포커스. 복도 → 방 생성 순서 = 그리기 순서 (복도 끝이 방 뒤로 숨음 — 사이블링 원칙)
- **좌표계 단일화:** `originRoot` 기준점 — 그래프 좌표를 변환 없이 그대로 배치, 좌측 여백은 `Init`에서 기준점 이동 1줄로 일괄 적용 (배치·포커스 계산 불일치 원천 차단)
- **역할 분리:** 이동 요청 = 콜백 주입(`Action<int>` — BattleHudView 패턴), 갱신 시점 = DungeonManager 주도 폴링형 (맵은 이동 시점에만 변하므로 구독형보다 단순), 상태 우선순위 = 잠식 > 현재 > 방문 > 기본 + 이동 가능 강조 + 광기 방 † 라벨
- **풀링 없음:** 던전당 1회 생성(~30방·~40복도) — 풀링 경계 원칙(반복 스폰만) 준수
- **씬 검증 소화:** 도면 표시·방 클릭 이동·전투 전환·복귀 포커스 실동작 확인

#### 1-6 완료 기록 (2026-07-24)

- **산출물:** `DungeonState` SAN 이월·침식 상태 보관 (`CarriedSanity`/`MoveCount`/`AshConsumedFloor`) + `DungeonManager` 광기 통행(`IsPartyMadness` → `GetNextNodes(includeMadness)`)·잿불 침식(`AdvanceAshErosion`)·던전 수위 SAN API(`ChangeDungeonSanity`) + `BattleManager` 시작 시 주입(`HasCarriedSanity` 폴백)·종료 시 기록(`OnBattleEnded` 발화 전) + `MapGraph.ConsumeFloorsByAsh`
- **SAN 이월 = 값 이월:** Holder가 아닌 int 값만 이월 (-1 = 미기록 → BattleTest 단독 경로 무손상). 상한 클램프는 전투 진입 시 SanityHolder 생성자가 처리 — **던전 수위 상한 미클램프는 잠정, P2-M4 재점검**
- **광기 통행 판정 일원화:** `CarriedSanity < PartyData.SanityThreshold` — SanityHolder 임계값 계약(M1)과 동일식
- **침식 패배 규칙 (잠정):** 잠식 층 ≥ 현재 층 = 던전 패배. 갇힘은 원리상 불가 (모든 방 = 랜덤 워크 산물 → 일반 복도 출구 보유). `AshAdvanceInterval = 0` = 완전 비활성 (순정 STS). 침식 판정은 노드 진입 처리보다 선행 — 잠식 패배 시 노드 진입 스킵
- **던전 수위 SAN 계약:** `ChangeDungeonSanity`는 **전투 중 호출 무시** (전투 중 진실 원본 = `partySanityHolder` — `EndBattle` 기록이 덮어쓰는 유실 방지). 의도적 광기 진입 수단은 API만 확보 → 1-4에서 휴식 선택지 데이터로 해소
- **버그 수정 (데이터):** Enemy_Test1의 `SanityChangeEffect` 델타 부호 +5 → -5 — 파티 SAN 최대치 상태에서 +5는 클램프 조기 반환으로 이벤트조차 발화하지 않아 "미적용"으로 위장, 의도 아이콘도 Buff로 오표시. 델타 부호 규약: 음수 = 압박, 양수 = 회복 (Tooltip 명시 권장)
- **임시 조치 기록:** **PartyData 이중 참조** — DungeonManager(던전 수위 판정: 임계값·시작 SAN) / BattleManager(전투 홀더 생성). SO 불변 데이터라 이중 참조 자체는 원칙 위반 아님, 단 **두 인스펙터는 동일 에셋 필수** (다르면 광기 판정 어긋남). **P2-M4 편성 화면 도입 시 DungeonState 경유 주입으로 일원화** → ✅ 이행 완료 (개정 14 — 4-1에서 조기 일원화)
- **씬 검증:** SAN 전투 간 이월 확인 완료. 침식·광기 통행·휴식 회복 검증은 리소스 연결 시점 이월

#### 1-4 완료 기록 (2026-07-24)

- **산출물:** `Data/DungeonEventData.cs` (`DungeonEventChoice` — 문구 + SanityDelta 골격, 표시명/설명은 SWIdentifiedObject 필드 재사용, 선택지 1~3 OnValidate 검증) + `View/UI/NodeScreenView.cs` (제목+설명+선택지 3슬롯 고정 배치 — IntentView 패턴, 콜백 주입 — 뷰는 노드 타입을 모름) + `DungeonManager` 노드 화면 통합 (`EDungeonPhase.Node` 추가, `ShowNodeScreen` 헬퍼)
- **노드 화면 통합 결정:** 노드 타입별 뷰 분리(EventNodeView/StorageNodeView) **기각** — 휴식/이벤트/보관 골격은 표현이 동일("제목+설명+선택지"). MapRoomView 단일화와 동일 논리 + 1-5 원칙(실요구 전 구조 확장 금지) 적용. **전용 뷰 신설 기준:** ① 보관 실 UI(다중 선택 목록·전송 개수 — P2-M6) ② 상점(구매 격자 — P2-M7) 등 선택지형이 아닌 표현이 실제 등장할 때
- **데이터 구성:** 휴식 = 고정 에셋 (`restEventData` — "의도적 광기 진입" 선택지가 데이터로 성립, 코드 0줄) / 보관 = 고정 에셋 (`storageEventData` — 통과 문구 1선택지, P2-M6에서 전용 화면 교체) / 이벤트 = `eventDatas` 풀 무작위 (임시 조치 — 던전 구성 데이터로 대체 예정)
- **노드 이벤트 필드 정리 예정 (사용자 제안 채택 — 개정 11):** rest/storage/eventDatas 3필드 → **`타입(EMapNodeType) → 이벤트 풀` 매핑 목록**으로 통합 (풀 1개 = 고정, 복수 = 무작위 — 휴식 변형도 데이터로 성립). `HandleNodeEntry`의 switch는 전투 라우팅 때문에 유지 — 매핑은 인스펙터 정리 목적. **이행 시점: P2-M7 7-4 던전 구성 데이터(챕터 SO) 신설 시 그 필드 구조로 흡수** (Storage는 P2-M6 전용 화면 도입으로 매핑 이탈 — 지금 이행하면 이중 재작업). OnValidate: 중복 타입·전투 계열 타입 금지
- **수치 소유 이동:** 휴식 회복량 `MapConfigData.RestSanityRecovery` 제거 → 휴식 이벤트 선택지 데이터가 소유 (수치 = 데이터 소유 원칙)
- **통과 처리 방어:** 뷰 또는 데이터 미배선 시 노드 통과 처리 — 미배선 씬에서도 런 루프 완주 가능 (모듈 단독 테스트 원칙 유지)
- **표시 방식:** 노드 화면 = 맵 위 오버레이 (맵 숨김 없음), `Node` 상태가 `MoveToNode` 차단 + `IsDungeonRunning` 포함 (노드 화면 중 던전 재시작 방지). 화면 = enum 상태 유지 (SWStackStateMachine 재채택 기준 미충족)
- **씬 배선 확인:** Dungeon.unity — `nodeScreenView`·`restEventData`·`storageEventData` 연결 확인. ⚠ 씬의 NodeScreenView 오브젝트 비활성 저장 여부 확인 필요 — 규칙: **컴포넌트 루트 = 활성, panelRoot 자식만 비활성** (루트 비활성 시 Awake 미실행·화면 미표시)
- **실플레이 검증 이월:** 휴식/이벤트/보관 진입·선택·복귀 및 M1 DoD 런 완주는 리소스 연결 시점 일괄 검증

### P2-M2 — 런 중 저장 (1주) ✅ 완료 (개정 12 — 저장/복원 실검증은 씬 검증 이월분과 일괄)

| # | 산출물 | 책임 |
|---|--------|------|
| 2-1 | 저장 스키마 | **버전 필드 + 마이그레이션 계층 처음부터 포함** (기획서 15-5) — DungeonState 직렬화 + 메타(해금 상태) 분리 |
| 2-2 | 저장/복원 | `SWSaveDataManager` 활용 — 던전 스냅샷 (노드 진입 시점 저장 권장) |
| 2-3 | P2-D1 이행 | 시드 결정성 범위 확정 반영 (아래 조기 결정) |

**DoD:** 런 중 종료 → 재시작 → 같은 맵·같은 덱·같은 위치로 복원.

#### P2-M2 완료 기록 (2026-07-24)

- **P2-D1 이행 (스냅샷):** 무작위 소비 지점이 이미 다수(조우 무작위·이벤트 추첨·광기 이벤트 판정 3회·셔플·침식) — "시드+행동 로그" 방식은 모든 소비 순서를 영구 계약으로 만들어 코드 수정마다 재현이 깨짐. **재개 후 난수 비연속** (`TickCount` 재시드 — 같은 시드 재설정 시 소비된 난수열 재등장 = 세이브스커밍 여지). 시드는 생성 기록·재현용으로만 저장
- **산출물:** `Save/DungeonSaveData.cs` (version 필드 + 카드 `{codeName, isUpgrade}` — **강화가 던전 지속 상태라 codeName만으론 유실**) + `Save/DungeonSaveService.cs` (SWSaveDataManager 슬롯 `"dungeon"`, `Migrate` 계층 — v1 초판·미래 버전 거부·버전별 순차 변환 자리) + `MapGraph.RestoreFrom` (AddNode/AddEdge 재사용으로 인덱스·입구·층수 재구축, 보스 노드 타입 스캔 검증) + `DungeonState.IsCurrentNodeResolved`/`RestoreProgress` + `DungeonManager` 저장 호출·`ResumeDungeon`(SWButton "던전 이어하기")·`cardDatabase` 참조(codeName 복원)
- **저장 대상 = 가변 런 상태만:** 맵(노드+간선 — 방문/잠식 포함)·덱·SAN·이동/침식 카운터·현재 노드. 정적 구성(조우 풀·정신력 이벤트 풀·노드 이벤트·밸런스)은 인스펙터 재주입 — 저장 제외
- **저장 시점 계약:** ① 던전 시작 직후 ② 노드 진입 확정 직후·처리 직전 (`isResolved = false`) ③ 진입 처리 완료 지점 — 전투 승리 복귀·노드 화면 완료·통과 (`isResolved = true`). **복원 = 맵 상태 + 미해결 노드면 `HandleNodeEntry` 재실행 → 노드 스킵 불가 보장** (전투 재시작·노드 화면 재표시·이벤트 재추첨은 잠정 허용)
- **던전 종료 = 스냅샷 삭제:** 회수/해금 반영은 메타 저장 소관 (P2-M6/M7). **주의: SWSaveDataManager는 정적 단일 currentData — 던전/메타 슬롯 병용 시 SetData 순서 규약 필요 (메타 저장 도입 시 결정)**
- **복원 경고 노트:** 복원은 빈 덱으로 생성 후 `AddCard` — DungeonState 생성자의 "시작 덱 비어 있음" 경고 1회는 무해
- **프로젝트 반영 확인 (개정 12):** Save 모듈(`DungeonSaveData`/`DungeonSaveService`)·`DungeonState` 복원 경로(`SetMapGraph` 시 이동/침식 카운터 리셋 포함)·`MapGraph.RestoreFrom`·`DungeonManager` 저장 배선 전부 반영 확인. SWSaveDataManager 네임스페이스 = `SW.Data` 확정 (컴파일 통과)
- **실검증 이월 ⚠ (씬 검증 이월분과 일괄):** `cardDatabase` 인스펙터 연결 (전 카드 등록·codeName 유일 — SWIODatabase 중복 검사 활용) → 검증: 던전 시작 → 이동/전투/노드 화면 → 플레이 중지 → "던전 이어하기" → 같은 맵(방문/잠식 포함)·같은 덱(강화 포함)·같은 위치·SAN·침식 카운터 복원 + 미해결 노드(전투 중 종료) 재진입 확인 + 저장 버전 조작 시 마이그레이션 거부 확인

### P2-M3 — 제작 도구 + 상태이상 구조 (1주) ✅ 완료 (개정 13 — 취약 실검증은 씬 검증 이월분과 일괄)

| # | 산출물 | 책임 |
|---|--------|------|
| 3-1 | `CardSystemWindow` | SWStatSystemWindow 패턴 복제 — 카드 열람/검색/생성 에디터 (콘텐츠 50장 제작의 도구 선행) → **신설 기각, 기존 `EchoesOfAshDataWindow` 증축으로 대체 (완료 기록 참조)** |
| 3-2 | 상태이상 모듈 구조 | Study-ModuleSkill의 Effect 생명주기를 턴제로 번안 — 상태이상 정의 SO(수치·아이콘·설명) + 틱 로직 매핑 (15-4 하이브리드 구조). `IStatusReceiver` 실이행 |
| 3-3 | 검증 상태이상 1종 | 취약(받는 피해 +50%, N턴) — 파이프라인 관통 확인용 |

**DoD:** 에디터에서 카드 생성 가능. 상태이상 1종이 부여 → 틱 → 만료까지 전투에서 동작.

#### P2-M3 완료 기록 (2026-07-24)

- **3-1 `CardSystemWindow` 신설 기각 → 기존 창 증축으로 마감:** 카드 열람/검색/생성/복제/삭제/자동 ID는 `EchoesOfAshDataWindow`가 기충족 (창 중복 방지 — SWStackStateMachine 철회와 같은 "돌아가는 구조 복잡화 금지" 원칙). **상태이상 탭 증축**으로 제작 도구 완비 — `ManagedTypes`/`ManagedTabNames`/`DefaultPaths`/`DefaultPrefixes` 배열 5종 인덱스 정합 (`StatusEffectData`, "상태이상", `Assets/02_Res/Data/Status`, `Status_`). EditorPrefs 잔존 설정은 "기본값 복원" 1회로 정리
- **3-2 하이브리드 구조 (기획서 15-4):** `Data/StatusEffectData` (정의 SO — 감소 규칙·배율 수치 소유, `statusEffectType` enum = 로직 매핑 키, `EStatusDecayType` 신설: 지속/라운드마다 1 감소) + `Battle/StatusController` (순수 클래스 — 부여/조회/라운드 감소/만료 단일 소유, **부여 순 순회 고정** = 발화 순서 결정성, `OnStatusChanged` 이벤트, 미정의 유형 = 경고 + 카운트다운 폴백) + `BattleEntity` 위임 (`IStatusReceiver` 실이행 — SanityHolder 위임 전례, `SetStatusDatas` 주입·`ResetEntity`에 `ResetAll` 통합)
- **중첩 = 남은 라운드 수 (STS 방식):** 취약 2 = 2라운드 — `IStatusReceiver` 시그니처·기존 `StatusEffect` 효과 블록 **무수정**으로 성립. 배율은 중첩 수와 무관하게 활성 여부로만 적용
- **모듈 매핑 이탈 (별도 `05_Scripts/Status/` 기각):** 정의 SO = **Data 흡수** (콘텐츠 SO 일관성 — 데이터 윈도우 관리 타입 = Data 네임스페이스), 생명주기 = **Battle 흡수** (전투 1회 수명 순수 클래스 계열 — Sanity와 달리 던전 지속 상태 아님). **재분리 기준:** 상태이상이 던전 지속 상태가 되거나(저장 편입) 틱 전용 효과 블록·뷰 계층 증축으로 파일이 불어날 때
- **틱 시점 = `OnRoundEnded`** ("턴 경계 = 라운드 종료" 매핑 유지). 순회 = 파티 → 적 (스폰 순 고정, 사망자 스킵). **구독 순서 계약: 상태이상 감소(`HandleStatusRoundTick`) → 의도 재평가(`HandleRoundEnded` — PrepareNextTurn)** — P2-M5 도발(지속 턴 = 이 구조 재사용)이 만료 반영 후 대상 선정을 보도록 선행 고정. ⚠ 초기 반영분은 역순이었음 — 구독 두 줄 순서 교환으로 수정 → ✅ 수정 확인 (개정 14)
- **3-3 취약:** `Battle/StatusDamageCalculator` — 기반 계산기(`DefaultDamageCalculator`) 래핑, 대상 활성 상태이상 배율 곱, **방어막 이전 적용·소수점 버림** (STS 방식 — `TakeDamage`의 `Calculate`가 방어막 차감보다 선행). 배율 1.5는 SO 소유 (수치 = 데이터 소유 원칙). `SetDamageCalculator` 기존 확장 지점 활용 — 파티/적 스폰 직후 조립 지점 주입
- **임시 조치:** `BattleManager.statusDatas` 인스펙터 목록 (이 전투에서 유효한 상태이상 정의) — **P2-M7 7-4 던전 구성 데이터(챕터 SO) 신설 시 이동**
- **프로젝트 반영 확인 (개정 13):** 신규 3파일(`Data/StatusEffectData`·`Battle/StatusController`·`Battle/StatusDamageCalculator`)·enum 2종·`BattleEntity` 위임·`BattleManager` 배선(주입 2곳 + 틱 핸들러 + `ResetBattle` 해제 대칭)·데이터 윈도우 배열 5종 전부 반영 확인
- **실검증 이월 ⚠ (씬 검증 이월분과 일괄):** `Status_Vulnerable` 에셋(취약·디버프·카운트다운·배율 1.5) 제작 → `BattleManager.statusDatas` 등록 → 테스트 카드 또는 적 행동에 `StatusEffect` 블록(취약 2)으로 부여 → 검증: 부여(중첩 2) → 피해 7 = 10 (방어막 이전 배율) → 라운드마다 2→1→0 감소 → 만료 후 배율 미적용 + `OnStatusChanged(취약, 0)` 발화 + 전투 재시작 시 잔존 없음 + 정의 미등록 폴백 경고

---

> **◆ 밸런스 게이트 (리소스 작업과 병행):** 6-2~6-5 이행 — 이벤트 5종·카드 15장·적 3종·조우 3종 제작(구성안 확정본) + 씬 검증 이월분(5-1 잔여·5-3~5-6) + 밸런스 튜닝 + 재미 확인. **이 시점 이후 P2-M4~M7의 콘텐츠 수치가 신뢰 가능해진다.**

---

### P2-M4 — 파티 시스템 (1.5주) ✅ 완료 (개정 17 — 실검증은 씬 검증 이월분과 일괄)

| # | 산출물 | 책임 |
|---|--------|------|
| 4-1 | 파티 3인 전투 | `CharacterEntity` 복수 스폰 (`characterData` → 목록), 공유 덱·공유 턴·공유 SAN 유지 (구조는 이미 공유 설계 — 확장만) → ✅ 완료 (개정 14 — 완료 기록 참조) |
| 4-2 | 전용 카드 소속 | 카드 ↔ 소유 캐릭터 연결 (P2-D3 결정) — **전투불능 시 전용 카드 드로우 풀 제외** (`DeckSystem` 필터) → ✅ 완료 (개정 15 — 완료 기록 참조) |
| 4-3 | 캐릭터 패시브 기초 | 검사 1인 + 테스트 캐릭터 — 패시브는 유물 트리거 구조 재사용 (P2-D4와 통합 설계) → ✅ 완료 (개정 16 — 완료 기록 참조) |
| 4-4 | 뷰 확장 | `PartyStatusView` 3인 배치, 적 대상 선정 표시(누굴 노리는지), 파티 편성 화면 기초 (Canvas) + **아군 지정 UI 연결** (4-1 이월분) → ✅ 완료 (개정 17 — 완료 기록 참조) |

**DoD:** 3인 파티로 전투 1사이클 — 1인 전투불능 시 전용 카드가 드로우에서 제외되고, 부활 규칙 자리만 확보.

#### 4-1 완료 기록 (2026-07-24)

- **파티 구성 = `DungeonState` 소유:** 생성자 확장 `(seed, partyData, characterDatas, startingCards, sanityEventDatas)` + `PartyData`/`CharacterDatas` 프로퍼티. **PartyData 이중 참조 일원화 이행** (1-6 임시 조치 — 계획보다 조기 해소): 단일 원본 = DungeonManager 인스펙터 → DungeonState 경유 주입, BattleManager 데이터 필드 2종(`partyData`/`characterData`) 제거 = **Phase 1 임시 조치 전량 해소 (Aggro 폴백 1건 제외 — P2-M5)**
- **파티 구성 = 저장 제외 유지 (잠정):** 인스펙터 재주입되는 정적 구성 원칙 준수 — 스키마 v1 무수정. **4-4 편성 화면에서 구성이 가변화되는 시점에 스키마 v2 + 마이그레이션으로 편입**
- **복수 스폰:** `SetupParty` 목록 순회 (스폰 순서 = 목록 순서 — 발화 순서 결정성), 개체별 상태이상 정의·피해 계산기 주입, `ValidateData` 파티 1~3인 검증. 패배 판정 = **전원 사망** (`HandleCharacterDied` 생존자 검사)
- **잠정 규칙 3종:** ① 공용 카드 시전자 = **파티 첫 생존자** (`GetDefaultCaster` — 4-2에서 전용 카드 = 소유자 시전으로 확장) ② Self 타겟팅 = **지정 아군(생존) 우선 · 시전자 폴백** (`TargetResolver.ResolveSelf(caster, target, results)` — 사망/미지정 시 폴백) ③ 광기 이벤트 대상 = **판정 시점의 파티 첫 생존자** (`MadnessEventRunner` 고정 시전자 폐기 — 사망자 대상 효과 버그 예방)
- **의존 방향 보호:** MadnessEventRunner 파라미터 = `IReadOnlyList<ITargetable>` — `List<CharacterEntity>`의 공변 전달로 Sanity 모듈이 Battle을 참조하지 않음 (Interface만 의존)
- **아군 지정 UI = 4-4 이월:** 현재 파티원은 뷰/콜라이더 없음 → 드래그 경로의 Self 카드 = 시전자 폴백 동작 (정상). 로직·전달 경로(`PlayCard` target)는 완성 — BattleTest OnGUI 아군 선택 버튼으로 검증. **4-4 권장안: `CharacterView` 신설** (EnemyView 대칭 — 월드 콜라이더 보유 → 기존 드롭 판정 경로(`ScreenToWorldPoint`+콜라이더) 재사용, CardDragController 무수정에 근접). PartyStatusView 슬롯 드롭 대상화는 UI 레이캐스트 경로 신설이 필요해 차선 — 확정은 4-4 착수 시
- **PartyStatusView 1인 잠정:** `party[0]` 연결 — 3인 배치는 4-4
- **BattleTest 3인 검증 UI:** 파티 상태 루프(HP/방어막/생존) + 아군 선택 버튼(Self 카드 대상) + 시전자/폴백 경로 검증 가능. 사소 노트: Self 분기 주석 "null 전달"은 실제로는 사망 엔티티 전달(IsTargetable 검사로 동일 폴백) — 주석 정정 권장
- **프로젝트 반영 확인 (개정 14):** DungeonState·DungeonManager(characterDatas 필드 + DungeonState 생성 2곳)·BattleManager(스폰/검증/시전자/패배/정리/배선)·TargetResolver·MadnessEventRunner·BattleTest 전부 반영 확인. M3 구독 순서 수정분 반영 확인
- **실검증:** 아군 방어막 부여(지정 아군 적용) 확인 완료. **4-1 검증 절차 잔여(시전자 폴백·전원 사망 패배·광기 이벤트 대상 폴백·던전 경로 3인·인스펙터 잔여 오버라이드 정리)는 리소스 작업 시점 씬 검증 이월분과 일괄**

#### 4-2 완료 기록 (2026-07-25)

- **P2-D3 이행:** `CharacterData.exclusiveCards` 신설 (OnValidate: null 경고·중복 에러) + `CardData` 소유자 참조 필드 2종(`isCharacterCard`/`ownerCharacter`) 제거 — D3 기각안(카드→캐릭터 참조)의 잔재 정리. 기존 카드 에셋의 직렬화 잔존 값은 무해 (다음 저장 시 자연 소거)
- **드로우 제외 = 제외 더미(exclusionPile) 물리 이동 + 판정 주입:** `DeckSystem.SetDrawExclusion(Func<CardInstance, bool>)` / `RefreshDrawExclusion` — 제외 카드를 덱·버림에 남겨두고 건너뛰는 방식은 "덱에 제외 카드만 잔존" 시 무한 재셔플 위험이 있어 별도 더미로 격리 (원리상 차단). **판정 주입으로 Deck → Battle 의존 차단** (MadnessEventRunner 인터페이스 의존 전례와 동일 원칙)
- **양방향 갱신 = 부활 자리 확보:** `RefreshDrawExclusion`이 제외 해제된 카드를 버림 더미로 복귀 — 부활 로직 도입 시 이 호출 하나로 성립 (DoD "부활 규칙 자리만 확보" 충족)
- **Draw 시점 안전망:** 인출 카드가 제외 대상이면 제외 더미로 이동 + 드로우 미소모(`i--` 재시도) — 사망 갱신 이후 손패에서 버려진 전용 카드가 버림 더미를 거쳐 재드로우되는 경로 차단. 제외 카드는 덱에서 영구 이탈하므로 무한 루프 없음
- **소유 조회 표:** `BattleManager.cardOwnerLookup` — 전투 시작 시 1회 구성 (D3 확정 기록의 역조회 방식 이행). **원본 + 강화 버전 CardData 동시 등록** (판정 키 = `CardInstance.CardData` 원본 — 강화 카드 소유 판정 유실 방지), 캐릭터 간 중복 등록 = 에러 로그. `ResetBattle`에서 Clear (수명 = 전투 1회)
- **시전자 확장:** `GetCasterFor` — 전용 카드 = 소유자 시전 (4-1 잠정 규칙 ①의 예고된 확장 이행), 공용 카드·소유자 전투불능 시 = 첫 생존자 폴백
- **잠정 규칙: 손패의 전용 카드는 제외 대상 아님** — 기획서 4-4 문언 "드로우 풀에서 제외" 준수. 소유자 사망 후에도 손패 카드는 사용 가능 (시전자 = 첫 생존자 폴백), 버려진 뒤에는 Draw 안전망이 재드로우 차단
- **배선 순서:** `SetupSystems`에서 deckSystem 생성 직후 조회 표 구성 + 판정 주입 (SetupParty 후 = 파티 확정 상태). `HandleCharacterDied` 첫 줄 `RefreshDrawExclusion` (패배 판정 선행 — 패배여도 무해, `deckSystem?.` 가드)
- **저장 무관:** 제외 더미 수명 = DeckSystem 수명 = 전투 1회. CardInstance는 DungeonState.Deck과 공유되므로 전투 종료 후 별도 복원 불필요 — 대신 `ResetDeckSystem`의 AP 보정 초기화에 제외 더미 포함 (필수)
- **프로젝트 반영 확인 (개정 15):** `CharacterData`·`CardData`·`DeckSystem`(필드·`ExclusionPileCount`·제외 region·Draw 안전망·리셋)·`BattleManager`(조회 표 3메서드·SetupSystems 배선·PlayCard·HandleCharacterDied·ResetBattle) + `Character_Test 1` 전용 카드 등록(`Card_Test_Character`) 전부 반영 확인
- **실검증 이월 ⚠ (씬 검증 이월분과 일괄):** 소유자 생존 시 전용 카드 시전자 = 소유자 → 소유자 처치 시 덱/버림의 전용 카드 제외 카운터 이동·재드로우 미등장 → 손패 잔존 카드 폴백 시전 + 버림 후 재드로우 차단 → 재전투 시 제외 잔존 없음 → 중복 등록 에러 로그
- **사소 권장:** `ExclusiveCards` 프로퍼티 summary 주석·`exclusiveCards` Tooltip 추가 (스타일 일관성 — 7-1 콘텐츠 제작 전 권장) → ✅ 반영 확인 (개정 16)

#### 4-3 완료 기록 (2026-07-25)

- **P2-D4 확정 이행 — 훅 열거 매핑** (결정 근거는 조기 결정 표 참조). 패시브·유물 공용 구조의 최소 단위 완성
- **enum 중복 신설 방지 (사용자 발견):** 초안은 `ETriggerType` 신설이었으나 기존 `ERelicTriggerType`이 이미 존재 — 미사용 확인 후 **재사용 + `ETriggerType` 개명** (패시브 공용화로 "Relic" 이름 범위 불일치 — Run→Dungeon 개명 전례) + **`Passive` 값 제거** (디스패처에 발화 지점이 없는 함정값 — 상시 배율형은 피해 계산기 래핑으로 확정) + **`BattleEnd` 유지** (기획서 8-1 재검토 항목 — 미배선 경고로 자리 보존). `ESanityCondition` 신설 (None/CalmOnly/MadnessOnly)
- **산출물:** `Effect/Trigger/TriggerEffect.cs` (인라인 직렬화 — 시점 + 정신력 조건 + 효과 블록 + `GetDescription` 조합) + `Effect/Trigger/TriggerEffectController.cs` (순수 클래스 — 등록·발화 단일 소유) + `CharacterData.passives` 목록 + `BattleManager` 배선. **배치 조정 (사용자):** 계획의 Data/Battle 분산 대신 **Effect 모듈 하위 `Effect/Trigger/` 통합** (네임스페이스 `EchoesOfAsh.Effect.Trigger`) — 트리거 구조가 효과 실행 계열임을 반영
- **결정성 계약:** 등록 순서 = 발화 순서 (단일 목록 순회 — StatusController 부여 순 전례). 파티 패시브 = 스폰 순 등록, P2-M7 유물 = 획득 순 등록으로 기획서 15-2 충족. 발화 시점 검사: 소유자 사망 스킵 + 정신력 조건 + 빈 블록 등록 거부 + 미배선 3종(피격/가해/전투종료) 경고
- **BattleManager 배선:** **구독 순서 계약 — 방어막 리셋(`HandleTurnStarted`) → 패시브 트리거** (리셋이 패시브 방어막을 지우지 않도록 선행 고정, M3 "감소 → 의도 재평가" 계열). `BattleStart` 발화 = `OnBattleStarted` 후 · `turnManager.StartBattle()` 전 (뷰 초기화 후 발동 → 게이지 즉시 반영). 검사 패시브 = 데이터 성립(코드 0줄). **의존 방향 노트: Effect.Trigger ↔ Battle·Card 상호 참조 — P2-M7 유물 등록 확장 시 ITargetable 전환 + 카드 파라미터 제거로 해소 예정.** 실검증은 씬 검증 이월분과 일괄

#### 4-4 완료 기록 (2026-07-26)

> **뷰 플레이스홀더 전제:** 이 항목의 뷰 산출물 전부 테스트용 임시 세팅 — 리소스 작업 시점에 표시물은 자유 교체된다. 교체 시 유지할 계약은 겉모습이 아니라 **연결 규칙(Init/Release 쌍 · 이벤트 구독+표시만 · null 허용)**뿐.

- **4-4a 전투 뷰 확장:**
  - `View/CharacterView` 신설 (4-1 권장안 이행) — EnemyView 대칭: 월드 콜라이더(신규 물리 레이어 `Character`) + 전투불능 회색 표시. 파티원 엔티티 자식으로 부착 (위치 자동 추종). HP/개별 게이지는 미보유 — 상태창 슬롯 소관 (역할 분리)
  - **아군 드롭:** `CardDragController.TryPlayOnAlly` (Self 타입 한정 + `characterLayerMask`) — 실패 시 기준선 폴백 = 기존 동작 보존. 기존 드롭 판정 경로(`OverlapPoint`) 재사용으로 CardDragController 구조 무수정에 근접 → **개정 18에서 Self 조준 UX로 개정 (P2-M5 완료 기록 참조 — 기준선 폴백 폐지)**
  - **상태창 3인:** `PartyCharacterSlotView` 신설 (1인 표시를 슬롯 부품으로 분리 — 이름·HP·방어막·전투불능 마크) + `PartyStatusView` 슬롯 3개 고정 배치 개편 (IntentView 슬롯 전례). 공유 SAN은 상태창 유지
  - **적 대상 예고 = 의도 시점 확정:** `EnemyAI` 파티 생성자 주입 + `PickNextTarget` (공격·SAN 압박 행동만 예고, `OnTargetChanged`) + `SelectTargets` 시그니처 축소 (파라미터 파티 제거 — 주입 필드 사용). 예고 대상 생존 시 **예고 = 실행 일치**, 전투불능 시 실행 시점 조용한 재선정 (표시 미갱신 — 방어 행동 오표시 방지). `EnemyView` 대상 이름(`targetText`) 증축 + 사망 시 숨김. **난수 소비 시점 이동 노트:** 공격류 대상 추첨 = 예고 시 — 스냅샷 저장(P2-D1)이라 재현 계약 무관, 순회 순서 고정으로 결정성 유지
  - ⚠ **수정 이력 (검토 발견 2건):** ① `PrepareNextTurn`의 `PickNextTarget` 누락 — 2라운드부터 표적 고정 + 예고 대상 우선 사용으로 **무작위 규칙이 사실상 고정 대상화**되는 로직 버그 → 수정 확인 ② 사망 적의 표적 글자 잔존 → 숨김 추가 확인
- **4-4b 파티 편성 기초:**
  - `View/UI/PartySetupView` 신설 — 후보 슬롯 고정 배치(토글 선택), **선택 순서 = 파티 순서** (스폰 순서 결정성), 1~3인 검증, 캐릭터 정보 = **패시브 설명 `TriggerEffect.GetDescription` 재사용 (코드 0줄)** + 전용 카드 목록 + 시작 덱 미리보기. 콜백 주입 — 뷰는 DungeonManager를 모름
  - `DungeonManager` 편성 흐름: `OpenPartySetup` → 확정(`selectedParty`) → `StartDungeon` (편성분 우선, 없으면 인스펙터 기본 파티). **미배선 = 기본 파티 즉시 시작 폴백** (미배선 통과 원칙). `EndDungeon`에서 편성분 정리. 임시 조치: `availableCharacters` 인스펙터 목록 — 메타 저장(보유 캐릭터) 도입 시 대체 → ✅ 해소 (개정 20 — 마을 보유 명단 기반)
  - **저장 스키마 v2 이행 (4-1 예고 결정):** 편성 가변화 → `partyCharacterCodeNames` 저장 + `characterDatabase` 코드명 복원 + **v1→v2 마이그레이션 (구버전 = 빈 명단 → 인스펙터 기본 파티 폴백 + 경고) — 버전 체계 첫 실사용.** ⚠ **수정 이력 (검토 발견 2건):** ① `CurrentVersion` 미인상(1 잔존 — v1/v2 구분 불가·마이그레이션 사문화) → 2로 수정 확인 ② `Migrate` 흐름 — 변환 성공 후에도 무조건 실패 반환 → "변환 후 현재 버전 도달 검사" 구조로 수정 확인
- **프리팹 현황:** `PartyStatusView` 슬롯 3개 배치 완료 / `CharacterView`·레이어·드래그 마스크 = 에디터 체크리스트 / `PartySetupView` 프리팹 = 빈 골격 (**폴백 동작으로 후순위 가능** — 구조 가이드 전달됨, 규칙: 루트 활성·panelRoot 자식만 비활성)
- **잔여 ⚠ (다음 코드 작업 시 반영):** `SetupParty` 파티원 간격 배치 (`partySpacing` — 현재 전원 (0,0) 겹침: 뷰 문제가 아니라 **아군 드롭 판정이 최상단 1인에만 가는 로직 문제**) → **간격 방식 기각 (개정 18) — 인원별(1/2/3인) 배치 프리팹 방식으로 대체, `SetupParty` 적용 이월.** → ✅ 적용 완료 (개정 19 — `PartyFormation`) 사소: `PartyCharacterSlotView` 로그 태그·`PartyStatusView.Init` 문서 주석 파라미터명 정리
- **실검증 이월 ⚠ (씬 검증 이월분과 일괄):** 편성 선택/토글/순서 → 3인 전투 상태창·전투불능 마크 → 아군 드롭 지정/기준선 폴백 → 적 표적 표시(공격만·매 라운드 갱신·사망 재선정) → v2 저장 파티 복원 + v1 폴백 경고

### P2-M5 — 타겟팅 완성 (1주) ✅ 완료 (개정 18 — 실검증은 씬 검증 이월분과 일괄)

| # | 산출물 | 책임 |
|---|--------|------|
| 5-1 | 어그로 시스템 | 피해 기여 기반 어그로 수치 (산정식 = 밸런스 영역, Balance 외부화) — `Aggro` 무작위 폴백 대체 → ✅ 완료 (개정 18 — 완료 기록 참조) |
| 5-2 | 도발·지정 효과 블록 | 적 대상 선정 개입 카드 — 지속 턴 수는 상태이상 구조(P2-M3) 재사용 → ✅ 완료 (개정 18 — 완료 기록 참조, 도발 카드 = 코드 0줄) |

**DoD:** 규칙 3종(랜덤/어그로/지정) 전부 실동작 + 도발 카드로 대상 조작 확인. → 코드 완료, 실동작 확인은 씬 검증 이월분과 일괄.

#### P2-M5 완료 기록 (2026-07-27)

- **P2-D5 확정 이행 — 어그로 산정식 (잠정, 전부 Balance 외부화):** 어그로 = 카드로 입힌 **원본 피해량 × `aggroDamageWeight`(기본 1)**, 라운드 종료마다 **`aggroRoundDecayRate`(기본 0.5) 곱 감쇠** — 최근 기여 가중. 방어막 흡수분 포함 기여 인정·취약 배율 미반영 (원본 기준 — 잠정). 동률 = 파티 순서 앞 사람 (결정성), 유효 어그로 없음(첫 라운드·시스템 미주입) = **무작위 폴백** (STS 감각·BattleTest 단독 경로 무손상)
- **5-1 산출물:** `Battle/AggroSystem.cs` 순수 클래스 (전투 1회 수명 — ApSystem 계열). **귀속 구간 방식**: `BeginAttribution(caster)` → 카드 실행 → `EndAttribution()` (`PlayCard` try/finally 래핑) — 적 `OnDamaged` 구독으로 누적, 귀속 구간 밖 피해(상태이상 틱 등)는 미반영. `MIN_AGGRO` 미만 = 만료 제거. `EnemyAI` 생성자 주입 (null 허용 → 무작위 폴백 유지)
- **5-2 도발:** `EStatusEffectType.Taunt` 신설 (기존 값 뒤 추가 — 직렬화 안전). 지속 = **상태이상 구조 재사용** (중첩 = 남은 라운드·TurnCountdown — P2-M3 예고 이행). 도발 카드 = 기존 `StatusEffect` 블록 + Self 타겟팅 — **코드 0줄** (15-3 원칙 관통). 도발 = 모든 대상 선정 규칙에 우선, 다수 도발자 = 파티 순서 (결정성), **실행 시점 강제** + `RefreshTauntPreview`(부여/해제 시 예고 표시 즉시 갱신 — 난수 소비 0). 만료 복구 = 기존 순서 계약(감소 → 재평가)이 자연 처리 — 추가 코드 없음
- **순서 계약 확장:** 상태이상 감소 → **어그로 감쇠(`aggroSystem.TickRound` — `HandleStatusRoundTick` 말미)** → 의도 재평가(`HandleRoundEnded`). 해제 대칭: `ResetBattle`에서 `Release()`(적 구독 전량 해제) + null
- **Self 드래그 지정 UX 개정 (사용자 발견):** 기존 Self 카드 = 비조준 취급 → 아군 콜라이더를 빗나가면 기준선 폴백으로 **대상 선택 없이 시전자에게 적용**되는 문제. `CardDragController` 개정 — `isSingleTarget` → **`isAimedTargeting`** (Single + Self = 화살표 조준, STS 표준 UX 통일), 드롭 판정 = 타겟팅 방식별 분기 (**Self = 아군 콜라이더 위에서만 성립·빗나감 = 취소** — Single 대칭), 기준선 판정 = 비대상 카드 전용으로 축소. `TryPlayOnAlly`·로직 계층(`ResolveSelf` 시전자 폴백) 무수정. 트레이드오프: "위로 던져 빠른 자기 시전" 편의 제거 — 자기 시전 = 자기 캐릭터 위 드롭
- **파티 배치 결정 (사용자):** 개정 17 잔여의 `partySpacing` 간격 배치 **기각** → **편성 인원별(1인/2인/3인) 배치 프리팹 방식** 확정 — 배치 프리팹 등록 완료, `SetupParty` 배치 적용 = **이월 ⚠** (적용 전까지 파티 (0,0) 겹침 지속 → **아군 드롭 판정이 최상단 1인에만 감** — Self 드래그 실검증은 배치 적용 후) → ✅ 적용 완료 (개정 19 — `PartyFormation`)
- **프로젝트 반영 확인 (개정 18):** `AggroSystem`·`GameEnum`(Taunt)·`BattleBalanceData`(어그로 그룹)·`EnemyAI`(도발 우선·어그로·RefreshTauntPreview)·`BattleManager`(귀속·감쇠·해제 대칭)·`CardDragController`(조준 확장·드롭 분기) 전부 반영 확인
- **사소 잔여 (다음 코드 작업 시):** `CardDragController` 미사용 `isSingleTarget` 필드 제거 / `GameEnum` 중독 주석 들여쓰기 정리 / `EnemyAI` 생성자 문서 주석에 `party`·`aggroSystem` 파라미터 설명 보강
- **에디터 작업 체크리스트:** ① `Status_Taunt` 에셋 (도발·isDebuff = false·라운드마다 1 감소·배율 1) ② `BattleManager.statusDatas` 등록 ③ 도발 테스트 카드 (Self + 상태이상 부여(도발, 2)) ④ 테스트 적 1종 규칙 = 어그로 기반 ⑤ `CharacterView` 콜라이더 = `Character` 레이어 + `characterLayerMask` 체크
- **실검증 이월 ⚠ (씬 검증 이월분과 일괄 — 파티 배치 프리팹 적용 후):** 어그로 적 1라운드 무작위 → 딜 집중 파티원 표적화 → 미공격 시 감쇠로 분산 → 도발 카드 사용 즉시 그 턴 공격 강제 + 표적 글자 교체 → 2라운드 후 만료 복귀 → 지정 고정 = 첫 생존자·사망 시 다음 사람 → Self 카드 화살표 조준·아군별 드롭 적용·빗나감 = 손패 복귀 → 도발자 사망 시 조용한 재선정

### P2-M6 — 마을(Town) + 드랍/회수 (1.5주) ✅ 완료 (개정 20 — 실검증·팝업은 아트 시점 이월)

| # | 산출물 | 책임 |
|---|--------|------|
| 6-1 | 마을 씬 (하이브리드) | 건물 골격 + 막사 캐릭터 영입 기초 → ✅ 완료 (개정 20 — 완료 기록 참조. 팝업 뷰·실검증 = 아트 시점 이월) |
| 6-2 | 드랍 테이블 | 조우·노드별 드랍 데이터 (설계도·시설 재료 포함) → ✅ 완료 (개정 19 — 완료 기록 참조, 가중치 추첨 방식) |
| 6-3 | 회수 시스템 | 절충형 + **보관 노드 전송** (P2-M1 골격에 연결) — 게임 오버 시 회수 실패/보존 구분 → ✅ 완료 (개정 19 — 완료 기록 참조, 보관 선택 UI는 잔여) |

**DoD:** 전투 보상 → 드랍 → 보관 전송 or 소지 → 런 종료 시 회수 판정 → 마을 반영. → 코드 경로 완주, 실검증은 아트 시점 이월분과 일괄.

#### P2-M6 착수 기록 — 6-2·6-3 + 저장 재편 (2026-07-27)

- **파티 배치 프리팹 적용 (P2-M4 이월 최종 해소):** `Battle/PartyFormation` 컴포넌트 (사용자 설계 — 표식 Transform 3개를 에디터 버튼으로 인원수별 좌표 데이터에 굽는 방식) + 완성분: `GetSpawnPosition(인원수, 순번)` 조회 API (미비 = 원점 폴백 + 경고), 좌표 기준 `localPosition` 통일 (루트 이동 내성), **정렬 no-op 버그 수정** (`OrderByDescending` 결과 폐기 → `Sort`), "포메이션 보기" = 저장 좌표를 표식에 역복원 (왕복 편집). `BattleManager.SetupParty` = 인원수별 좌표 스폰 (프리팹 **에셋 직접 참조** — 표식·스프라이트 런타임 미존재, 미배선 = 원점 폴백). **아군 드롭·Self 드래그 실검증 선행 조건 충족**
- **P2-D7 확정 — 통합 저장 (프로필 대비, 사용자 결정):** 결정 과정 기록 — 최초 슬롯 분리안("dungeon"/"hub" 파일 분리) → **사용자 지적: SWSave 슬롯의 본래 의미 = 저장 칸(프로필)** → 프로필 기능 도입 의사 확인 → 통합 전환. **프로필 1칸 = 파일 1개 = `GameSaveData` 루트** (hub 구획 + `hasDungeon` 깃발 + dungeon 구획 — JsonUtility 중첩 null 미보존 대응). 입출력 = `GameSaveService` 단일 진입점 (SetData 재등록 내부 국소화 — 병용 순서 규약 소멸) + `SelectProfile`(프로필 화면 대비), 구획 파사드(`HubSaveService`/`DungeonSaveService`)로 호출부 무수정. 용어 매핑: 메타 = `Hub` → **재개정 (개정 20): 거점 = `Town`**. **마이그레이션 잠정 제거 (사용자 결정)** — 버전 불일치 = 폐기 (15-5 의도적 이탈 — 데이터 보존 시작 시점 계층 복원 필수), 던전 스키마 버전 1 리셋
- **6-2 드랍 테이블 (가중치 추첨 — 사용자 결정):** `Data/ItemData`(타입 = 일반 자원/시설 재료/설계도 — 회수 리스크 판정 키)·`ItemStackData`·`DropEntryData`(가중치 항목)·`DropTableData`(**굴림 횟수 범위 + 꽝 가중치 전부 데이터 소유**, 굴림 로직 = SO 소유)
- **6-3 회수:** 소지 = `DungeonState` (스냅샷 편입·`itemDatabase` 복원). **굴림 시점 = 승리 확정 직후·보스 분기 앞** (`currentEncounterData` 필드 승격 — 진입 대입 → 굴림 후 null → 종료/복원 정리). **보관 전송 = 즉시 거점 귀속 + 저장** (전송 순간 파일 기록 → 이후 사망해도 보존 자연 성립 — 별도 "전송분" 상태 불필요. v1 = 진입 시 전량 자동, **선택 전송 UI = 보관 전용 화면 잔여**). 회수 판정 = `EndDungeon` 첫 줄 `ResolveCarriedItems` (**승리 = 전량 / 패배 = 기본 자원만** — 타입 한 줄 판정) → `DeleteSave` 선행 계약 (소지 목록 생존 상태에서 판정). 침식 패배 경로도 같은 길 = 자동 성립
- **모듈 배치 (매핑 이탈 기록):** `ItemData`·`ItemStackData`·`DropEntryData`·`DropTableData` = **Data 흡수** (데이터 윈도우 관리 타입 — StatusEffectData 전례), 회수 판정 = **DungeonManager 흡수**. `05_Scripts/Drop/` 분리 기준 = 보관 선택 UI·회수 확장 실등장 시 (1-5 실요구 전 구조 확장 금지)
- **검토 발견 수정 6건 (전부 수정 확인):** ① 전송/회수의 `MetaSaveService` 잔존 호출 (Hub 개명 전 원문 — 컴파일 에러) ② 드랍 루프 `ItemStack` 구 타입 잔존 ③ **드랍 굴림이 보스 분기 뒤 = 보스 드랍 영구 누락** (조기 반환) → 승리 직후로 이동 ④ `currentEncounterData` 필드 미선언 → 필드 승격 + 대입/정리 3곳 ⑤ **`HubSaveData` `[Serializable]` 누락** — 거점 구획이 파일에 직렬화되지 않아 전송·회수 자원이 조용히 증발하는 실버그 ⑥ **`CharacterView` 스프라이트 투명 버그 (사용자 발견)** — `Init` 첫 줄 `Release()`가 미저장 `originColor` 기본값 **(0,0,0,0) 투명**으로 칠한 뒤 그 색을 원래 색으로 오염 → `Awake` 1회 저장으로 이전 (전투불능 회색 재초기화 오염도 동시 차단)
- **에디터 작업 체크리스트:** ① `PartyFormation` 프리팹을 BattleManager에 연결 (Test_Battle·Dungeon 2씬) ② 데이터 윈도우 **아이템·드랍 탭 증축** (배열 인덱스 정합 — 상태이상 탭 전례) ③ 테스트 에셋: `Item_Ash`(일반 자원)·`Item_Blueprint_Test`(설계도) + `Drop_Test`(가중치 구성) → 테스트 조우 `dropTable` 연결 ④ `DungeonManager.itemDatabase` 연결 (전 아이템 등록) ⑤ 구버전 저장 파일(dungeon/hub/meta.json) 수동 삭제
- **실검증 이월 ⚠ (씬 검증 이월분과 일괄):** 3인 배치(-1.45/0/1.36) → 아군 드롭·Self 드래그 (P2-M5 이월분 해금) → 전투 승리 드랍 로그·가중치 체감 → 보스 드랍 → 보관 진입 전송 + `save.json` 거점 구획 기록 → 전송 후 사망 = 전송분 보존 → 사망 = 기본 자원만·설계도 소실 → 승리 = 전량 → 이어하기 소지 유지 → 버전 조작 = 폐기 경고

#### 6-1 완료 기록 — 마을 씬 (2026-07-28)

**용어 매핑 재개정 (사용자 결정):**
- **거점 = `Town`** — 개정 19 "메타 = Hub" 재개정. 씬 3구성 확정: **Loading · Town · Dungeon** (P2-D6 재개정). 연쇄 개명: `TownManager`·`TownSaveData`·`TownSaveService`·`TownHUDView`·`GameSaveData.town` 구획 (⚠ 필드 개명으로 구저장 거점 구획 미판독 — 비보존 방침상 무해, 구저장 삭제). 세계관 표시명(AshTown 등)은 코드와 분리 — 리소스 단계 자유 (기획 용어 ↔ 코드 접두어 분리 매핑 방식)
- **시설 = `Building`** (사용자 결정) — `Data/BuildingData`(레벨 목록)/`BuildingLevelData`(승급 비용·효과 설명 — **비용 = 레벨 정의 소유**, 수치 = 데이터 소유 원칙). 영입 항목 = `Data/CharacterRecruitData` (사용자 개명 — **Data 흡수**, 캐릭터+비용 묶음·CharacterData 무수정 = 개방-폐쇄)

**마을 표현 = 하이브리드 (사용자 결정 — "신규 화면 전부 Canvas" 계획의 의도적 이탈, 다키스트 던전 햄릿 참조):**
- 건물/배경 = **월드 스프라이트** (연출·화면 전환 확장 계층, 씬 수동 배치 = 위치 씬 소유) / 팝업·HUD = **Canvas** (기능 계층 — DD도 실기능은 UI 계층이라는 분석 반영)
- `View/TownBuildingView` — 호버 하이라이트 + 클릭 알림. **원색 = Awake 1회 저장** (개정 19 CharacterView 투명 버그 교훈 선반영). 자신의 `BuildingData` 참조 소유 (배치-데이터 연결 = 씬 소유)
- `View/TownInputController` — **마을 월드 입력 단일 주체** (포인터 폴링 + OverlapPoint — CardDragController 전례). 팝업 = 모달 → 월드 입력 잠금 API(`SetInputEnabled`)
- `View/UI/TownHUDView` — 자원 요약 + 던전 출발/이어하기 (BattleHUDView 전례, 이어하기 버튼 = 스냅샷 존재 시만 표시)
- 신규 물리 레이어 `Building` (Character 레이어 전례)

**마을 구성 = `TownConfigData` SO (사용자 제안 채택):**
- 최초 반영분(TownManager 인스펙터 목록: buildingEntries·characterRecruitData·starterCharacters)은 데이터가 프리팹/씬에 직렬화 → 관리·조회 곤란 → **구성 전량 SO 이관** (`Data/TownConfigData` — 건물 목록·막사 지정·영입 항목·기본 캐릭터). MapConfigData 전례 + P2-M7 7-4 던전 구성 데이터(챕터 SO)의 마을 대응물
- **구조 제약에 의한 분리:** SO는 씬 오브젝트 참조 불가 → 데이터 = SO / 배치-데이터 연결 = 씬 (`TownBuildingView.buildingData` 참조). TownManager 인스펙터 = **구성 SO 1칸 + 씬 뷰 목록**으로 축소. 두 참조는 역할 분리(구성 명단 vs 배치 표식)라 중복 아님 — 실위험 = 불일치뿐: 정방향(씬 배치 ↔ 구성 미등록) = `HasBuilding` 경고 반영, **역방향(구성 등록 ↔ 씬 미배치) 검사 = 선택 잔여** (4~5줄 — 필요 시 추가)
- **막사 판정 개정:** 항목별 `isBarracks` 체크 → `TownConfigData.barracksBuilding` **단일 참조** (막사 = 1개 — 기획서 6-3)

**판정·저장:**
- 기본/고급 건물 구분 = **레벨 비용의 아이템 구성으로 성립 (코드 0줄)**. 건물 효과 실반영 = 소비처 등장 시 잔여 (레벨 조회 API만 확보)
- `TownSaveData` = 자원 + 건물 레벨 + 보유 캐릭터 (**필드 추가·버전 유지** — 개정 19 규칙) / `TownSaveService` 증축: `HasItems`/`TryConsumeItems`(**검사 후 일괄 차감 — 부분 차감 방지**, 중복 항목 합산 판정)·건물/캐릭터 API — 변경 API 무저장·호출자 일괄 저장 계약 유지
- 승급/영입 = `TryUpgradeBuilding`/`TryRecruit` **공개 API** (임시 테스트 버튼과 향후 팝업의 공용 진입로). 최초 실행 = `starterCharacters` 자동 영입 + 저장

**씬 전환:**
- `Dungeon/DungeonLaunchRequest` — **1회성 정적 출발 요청** (`EDungeonLaunchMode` — GameEnum 병합). 소비 시 초기화 = Dungeon 씬 단독 테스트·BattleTest 경로 무손상. **재명명 기준 명문화: 로딩씬이 씬 전환 중계자로 확정되면 `SceneLaunchRequest`(대상 씬 + 모드)로 확장**
- `DungeonManager` 증축: `Start` 요청 소비 / `ReturnToTown` (**던전 진행 중 차단 — 중간 탈출 없음**, 기획서 7-2) / `townSceneName`
- **개정 17 임시 조치 해소:** `availableCharacters` 인스펙터 목록 → **마을 보유 명단 기반** (`RefreshAvailableCharactersFromTown` — 빈 명단 = 인스펙터 폴백, 영입 순 = 후보 순, 미등록 코드명 = 경고 후 건너뜀)

**임시 조치 (해소 시점 명시):**

| 임시 조치 | 대체 시점 |
|-----------|-----------|
| 건물 클릭 = 상태 로그만 출력 | **팝업 도입 시** (아트 시점) — `TownPopupView` 코드째 이월 (보관본 존재: 건물 승급 + 막사 영입 구역 토글 — NodeScreenView 통합 전례) |
| `TestUpgradeFirstBuilding`/`TestRecruitFirstOffer` SWButton 2종 | 팝업 도입 시 제거 (도입 전 저장/차감 경로 검증용) |
| 던전 종료 → 마을 자동 복귀 미배선 (수동 버튼) | 결과 화면(승리/패배 요약) 등장 시 흐름과 함께 결정 |
| 로딩씬 → Town 전환 | 로딩씬 소관 (6-1 범위 밖) |
| 보관 선택 전송 UI | 6-3 이월분 유지 (v1 = 전량 자동 전송) |

- **프로젝트 반영 확인 (개정 20):** Town 씬 + TownManager 프리팹(`townConfigData` 에셋 연결)·`TownConfigData`/`TownBuildingView`(buildingData 참조)/`TownInputController`/`TownHUDView`/`CharacterRecruitData`/`BuildingData`/`BuildingLevelData`, 저장 3파일(`TownSaveData`/`TownSaveService`/`GameSaveData.town`), `DungeonLaunchRequest`·`EDungeonLaunchMode`, DungeonManager 패치 4종(Start 소비·보유 명단 갱신·ReturnToTown·TownSaveService 치환 — HubSaveService 잔존 없음) 전부 반영 확인
- **잔여 ⚠:** ① **`buildingViews` 씬 배선 확인 필요** — 구성 SO 이관으로 구 필드(buildingEntries) 씬 오버라이드가 잔존하고 신 필드 배선이 미확인 상태 (미배선 시 건물 클릭·호버 무반응, InitViews 경고 0건이면 미배선 의심) ② `TownHUDView` 내부 로그 태그 `[TownHudView]` 정리 (사소 — 다음 코드 작업 시) ③ 역방향 정합 검사 (선택)
- **에디터 작업 체크리스트:** ① Town 씬 카메라 직교 확인 (개정 7 교훈 — 월드 콜라이더 판정까지 걸려 이중 중요) ② 물리 레이어 `Building` + `TownInputController.buildingLayerMask` ③ 씬의 각 `TownBuildingView`에 `buildingData` 연결 + TownManager `buildingViews` 목록 배선 ④ 데이터 윈도우 건물 탭 증축 (배열 인덱스 정합) ⑤ 빌드 설정 3씬(Loading·Town·Dungeon) 등록 + 씬 이름 필드 일치 ⑥ 구저장 파일 수동 삭제 (hub → town 개명)
- **실검증 이월 ⚠ (아트 시점 — 사용자 결정: 팝업과 함께 진행이 직관적):** 최초 실행 기본 캐릭터 자동 영입 + 저장 기록 → 건물 호버 하이라이트/원색 복귀 → 클릭 로그(레벨·막사 표기) → 테스트 버튼 승급(비용 차감·레벨 상승·재시작 유지·부족 시 실패 로그) → 영입 → 편성 후보 반영(영입 순) → 던전 출발/이어하기(스냅샷 존재 시만 버튼 표시) → Dungeon 씬 단독 재생 무동작 → 던전 종료 후 마을 복귀 → 회수 자원 HUD 반영 → 팝업 도입 후: 모달 입력 잠금·막사 영입 구역 토글·승급/영입 팝업 경유

### P2-M7 — 콘텐츠 + 시스템 잔여 (2.5주)

| # | 산출물 | 책임 |
|---|--------|------|
| 7-1 | 공용 카드 50장 (반응형 포함) | `EchoesOfAshDataWindow` 카드 탭으로 제작 (구 계획명 CardSystemWindow — 개정 13) — 코드 0줄 원칙 검증 |
| 7-2 | 유물 20개 | 트리거 구조(P2-D4) + 정신력 연동 포함 |
| 7-3 | 적 12종 + 조우 테이블 | SAN 압박 행동 포함 — `SpawnRange` 조우 풀 실가동 |
| 7-4 | 이벤트 10개 / 상점 / 보상 화면 | 데이터 기반 선택지·구매·카드 보상 — **던전 구성 데이터(챕터 SO) 신설 ✅ 완료 (개정 21 — 착수 기록 참조. 노드 이벤트 매핑 흡수(개정 11 결정) + `statusDatas` 흡수(개정 13 결정) + `sanityEventDatas` 흡수(P2-M0 결정))**. 상점·보상 화면·이벤트 10개는 잔여 |
| 7-5 | 카드 해금 2종 | 발견형 자동 해금 + 제작형 설계도 해금 (메타 저장 연동) |
| 7-6 | 보스 1개 | HP 페이즈 패턴 활용 (구조는 M4에서 기완성) |

#### P2-M7 착수 기록 — 7-4 챕터 SO + 정신력 이벤트 DD 개정 (2026-07-29)

**7-4a — 던전 구성 데이터(챕터 SO) 신설:**
- **산출물:** `Data/DungeonChapterData.cs` — 맵 생성 규칙 참조(`MapConfigData`) + 노드 타입별 조우 풀 3종(일반/엘리트/보스 — **빈 전용 풀 = 일반 풀 폴백 경고**) + 노드 이벤트 `타입 → 풀` 매핑(`EventNodePoolEntry` — 풀 1개 = 고정, 복수 = 무작위) + 정신력 이벤트 풀 + 상태이상 정의 목록. 무작위 굴림 = SO 소유 (DropTableData 전례 — SWRandom D3 일원화)
- **예약 임시 조치 3건 해소:** ① DungeonManager 노드 이벤트 3필드(rest/storage/eventDatas — 개정 11 결정) ② `BattleManager.statusDatas`(개정 13 결정 — 인스펙터 필드는 **BattleTest 단독 경로 폴백으로 잔존**, `SetChapterStatusDatas` 주입 시 챕터가 우선) ③ `sanityEventDatas`(P2-M0 결정)
- **부수 개선:** 엘리트/보스 조우가 일반 풀과 분리 — 기존에는 노드 타입 무관 단일 풀 무작위였음. `StartBattleForNode`가 `GetRandomEncounter(node.NodeType)` 경유
- **DungeonManager 필드 6종 → `chapterData` 1종:** mapConfigData/sanityEventDatas/enemyEncounterDatas/restEventData/storageEventData/eventDatas 제거, `MapConfig` 프로퍼티 경유. 미배선 방어 유지 (`GetNodeEventData` — 챕터 없음/매핑 없음 = 통과 처리)
- **OnValidate:** 매핑 중복 타입·전투 계열(전투/엘리트/보스) 타입 금지 경고 (개정 11 결정 이행) + 맵 규칙·일반 조우 풀 공백 경고
- **검토 발견 버그 1건 (수정 확인):** `EventNodePoolEntry` **`[System.Serializable]` 누락** — nodeEventPools가 직렬화되지 않아 인스펙터 미표시·노드 이벤트 전부 통과 처리되는 실버그 (개정 19 `HubSaveData` 누락과 동일 계열 — 중첩 직렬화 클래스 신설 시 체크 항목으로 각인)
- **명명 검토 (사용자 발의):** `DungeonData` 개명안 기각 — Dungeon 접두어 과밀(State/SaveData/EventData와 구분 불가) + 챕터 = 실제 에셋 단위(EA "챕터 2개" = 에셋 2개). `DungeonChapterData` 유지

**7-4b — 정신력 이벤트 발동 규칙 개정 (사용자 결정 — DD 결의 판정 방식):**
- **규칙:** 매 턴 확률 판정 → **광기 구간에서 맞는 첫 턴 시작에 확정 발동, 던전당 1회.** 붕괴/부정·기인/긍정 분기 = 기존 `SanityEventData`의 `isPositiveEffect`+`weight` 재사용 (**데이터 무수정** — DD 붕괴/기인과 1:1 대응). 발동 시 풀 균등 무작위 1건 → 이벤트 내 분기 (기존 선택 구조 유지)
- **진실 원본 = `DungeonState.HasMadnessEventOccurred`** + `MarkMadnessEventOccurred`. 저장 편입: `DungeonSaveData.hasMadnessEventOccurred` (**필드 추가·버전 유지** — 기본 false = 구저장 호환, Town 전례) + `RestoreProgress` 파라미터 확장
- **의존 차단:** 러너는 `Func<bool>`/`Action` 델리게이트 주입으로 던전 상태를 조회·기록 (Sanity → Dungeon 의존 금지 — EffectExecutor drawRequest 전례). BattleManager가 람다 배선, BattleTest = 자체 DungeonState라 "테스트 1회 = 던전 1회"로 무수정 정상
- **마킹 시점 = 효과 실행 직전** (유효성 검증 통과 후) — 이벤트 효과가 SAN을 다시 바꿔도 재발동 없음. 데이터 오류(빈 효과)·전원 사망 시는 미마킹 (침묵 소진 방지)
- **확률 곡선 폐기:** `BattleBalanceData.GetMadnessEventChance` + `madnessEventBaseChance`/`madnessEventMaxChance` 제거 (에셋 잔여 직렬화 값 무해). **밸런스 축 이동:** 발동 확률 튜닝 → 개별 이벤트 `weight`(기인 확률 — 출발점 0.25) + 풀 구성. Phase 1 6-2 구성안의 "부정 합 > 긍정 합 = 풀 비율 제어" 문구는 weight 중심으로 재해석
- **잠정 규칙 3종:** ① 순간 스침(턴 중 광기 진입 → 같은 턴 회복) 미발동 — 판정 = 턴 시작 시점 (M4 발화 순서 계약 보존, D2 턴 경계 지연 전례. 교차 순간 즉시 실행은 효과 파이프라인 중첩 위험으로 기각) ② 전투 밖 광기 진입(휴식 선택지 등) = 다음 전투 첫 턴 발동 (플래그 = 던전 수명이라 자연 성립) ③ **다챕터 런 도입 시(Phase 3 — 챕터 전환) 발생 플래그 = 챕터 전환 시점 리셋 = 챕터당 1회** (사용자 질의로 확정 — 런 전체 1회는 후반 광기 리스크 소멸로 기각, 전환 기능 신설 시 리셋 API 1줄로 이행·저장 무수정)

**명명 개정 (사용자 제안):**
- `MadnessEventRunner` → **`SanityEventRunner`** (+ `OnMadnessEventTriggered` → `OnSanityEventTriggered`, 파일명 동반 개명) — `SanityEventData`와 데이터-러너 접두어 대칭 (Phase 1 MadnessEventData → SanityEventData 개명의 완결)
- **경계 확정: Madness = 구간(상태) 명칭 유지** (`ESanityType.Madness`·`MadnessOverlayView`·`IsPartyMadness`·`HasMadnessEventOccurred`) / **SanityEvent = 이벤트 콘텐츠 명칭** — 기획 용어 "결의 판정" ↔ 코드 접두어 분리 매핑 방식 유지

**전수 검사 결과 (사소 정리 권고 — 다음 코드 작업 시):**
- `GameEnum.EEnemyType.Noraml` 오타 → `Normal` (enum = int 직렬화라 개명 무해, 값 순서 유지 조건) / `TargetResolver` 구 명칭 로그 태그 `[TargetingResolver]` 2곳 / `BattleManager.statusDatas` Tooltip "(임시 조치)" → "(BattleTest 폴백)" 갱신 / `BattleBalanceData.sanityEvent` 필드 — 사용처 없는 초기 잔재로 확인 시 제거

**에디터 작업 체크리스트:** ① `DungeonChapterData` 에셋 이관 — MapConfig 연결 + 기존 조우 → 일반 풀 + 노드 이벤트 매핑 3줄(휴식·보관 = 고정 1개, 이벤트 = 풀) + 정신력 이벤트 + 상태이상 목록 복사 ② DungeonManager 프리팹 `chapterData` 연결 + 제거 필드 오버라이드 잔여값 정리 ③ 데이터 윈도우 챕터 탭 증축 (7-1 착수 시 일괄 — 배열 인덱스 정합) ④ 구 던전 저장 파일 확인 (필드 추가라 폐기 불필요 — 버전 유지)

**실검증 이월 ⚠ (씬 검증 이월분과 일괄):** 챕터 미배선 = 노드 통과 → 매핑 등록 후 휴식/이벤트/보관 표시 → 엘리트/보스 노드 = 전용 풀 (빈 풀 = 폴백 경고) → 광기 진입 후 첫 턴 결의 판정 1회 발동 → 같은 던전 재광기 = 미발동 → 이어하기 후에도 미재발동 (저장 반영) → weight 분기 체감

**다음: P2-M7 7-2 (유물 시스템 — RelicData + 획득 관리 + 획득 순 발화 계약, 개정 16 예고 의존 정리 및 미배선 트리거 3종 발화 지점 연결 동반)**

### P2-M8 — 통합 검증 (1주)

**DoD (Phase 2 전체):** 거점 → 파티 편성 → 런 1회 완주(보스 처치 or 게임 오버) → 회수 → 거점 복귀 → 해금 반영이 저장/복원 포함 완주된다.

---

## 3. 모듈 ↔ 산출물 매핑 (신규 모듈)

| 모듈 | Phase 2 산출물 | 폴더 |
|------|----------------|------|
| 런 | `DungeonState`, `DungeonManager` | `05_Scripts/Dungeon/` |
| 맵 | `MapGenerator`, 노드 데이터, 맵 화면 | `05_Scripts/Map/` |
| 저장 | **통합 프로필 저장 (개정 19 — P2-D7):** `GameSaveData`(루트 — hub 구획 → **town 구획**, 개정 20) + `GameSaveService`(파일 입출력 단일 진입점·프로필) + 구획 파사드 `TownSaveService`/`DungeonSaveService` + 스키마 `TownSaveData`/`DungeonSaveData` — 마이그레이션 잠정 제거 (버전 불일치 = 폐기) | `05_Scripts/Save/` |
| 상태이상 | `StatusEffectData`(정의 SO) + `StatusController`(생명주기) + `StatusDamageCalculator` — **별도 폴더 기각 (개정 13 — P2-M3 완료 기록 참조)** | `05_Scripts/Data/`, `05_Scripts/Battle/` |
| 어그로 | `AggroSystem` (전투 1회 수명 순수 클래스 — 산정 수치는 Balance 소유, 개정 18) | `05_Scripts/Battle/` |
| 유물 | 유물 SO(RelicData)·획득 관리 — **트리거 구조는 패시브 공용이라 `Effect/Trigger/`에 배치 완료 (개정 16 — `TriggerEffect`/`TriggerEffectController`)**, Relic 폴더에는 P2-M7에서 유물 고유분만 | `05_Scripts/Relic/`, `05_Scripts/Effect/Trigger/` |
| 마을 (구 거점) | `TownManager`(조립 지점 — 씬 기준 명명 대칭) + `TownConfigData`(구성 SO — **Data 흡수**) + `CharacterRecruitData`(**Data 흡수** — 인라인 직렬화) + `BuildingData`/`BuildingLevelData` + 월드/UI 뷰(`TownBuildingView`·`TownInputController`·`TownHUDView`·팝업 이월) — 개정 20 | `05_Scripts/Town/`, `05_Scripts/Data/`, `View/`, `View/UI/` |
| 드랍·회수 | 드랍 데이터(`ItemData`·`ItemStackData`·`DropEntryData`·`DropTableData`) = **Data 흡수**, 회수 판정·보관 전송 = **DungeonManager 흡수** (개정 19) — `Drop/` 분리 기준 = 보관 선택 UI 등장 시 | `05_Scripts/Data/`, `05_Scripts/Dungeon/` |
| 던전 구성 (챕터) | `DungeonChapterData` (챕터 SO — 맵 규칙 참조 + 노드 타입별 조우 풀 3종 + 노드 이벤트 `타입 → 풀` 매핑 + 정신력 이벤트 풀 + 상태이상 정의. **굴림 = SO 소유** — DropTableData 전례) — 챕터 1개 = 에셋 1개 (EA 스코프 "챕터 2개" = 에셋 2개, 개정 21) | `05_Scripts/Data/` |
| (기존) 전투/뷰 | 파티 3인·타겟팅 확장 — 기존 폴더 증축 | `Battle/`, `View/`, `View/UI/` |

통신 원칙 유지: C# event 우선·구독/발화 순서 결정성 (유물 다중 발동 = **획득 순 고정** — 기획서 15-2 명시 / 상태이상 = 부여 순 순회 + 라운드 종료 구독 순서 "감소 → 어그로 감쇠 → 의도 재평가" — 개정 13·18 / 파티 = 스폰 순서 = 목록 순서 — 개정 14), 뷰 배선 예외는 조립 지점만 (`BattleManager` + 신규 `DungeonManager` + 신규 `TownManager` — 개정 20).

---

## 4. 조기 결정 필요 사항

| # | 결정 | 시점 | 상태 |
|---|------|------|------|
| P2-D1 | **시드 결정성 범위**: 같은 시드 = 같은 런 보장 여부 (런 중 저장 방식과 직결) | P2-M2 전 | ✅ 확정 (개정 11) — **전체 상태 스냅샷.** 무작위 소비 지점 다수(조우·이벤트 추첨·광기 판정·셔플·침식)로 "시드+행동 로그"는 소비 순서 영구 계약 요구 → 코드 수정마다 재현 파손. 시드는 맵 생성 기록·재현용으로만, **재개 후 난수 비연속** (같은 시드 재설정 시 소비된 난수열 재등장 = 세이브스커밍 여지 차단) |
| P2-D2 | **노드 그래프 구조** | P2-M1 착수 시 | ✅ 확정 — **구조: STS식 층×레인 그래프** (축소 규격 12층×3레인 — 노드 수 선형·전체 공개·경로 계획 성립, 단순 분기 트리는 노드 수 배증 또는 계획성 상실로 기각) **+ 잿불 침식** (이동마다 입구층부터 잠식 — 시간 압박 축, 속도는 Balance 외부화·0이면 순정 STS) **+ 광기 간선** (광기 상태에서만 열리는 간선·노드 — 정신력 댄스의 던전 확장, 토글 가능. 의도적 광기 진입 수단 필요 — M1 규칙 설계). **표현: 던전 도면식** — 노드=방·간선=복도, 시드 기반 좌표 지터(결정성 유지), **가로 심부 진행**(입구→폐허 심부 — 침식=입구부터 타들어오는 재). 데이터(그래프)와 표현(MapView) 분리 — 다키스트 던전 참조. 플레이스홀더=사각 방+통로, 양피지·지명·랜드마크는 리소스 단계 |
| P2-D3 | **전용 카드 소속 표현**: CardData가 소유 캐릭터 참조 vs CharacterData가 전용 카드 목록 보유 | P2-M4 전 | ✅ 확정 (개정 14) — **CharacterData가 전용 카드 목록 보유.** 근거: ① 전투불능 드로우 제외 필터 = 파티 구성원(최대 3)의 목록 합산만 순회 — 카드 전수 역추적 불필요 ② 공용 카드 무수정 (빈 소유자 필드·오배정 사고 여지 차단, 캐릭터 추가 시 기존 카드 에셋 무수정 = 개방-폐쇄) ③ 캐릭터 에셋 1개 = 전용 카드 구성 한눈에. 카드→주인 역조회(툴팁 "전용" 표시 등)는 전투 시작 시 1회 표 구성으로 해소 → **✅ 이행 완료 (개정 15 — 4-2)** |
| P2-D4 | **유물 트리거 구조**: 전투 이벤트 구독형 리스너 vs 훅 열거 매핑 (캐릭터 패시브와 공용) | P2-M7 전 (P2-M4 패시브와 통합 설계) | ✅ 확정 (개정 16) — **훅 열거 매핑.** 기각: 리스너형 — 유물 56개 × 클래스 = 15-3 "코드 0줄" 위반 + "획득 순 고정"(15-2)이 개별 구독 관리에 분산. 근거: ① 15-4 enum 기준표에 유물 트리거 명시 (닫힌 집합) ② 단일 디스패처 목록 순회 = **등록 순 = 발화 순** (StatusController 부여 순 전례 — 결정성 구조적 보장) ③ 특수 유물 = 커스텀 EffectBlock 폴백 (신규 블록은 공용 부품화 — 후반 0줄 비율 상승). 부수 확정: **패시브 = `TriggerEffect` 인라인 직렬화** (CharacterData 보유 — 독립 콘텐츠 아님, RelicData가 동일 타입 목록 보유로 구조 공유) / **상시 배율형 효과 = 피해 계산기 래핑 영역** (트리거 구조 밖 — StatusDamageCalculator 전례) → ✅ 이행 완료 (개정 16 — 4-3) |
| P2-D5 | 어그로 산정식 (피해 기여 가중 등) | P2-M5 | ✅ 확정 (개정 18) — **원본 피해량 × 가중치 + 라운드 감쇠 (전부 Balance 외부화 — `aggroDamageWeight`/`aggroRoundDecayRate`).** 방어막 흡수분 포함 기여·취약 배율 미반영 (잠정 — 수치 조정은 에셋만). 동률 = 파티 순서, 유효 어그로 없음 = 무작위 폴백. 도발 = 산정식 밖의 강제 규칙 (모든 규칙에 우선) |
| P2-D7 | **저장 파일 구성** (거점/던전 병용 방식) | P2-M6 | ✅ 확정 (개정 19) — **통합 프로필 저장.** 슬롯 분리안 폐기 (사용자 지적: SWSave 슬롯 = 저장 칸 본래 의미 + 프로필 도입 의사) → 프로필 1칸 = 파일 1개 = `GameSaveData` 루트 (hub + hasDungeon 깃발 + dungeon). 입출력 = `GameSaveService` 단일 진입점 (SetData 재등록 국소화 = 병용 순서 규약 소멸), 던전 소멸 = 깃발 하강 (거점 무사), `SelectProfile` = 프로필 화면 대비. 부수: 마이그레이션 잠정 제거 (버전 불일치 = 폐기 — 15-5 의도적 이탈, 데이터 보존 시작 시점 복원) → **(개정 20) hub 구획 → `town` 개명 — 구저장 거점 구획 미판독 (비보존 방침상 무해)** |
| P2-D6 | **씬 구성** | P2-M0 착수 시 | ✅ 확정 — **2씬: Hub(거점) + Dungeon(던전 = 런 1회)**. 맵은 씬이 아니라 Dungeon 씬 내 Canvas 화면 — 맵 ⇄ 전투 ⇄ 노드 화면(이벤트/상점/보관)을 씬 로드 없이 전환 (전투 인프라 1회 구성·런 템포 보존·DungeonState 수명 = Dungeon 씬 수명). `DungeonManager` = Dungeon 씬 내 화면 상태 머신 + 거점⇄던전 씬 전환 소유 → **재개정 (개정 20): 씬 3구성 = Loading · Town · Dungeon** — 거점 = `Town` 개명, 씬 전환 = `DungeonLaunchRequest` 1회성 정적 요청 (Town → Dungeon), 복귀 = `ReturnToTown` (DungeonManager 소유 유지). 로딩씬 → Town 전환 = 로딩씬 소관, 로딩씬이 전환 중계자로 확정 시 `SceneLaunchRequest` 확장 |

---

## 5. 리스크와 완충

- **콘텐츠 물량이 병목** (P2-M7) — 카드 50·유물 20·적 12·이벤트 10은 코드가 아니라 제작 시간. → 도구 선행(P2-M3 — `EchoesOfAshDataWindow` 증축 완료)으로 완충 + "코드 0줄 추가" 원칙이 깨지는 카드는 즉시 구조 재점검
- **저장 결정성 미결 재작업** — ~~P2-D1을 P2-M2 전에 반드시 확정~~ → **확정 완료 (스냅샷 — 개정 11).** 잔여 리스크: 강화 외 카드 가변 상태(P2-M7 이후) 추가 시 저장 스키마 버전 증가 + 마이그레이션 필수. **상태이상은 전투 한정(전투마다 리셋)이라 현재 저장 스키마 무관 — 던전 지속화 시 재분리 기준 발동 (개정 13). ~~파티 구성은 저장 제외(정적 구성) — 4-4 편성 화면 가변화 시 스키마 v2 편입 (개정 14)~~ → ✅ v2 편입 완료 (개정 17 — 파티 명단 저장 + v1 폴백 마이그레이션, 버전 체계 첫 실사용). 드로우 제외 더미도 전투 한정 — 저장 무관 (개정 15). 어그로도 전투 한정 (귀속·감쇠 전부 전투 수명) — 저장 무관 (개정 18). 드랍 소지 = 던전 스냅샷 편입 (개정 19). 마을 진행(자원·건물 레벨·보유 캐릭터) = town 구획 필드 추가·버전 유지 (개정 20). ⚠ 마이그레이션 계층 잠정 제거 (개정 19 — 버전 불일치 = 폐기): "버전 증가 + 마이그레이션 필수" 규칙은 데이터 보존 시작 시점(얼리액세스 전) 계층 복원과 함께 재가동**
- **파티 확장 파급** — 공유 SAN·피격 SAN 판정(피격자 개인 HP 기준)·전투불능 드로우 제외가 맞물림. P2-M4에서 `Test_Battle` 3인 버전으로 단독 검증 후 통합. ~~PartyData 이중 참조(DungeonManager/BattleManager — 동일 에셋 필수)도 이 시점에 주입 경로로 일원화~~ → **✅ 일원화 완료 (개정 14 — 4-1에서 DungeonState 경유 주입, BattleManager 데이터 필드 제거).** ~~전투불능 드로우 제외~~ → **✅ 완료 (개정 15 — 제외 더미 격리로 재셔플 간섭 없음).** ~~Aggro 무작위 폴백~~ → **✅ 해소 (개정 18 — P2-M5 어그로 실구현·도발). Phase 1 임시 조치 전량 해소 완료.** ~~신규 잔여: 파티 배치 프리팹 `SetupParty` 적용 이월~~ → **✅ 적용 완료 (개정 19 — `PartyFormation` 인원수별 좌표 스폰). 아군 드롭·Self 드래그 실검증 선행 조건 충족**
- ~~**메타 저장 병용** — SWSaveDataManager는 정적 단일 currentData 구조. 메타 저장(해금/거점) 도입 시(P2-M6/M7) 던전 슬롯과의 SetData 순서 규약 필요~~ → **✅ 해소 (개정 19 — P2-D7 통합 저장):** 파일 입출력 = `GameSaveService` 단일 진입점으로 병용 자체가 사라짐 (SetData 재등록 내부 국소화). 잔여 주의: 향후 SWPlayerPrefs 사용 시작 시 슬롯 = 프로필 전제 재확인
- **범위 방어** — Phase 3 항목 침범 금지: 적 SAN 보스 프로토타이핑(**SWUtils Behavior는 이 시점의 보스 AI 후보로 보류** — 현행 데이터 주도 패턴 순환에는 과함), 캐릭터 2·3번, 유물 21개 이상, 이벤트 11개 이상, Ascension, 튜토리얼, 아트 완성. **밸런스 게이트 전에 P2-M7 수치 확정 금지** (제작은 가능, 확정은 게이트 후)

---

## 6. Phase 3 예고 (참고)

콘텐츠 전량(카드·유물·적·이벤트), 적 SAN 보스 프로토타이핑, 캐릭터 2번(재의 궁수), 튜토리얼/FTUE, 픽셀아트·사운드, Steam 페이지. — 상세 계획은 Phase 2 완료 후.

---

## 개정 이력

| 개정 | 일자 | 내용 |
|------|------|------|
| 초판 | 2026-07-22 | Phase 2 스케줄 수립 — 메타 계층 우선 순서(P2-M0~M3), 밸런스 게이트 병행 배치, Phase 1 임시 조치 5종 = P2-M0 작업 목록화, 조기 결정 P2-D1~D5 정의 |
| 개정 21 | 2026-07-29 | **P2-M7 7-4 챕터 SO 신설 + 정신력 이벤트 DD 방식 개정 (사용자 결정) + 명명 개정** — `Data/DungeonChapterData` (맵 규칙 + 조우 풀 3종(엘리트/보스 분리·빈 풀 폴백) + 노드 이벤트 매핑 + 정신력 이벤트 + 상태이상 — 예약 임시 조치 3건 해소: 개정 11·13·P2-M0). DungeonManager 필드 6종 → 1종, `SetChapterStatusDatas` (인스펙터 = BattleTest 폴백). 검토 버그: `EventNodePoolEntry` [Serializable] 누락 (HubSaveData 전례 계열 — 수정 확인). **정신력 이벤트 = DD 결의 판정** — 던전당 1회 확정 발동 (광기 구간 첫 턴 시작), 붕괴/기인 = 기존 weight 재사용 (데이터 무수정), 플래그 = DungeonState + 저장 편입 (필드 추가·버전 유지), 델리게이트 주입 (Sanity → Dungeon 차단), 확률 곡선 폐기 (밸런스 축 = weight·풀 구성). 잠정 규칙 3종: 순간 스침 미발동·전투 밖 진입 = 다음 전투 첫 턴·**다챕터 도입 시 챕터당 1회 리셋**. **명명: `MadnessEventRunner` → `SanityEventRunner`** (데이터-러너 접두어 대칭 — Madness = 구간 / SanityEvent = 이벤트 경계 확정). `DungeonData` 개명안 기각 (접두어 과밀·챕터 = 에셋 단위). **다음: 7-2 유물** |
| 개정 20 | 2026-07-28 | **P2-M6 6-1 마을 씬 완료 = P2-M6 마일스톤 완료 (프로젝트 반영 확인) + 용어 재개정 + 마을 구성 SO** — **용어 (사용자 결정): 거점 = `Town` (개정 19 "메타 = Hub" 재개정, 씬 3구성 Loading·Town·Dungeon — P2-D6 재개정) / 시설 = `Building` / 영입 = `CharacterRecruitData` (Data 흡수).** `GameSaveData.hub → town` (구저장 거점 구획 미판독 — 비보존 방침·구저장 삭제). **표현 = 하이브리드 (사용자 결정 — "신규 화면 전부 Canvas" 의도적 이탈, 다키스트 던전 햄릿 참조):** 건물/배경 = 월드 스프라이트(입구·연출 계층) / 팝업·HUD = Canvas(기능 계층 — DD도 실기능은 UI라는 분석) — `TownBuildingView`(원색 Awake 1회 저장 — 개정 19 교훈 선반영·buildingData 참조 = 배치-데이터 연결 씬 소유)·`TownInputController`(단일 입력 주체 — 폴링+OverlapPoint·팝업 모달 잠금)·`TownHUDView`, 물리 레이어 `Building` 신설. **구성 = `TownConfigData` SO (사용자 제안 채택)** — 인스펙터 목록 직렬화 기각(프리팹/씬 박제 = 관리·조회 곤란), MapConfigData 전례·7-4 챕터 SO 대응물. SO ↔ 씬 참조 불가 → 데이터 = SO / 연결 = 씬, 막사 = `barracksBuilding` 단일 참조(체크박스 폐기), 정방향 정합 검사(`HasBuilding` 경고 — 역방향 = 선택 잔여). 판정: 기본/고급 = 비용 구성 성립(코드 0줄)·`TryConsumeItems` 검사 후 일괄 차감·승급/영입 = 공개 API(팝업·테스트 공용 진입로)·`TownSaveData` 필드 추가 버전 유지. 씬 전환 = `DungeonLaunchRequest` 1회성 요청(소비 시 초기화 = 단독 테스트 무손상, **로딩씬 중계 확정 시 `SceneLaunchRequest` 확장 기준 명문화**) + `ReturnToTown`(진행 중 차단 — 중간 탈출 없음). **개정 17 임시 조치 해소: 편성 후보 = 마을 보유 명단** (빈 명단 = 인스펙터 폴백·영입 순 = 후보 순). **팝업 = 코드째 아트 시점 이월 (사용자 결정 — 실검증도 팝업과 일괄):** 클릭 = 로그·SWButton 테스트 2종(도입 시 제거)·TownPopupView 보관본 존재. 잔여 ⚠: `buildingViews` 씬 배선 확인·TownHUDView 로그 태그·역방향 검사(선택)·던전 종료 자동 복귀(결과 화면 시점)·건물 효과 소비처·보관 선택 UI(6-3 이월)·로딩씬 → Town 전환(로딩씬 소관). **다음: 밸런스 게이트(6-2~6-5 리소스 + 마을 아트/팝업) 또는 P2-M7 (콘텐츠 + 시스템 잔여)** |
| 개정 19 | 2026-07-27 | **P2-M6 6-2·6-3 완료 + P2-D7 확정 + 배치 프리팹 적용 (프로젝트 반영 확인)** — 배치: `PartyFormation` (사용자 설계 표식 굽기 + 완성분 `GetSpawnPosition`·localPosition 통일·정렬 no-op 수정·포메이션 보기 역복원), `SetupParty` 인원수별 스폰 (에셋 직접 참조·원점 폴백) = P2-M4 이월 최종 해소. **P2-D7: 통합 프로필 저장 (사용자 결정)** — 슬롯 분리안 폐기 (슬롯 = 저장 칸 본래 의미) → `GameSaveData` 루트(hub + hasDungeon + dungeon) + `GameSaveService` 단일 진입점 (병용 순서 규약 소멸) + `SelectProfile`, 구획 파사드로 호출부 무수정. 용어 매핑: 메타 = `Hub`. **마이그레이션 잠정 제거 (사용자 결정)** — 버전 불일치 = 폐기, 15-5 의도적 이탈 (데이터 보존 시점 복원 필수), 던전 버전 1 리셋. 6-2: `ItemData`·`ItemStackData`·`DropEntryData`·`DropTableData` **가중치 추첨 (사용자 결정)** — 굴림 횟수 범위 + 꽝 가중치 전부 데이터 소유, 굴림 = SO 소유. 6-3: 소지 = DungeonState (스냅샷 편입·itemDatabase 복원), 굴림 = 승리 직후·보스 분기 앞, **보관 전송 = 즉시 거점 귀속** (사망 보존 자연 성립·선택 UI 잔여), 회수 = `EndDungeon` 첫 줄 (승리 전량 / 패배 기본 자원만). **검토 발견 수정 6건**: Meta 잔존·구 타입·보스 드랍 누락·`currentEncounterData` 미선언·`HubSaveData` `[Serializable]` 누락(거점 저장 증발)·**`CharacterView` 투명 버그 (사용자 발견 — Init→Release 순서로 originColor 미저장 기본값 (0,0,0,0) 오염 → Awake 1회 저장)**. **다음: 6-1 거점 씬 (Hub 씬 카메라 직교 — 개정 7 교훈)** |
| 개정 18 | 2026-07-27 | **P2-M5 완료 (프로젝트 반영 확인) = Phase 1 임시 조치 전량 해소** — P2-D5 확정(원본 피해 × 가중치 + 라운드 감쇠 — Balance 외부화 `aggroDamageWeight`/`aggroRoundDecayRate`, 동률 = 파티 순서, 전원 0 = 무작위 폴백). 5-1: `AggroSystem` 신설(순수 클래스 — 귀속 구간 `Begin/EndAttribution`·적 OnDamaged 구독·MIN_AGGRO 만료, EnemyAI 생성자 주입·null 허용). 5-2: `EStatusEffectType.Taunt`(지속 = 상태이상 구조 재사용) — 도발 카드 = `StatusEffect` 블록 재사용 **코드 0줄**, 모든 규칙 우선·다수 = 파티 순서·**실행 시점 강제** + `RefreshTauntPreview`(표시 즉시 갱신 — 난수 소비 0)·만료 복구 = 기존 순서 계약 자연 처리. 순서 계약 확장: 상태이상 감소 → **어그로 감쇠** → 의도 재평가. **Self 드래그 지정 UX 개정 (사용자 발견):** 기준선 폴백에 의한 무선택 시전 문제 → `isAimedTargeting`(Single + Self 화살표 조준)·드롭 = 타겟팅별 분기(Self = 아군 위에서만·빗나감 = 취소 — Single 대칭)·기준선 = 비대상 전용. **파티 배치 결정 (사용자):** `partySpacing` 기각 → 인원별(1/2/3인) 배치 프리팹 방식 — 프리팹 등록 완료, `SetupParty` 적용 이월 ⚠ (겹침 지속 = Self 드래그 실검증 배치 후 일괄). 사소 잔여: 미사용 `isSingleTarget` 필드 제거 등 3건. **다음: P2-M6 (거점 + 드랍/회수)** |
| 개정 17 | 2026-07-26 | **P2-M4 4-4 완료 = P2-M4 마일스톤 완료 (프로젝트 반영 확인)** — 4-4a: `CharacterView` 신설(EnemyView 대칭 콜라이더·`Character` 레이어) + 아군 드롭(`TryPlayOnAlly` — Self 한정·기준선 폴백 보존) + `PartyCharacterSlotView`/`PartyStatusView` 3인 슬롯 개편 + **적 대상 예고 = 의도 시점 확정** (`PickNextTarget`·`OnTargetChanged`·예고 = 실행 일치·전투불능 시 조용한 재선정·난수 소비 시점 이동 노트 — 스냅샷 저장이라 재현 계약 무관). 4-4b: `PartySetupView`(선택 순서 = 파티 순서·패시브 설명 = GetDescription 재사용 코드 0줄·미배선 = 기본 파티 폴백) + **저장 스키마 v2** (파티 명단 + v1→v2 마이그레이션 — 버전 체계 첫 실사용). **검토 발견 버그 4건 수정 확인:** PrepareNextTurn 표적 갱신 누락(무작위 규칙 고정 대상화)·사망 적 표적 글자 잔존·CurrentVersion 미인상·Migrate 무조건 실패 흐름. **뷰 = 테스트용 플레이스홀더 전제 명시** (교체 시 유지 계약 = Init/Release·구독+표시만). 잔여 ⚠: `SetupParty` 파티 간격 배치(겹침 = 아군 드롭 판정 문제 — 다음 코드 작업 시). 실검증은 씬 검증 이월분과 일괄. **다음: P2-M5 (타겟팅 완성 — 어그로 실구현 + 도발, P2-D5 산정식·Phase 1 임시 조치 최종 해소)** |
| 개정 16 | 2026-07-25 | **P2-D4 확정 + P2-M4 4-3 완료 (프로젝트 반영 확인)** — D4: **훅 열거 매핑** (기각: 리스너형 — 콘텐츠당 클래스 = 15-3 위반 + 획득 순 고정 분산 / 근거: 단일 디스패처 = **등록 순 = 발화 순** 구조적 보장·특수분 = 커스텀 EffectBlock 폴백). 부수 확정: 패시브 = 인라인 직렬화(CharacterData 보유·RelicData와 구조 공유) / 상시 배율형 = 피해 계산기 래핑 영역. **enum 중복 신설 방지 (사용자 발견):** 기존 `ERelicTriggerType` 재사용 — `ETriggerType` 개명 + `Passive` 값 제거(발화 지점 없는 함정값) + `BattleEnd` 유지(8-1 재검토 — 미배선 경고). 산출물: `Effect/Trigger/TriggerEffect`·`TriggerEffectController`(등록 순 발화·사망자 스킵·정신력 조건·미배선 3종 경고) + `CharacterData.passives` + BattleManager 배선(**구독 순서 계약: 방어막 리셋 → 패시브** / BattleStart = OnBattleStarted 후·StartBattle 전 / 해제 대칭). 검사 패시브 = 데이터 성립(코드 0줄). **의존 방향 노트: Effect.Trigger ↔ Battle·Card 상호 참조 — P2-M7 유물 등록 확장 시 ITargetable 전환 + 카드 파라미터 제거로 해소 예정.** 실검증은 씬 검증 이월분과 일괄. **다음: 4-4 (뷰 확장 — CharacterView·PartyStatusView 3인·적 대상 표시·편성 화면 기초)** |
| 개정 15 | 2026-07-25 | **P2-M4 4-2 완료 (프로젝트 반영 확인)** — P2-D3 이행: `CharacterData.exclusiveCards` 신설 + `CardData` 소유자 참조 2종 제거 (기각안 잔재 정리 — 직렬화 잔존 무해). `DeckSystem` 드로우 제외 = **제외 더미 물리 이동 + 판정 주입** (무한 재셔플 원리상 차단·Deck→Battle 의존 차단) + Draw 안전망(버림 경유 재드로우 차단·`i--` 미소모) + **양방향 갱신 = 부활 자리 확보** (해제 → 버림 복귀). `BattleManager` 소유 조회 표(전투 시작 1회 — 원본+강화 등록·중복 에러·ResetBattle Clear), 시전자 확장 `GetCasterFor`(전용 = 소유자, 사망 시 첫 생존자 폴백). **잠정 규칙: 손패의 전용 카드는 제외 대상 아님** (기획 문언 "드로우 풀" 준수). 제외 더미 = 전투 수명 → 저장 스키마 v1 무수정. 실검증은 씬 검증 이월분과 일괄. **다음: 4-3 (캐릭터 패시브 기초 — P2-D4 유물 트리거 구조 결정 선행)** |
| 개정 14 | 2026-07-24 | **P2-D3 확정 + P2-M4 4-1 완료 (프로젝트 반영 확인)** — D3: CharacterData가 전용 카드 목록 보유 (드로우 제외 필터 = 파티 순회·공용 카드 무수정·개방-폐쇄, 역조회 = 전투 시작 시 1회 표). 4-1: 파티 구성(PartyData+캐릭터 목록) = **DungeonState 소유 — PartyData 이중 참조 일원화 조기 이행** (단일 원본 = DungeonManager 인스펙터, BattleManager 데이터 필드 2종 제거 = **Phase 1 임시 조치 전량 해소, Aggro 폴백 1건 제외**). 파티 구성 = 저장 제외 유지 (4-4 편성 가변화 시 스키마 v2). 복수 스폰(스폰 순 = 목록 순)·전원 사망 = 패배. 잠정 규칙 3종: 공용 카드 시전자 = 첫 생존자(4-2 확장) / Self = 지정 아군 우선·시전자 폴백 / 광기 이벤트 대상 = 판정 시점 첫 생존자(고정 시전자 폐기 — `IReadOnlyList<ITargetable>` 공변 전달로 Sanity→Battle 의존 차단). **아군 지정 UI = 4-4 이월** (권장: CharacterView 신설 — EnemyView 대칭 콜라이더로 드롭 경로 재사용, 드래그 Self = 시전자 폴백 정상 동작). M3 구독 순서 수정분 반영 확인. 실검증: 아군 방어막 부여 확인, 잔여 절차는 씬 검증 이월분과 일괄. **다음: 4-2 (전용 카드 소속 + DeckSystem 드로우 제외 필터)** |
| 개정 13 | 2026-07-24 | **P2-M3 완료 (프로젝트 반영 확인)** — 3-1 CardSystemWindow 신설 기각(기존 `EchoesOfAshDataWindow` 기충족 — 창 중복 방지 + 상태이상 탭 증축, 배열 5종 인덱스 정합), 3-2 하이브리드(`Data/StatusEffectData` = 정의 SO — 감소 규칙·배율 수치 소유 / `Battle/StatusController` = 생명주기 순수 클래스 — 부여 순 순회 고정·미정의 폴백 / `BattleEntity` 위임 = IStatusReceiver 실이행). **별도 Status 모듈 기각** — 정의 = Data·생명주기 = Battle 흡수 (전투 한정 상태 — Sanity와 달리 던전 지속 아님), 재분리 기준 명문화(던전 지속화·틱 블록/뷰 증축 시). **중첩 = 남은 라운드 수 (STS)** — IStatusReceiver·StatusEffect 블록 무수정. 틱 = OnRoundEnded, **구독 순서 계약: 상태이상 감소 → 의도 재평가** (P2-M5 도발 대비 — 초기 역순 반영분 1건 수정). 3-3 취약 = `StatusDamageCalculator`(기반 계산기 래핑·방어막 이전 배율·버림·배율 1.5 = SO 소유). 임시 조치: `BattleManager.statusDatas` → P2-M7 7-4 던전 구성 데이터로 이동. 취약 실검증은 씬 검증 이월분과 일괄. **다음: P2-M4 (파티 시스템 — P2-D3 확정 선행), 밸런스 게이트(6-2~6-5)는 에디터 작업으로 병행** |
| 개정 12 | 2026-07-24 | **P2-M2 완료 (프로젝트 반영 확인)** — Save 모듈·DungeonState 복원 경로·MapGraph.RestoreFrom·DungeonManager 저장 배선 반영 확인, SWSaveDataManager 네임스페이스 `SW.Data` 확정. 저장/복원 실검증(이어하기·미해결 노드 재진입·마이그레이션 거부)과 `cardDatabase` 배선은 씬 검증 이월분과 일괄 처리. **다음: P2-M3 (CardSystemWindow + 상태이상 모듈)** |
| 개정 11 | 2026-07-24 | **P2-M1 완료 + P2-D1 확정(전체 상태 스냅샷) + P2-M2 코드 완료** — 1-4 씬 배선 확인(실플레이 통합 검증은 리소스 시점 이월). D1 확정 근거: 무작위 소비 지점 다수 → 행동 로그 방식의 소비 순서 영구 계약 리스크, 재개 후 난수 비연속. M2 산출물: `Save/DungeonSaveData`(버전 + 카드 `{codeName, isUpgrade}` — 강화 유실 방지)·`Save/DungeonSaveService`(슬롯 "dungeon"·마이그레이션 계층)·`MapGraph.RestoreFrom`·`DungeonState.IsCurrentNodeResolved`/`RestoreProgress`·`DungeonManager` 저장 시점 3종 + `ResumeDungeon`. **저장 시점 계약: 진입 직전(미해결) + 처리 완료 지점(해결) — 복원 시 미해결 노드 재실행 = 노드 스킵 불가.** 사용자 제안 채택: 노드 이벤트 3필드 → `타입 → 풀` 매핑 통합 — **이행은 P2-M7 7-4 던전 구성 데이터 신설 시** (Storage의 P2-M6 매핑 이탈로 즉시 이행 시 이중 재작업). **다음: cardDatabase 배선 + M2 저장/복원 검증 → P2-M3 (CardSystemWindow + 상태이상)** |
| 개정 10 | 2026-07-24 | **1-4 코드 완료 (씬 검증 대기) + 노드 화면 통합 결정** — 노드 타입별 뷰 분리 기각 → 선택지형 공용 `NodeScreenView` 1클래스 + `DungeonEventData`(표시명/설명 = IdentifiedObject 재사용, 선택지 1~3). 휴식/보관 = 고정 이벤트 에셋, 이벤트 = 풀 무작위(임시 조치). 휴식 회복 수치 `MapConfigData.RestSanityRecovery` 제거 → 휴식 이벤트 데이터로 이동, **의도적 광기 진입 = 휴식 선택지 데이터로 성립 (코드 0줄 — 1-6 이월분 해소)**. `EDungeonPhase.Node` 추가 (IsDungeonRunning 포함), 미배선 시 통과 처리. 전용 뷰 신설 기준 명문화(보관 실 UI = P2-M6·상점 = P2-M7 등 비선택지형 표현 등장 시) |
| 개정 9 | 2026-07-24 | **1-6 완료** — 파티 SAN 던전 지속화(`DungeonState.CarriedSanity` 값 이월, -1 = 미기록 → BattleTest 무손상, 상한 클램프 = 전투 진입 시점 잠정), 광기 복도 통행(`IsPartyMadness` — 임계값 판정 일원화), 잿불 침식(이동 카운터 → 간격 배수마다 잠식 층 전진, **잠식 층 ≥ 현재 층 = 패배 잠정** — 갇힘 원리상 불가), `ChangeDungeonSanity` API(**전투 중 호출 가드** — 전투 중 진실 원본 = partySanityHolder). 버그 수정: Enemy_Test1 `SanityChangeEffect` 델타 부호(+5 → -5 — 최대치 클램프 조기 반환으로 무변화 위장·의도 Buff 오표시). 임시 조치: **PartyData 이중 참조**(DungeonManager = 던전 수위 판정 / BattleManager = 전투 홀더 생성 — 동일 에셋 필수, P2-M4 일원화). SAN 전투 간 이월 씬 검증 완료, 침식·광기 통행·휴식 회복은 리소스 연결 시점 이월 |
| 개정 8 | 2026-07-24 | **P2-M1 1-3 완료** — MapView/MapRoomView 던전 도면식 수평 스크롤 맵 (originRoot 좌표계 단일화·복도→방 그리기 순서·자동 포커스·콜백 주입·폴링형 갱신), DungeonManager 뷰 배선. M1 잔여 = 1-6(침식·광기 통행·SAN 던전 지속화) + 1-4(노드 화면 골격) |
| 개정 7 | 2026-07-23 | **씬 검증 이월분 일부 소화** — Dungeon 씬에서 카드 호버/드래그/드롭/툴팁 실동작 확인. 발견 이슈: 신규 씬의 기본 카메라가 원근(Perspective)이라 `ScreenToWorldPoint` 좌표가 근평면에 맺혀 콜라이더 판정 전체 실패 → **직교(Orthographic) 전환 + Test_Battle 카메라 값 복사로 해결.** 교훈 기록: 씬 신규 생성 시 카메라 직교·좌표계 확인 필수 (Hub 씬 생성 시 재발 주의). Canvas SS-Camera 설정은 정상이었음 (루트 Canvas 단일 구성 확인) |
| 개정 6 | 2026-07-23 | **1-5 완료 + SWStackStateMachine 채택 철회** — 구현 결과 상태 클래스가 전부 컨텍스트 메서드 위임(빈 껍데기)으로 확인되어 enum 화면 상태로 회귀 (돌아가는 구조를 유틸 채택 목적으로 복잡화하지 않는다 원칙). 재채택 기준 명문화(중첩 스택 실요구·상태별 Tick). DungeonManager 맵 통합: 조우 순차 목록 폐기 → `MoveToNode` 노드 이동 + 조우 풀 무작위(임시 조치), `GetAvailableNodes` 논할당 API(1-3 MapView 소비 예정), 보스 승리 = 던전 승리, 맵 복귀 지점 = 1-6 침식 전진 연결 예정 |
| 개정 5 | 2026-07-23 | **P2-M1 1-1·1-2 완료** — MapGraph(순수 데이터·직렬화 대비·논할당 조회·잠식 자동 제외)/MapGenerator(생성 5단계·광기 전용 방 자동 판정)/MapConfigData(전 수치 외부화)/도달성 불변식 검증 테스트. **다음: 1-5 DungeonManager SWStackStateMachine 전환 + 맵 통합** |
| 개정 4 | 2026-07-23 | **P2-D2 확정** — 구조: STS식 층×레인(12×3 축소 규격) + 잿불 침식(시간 압박 — Balance 토글) + 광기 간선(정신력 댄스의 던전 확장 — 토글) / 표현: 던전 도면식(방·복도·시드 지터·가로 심부 진행 — 다키스트 던전 참조), 데이터·표현 분리로 M1 계획 불변. 단순 분기 트리 기각 근거(노드 수 배증 vs 계획성 상실) 기록 |
| 개정 3 | 2026-07-23 | **P2-M0 완료** — DungeonState/DungeonManager 신설, BattleManager 주입 경로 전환(인스펙터 임시 조치 3종 → DungeonManager 상승 이동), 던전 수명 덱 확립(15-5 토대), 주입→검증→리셋 순서 계약, BattleTest 단독 경로 유지. **다음: P2-M1 (맵/런 루프 + SWStackStateMachine + P2-D2)** |
| 개정 2 | 2026-07-22 | **명명 확정: `Run` 계열 → `Dungeon` 계열** (`DungeonState`/`DungeonManager`, `05_Scripts/Dungeon/`, `EchoesOfAsh.Dungeon`) — 씬 이름(Dungeon)·상태 수명(P2-D6: 던전 씬 수명)과 일치, 향후 `HubManager`와 씬 기준 명명 대칭 (SanityGauge→SanityHolder 명명 개정 전례). **용어 매핑 확정: 기획 용어 "런" = 던전 1회 도전 = 코드 접두어 `Dungeon`** (M4 "턴 경계 = OnRoundEnded" 매핑과 같은 방식 — 문서의 "런 중 저장" 등 기획 용어는 유지) |
| 개정 1 | 2026-07-22 | P2-D6 씬 구성 확정(2씬 — 맵은 Dungeon 씬 내 화면) + **SWUtils 신규 기능 적용 방침**: `SWStackStateMachine`을 P2-M1 DungeonManager 화면 상태 머신에 채택(1-5 신설), 기존 완료 코드(TurnManager·CardDragController·EnemyAI)는 재작성 금지 — 검증된 발화 순서 계약 보호, Behavior는 Phase 3 보스 AI 후보로 보류 |