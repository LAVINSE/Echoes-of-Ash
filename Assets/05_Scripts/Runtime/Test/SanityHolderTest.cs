using EchoesOfAsh.Data;
using EchoesOfAsh.Enum;
using EchoesOfAsh.Sanity;
using SW.Attributes;
using SW.Base;
using SW.Util;
using UnityEngine;

namespace EchoesOfAsh.Test
{
    /// <summary>
    /// 정신력 모듈 테스트입니다.
    /// 빈 게임 오브젝트에 부착하고 파티 및 적 데이터 에셋을 연결하여 정신력 기능을 검증합니다.
    /// </summary>
    public class SanityHolderTest : SWMonoBehaviour
    {
        #region 필드
        [SWGroup("데이터")]
        [SerializeField] private PartyData partyData;
        [SerializeField] private EnemyData enemyData;

        private SanityHolder partySanityHolder;
        private SanityHolder enemySanityHolder;

        private int partyTypeChangedCount;
        private int enemyTypeChangedCount;

        private bool isRun = false;
        #endregion // 필드


        [SWButton("테스트 시작")]
        private void RunTest()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            isRun = true;

            if (partyData != null)
            {
                partySanityHolder = new SanityHolder(partyData.MaxSanityStat, partyData.SanityThreshold, partyData.StartSanity);
                Subscribe(partySanityHolder, "파티", () => partyTypeChangedCount++);
            }
            else
            {
                SWLog.LogError("[SanityHolderTest] PartyData가 비어 있습니다");
            }

            if (enemyData != null)
            {
                enemySanityHolder = new SanityHolder(enemyData.MaxSanityStat, enemyData.SanityThreshold, enemyData.StartSanity);
                Subscribe(enemySanityHolder, "적", () => enemyTypeChangedCount++);
            }
            else
            {
                SWLog.LogError("[SanityHolderTest] enemyData가 비어 있습니다");
            }
        }

        [SWButton("테스트 초기화")]
        private void ResetTest()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            partySanityHolder?.Dispose();
            enemySanityHolder?.Dispose();

            isRun = false;
        }

        private void OnDestroy()
        {
            partySanityHolder?.Dispose();
            enemySanityHolder?.Dispose();
        }

        private void OnGUI()
        {
            if (!isRun)
            {
                return;
            }
            
            GUILayout.BeginArea(new Rect(20f, 20f, 420f, 600f));

            DrawGaugeControls("파티 (PartyData 경로)", partySanityHolder, partyTypeChangedCount);
            GUILayout.Space(20f);
            DrawGaugeControls("적 (EnemyData 경로)", enemySanityHolder, enemyTypeChangedCount);

            GUILayout.EndArea();
        }

        #region 테스트 UI
        private void DrawGaugeControls(string label, SanityHolder holder, int typeChangedCount)
        {
            GUILayout.Label($"=== {label} ===");

            if (holder == null)
            {
                GUILayout.Label("(Holder 없음)");
                return;
            }

            GUILayout.Label($"SAN {holder.CurrentSanity}/{holder.MaxSanity}  " +
                            $"[{holder.CurrentSanityType}]  임계값: {holder.SanityThreshold}  " +
                            $"구간 전환 누적: {typeChangedCount}회");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("-10")) holder.ChangeSanity(-10);
            if (GUILayout.Button("-5")) holder.ChangeSanity(-5);
            if (GUILayout.Button("-1")) holder.ChangeSanity(-1);
            if (GUILayout.Button("+1")) holder.ChangeSanity(1);
            if (GUILayout.Button("+5")) holder.ChangeSanity(5);
            if (GUILayout.Button("+10")) holder.ChangeSanity(10);

            GUILayout.EndHorizontal();

            if (GUILayout.Button("정신력 전환 테스트 (임계값 ±1 왕복 10회 — 전환 발화 수 확인)"))
            {
                RunBoundaryOscillationTest(holder);
            }
        }

        /// <summary>
        /// 정신력 값이 경계를 반복해서 오갈 때 실제 구간 변경에만 이벤트가 발생하는지 검증합니다.
        /// 임계값 미만과 임계값 사이를 왕복하면 광기 및 평정 전환이 각각 한 번씩 발생해야 합니다.
        /// 임계값과 임계값 초과 값 사이를 왕복하면 구간 전환이 발생하지 않아야 합니다.
        /// </summary>
        private void RunBoundaryOscillationTest(SanityHolder holder)
        {
            SWLog.Log("[SanityTester] --- 정신력 전환 테스트 시작 ---");

            // 1) 평정 구간 내 진동 (임계값 ↔ 임계값+1) — 전환 0회 기대
            holder.ChangeSanity(holder.SanityThreshold + 1 - holder.CurrentSanity);

            for (int i = 0; i < 10; ++i)
            {
                holder.ChangeSanity(-1); // 임계값 (평정 유지)
                holder.ChangeSanity(1);  // 임계값+1 (평정 유지)
            }

            SWLog.Log("[SanityTester] 기본 구간 내 진동 완료 — 전환 로그가 없어야 정상");

            // 2) 경계 교차 진동 (임계값-1 ↔ 임계값) — 왕복마다 광기/평정 각 1회 기대
            for (int i = 0; i < 3; ++i)
            {
                holder.ChangeSanity(-1); // 임계값-1 → 광기
                holder.ChangeSanity(1);  // 임계값 → 평정
            }

            SWLog.Log("[SanityTester] --- 정신력 전환 테스트 종료 (교차 3왕복 = 전환 6회 기대) ---");
        }
        #endregion // 테스트 UI


        private void Subscribe(SanityHolder sanityHolder, string label, System.Action onTypeChanged)
        {
            sanityHolder.OnSanityChanged += (current, max)
                => SWLog.Log($"[SanityTester] {label} OnSanityChanged: {current}/{max}");

            sanityHolder.OnSanityTypeChanged += type =>
            {
                onTypeChanged();
                SWLog.Log($"[SanityTester] {label} OnSanityTypeChanged: {(type == ESanityType.Madness ? "광기" : "평정")} ★");
            };
        }
    }
}
