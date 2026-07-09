# Echoes of Ash — Phase 1 개발 스케줄

> 기준 문서: 기획서 v3.2 (16장 Phase 1 체크리스트) · 작성일: 2026-07-07 (개정 1 — PartyData 신설 반영)
> 목표: **전투 1사이클 + 정신력이 플레이 가능한 상태 (1인 기준)** — 기간 잠정 8주

---

## 1. 현황 요약

### 완료 (v4 적용 기준)

| 구분 | 내용 |
|------|------|
| 데이터 구조 | `CardData` / `CharacterData` / `EnemyData` / `MadnessEventData` / `PartyData` / `BattleBalanceData` (전부 `SWIdentifiedObject`·`SWScriptableObject` 기반, 수치 외부화 완료). 경계 원칙: **전투 규칙 = BattleBalanceData, 전투 주체 속성 = 각자의 Data SO** (파티 SAN은 `PartyData`, 적 SAN은 `EnemyData` — 대칭 구조) |
| 정신력 모듈 | `Sanity/SanityHolder.cs` — `ISanityHolder` 구현체 (파티/적 공용, SWStat 클론 래핑,
임계값 교차 판정: SAN < 임계값 = 광기). `Test/SanityHolderTest.cs` 검증 완료 (M1 DoD 통과) |
| 효과 시스템 | `EffectBlock` 추상 베이스 + 기본 블록 9종 + `EffectContext` (카드/적/광기 이벤트 공용 파이프라인) |
| 인터페이스 | `ITargetable` / `IDamageable` / `ISanityHolder` / `IStatusReceiver` **정의** (구현체 없음) |
| 스탯 | HP/SAN을 `SWStatOverride`로 전환 (SWStat 채택 확정) |
| 의도 시스템 데이터 | `EIntentType` + 블록별 `IntentContribution` 자동 유도, `EnemyActionData.GetIntentTypes()` |
| 전투원 | `Battle/Base/BattleEntity.cs` (SWMonoBehaviour 기반 공통 베이스 — HP SWStat 클론 직접 보유,
방어막 int, IDamageable/ITargetable/IStatusReceiver 구현, ResetEntity 정리 패턴) +
`CharacterEntity` / `EnemyEntity`(개체별 SanityHolder 위임, ActionIndex) +
`IDamageCalculator`/`DefaultDamageCalculator`. `Test/BattleEntityTest.cs` 검증 완료 (M2 DoD 통과) |
| 데이터 보강 | `EnemyData.StartSanity` 필드 추가 — 광기 상태 등장 적 지원 (PartyData와 대칭) |
| 인프라 | SWUtils v1.0.11 통합 (SWRandom 시드 고정, SWLog, SWSubClassSelector, SWIODatabase 사용 가능) |

### 미구현 (이 문서의 대상)

전투원(인터페이스 구현체 전부), **`CardInstance` 런타임 래퍼**, 효과 실행기, 덱/손패/AP, 턴 흐름, 적 AI/의도, 타겟팅, 승패 판정, 전투 UI, 광기 이벤트 러너, 테스트 콘텐츠 데이터.

---

## 2. 마일스톤 로드맵 (M0 ~ M6, 잠정 8주)

> 순서는 의존성 순. 각 마일스톤은 "단독 테스트 가능한 모듈" 단위로 끊는다 (기획서 15-2).
> 주차는 잠정 — 지연 시 M5(UI 폴리싱)와 M6 후반(콘텐츠 물량)에서 흡수한다.

```
M0 준비 ─▶ M1 정신력 ─▶ M2 전투원 ─▶ M3 카드 실행 ─▶ M4 턴/적 AI ─▶ M5 UI/입력 ─▶ M6 광기 이벤트+통합
(0.5주)    (1주)        (1주)        (1.5주)         (1.5주)        (1.5주)       (1주)
```

---

### M0 — 준비 작업 (0.5주)

코드 작성 전에 끝내야 하는 에디터/패키지 작업.

| # | 작업 | 비고 |
|---|------|------|
| 0-1 | `STAT_MAX_HP`, `STAT_MAX_SAN` 스탯 에셋 생성 | SWTools > Utils > Stat System Window |
| 0-2 | 카테고리 에셋 기초 생성 (예: `CATEGORY_정신공격`) | 기존 자체 `Category` 에셋이 있다면 `SWCategory`로 재생성 |
| 0-3 | **SWUtils 확장 ①** — `SWStatOverride.OverrideDefaultValue` 읽기 게터 추가 | 데이터 레벨 수치 검증/미리보기용 |
| 0-4 | **SWUtils 확장 ②** — `SWStats.Setup(IEnumerable<SWStatOverride>)` 오버로드 추가 | 데이터 SO 재정의로 셋업하는 경로 (전투원에서 사용 여부는 M2에서 확정) |
| 0-5 | 테스트 데이터 최소 세트: 카드 3장(타격/방어/어둠의 일격), 적 1종, 캐릭터 1인, `PartyData` 에셋, `BattleBalanceData` 에셋 | M1~M4 개발 중 사용할 검증용 |
| 0-6 | 구 `Base/IdentifiedObject.cs`, `Base/Category.cs` 삭제 확인 | SWUtils로 대체 완료 여부 점검 |

**완료 기준(DoD):** 프로젝트가 컴파일되고, 테스트 카드 에셋의 효과 블록이 인스펙터(SWSubClassSelector)에서 편집된다.

---

### M1 — 정신력 모듈 ✅ 완료
- 명명: SanityGauge → `SanityHolder` (UI 뉘앙스 배제)
- D2 확정: 홀더는 즉시 발화, 턴 경계 지연은 적 AI(M4) 책임
- 확정 규칙: SAN < 임계값 → 광기 (임계값 = 평정) / OnSanityChanged(현재값, 최대값)

### M2 — 전투원 ✅ 완료
- 명명: Combatant → `BattleEntity`, 파생 `CharacterEntity` / `EnemyEntity` (Entity 계열 채택)
- D1 확정: SWStat 클론 직접 보유 (SWStats 컴포넌트 미사용 → 0-4 SWUtils 오버로드 작업 취소)
- 방어막: SWStat화하지 않고 런타임 int (기본값·보너스 합산 개념 없음)
- 피해 계산기: SO화하지 않고 인터페이스 유지 — 수치는 BattleBalanceData 담당
- 정리 패턴: 파괴/재사용 공용 `ResetEntity()` 도입

---

### M3 — 카드 실행 계층 (1.5주)

데이터(효과 블록)가 실제로 실행되는 파이프라인. 기획서 15-3의 런타임 절반.

| # | 산출물 | 책임 |
|---|--------|------|
| 3-1 | `Data/Runtime/CardInstance.cs` | 카드 1장의 런타임 래퍼 — 원본 SO 불변, 강화 상태·일시 AP 비용 보정만 보유 (기획서 15-5). 덱의 관리 단위 |
| 3-2 | `Deck/DeckSystem.cs` | `CardInstance` 리스트로 덱/손패/버림 더미 3존 관리, `Draw(n)`(최대 손패 10 초과 처리), `DiscardRandom(n)`, 리셔플. 셔플은 `SWRandom.Shuffle` (시드 결정성) |
| 3-3 | `Battle/ApSystem.cs` | 턴당 지급(3) + 이월 상한(2) + `Change(delta)` 클램프 — 수치는 전부 `BattleBalanceData` 참조 |
| 3-4 | `Effect/EffectExecutor.cs` | **`EffectContext` 조립 + 블록 리스트 순회 실행**의 단일 창구. 카드/적 행동/광기 이벤트 3경로가 모두 여기를 지나감 |
| 3-5 | `Battle/CardPlayService.cs` | 사용 가능 판정(AP·대상) → AP 차감 → `GetEffectBlocks(현재 SAN 구간)` → Executor 실행 → 버림 더미 이동 |
| 3-6 | 단독 테스트 씬 `Test_CardPlay` | UI 없이 코드로 카드 사용 → 적 HP/SAN, 파티 SAN, 드로우 동작 검증 |

**DoD:** "어둠의 일격"이 평정에서 6피해 / (SAN을 강제로 낮춘 뒤) 광기에서 14피해+자해3으로 분기 실행된다. 반응형 분기의 첫 실동작 확인 지점.

---

### M4 — 턴 흐름 + 적 AI (1.5주)

| # | 산출물 | 책임 |
|---|--------|------|
| 4-1 | `Battle/TurnManager.cs` | 턴 상태 머신: 턴 시작(AP 지급·드로우·광기 이벤트 판정 훅) → 플레이어 행동 → 턴 종료 → 적 행동 → 판정. 이벤트 발화 순서 결정적 보장 (기획서 15-2) |
| 4-2 | `Battle/EnemyAI.cs` | 행동 패턴 순환 인덱스, **다음 행동 예고(의도) 선정**, HP 페이즈 전환, SAN 광기 패턴 전환(**턴 경계 판정** — M1-3 결정 반영, 빈 패턴이면 기본 유지) |
| 4-3 | 대상 선정 규칙 기초 | Phase 1은 1인이라 실질 단일 대상이지만 `EEnemyTargetRule` 분기 구조만 구현 (Phase 2 확장 대비) |
| 4-4 | `Battle/TargetingResolver.cs` | `ETargetingType` → `Targets` 리스트 해석 (단일=지정 적 / 전체 / 무작위=`SWRandom.Pick` / 자신) |
| 4-5 | `Battle/BattleManager.cs` | 조우 셋업(적 1~3체 스폰), 파티 공유 SanityGauge 생성(`PartyData.MaxSanityStat` + `SanityThreshold` + `StartSanity`), 승리/패배 판정, 모듈 배선(컨텍스트 재료 주입) |
| 4-6 | 통합 테스트 씬 `Test_Battle` (UI 최소) | 디버그 버튼만으로 전투 1사이클 완주 |

**DoD:** 코드 레벨에서 전투 시작→N턴 진행→승리/패배까지 완주. 적 3체가 각자 패턴 순환하고, 적 하나를 광기로 떨어뜨리면 다음 턴 경계에 패턴이 전환된다.

---

### M5 — 전투 UI / 입력 (1.5주)

기획서 14-5 전투 화면의 Phase 1 범위 구현. 로직은 이미 완성 상태이므로 이 단계는 표시/입력만.

| # | 산출물 | 책임 |
|---|--------|------|
| 5-1 | 손패 UI | 카드 표시(이름/AP/타입), 드로우·버림 반영, 사용 불가(AP 부족) 표시 |
| 5-2 | **드래그 타겟팅** | 단일 대상 카드 드래그 → 적 위 드롭 (STS 표준 UX). `TargetingResolver` 연동 |
| 5-3 | 게이지 UI | 파티 HP/방어막, **공유 SAN(경계 표시 포함)**, 적별 HP + **얇은 SAN 보조 바** (기획서 3-2 UI 원칙) — `OnSanityChanged`/`OnDamaged` 이벤트 구독 |
| 5-4 | 의도 표시 UI | `GetIntentTypes()` 복수 아이콘 + `GetIntentDamageValue()`/`GetIntentSanityPressureValue()` 수치 |
| 5-5 | 카드 툴팁 | 반응형 카드는 평정/광기 양쪽 효과 표시 (`GetDescription()` 조합), 현재 구간 강조 |
| 5-6 | 턴 종료 버튼 / AP 표시 / 광기 진입 연출(채도·비네팅 가볍게) | 연출은 최소 — 아트 부담 억제 원칙 |

**DoD:** 마우스만으로 전투 1사이클 완주 가능. 광기 진입 시 반응형 카드의 표시가 실시간 전환된다.

---

### M6 — 광기 랜덤 이벤트 + 통합 검증 (1주)

| # | 산출물 | 책임 |
|---|--------|------|
| 6-1 | `Sanity/MadnessEventRunner.cs` | 턴 시작 훅에서 광기 구간이면 `GetMadnessEventChance(현재SAN, PartyData.SanityThreshold)` 판정(`SWRandom.Chance`) → `PickRandomMadnessEvent()` → Executor 실행 + UI 알림 (확률 곡선=룰은 Balance, 임계값=주체 속성은 PartyData에서 주입) |
| 6-2 | 광기 이벤트 데이터 4~6종 | 부정(자해 5 / 손패 1장 버림 / SAN -5) + 긍정(AP +1 / 드로우 +1) — 가중치는 부정 합 > 긍정 합 |
| 6-3 | 콘텐츠 데이터 | **카드 15장**(반응형 5~6장 포함), **적 3종**(SAN 압박형 1종 포함, 광기 패턴 1종만 정의), 조우 1~3체 구성 |
| 6-4 | 밸런스 1차 조정 | `BattleBalanceData` 수치만으로 튜닝 (코드 수정 0 확인 — 데이터 주도 검증 겸함) |
| 6-5 | **재미 검증** (부록 체크리스트) | ① 정신력이 카드 효과를 바꾸는 결정축인가 ② 광기 = 확정 이득 + 리스크 도박인가 ③ "정신력 댄스"가 재미있는가 ④ 모든 적 SAN 노출이 템포를 해치지 않는가 (광기 보상 임시 원칙 — `MadEnemyDamageTakenMultiplier` — 켜고/끄고 비교) |

**DoD:** Phase 1 목표 달성 — 1인 기준 전투 1사이클이 재미 판단 가능한 상태로 플레이된다. 검증 결과에 따라 Phase 2 진입 or 코어 재조정 결정.

---

## 3. 모듈 ↔ 산출물 매핑 (기획서 15-2 기준)

| 모듈 | Phase 1 산출물 | 폴더(안) |
|------|----------------|----------|
| 정신력 | `SanityGauge`, `MadnessEventRunner` | `05_Scripts/Sanity/` |
| 전투 | `BattleManager`, `TurnManager`, `Combatant`(Player/Enemy), `ApSystem`, `IDamageCalculator`, `CardPlayService` | `05_Scripts/Battle/` |
| 덱·카드 | `DeckSystem`, `CardInstance` | `05_Scripts/Deck/`, `05_Scripts/Data/Runtime/` |
| 타겟팅 | `TargetingResolver` | `05_Scripts/Battle/` (Phase 2에 분리 검토) |
| 효과 | `EffectExecutor` (+ 기존 Effect 모듈) | `05_Scripts/Effect/` |
| UI | 손패/게이지/의도/툴팁 | `05_Scripts/UI/Battle/` |

모듈 간 통신: 직접 참조 최소화 — 상태 변화 알림은 C# event(인터페이스에 이미 정의) 우선, 모듈 경계를 넘는 광역 알림(전투 종료 등)만 `SWEventBus` 사용 검토. **구독·발화 순서 결정성 유지** (기획서 15-2).

---

## 4. 조기 결정 필요 사항

| # | 결정 | 시점 | 비고 |
|---|------|------|------|
| D1 | 전투원 스탯 보유 방식: `SWStats` 컴포넌트 vs SWStat 클론 직접 보유 | M2 착수 시 | 0-4 오버로드 추가 여부와 연동 |
| D2 | 적 SAN 광기 전환의 "턴 경계 지연" 처리 위치 (게이지 vs 적 AI) | M1 | 권장: 적 AI 책임 (게이지 순수 유지) |
| D3 | 시드 결정성 범위: Phase 1부터 `SWRandom.SetSeed` 런 시드 적용 여부 | M3 (DeckSystem 셔플 전) | 기획서 15-5 — Phase 2 확정이지만 지금 켜두는 비용이 낮음 (권장: 적용) |
| D4 | 광기 보상 임시 원칙(`MadEnemyDamageTakenMultiplier`) 1차 채택값 | M6 | 1.0(비활성)과 1.25 비교 플레이 |

---

## 5. 리스크와 완충

- **UI 작업량 과소평가** (M5) — 드래그 타겟팅이 통상 예상보다 오래 걸림. 지연 시 툴팁(5-5)·연출(5-6)을 M6 이후로 이월.
- **재미 검증 실패 시** (M6-5) — 수치 문제면 `BattleBalanceData`로 해결(빠름), 구조 문제(2단계 구간이 얕음 등)면 Phase 2 진입을 멈추고 3-1 재설계. 이 판단을 위해 M6에 검증 시간을 확보해둔 것.
- **범위 방어** — Phase 1에 다음을 넣지 않는다: 파티 3인, 맵/노드, 유물, 상점/이벤트, 저장, 상태이상 틱 로직, 카드 강화 UI, `CardSystemWindow`. 전부 Phase 2 이후 (기획서 16장).

---

## 6. Phase 2 예고 (참고 — 상세 계획은 Phase 1 검증 후)

파티 3인(공유 덱·전투불능 드로우 제외), 타겟팅 완성(도발·지정), 맵/런 루프, 거점 기초, 드랍·회수, 카드 해금 2종, 런 중 저장(버저닝), 상태이상 모듈(Study-ModuleSkill의 Effect 생명주기 턴제 번안), `CardSystemWindow`(SWStatSystemWindow 패턴 복제), 공용 카드 50장.
