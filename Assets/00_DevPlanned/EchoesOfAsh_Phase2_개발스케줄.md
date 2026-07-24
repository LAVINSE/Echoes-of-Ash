# Echoes of Ash — Phase 2 개발 스케줄

> 기준 문서: 기획서 v3.2 (16장 Phase 2 체크리스트) + Phase 1 개발스케줄 개정 11 · 작성일: 2026-07-22 (초판)
> 목표: **런 1회 완주 + 거점 순환 + 파티가 가능한 상태** — 기간 잠정 12주 (기획 2~4개월 범위)

---

## 1. 착수 전제 (Phase 1 → Phase 2 인계)

### 인계 상태

- **Phase 1 코드 완료** (M0~M6-1) — 전투 1사이클·정신력·뷰/입력 전부 동작. 6-2~6-5(콘텐츠 데이터·밸런스·검증)는 리소스 작업 시점 이월 (구성안 확정 보관)
- **정신력 시스템 유지 확정** (코어 디자인 결정 — Phase 1 개정 11). 6-5는 **밸런스 게이트** — 수치·이벤트 효과 변경은 전부 에셋 교체로 대응, 코드 무수정
- **하이브리드 렌더링 확정** (D5 개정 7): 전장 = 월드 / 화면 부착 UI = Canvas(Screen Space - Camera). Phase 2 신규 화면(맵·거점·상점·편성)은 **전부 Canvas** — 결정 불필요
- **아트 부담 억제 유지**: Phase 2도 플레이스홀더 — 기능적 아트는 리소스 작업 시점

### Phase 1이 남긴 임시 조치 = P2-M0의 작업 목록

| 임시 조치 | 대체 |
|-----------|------|
| `BattleManager.startingCards` 인스펙터 주입 | 던전 상태(DungeonState)의 덱 주입 |
| `BattleManager.sanityEvents` 인스펙터 풀 | 던전 상태 경유 주입 |
| `BattleManager.characterData` 단수 필드 | 파티 구성 목록 (P2-M4에서 3인 확장) |
| `BattleManager.enemyEncounterData` 단일 지정 | 맵 노드 → 조우 참조 (P2-M1) |
| `EEnemyTargetRuleType.Aggro` 무작위 폴백 | 어그로 실구현 (P2-M5) |

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
- **임시 조치 기록:** **PartyData 이중 참조** — DungeonManager(던전 수위 판정: 임계값·시작 SAN) / BattleManager(전투 홀더 생성). SO 불변 데이터라 이중 참조 자체는 원칙 위반 아님, 단 **두 인스펙터는 동일 에셋 필수** (다르면 광기 판정 어긋남). **P2-M4 편성 화면 도입 시 DungeonState 경유 주입으로 일원화**
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

### P2-M3 — 제작 도구 + 상태이상 구조 (1주)

| # | 산출물 | 책임 |
|---|--------|------|
| 3-1 | `CardSystemWindow` | SWStatSystemWindow 패턴 복제 — 카드 열람/검색/생성 에디터 (콘텐츠 50장 제작의 도구 선행) |
| 3-2 | 상태이상 모듈 구조 | Study-ModuleSkill의 Effect 생명주기를 턴제로 번안 — 상태이상 정의 SO(수치·아이콘·설명) + 틱 로직 매핑 (15-4 하이브리드 구조). `IStatusReceiver` 실이행 |
| 3-3 | 검증 상태이상 1종 | 취약(받는 피해 +50%, N턴) — 파이프라인 관통 확인용 |

**DoD:** 에디터에서 카드 생성 가능. 상태이상 1종이 부여 → 틱 → 만료까지 전투에서 동작.

---

> **◆ 밸런스 게이트 (리소스 작업과 병행):** 6-2~6-5 이행 — 이벤트 5종·카드 15장·적 3종·조우 3종 제작(구성안 확정본) + 씬 검증 이월분(5-1 잔여·5-3~5-6) + 밸런스 튜닝 + 재미 확인. **이 시점 이후 P2-M4~M7의 콘텐츠 수치가 신뢰 가능해진다.**

---

### P2-M4 — 파티 시스템 (1.5주)

| # | 산출물 | 책임 |
|---|--------|------|
| 4-1 | 파티 3인 전투 | `CharacterEntity` 복수 스폰 (`characterData` → 목록), 공유 덱·공유 턴·공유 SAN 유지 (구조는 이미 공유 설계 — 확장만) |
| 4-2 | 전용 카드 소속 | 카드 ↔ 소유 캐릭터 연결 (P2-D3 결정) — **전투불능 시 전용 카드 드로우 풀 제외** (`DeckSystem` 필터) |
| 4-3 | 캐릭터 패시브 기초 | 검사 1인 + 테스트 캐릭터 — 패시브는 유물 트리거 구조 재사용 (P2-D4와 통합 설계) |
| 4-4 | 뷰 확장 | `PartyStatusView` 3인 배치, 적 대상 선정 표시(누굴 노리는지), 파티 편성 화면 기초 (Canvas) |

**DoD:** 3인 파티로 전투 1사이클 — 1인 전투불능 시 전용 카드가 드로우에서 제외되고, 부활 규칙 자리만 확보.

### P2-M5 — 타겟팅 완성 (1주)

| # | 산출물 | 책임 |
|---|--------|------|
| 5-1 | 어그로 시스템 | 피해 기여 기반 어그로 수치 (산정식 = 밸런스 영역, Balance 외부화) — `Aggro` 무작위 폴백 대체 |
| 5-2 | 도발·지정 효과 블록 | 적 대상 선정 개입 카드 — 지속 턴 수는 상태이상 구조(P2-M3) 재사용 |

**DoD:** 규칙 3종(랜덤/어그로/지정) 전부 실동작 + 도발 카드로 대상 조작 확인.

### P2-M6 — 거점 + 드랍/회수 (1.5주)

| # | 산출물 | 책임 |
|---|--------|------|
| 6-1 | 거점 씬 (Canvas) | 기본 시설 골격 + 막사 캐릭터 영입 기초 |
| 6-2 | 드랍 테이블 | 조우·노드별 드랍 데이터 (설계도·시설 재료 포함) |
| 6-3 | 회수 시스템 | 절충형 + **보관 노드 전송** (P2-M1 골격에 연결) — 게임 오버 시 회수 실패/보존 구분 |

**DoD:** 전투 보상 → 드랍 → 보관 전송 or 소지 → 런 종료 시 회수 판정 → 거점 반영.

### P2-M7 — 콘텐츠 + 시스템 잔여 (2.5주)

| # | 산출물 | 책임 |
|---|--------|------|
| 7-1 | 공용 카드 50장 (반응형 포함) | `CardSystemWindow`로 제작 — 코드 0줄 원칙 검증 |
| 7-2 | 유물 20개 | 트리거 구조(P2-D4) + 정신력 연동 포함 |
| 7-3 | 적 12종 + 조우 테이블 | SAN 압박 행동 포함 — `SpawnRange` 조우 풀 실가동 |
| 7-4 | 이벤트 10개 / 상점 / 보상 화면 | 데이터 기반 선택지·구매·카드 보상 — **던전 구성 데이터(챕터 SO) 신설: 노드 이벤트 `타입 → 풀` 매핑 흡수 (개정 11 결정)** |
| 7-5 | 카드 해금 2종 | 발견형 자동 해금 + 제작형 설계도 해금 (메타 저장 연동) |
| 7-6 | 보스 1개 | HP 페이즈 패턴 활용 (구조는 M4에서 기완성) |

### P2-M8 — 통합 검증 (1주)

**DoD (Phase 2 전체):** 거점 → 파티 편성 → 런 1회 완주(보스 처치 or 게임 오버) → 회수 → 거점 복귀 → 해금 반영이 저장/복원 포함 완주된다.

---

## 3. 모듈 ↔ 산출물 매핑 (신규 모듈)

| 모듈 | Phase 2 산출물 | 폴더 |
|------|----------------|------|
| 런 | `DungeonState`, `DungeonManager` | `05_Scripts/Dungeon/` |
| 맵 | `MapGenerator`, 노드 데이터, 맵 화면 | `05_Scripts/Map/` |
| 저장 | 저장 스키마·마이그레이션 (SWSaveDataManager 활용) — `DungeonSaveData`/`DungeonSaveService` | `05_Scripts/Save/` |
| 상태이상 | 상태이상 SO + 틱 매핑 | `05_Scripts/Status/` |
| 유물 | 유물 SO + 트리거 리스너 | `05_Scripts/Relic/` |
| 거점 | 시설·영입 | `05_Scripts/Hub/` |
| 드랍·회수 | 드랍 테이블·회수 판정·보관 전송 | `05_Scripts/Drop/` |
| (기존) 전투/뷰 | 파티 3인·타겟팅 확장 — 기존 폴더 증축 | `Battle/`, `View/`, `View/UI/` |

통신 원칙 유지: C# event 우선·구독/발화 순서 결정성 (유물 다중 발동 = **획득 순 고정** — 기획서 15-2 명시), 뷰 배선 예외는 조립 지점만 (`BattleManager` + 신규 `DungeonManager`).

---

## 4. 조기 결정 필요 사항

| # | 결정 | 시점 | 상태 |
|---|------|------|------|
| P2-D1 | **시드 결정성 범위**: 같은 시드 = 같은 런 보장 여부 (런 중 저장 방식과 직결) | P2-M2 전 | ✅ 확정 (개정 11) — **전체 상태 스냅샷.** 무작위 소비 지점 다수(조우·이벤트 추첨·광기 판정·셔플·침식)로 "시드+행동 로그"는 소비 순서 영구 계약 요구 → 코드 수정마다 재현 파손. 시드는 맵 생성 기록·재현용으로만, **재개 후 난수 비연속** (같은 시드 재설정 시 소비된 난수열 재등장 = 세이브스커밍 여지 차단) |
| P2-D2 | **노드 그래프 구조** | P2-M1 착수 시 | ✅ 확정 — **구조: STS식 층×레인 그래프** (축소 규격 12층×3레인 — 노드 수 선형·전체 공개·경로 계획 성립, 단순 분기 트리는 노드 수 배증 또는 계획성 상실로 기각) **+ 잿불 침식** (이동마다 입구층부터 잠식 — 시간 압박 축, 속도는 Balance 외부화·0이면 순정 STS) **+ 광기 간선** (광기 상태에서만 열리는 간선·노드 — 정신력 댄스의 던전 확장, 토글 가능. 의도적 광기 진입 수단 필요 — M1 규칙 설계). **표현: 던전 도면식** — 노드=방·간선=복도, 시드 기반 좌표 지터(결정성 유지), **가로 심부 진행**(입구→폐허 심부 — 침식=입구부터 타들어오는 재). 데이터(그래프)와 표현(MapView) 분리 — 다키스트 던전 참조. 플레이스홀더=사각 방+통로, 양피지·지명·랜드마크는 리소스 단계 |
| P2-D3 | **전용 카드 소속 표현**: CardData가 소유 캐릭터 참조 vs CharacterData가 전용 카드 목록 보유 | P2-M4 전 | 미결 — 권장: CharacterData 보유 (드로우 제외 필터가 파티 구성만 보면 됨) |
| P2-D4 | **유물 트리거 구조**: 전투 이벤트 구독형 리스너 vs 훅 열거 매핑 (캐릭터 패시브와 공용) | P2-M7 전 (P2-M4 패시브와 통합 설계) | 미결 |
| P2-D5 | 어그로 산정식 (피해 기여 가중 등) | P2-M5 | 밸런스 영역 — Balance 외부화 |
| P2-D6 | **씬 구성** | P2-M0 착수 시 | ✅ 확정 — **2씬: Hub(거점) + Dungeon(던전 = 런 1회)**. 맵은 씬이 아니라 Dungeon 씬 내 Canvas 화면 — 맵 ⇄ 전투 ⇄ 노드 화면(이벤트/상점/보관)을 씬 로드 없이 전환 (전투 인프라 1회 구성·런 템포 보존·DungeonState 수명 = Dungeon 씬 수명). `DungeonManager` = Dungeon 씬 내 화면 상태 머신 + 거점⇄던전 씬 전환 소유 |

---

## 5. 리스크와 완충

- **콘텐츠 물량이 병목** (P2-M7) — 카드 50·유물 20·적 12·이벤트 10은 코드가 아니라 제작 시간. → 도구 선행(P2-M3 CardSystemWindow)으로 완충 + "코드 0줄 추가" 원칙이 깨지는 카드는 즉시 구조 재점검
- **저장 결정성 미결 재작업** — ~~P2-D1을 P2-M2 전에 반드시 확정~~ → **확정 완료 (스냅샷 — 개정 11).** 잔여 리스크: 강화 외 카드 가변 상태(P2-M7 이후) 추가 시 저장 스키마 버전 증가 + 마이그레이션 필수
- **파티 확장 파급** — 공유 SAN·피격 SAN 판정(피격자 개인 HP 기준)·전투불능 드로우 제외가 맞물림. P2-M4에서 `Test_Battle` 3인 버전으로 단독 검증 후 통합. **PartyData 이중 참조(DungeonManager/BattleManager — 동일 에셋 필수)도 이 시점에 주입 경로로 일원화**
- **메타 저장 병용** — SWSaveDataManager는 정적 단일 currentData 구조. 메타 저장(해금/거점) 도입 시(P2-M6/M7) 던전 슬롯과의 SetData 순서 규약 필요
- **범위 방어** — Phase 3 항목 침범 금지: 적 SAN 보스 프로토타이핑(**SWUtils Behavior는 이 시점의 보스 AI 후보로 보류** — 현행 데이터 주도 패턴 순환에는 과함), 캐릭터 2·3번, 유물 21개 이상, 이벤트 11개 이상, Ascension, 튜토리얼, 아트 완성. **밸런스 게이트 전에 P2-M7 수치 확정 금지** (제작은 가능, 확정은 게이트 후)

---

## 6. Phase 3 예고 (참고)

콘텐츠 전량(카드·유물·적·이벤트), 적 SAN 보스 프로토타이핑, 캐릭터 2번(재의 궁수), 튜토리얼/FTUE, 픽셀아트·사운드, Steam 페이지. — 상세 계획은 Phase 2 완료 후.

---

## 개정 이력

| 개정 | 일자 | 내용 |
|------|------|------|
| 초판 | 2026-07-22 | Phase 2 스케줄 수립 — 메타 계층 우선 순서(P2-M0~M3), 밸런스 게이트 병행 배치, Phase 1 임시 조치 5종 = P2-M0 작업 목록화, 조기 결정 P2-D1~D5 정의 |
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