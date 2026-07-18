# Echoes of Ash — Phase 1 개발 스케줄

> 기준 문서: 기획서 v3.2 (16장 Phase 1 체크리스트) · 작성일: 2026-07-07 (개정 5 — 2026-07-18, M5 5-3 게이지 뷰 완료 반영)
> 목표: **전투 1사이클 + 정신력이 플레이 가능한 상태 (1인 기준)** — 기간 잠정 8주

---

## 1. 현황 요약

### 완료 (v6 적용 기준)

| 구분 | 내용 |
|------|------|
| 데이터 구조 | `CardData` / `CharacterData` / `EnemyData` / `MadnessEventData` / `PartyData` / `BattleBalanceData` (전부 `SWIdentifiedObject`·`SWScriptableObject` 기반, 수치 외부화 완료). 경계 원칙: **전투 규칙 = BattleBalanceData, 전투 주체 속성 = 각자의 Data SO** (파티 SAN은 `PartyData`, 적 SAN은 `EnemyData` — 대칭 구조) |
| 정신력 모듈 | `Sanity/SanityHolder.cs` — `ISanityHolder` 구현체 (파티/적 공용, SWStat 클론 래핑, 임계값 교차 판정: SAN < 임계값 = 광기). `Test/SanityHolderTest.cs` 검증 완료 (M1 DoD 통과) |
| 효과 시스템 | `EffectBlock` 추상 베이스 + 기본 블록 9종 + `EffectContext` (카드/적/광기 이벤트 공용 파이프라인) + **`EffectExecutor` 실행기 (M3)** |
| 인터페이스 | `ITargetable` / `IDamageable` / `ISanityHolder` / `IStatusReceiver` 정의 + 전투원 구현 |
| 스탯 | HP/SAN을 `SWStatOverride`로 전환 (SWStat 채택 확정) |
| 의도 시스템 데이터 | `EIntentType` + 블록별 `IntentContribution` 자동 유도, `EnemyActionData.GetIntentTypes()` |
| 전투원 | `Battle/Base/BattleEntity.cs` (SWMonoBehaviour 기반 공통 베이스 — HP SWStat 클론 직접 보유, 방어막 int, IDamageable/ITargetable/IStatusReceiver 구현, ResetEntity 정리 패턴) + `CharacterEntity` / `EnemyEntity`(개체별 SanityHolder 위임, ActionIndex) + `IDamageCalculator`/`DefaultDamageCalculator`. `Test/BattleEntityTest.cs` 검증 완료 (M2 DoD 통과) |
| 카드 실행 계층 (M3) | `Card/CardInstance.cs` (런타임 래퍼) + `Deck/DeckSystem.cs` (3존 관리·SWRandom 셔플) + `Battle/ApSystem.cs` (지급/이월/클램프 — 수치는 BattleBalanceData) + `Effect/EffectExecutor.cs` (컨텍스트 조립·블록 순회 단일 창구) + `Battle/CardPlayService.cs` (사용 파이프라인). `Test/CardPlayTest.cs` 검증 완료 (M3 DoD 통과) |
| 턴 흐름 + 적 AI (M4) | `Battle/TurnManager.cs` (턴 상태 머신 — 발화 순서 고정) + `Battle/EnemyAI.cs` (패턴 순환·의도 선정·페이즈/광기 전환) + `Battle/TargetResolver.cs` (대상 목록 구성) + `Battle/BattleManager.cs` (조우 셋업·모듈 배선·승패 판정·종료 정리) + `ETurnPhase`/`EBattleResult` (`GameEnum.cs`에 병합). `Test/BattleTest.cs` (`Test_Battle` 씬) 검증 완료 (M4 DoD 통과) |
| **전투 뷰/입력 (M5 5-1~5-3)** | `View/CardView.cs` (카드 표시 + 호버 강조 + SortingGroup 정렬) + `View/HandView.cs` (손패 부채꼴 배치·SWPool 풀링·`OnHandChanged`/`OnApChanged` 구독) + `View/EnemyView.cs` (적 플레이스홀더 — 드롭 판정 콜라이더·사망 처리 + **5-3 증축: HP 바 + 얇은 SAN 보조 바 + 방어막 표시, Release 신설**) + `View/BezierArrowsView.cs` (대상 지정 화살표 — 뷰 전용, 외부 구동) + `Battle/CardDragController.cs` (호버→픽업→드래그→드롭 단일 입력 주체) + `Data/EnemyEncounterData.cs` (조우 구성·배치 SO — M6-3 선행) + **`View/GaugeView.cs`** (공용 채움 바 — FillRoot 스케일 방식·임계값 마커·에디터 라이브 테스트) + **`View/PartyStatusView.cs`** (파티 HP/방어막 + 공유 SAN 경계 표시) + `BattleManager` 뷰 배선(조우 스폰·`handView`/`partyStatusView` Init/Release) + `Test/PartyStatusViewTest.cs`. 드래그→화살표→드롭→피해→턴 순환→재드로우 실동작 검증 완료 |
| 데이터 보강 | `EnemyData.StartSanity` 필드 추가 — 광기 상태 등장 적 지원 (PartyData와 대칭) / `BattleBalanceData`에 AP 그룹 추가 (`apPerTurn` 3, `apCarryOverMax` 2) / **`EnemyEncounterData` 신설** (개정 4 — 조우 = 적 구성+배치+행동 순서의 단일 소유 데이터) |
| 인프라 | SWUtils v1.0.11 통합 (SWRandom 시드 고정, SWLog, SWSubClassSelector, SWIODatabase, **SWPool** 사용) |

### 미구현 (이 문서의 대상)

전투 UI 잔여(의도 표시/툴팁/턴 종료 버튼·AP 표시·광기 연출 — 5-4~5-6), 5-1 마무리(AP·타입 텍스트, 반응형 마커 스프라이트, 한글 TMP 폰트 폴백, 덱/버림 더미 카운터), 광기 이벤트 러너(`MadnessEventRunner`), 테스트 콘텐츠 데이터(카드 15장·적 3종·광기 이벤트 4~6종·조우 구성).

---

## 2. 마일스톤 로드맵 (M0 ~ M6, 잠정 8주)

> 순서는 의존성 순.

<!-- ⚠ M0 ~ M3 절: 개정 5 변경 없음 — 기존 원문 그대로 유지 (아래 M4부터 이어 붙일 것) -->

### M0 ~ M3 — ✅ 완료 (원문 유지 — 개정 5 변경 없음)

---

### M4 — 턴 흐름 + 적 AI ✅ 완료

- 산출물: `Battle/TurnManager.cs` / `Battle/EnemyAI.cs` / `Battle/TargetResolver.cs` / `Battle/BattleManager.cs` / `Test/BattleTest.cs` (`Test_Battle` 씬). `ETurnPhase`·`EBattleResult`는 별도 파일 대신 **`Enum/GameEnum.cs`에 병합** (기존 enum 단일 파일 원칙 유지)
- **명명 확정:** 계획명 `TargetingResolver` → **`TargetResolver`** (M1 SanityGauge→SanityHolder와 같은 명명 개정 전례). 적 행동 실행은 `EnemyAI.PlayAction`, 적 행동 단계 이벤트는 `OnEnemyActionsStarted`(복수형)
- **클래스 성격 경계 확정:** `TurnManager` / `EnemyAI` / `TargetResolver`는 **순수 C# 클래스** (생성자 주입 + 전투 1회 수명 — ApSystem/DeckSystem/CardPlayService 계열과 동일). 씬 부착 컴포넌트는 `BattleManager` / `BattleTest`(SWMonoBehaviour)만. ⚠ 순수 클래스 3종에 MonoBehaviour 상속 금지 — `new` 생성 시 Unity fake null로 흐름 전체가 조용히 죽는다 (실제 발생 → 수정)
- **용어 확정 ① — "턴 경계" 코드 매핑:** 기획 문서의 "턴 경계" 판정 지점은 코드에서 **라운드 종료**로 표기 — `TurnManager.OnRoundEnded` 이벤트 / `EnemyAI.PrepareNextTurn()` 메서드. 광기·HP 페이즈 패턴 전환은 이 지점에서만 반영 (D2 이행)
- **용어 확정 ② — 대상 처리 3단계:** **구성**(`TargetResolver.Resolve` — 지정 방식 → 대상 목록 생성) / **검증**(`CardPlayService.AreTargetsValid` — 최종 유효성 관문) / **선정**(`EnemyAI.SelectTargets` — 규칙 기반 파티 대상 선택). 세 책임은 분리 유지하되 `BattleManager.PlayCard`가 구성 → 검증 → 실행의 단일 조립 지점
- **발화 순서 고정 (기획서 15-2):** 턴 시작 = `OnTurnStarted`(엔티티 정리) → AP 지급 → 드로우 → `OnTurnStartHook`(광기 이벤트 판정 지점 — M6 `MadnessEventRunner` 구독 예정) / 턴 종료 = 손패 버림 → `OnTurnEnded` → 적 행동(`OnEnemyActionsStarted` — 스폰 순서 고정 순회) → `OnRoundEnded`(패턴 재평가·의도 선정). 각 이벤트 발화 후 `BattleEnd` 체크로 중간 승패 판정 시 즉시 중단
- **적 패턴 우선순위:** 광기 패턴(비어 있으면 기본 유지) → HP 페이즈 패턴(조건 만족 중 최저 임계값 = 가장 진행된 페이즈) → 기본 패턴. 패턴 전환 시 `ActionIndex` 초기화. 의도는 예고대로 실행되고 전환은 다음 라운드 의도부터 반영
- **잠정 규칙 (Balance 외부화 후보):** 방어막 소멸 — 파티는 자기 턴 시작 시, 적은 자기 행동 직전 (STS 표준)
- **임시 조치 (Phase 2 대체 예정):** ① `BattleManager.startingCards` 인스펙터 주입 → 런 루프 도입 시 런 상태(RunState) 주입으로 변경 ② `EEnemyTargetRuleType.Aggro`는 무작위 폴백 (도발/어그로 수치는 Phase 2) ③ `characterData` 단수 필드 → 파티 3인 시 목록으로 변경
- **DoD 통과:** 전투 시작 → N턴 → 승리/패배 완주 확인. 적 3체 각자 패턴 순환, 광기 전환 시 다음 라운드부터 패턴 교체 확인 (`Test_Battle`, 시드 고정)

---

### M5 — 전투 UI / 입력 (1.5주) ◀ 진행 중 (5-1·5-2·5-3 완료 — 개정 5)

기획서 14-5 전투 화면의 Phase 1 범위 구현. 로직은 이미 완성 상태이므로 이 단계는 표시/입력만.

| # | 산출물 | 책임 | 상태 |
|---|--------|------|------|
| 5-1 | 손패 뷰 | 카드 표시(이름/AP/타입), 드로우·버림 반영(`OnHandChanged` 구독), 사용 불가(AP 부족) 표시(`CanPlay` + `OnApChanged`), 호버 강조 | ✅ (잔여: AP·타입 텍스트/마커 스프라이트/한글 폰트/`OnPileChanged` 더미 카운터 → 5-6에 병합) |
| 5-2 | **드래그 타겟팅** | 단일 대상 카드 드래그 → 화살표 → 적 위 드롭 (STS 표준 UX). 비대상 카드는 사용 기준선 방식. `BattleManager.PlayCard`의 target 경로 연동 | ✅ |
| 5-3 | 게이지 뷰 | 파티 HP/방어막, **공유 SAN(경계 표시 포함)**, 적별 HP + **얇은 SAN 보조 바** (기획서 3-2 UI 원칙) — `OnSanityChanged`/`OnDamaged` 이벤트 구독 | ✅ |
| 5-4 | 의도 표시 뷰 | `EnemyAI.OnIntentChanged` 구독 — `GetIntentTypes()` 복수 아이콘 + `GetIntentDamageValue()`/`GetIntentSanityPressureValue()` 수치 | ◀ 다음 |
| 5-5 | 카드 툴팁 | 반응형 카드는 평정/광기 양쪽 효과 표시 (`GetDescription()` 조합), 현재 구간 강조 | |
| 5-6 | 턴 종료 버튼(`TurnManager.CurrentPhase` 연동) / AP 표시(`OnApChanged` 구독) / 덱·버림 카운터(`OnPileChanged`) / 광기 진입 연출(채도·비네팅 가볍게) / **게이지 보간 연출 (5-3 이월)** | 연출은 최소 — 아트 부담 억제 원칙 | |

**DoD:** 마우스만으로 전투 1사이클 완주 가능. 광기 진입 시 반응형 카드의 표시가 실시간 전환된다.

#### 5-1·5-2 완료 기록 (2026-07-16)

- **산출물:** `View/CardView.cs` / `View/HandView.cs` / `View/EnemyView.cs` / `View/BezierArrowsView.cs` / `Battle/CardDragController.cs` / `Data/EnemyEncounterData.cs` + `BattleManager` 뷰 배선(조우 스폰·`handView.Init/Release`) + `CardView`·`EnemyView`·`BezierArrows` 프리팹
- **명명·모듈 확정 — UI 모듈 → View 모듈:** 손패 포함 전투 화면 전체가 Canvas가 아닌 **월드 스페이스 스프라이트**로 확정(D5)되어, 계획명 `UI` 모듈을 **`View`** 로 개정 — 폴더 `05_Scripts/View/`, 네임스페이스 `EchoesOfAsh.View` (렌더링 기술이 아닌 "표시 전용 책임"만 담는 이름 — SanityGauge→SanityHolder와 같은 명명 원칙). 화살표는 `BezierArrows` → `BezierArrowsView`
- **뷰 계층 원칙:** 뷰는 로직 없음 — 이벤트 구독 + 표시만. 의존성은 조립 지점(`BattleManager`)이 `Init(...)`으로 주입, 전투 종료 시 `Release()`로 구독 해제 (순수 클래스의 전투 1회 수명에 뷰가 죽은 참조를 붙들지 않도록 Init/Release 쌍 패턴). 뷰 참조는 전부 null 허용 — 뷰 없이도 로직 테스트(OnGUI) 경로 유지
- **입력 단일 주체:** `CardDragController`가 호버→픽업→드래그→드롭의 유일한 입력 주체. Input System `Pointer` 직접 폴링 (InputAction 전환은 게임패드/단축키 도입 시점으로 유보). 카드 질의는 논할당 `OverlapPoint`(ContactFilter2D + 재사용 버퍼) 프레임당 1회 — 호버와 픽업이 질의 공유. 판정 결과는 `BattleManager.PlayCard`(구성→검증→실행 조립 지점)에만 위임
- **정렬 체계 확정:** 카드 내부 요소 겹침은 소팅 오더가 z보다 우선하는 문제(이웃 카드 텍스트 관통)로 인해 **카드 루트 SortingGroup**으로 해결 — 카드 내부는 그룹 내 오더(프레임 0/아이콘 1/텍스트·마커 2), 카드 간은 z 간격(`depthStep`). 소팅 레이어: `Entity < Card < CardDrag < Arrow` / 물리 레이어: `Card`, `Enemy`. 호버 강조는 `visualRoot` 자식 확대(콜라이더는 루트 — 판정 플리커 방지) + 그룹 오더 승격
- **`EnemyEncounterData` 신설 (M6-3 조우 구성 선행):** 조우 = 적 구성 + 배치 위치 + 행동 순서의 단일 소유 SO — **항목 순서 = 스폰 순서 = 적 행동 순서** (M4 결정성 유지, 조우 에셋에서 행동 순서 제어 가능). `BattleManager.enemyDatas` 목록 대체. Phase 2 런 루프의 맵 노드 → 조우 참조 및 `EnemyData.SpawnRange` 결합 조우 풀 구성의 토대
- **풀링 경계 확정:** 손패 `CardView`는 **SWPool** 사용 (`Prewarm(MaxHandSize)`, Pool Monitor 관측 가능). 기준: 뷰 전용 프리팹은 코드 직접 참조 + SWPool, 소비자가 여럿인 공용 연출 리소스(데미지 텍스트·이펙트 — 5-6 이후)는 `SWPoolCatalog` 등록 방식
- **DoD 통과 (5-1·5-2 범위):** 호버 강조 → 단일 대상 드래그 → 베지어 화살표 → 적 드롭 → 피해 적용 → 손패 재배치 → 턴 종료 → 재드로우 실동작 확인
- **잔여 (5-3와 병행 → 5-6으로 이월):** CardView 프리팹 AP·타입 텍스트 연결, SanityMarker 스프라이트 부착, 한글 TMP 폰트 에셋 + 폴백 등록, `HandView.Prewarm` null 체크 순서 확인. `OnPileChanged` 덱/버림 카운터는 5-6으로 병합 (5-1 잔여는 프리팹 변동 가능성으로 보류 결정 — 개정 5)
- **구조 노트:** `CardDragController`는 현재 `05_Scripts/Battle/` 소속 — Battle(로직) → View 의존 방향이 생기므로 `View/` 이동 검토 (조립 지점 `BattleManager`만 예외 유지 원칙)

> M5 착수 노트: 적 행동 사이 연출 딜레이가 필요해지면 `TurnManager`를 Mono로 바꾸지 말고 **BattleManager(이미 Mono)가 코루틴으로 흐름을 구동**하는 방향 — 로직(상태 머신)과 타이밍(연출) 분리 유지.

#### 5-3 완료 기록 (2026-07-18)

- **산출물:** `View/GaugeView.cs` / `View/PartyStatusView.cs` / `View/EnemyView.cs` 증축 / `Test/PartyStatusViewTest.cs` + `BattleManager` 뷰 배선(`partyStatusView` — handView와 대칭 Init/Release) + Gauge·PartyStatus 프리팹
- **`GaugeView` 공용 컴포넌트 확정:** HP/SAN/파티/적 게이지 전부 단일 컴포넌트 재사용 (SRP — "값 하나를 바+텍스트로 표시"). `SetValue(current, max)` / `SetFillColor(color)` / `SetThreshold(threshold, max)` 3개 공개 API
- **채움 방식 확정 — FillRoot 스케일:** 스프라이트 피벗은 에셋에 종속된 값(내장 Square는 Center 고정·변경 불가)이라, **빈 부모 Transform(FillRoot, 바 왼쪽 끝 원점)을 피벗 대용**으로 두고 X 스케일 0~1로 채움 표현. 플레이스홀더든 향후 아트 교체든 피벗 설정과 무관하게 왼쪽 고정 보장. **정렬 규칙: Fill 로컬 X = 실제 폭 ÷ 2 = barWidth ÷ 2** (폭 변경 시 3값 세트 갱신 필요 — "Fill 자동 정렬" 에디터 버튼으로 자동화, `sprite.bounds` 기준이라 아트 교체에도 유효)
- **임계값 마커 (기획서 14-5 "경계 표시"):** `SetThreshold`가 `barWidth × (threshold/max)` 위치에 마커 배치. 파티 SAN 게이지만 사용 — SAN < 임계값 = 광기(M1 계약)이므로 마커 왼쪽 = 광기 구간. "정신력 댄스" 판단(임계값까지 여유 즉시 파악)의 핵심 표시
- **뷰 패턴 유지:** Init/Release 쌍 · 이벤트 구독+표시만 · 참조 전부 null 허용 · **초기 1회 직접 갱신** (SanityHolder/BattleEntity 이벤트는 변경 시에만 발화하므로 Init 시점 상태를 핸들러 직접 호출로 반영)
- **`EnemyView` Release 신설:** 기존 OnDied 단독 해제 → HP/방어막/SAN 이벤트 포함 `Release()`로 통합. `EnemyEntity`의 이벤트 위임 add/remove가 holder null 가드를 하므로 `ResetEntity`(holder 해제) 이후 뷰 해제가 와도 안전
- **에디터 테스트 체계 (`#if UNITY_EDITOR` — 빌드 제외):** GaugeView 내 Range 슬라이더(`testFillRatio`/`testThresholdRatio`) + `OnValidate` 라이브 갱신. `OnValidate` 내 렌더러/TMP 직접 갱신은 SendMessage 경고 유발 → **`EditorApplication.delayCall` 지연 적용 + fake null 가드** (M4 fake null 사례와 동일 메커니즘). `testLiveUpdate` 기본 꺼짐 — OnValidate는 모든 인스펙터 변경·리로드에 호출되므로 실전 배선 값 덮어쓰기 방지. 이벤트 구독 경로는 `PartyStatusViewTest`(실제 CharacterEntity·SanityHolder 생성)로 별도 검증
- **표시 특성 기록:** 게이지 계단식 이동 = int 양자화 정상 동작 (HP/SAN이 int라 실전도 1/max 단위 이산 표시). 부드러운 보간 연출은 **5-6 이월** — SetValue는 목표만 저장, 표시 스케일은 Update에서 MoveTowards 보간 (로직 즉시·표시 지연 표준 패턴, API 불변)
- **DoD 확인 필요 ⚠:** 프리팹 단독 검증(채움 방향·마커·색 전환)은 완료. `Test_Battle` 통합 검증(카드 사용 → 파티/적 게이지 실시간 반영, 광기 전환 색·라벨 전환)은 실행 후 이 표기를 제거할 것

---

### M6 — 광기 랜덤 이벤트 + 통합 검증 (1주)

| # | 산출물 | 책임 |
|---|--------|------|
| 6-1 | `Sanity/MadnessEventRunner.cs` | **`TurnManager.OnTurnStartHook` 구독** — 광기 구간이면 `GetMadnessEventChance(현재SAN, PartyData.SanityThreshold)` 판정(`SWRandom.Chance`) → `PickRandomMadnessEvent()` → Executor 실행 + UI 알림 (확률 곡선=룰은 Balance, 임계값=주체 속성은 PartyData에서 주입) |
| 6-2 | 광기 이벤트 데이터 4~6종 | 부정(자해 5 / 손패 1장 버림 / SAN -5) + 긍정(AP +1 / 드로우 +1) — 가중치는 부정 합 > 긍정 합 |
| 6-3 | 콘텐츠 데이터 | **카드 15장**(반응형 5~6장 포함), **적 3종**(SAN 압박형 1종 포함, 광기 패턴 1종만 정의), **조우 에셋**(`EnemyEncounterData` 1~3체 구성 — 구조는 M5에서 선행 완료, 여기서는 콘텐츠만) |
| 6-4 | 밸런스 1차 조정 | `BattleBalanceData` 수치만으로 튜닝 (코드 수정 0 확인 — 데이터 주도 검증 겸함) |
| 6-5 | **재미 검증** (부록 체크리스트) | ① 정신력이 카드 효과를 바꾸는 결정축인가 ② 광기 = 확정 이득 + 리스크 도박인가 ③ "정신력 댄스"가 재미있는가 ④ 모든 적 SAN 노출이 템포를 해치지 않는가 (광기 보상 임시 원칙 — `MadEnemyDamageTakenMultiplier` — 켜고/끄고 비교) |

**DoD:** Phase 1 목표 달성 — 1인 기준 전투 1사이클이 재미 판단 가능한 상태로 플레이된다. 검증 결과에 따라 Phase 2 진입 or 코어 재조정 결정.

---

## 3. 모듈 ↔ 산출물 매핑 (기획서 15-2 기준)

| 모듈 | Phase 1 산출물 | 폴더 |
|------|----------------|------|
| 정신력 | `SanityHolder` ✅, `MadnessEventRunner` | `05_Scripts/Sanity/` |
| 전투 | `BattleEntity`(Character/Enemy) ✅, `ApSystem` ✅, `CardPlayService` ✅, `IDamageCalculator` ✅, `BattleManager` ✅, `TurnManager` ✅, `EnemyAI` ✅ | `05_Scripts/Battle/` |
| 덱·카드 | `DeckSystem` ✅, `CardInstance` ✅ | `05_Scripts/Deck/`, `05_Scripts/Card/` (계획 `Data/Runtime/`에서 변경 — 개정 2) |
| 타겟팅 | `TargetResolver` ✅ (계획명 `TargetingResolver`에서 개정 — `Resolve` = 대상 목록 **구성**) | `05_Scripts/Battle/` (Phase 2에 분리 검토) |
| 효과 | `EffectExecutor` ✅ (+ 기존 Effect 모듈) | `05_Scripts/Effect/` |
| 뷰 (계획명 UI — 개정 4) | `CardView` ✅, `HandView` ✅, `EnemyView` ✅(게이지 증축 ✅ — 의도는 5-4에서 증축), `BezierArrowsView` ✅, **`GaugeView` ✅, `PartyStatusView` ✅** (개정 5), 의도/툴팁 | `05_Scripts/View/` (렌더링 = 월드 스프라이트, Canvas 비의존 — D5) |
| 입력 | `CardDragController` ✅ | 현재 `05_Scripts/Battle/` — `View/` 이동 검토 (구조 노트) |
| 조우 데이터 | `EnemyEncounterData` ✅ (개정 4 — M6-3 구조 선행) | `05_Scripts/Data/` |

모듈 간 통신: 직접 참조 최소화 — 상태 변화 알림은 C# event(인터페이스에 이미 정의) 우선, 모듈 경계를 넘는 광역 알림(전투 종료 등)만 `SWEventBus` 사용 검토. **구독·발화 순서 결정성 유지** (기획서 15-2 — M3 적용 예: 덱 변경 알림은 항상 손패 → 더미 순 발화 / M4 적용 예: 턴 이벤트 발화 순서 고정 + 적 행동 스폰 순서 고정 순회 / M5 적용 예: 조우 항목 순서 = 스폰 순서 = 행동 순서). 뷰 배선 예외: 조립 지점 `BattleManager`만 뷰를 직접 참조해 Init/Release 주입 — 로직 모듈은 뷰를 모른다.

---

## 4. 조기 결정 필요 사항

| # | 결정 | 시점 | 상태 |
|---|------|------|------|
| D1 | 전투원 스탯 보유 방식: `SWStats` 컴포넌트 vs SWStat 클론 직접 보유 | M2 착수 시 | ✅ 확정 — 클론 직접 보유 |
| D2 | 적 SAN 광기 전환의 "턴 경계 지연" 처리 위치 (게이지 vs 적 AI) | M1 | ✅ 확정 — 적 AI 책임 (홀더 순수 유지) · **M4 이행 완료** (`EnemyAI.PrepareNextTurn` — 라운드 종료 시점 판정) |
| D3 | 시드 결정성 범위: Phase 1부터 `SWRandom.SetSeed` 런 시드 적용 여부 | M3 (DeckSystem 셔플 전) | ✅ 확정 — 적용 (셔플·무작위 버림·무작위 대상 전부 SWRandom 일원화, 테스트 시드 고정 옵션) |
| D4 | 광기 보상 임시 원칙(`MadEnemyDamageTakenMultiplier`) 1차 채택값 | M6 | 미결 — 1.0(비활성)과 1.25 비교 플레이 |
| D5 | 전투 화면 렌더링 방식: uGUI Canvas vs 월드 스페이스 스프라이트 | M5 착수 시 | ✅ 확정 — **월드 스프라이트** (손패 포함 전체. SpriteRenderer + 월드 TMP + Collider2D 판정, 좌표 변환 계층 불필요. Canvas는 향후 화면 전환/메뉴 등 순수 2D UI에만 검토) |

---

## 5. 리스크와 완충

- **UI 작업량 과소평가** (M5) — 드래그 타겟팅이 통상 예상보다 오래 걸림. → **5-2 완료로 최대 리스크 해소 (개정 4). 5-3까지 완료로 잔여 UI는 소규모 (개정 5).** 잔여 지연 시 툴팁(5-5)·연출(5-6)을 M6 이후로 이월하는 완충은 유지.
- **재미 검증 실패 시** (M6-5) — 수치 문제면 `BattleBalanceData`로 해결(빠름), 구조 문제(2단계 구간이 얕음 등)면 Phase 2 진입을 멈추고 3-1 재설계. 이 판단을 위해 M6에 검증 시간을 확보해둔 것.
- **범위 방어** — Phase 1에 다음을 넣지 않는다: 파티 3인, 맵/노드, 유물, 상점/이벤트, 저장, 상태이상 틱 로직, 카드 강화 UI, `CardSystemWindow`. 전부 Phase 2 이후 (기획서 16장). 아트도 동일 — 뷰는 전부 플레이스홀더 유지, 기능적 아트(카드 프레임·아이콘 등)는 Phase 2, 비주얼 완성은 Phase 3 (M6 재미 검증 전 아트 투자 금지).

---

## 6. Phase 2 예고 (참고 — 상세 계획은 Phase 1 검증 후)

파티 3인(공유 덱·전투불능 드로우 제외), 타겟팅 완성(도발·지정 — `Aggro` 무작위 폴백 대체), 맵/런 루프(전투 덱을 런 상태에서 주입 — `startingCards` 인스펙터 임시 조치 대체 / 맵 노드 → `EnemyEncounterData` 참조 + `SpawnRange` 결합 조우 풀), 거점 기초, 드랍·회수, 카드 해금 2종, 런 중 저장(버저닝), 상태이상 모듈(Study-ModuleSkill의 Effect 생명주기 턴제 번안), `CardSystemWindow`(SWStatSystemWindow 패턴 복제), 공용 카드 50장. 입력 확장(게임패드/단축키) 시 `CardDragController`의 포인터 폴링 → InputAction 전환.

---

## 개정 이력

| 개정 | 일자 | 내용 |
|------|------|------|
| 초판 | 2026-07-07 | Phase 1 스케줄 수립 |
| 개정 1 | 2026-07-07 | PartyData 신설 반영 |
| 개정 2 | 2026-07-10 | M3 카드 실행 계층 완료 — CardInstance 폴더 `Card/`로 변경, "일시" → "전투 한정" 용어 확정, D3 시드 적용 확정, BattleBalanceData AP 그룹 추가 |
| 개정 3 | 2026-07-14 | M4 턴 흐름 + 적 AI 완료 — 순수 클래스/Mono 경계 확정, "턴 경계" 코드 매핑(`OnRoundEnded`/`PrepareNextTurn`), 대상 처리 3단계 용어 확정(구성/검증/선정), 턴 이벤트 발화 순서 고정, 명명 개정(`TargetingResolver`→`TargetResolver`), `ETurnPhase`/`EBattleResult`를 `GameEnum.cs`에 병합, 잠정 규칙·임시 조치 명시 |
| 개정 4 | 2026-07-16 | M5 전반(5-1 손패·5-2 드래그 타겟팅) 완료 — D5 렌더링 방식 확정(월드 스프라이트), UI 모듈 → **View 모듈** 개정(`05_Scripts/View/`, `EchoesOfAsh.View`), 뷰 Init/Release 쌍 패턴·조립 지점 주입 확정, `CardDragController` 단일 입력 주체(논할당 OverlapPoint·호버/픽업 질의 공유), SortingGroup 카드 단위 정렬 + 소팅/물리 레이어 체계, **`EnemyEncounterData` 신설**(M6-3 조우 구조 선행 — 항목 순서=스폰 순서=행동 순서), 풀링 경계(뷰 전용=SWPool 직접 참조 / 공용 연출=Catalog), 5-1 잔여·`OnPileChanged` 카운터 5-6 병합, UI 최대 리스크(드래그 타겟팅) 해소 |
| 개정 5 | 2026-07-18 | M5 5-3 게이지 뷰 완료 — `GaugeView` 공용 컴포넌트(FillRoot 스케일 채움·임계값 마커·정렬 규칙 barWidth 세트), `PartyStatusView` 신설, `EnemyView` 게이지 증축 + Release 통합, 에디터 라이브 테스트 체계(OnValidate + delayCall + fake null 가드), 게이지 보간 연출·5-1 잔여 5-6 이월 |