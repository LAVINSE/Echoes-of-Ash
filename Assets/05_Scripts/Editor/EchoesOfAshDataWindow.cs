using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using SW.Base;
using SW.EditorTools.Util;
using SW.Util;

using EchoesOfAsh.Data;

namespace EchoesOfAsh.EditorTools
{
    /// <summary>
    /// 게임 데이터 에셋의 생성, 복제, 삭제 및 편집 기능을 제공하는 통합 에디터 창입니다.
    /// 카드, 캐릭터, 적, 정신력 이벤트, 파티, 전투 균형 및 설정 탭을 제공합니다.
    /// SWUtils의 데이터 관리 창 구성에 맞춰 목록 표시, 정렬, 검색 및 자동 식별자 부여 기능을 제공합니다.
    /// </summary>
    public class EchoesOfAshDataWindow : EditorWindow
    {
        #region 상수
        private const string PrefPrefix = "EchoesOfAsh.DataEditor.";

        // 목록형 관리 대상 (탭 순서와 동일)
        private static readonly Type[] ManagedTypes = { typeof(CardData), typeof(CharacterData), typeof(EnemyData), typeof(SanityEventData) };
        private static readonly string[] ManagedTabNames = { "카드", "캐릭터", "적", "정신력 이벤트" };
        private static readonly string[] DefaultPaths =
        {
            "Assets/02_Res/Data/Card",
            "Assets/02_Res/Data/Character",
            "Assets/02_Res/Data/Enemy",
            "Assets/02_Res/Data/SanityEvent",
        };
        private static readonly string[] DefaultPrefixes = { "Card_", "Character_", "Enemy_", "SanityEvent_" };

        // 단일 에셋 탭 (목록형 다음 순서)
        private static readonly Type[] SingletonTypes = { typeof(PartyData), typeof(BattleBalanceData) };
        private static readonly string[] SingletonTabNames = { "파티", "밸런스" };
        private static readonly string[] SingletonDefaultPaths = { "Assets/02_Res/Data/Party", "Assets/02_Res/Data/Balance" };
        private static readonly string[] SingletonDefaultNames = { "Party", "BattleBalance" };

        private static readonly string[] SortModeNames = { "코드명순", "표시명순", "ID순" };
        private static readonly string[] LabelModeNames = { "코드명", "표시명", "에셋 이름" };

        private const float DefaultListRowHeight = 24f;
        private const float ListRowPadding = 2f;
        private const float DefaultListIconSize = 20f;
        private const float DefaultDeleteButtonWidth = 22f;
        private const float DefaultDeleteButtonHeight = 18f;
        private const int DefaultListLabelFontSize = 12;
        private const float ListRowRightSafePadding = 18f;
        #endregion // 상수

        #region 필드
        private int tabIndex;
        private string[] tabNames;

        private readonly Dictionary<Type, List<SWIdentifiedObject>> assetsByType = new();
        private readonly Dictionary<Type, Vector2> scrollPositionsByType = new();
        private readonly Dictionary<Type, SWIdentifiedObject> selectedObjectsByType = new();
        private readonly Dictionary<Type, string> searchTextsByType = new();

        private readonly ScriptableObject[] singletonAssets = new ScriptableObject[SingletonTypes.Length];
        private readonly Editor[] singletonEditors = new Editor[SingletonTypes.Length];
        private readonly Vector2[] singletonScrolls = new Vector2[SingletonTypes.Length];

        private Vector2 inspectorScrollPosition;
        private Vector2 settingsScrollPosition;
        private Editor cachedEditor;

        private Texture2D selectedBoxTexture;
        private GUIStyle selectedBoxStyle;

        // 설정 값 (EditorPrefs에 저장)
        private string[] createPaths;
        private string[] namePrefixes;
        private string[] singletonPaths;
        private bool useAutoId = true;
        private bool autoSaveAssets = true;
        private float listWidth = 300f;
        private float listRowHeight = DefaultListRowHeight;
        private float listIconSize = DefaultListIconSize;
        private float deleteButtonWidth = DefaultDeleteButtonWidth;
        private float deleteButtonHeight = DefaultDeleteButtonHeight;
        private int listLabelFontSize = DefaultListLabelFontSize;
        private int sortMode;
        private int labelMode = 2; // 기본: 에셋 이름
        #endregion // 필드

        #region 윈도우 열기
        /// <summary>
        /// Echoes of Ash Data Editor 창을 엽니다.
        /// </summary>
        [MenuItem("EchoesOfAsh/Data Editor")]
        public static void OpenWindow()
        {
            EchoesOfAshDataWindow window = GetWindow<EchoesOfAshDataWindow>();
            SWEditorUtils.SetupWindow(window, "EoA Data Editor", "d_ScriptableObject Icon", 760, 520);
            window.Show();
        }
        #endregion // 윈도우 열기

        #region 초기화
        private void OnEnable()
        {
            SetupStyle();
            LoadSettings();

            // 탭: 목록형 4 + 단일 2 + 설정 1
            tabNames = new string[ManagedTypes.Length + SingletonTypes.Length + 1];

            for (int index = 0; index < ManagedTypes.Length; ++index)
            {
                Type type = ManagedTypes[index];
                tabNames[index] = ManagedTabNames[index];

                assetsByType.TryAdd(type, new List<SWIdentifiedObject>());
                scrollPositionsByType.TryAdd(type, Vector2.zero);
                selectedObjectsByType.TryAdd(type, null);
                searchTextsByType.TryAdd(type, string.Empty);

                RefreshAssets(type);
            }

            for (int index = 0; index < SingletonTypes.Length; ++index)
            {
                tabNames[ManagedTypes.Length + index] = SingletonTabNames[index];
            }

            tabNames[^1] = "설정";

            RefreshSingletons();
        }

        private void OnDisable()
        {
            SaveSettings();
            DestroyImmediate(cachedEditor);

            for (int index = 0; index < singletonEditors.Length; ++index)
            {
                DestroyImmediate(singletonEditors[index]);
            }

            DestroyImmediate(selectedBoxTexture);
        }

        /// <summary>
        /// 선택 행 강조용 스타일을 준비합니다.
        /// </summary>
        private void SetupStyle()
        {
            selectedBoxTexture = new Texture2D(1, 1);
            selectedBoxTexture.SetPixel(0, 0, new Color(0.31f, 0.40f, 0.50f));
            selectedBoxTexture.Apply();
            // Play 상태에 종속되어 파괴되지 않도록 DontSave 설정
            selectedBoxTexture.hideFlags = HideFlags.DontSave;

            selectedBoxStyle = new GUIStyle();
            selectedBoxStyle.normal.background = selectedBoxTexture;
        }
        #endregion // 초기화

        #region 설정 저장/불러오기
        /// <summary>
        /// EditorPrefs에서 설정을 불러옵니다.
        /// </summary>
        private void LoadSettings()
        {
            createPaths = new string[ManagedTypes.Length];
            namePrefixes = new string[ManagedTypes.Length];
            singletonPaths = new string[SingletonTypes.Length];

            for (int index = 0; index < ManagedTypes.Length; ++index)
            {
                string typeName = ManagedTypes[index].Name;
                createPaths[index] = SWEditorUtils.LoadPref($"{PrefPrefix}Path.{typeName}", DefaultPaths[index]);
                namePrefixes[index] = SWEditorUtils.LoadPref($"{PrefPrefix}Prefix.{typeName}", DefaultPrefixes[index]);
            }

            for (int index = 0; index < SingletonTypes.Length; ++index)
            {
                string typeName = SingletonTypes[index].Name;
                singletonPaths[index] = SWEditorUtils.LoadPref($"{PrefPrefix}SingletonPath.{typeName}", SingletonDefaultPaths[index]);
            }

            useAutoId = SWEditorUtils.LoadPref($"{PrefPrefix}UseAutoId", true);
            autoSaveAssets = SWEditorUtils.LoadPref($"{PrefPrefix}AutoSave", true);
            listWidth = SWEditorUtils.LoadPref($"{PrefPrefix}ListWidth", 300f);
            listRowHeight = SWEditorUtils.LoadPref($"{PrefPrefix}ListRowHeight", DefaultListRowHeight);
            listIconSize = SWEditorUtils.LoadPref($"{PrefPrefix}ListIconSize", DefaultListIconSize);
            deleteButtonWidth = SWEditorUtils.LoadPref($"{PrefPrefix}DeleteButtonWidth", DefaultDeleteButtonWidth);
            deleteButtonHeight = SWEditorUtils.LoadPref($"{PrefPrefix}DeleteButtonHeight", DefaultDeleteButtonHeight);
            listLabelFontSize = SWEditorUtils.LoadPref($"{PrefPrefix}ListLabelFontSize", DefaultListLabelFontSize);
            sortMode = SWEditorUtils.LoadPref($"{PrefPrefix}SortMode", 0);
            labelMode = SWEditorUtils.LoadPref($"{PrefPrefix}LabelMode", 2);
        }

        /// <summary>
        /// EditorPrefs에 설정을 저장합니다.
        /// </summary>
        private void SaveSettings()
        {
            for (int index = 0; index < ManagedTypes.Length; ++index)
            {
                string typeName = ManagedTypes[index].Name;
                SWEditorUtils.SavePref($"{PrefPrefix}Path.{typeName}", createPaths[index]);
                SWEditorUtils.SavePref($"{PrefPrefix}Prefix.{typeName}", namePrefixes[index]);
            }

            for (int index = 0; index < SingletonTypes.Length; ++index)
            {
                string typeName = SingletonTypes[index].Name;
                SWEditorUtils.SavePref($"{PrefPrefix}SingletonPath.{typeName}", singletonPaths[index]);
            }

            SWEditorUtils.SavePref($"{PrefPrefix}UseAutoId", useAutoId);
            SWEditorUtils.SavePref($"{PrefPrefix}AutoSave", autoSaveAssets);
            SWEditorUtils.SavePref($"{PrefPrefix}ListWidth", listWidth);
            SWEditorUtils.SavePref($"{PrefPrefix}ListRowHeight", listRowHeight);
            SWEditorUtils.SavePref($"{PrefPrefix}ListIconSize", listIconSize);
            SWEditorUtils.SavePref($"{PrefPrefix}DeleteButtonWidth", deleteButtonWidth);
            SWEditorUtils.SavePref($"{PrefPrefix}DeleteButtonHeight", deleteButtonHeight);
            SWEditorUtils.SavePref($"{PrefPrefix}ListLabelFontSize", listLabelFontSize);
            SWEditorUtils.SavePref($"{PrefPrefix}SortMode", sortMode);
            SWEditorUtils.SavePref($"{PrefPrefix}LabelMode", labelMode);
        }

        /// <summary>
        /// 설정을 기본값으로 되돌립니다.
        /// </summary>
        private void ResetSettings()
        {
            for (int index = 0; index < ManagedTypes.Length; ++index)
            {
                createPaths[index] = DefaultPaths[index];
                namePrefixes[index] = DefaultPrefixes[index];
            }

            for (int index = 0; index < SingletonTypes.Length; ++index)
            {
                singletonPaths[index] = SingletonDefaultPaths[index];
            }

            useAutoId = true;
            autoSaveAssets = true;
            listWidth = 300f;
            listRowHeight = DefaultListRowHeight;
            listIconSize = DefaultListIconSize;
            deleteButtonWidth = DefaultDeleteButtonWidth;
            deleteButtonHeight = DefaultDeleteButtonHeight;
            listLabelFontSize = DefaultListLabelFontSize;
            sortMode = 0;
            labelMode = 2;
            SaveSettings();
        }
        #endregion // 설정 저장/불러오기

        #region 화면
        private void OnGUI()
        {
            tabIndex = SWEditorUtils.DrawTabBar(tabIndex, tabNames);

            // 설정 탭
            if (tabIndex == tabNames.Length - 1)
            {
                DrawSettingsTab();
                return;
            }

            // 단일 에셋 탭 (파티 / 밸런스)
            if (tabIndex >= ManagedTypes.Length)
            {
                DrawSingletonTab(tabIndex - ManagedTypes.Length);
                return;
            }

            // 목록형 탭
            DrawDataTab(ManagedTypes[tabIndex], tabIndex);
        }
        #endregion // 화면

        #region 목록형 탭
        /// <summary>
        /// 유형별 데이터 탭(좌측 목록 + 우측 인스펙터)을 그립니다.
        /// </summary>
        private void DrawDataTab(Type dataType, int typeIndex)
        {
            EditorGUILayout.BeginHorizontal();

            // ===== 좌측: 목록 =====
            EditorGUILayout.BeginVertical(GUILayout.Width(listWidth));
            DrawListToolButtons(dataType, typeIndex);
            DrawSearchField(dataType);
            DrawAssetList(dataType);
            EditorGUILayout.EndVertical();

            // ===== 우측: 인스펙터 =====
            EditorGUILayout.BeginVertical();

            SWIdentifiedObject selected = selectedObjectsByType[dataType];

            if (selected == null)
            {
                EditorGUILayout.HelpBox("좌측 목록에서 편집할 에셋을 선택하세요.", MessageType.Info);
            }
            else
            {
                DrawSelectedObjectHeader(selected);

                inspectorScrollPosition = EditorGUILayout.BeginScrollView(inspectorScrollPosition);
                Editor.CreateCachedEditor(selected, null, ref cachedEditor);
                cachedEditor.OnInspectorGUI();
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 목록 상단의 생성/복제/삭제/새로고침 버튼과 정렬 툴바를 그립니다.
        /// </summary>
        private void DrawListToolButtons(Type dataType, int typeIndex)
        {
            using (new SWEditorUtils.GUIBgColorScope(new Color(0.6f, 1f, 0.6f)))
            {
                GUIContent createContent = new(
                    $"새 {ManagedTabNames[typeIndex]} 생성",
                    "새 에셋을 생성합니다. 코드명은 GUID, ID는 설정에 따라 자동 부여됩니다");

                if (GUILayout.Button(createContent, GUILayout.Height(24f)))
                {
                    CreateNewAsset(dataType, typeIndex);
                }
            }

            EditorGUILayout.BeginHorizontal();

            SWIdentifiedObject selected = selectedObjectsByType[dataType];

            using (new SWEditorUtils.GUIEnabledScope(selected != null))
            {
                GUIContent duplicateContent = new("선택 복제", "선택 에셋을 복제합니다. 새 코드명(GUID)과 새 ID가 부여됩니다");
                if (GUILayout.Button(duplicateContent, GUILayout.Height(20f)))
                {
                    DuplicateAsset(dataType, selected);
                }

                using (new SWEditorUtils.GUIBgColorScope(new Color(1f, 0.6f, 0.6f)))
                {
                    if (GUILayout.Button(new GUIContent("선택 삭제", "선택 에셋을 삭제합니다 (확인 후)"), GUILayout.Height(20f)))
                    {
                        DeleteAsset(dataType, selected);
                    }
                }
            }

            if (GUILayout.Button(new GUIContent("새로고침", "프로젝트에서 에셋 목록을 다시 수집합니다"), GUILayout.Height(20f)))
            {
                RefreshAssets(dataType);
            }

            EditorGUILayout.EndHorizontal();

            // 정렬 단축 툴바
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("정렬", GUILayout.Width(30f));
            DrawSortShortcutButton("코드명", 0);
            DrawSortShortcutButton("표시명", 1);
            DrawSortShortcutButton("ID", 2);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 목록 정렬 기준을 바로 바꾸는 단축 버튼을 그립니다.
        /// </summary>
        private void DrawSortShortcutButton(string label, int targetSortMode)
        {
            bool isSelected = sortMode == targetSortMode;

            using (new SWEditorUtils.GUIBgColorScope(isSelected ? new Color(0.55f, 0.75f, 1f) : Color.white))
            {
                if (GUILayout.Button(label, EditorStyles.toolbarButton))
                {
                    SetSortMode(targetSortMode);
                }
            }
        }

        /// <summary>
        /// 정렬 기준을 변경하고 모든 목록을 재정렬합니다.
        /// </summary>
        private void SetSortMode(int newSortMode)
        {
            sortMode = newSortMode;

            foreach (var type in ManagedTypes)
            {
                SortAssets(type);
            }
        }

        /// <summary>
        /// 에셋 이름, 코드명 및 표시 이름을 조회하는 검색 필드를 그립니다.
        /// </summary>
        private void DrawSearchField(Type dataType)
        {
            EditorGUILayout.BeginHorizontal();

            searchTextsByType[dataType] = EditorGUILayout.TextField(searchTextsByType[dataType], EditorStyles.toolbarSearchField);

            if (GUILayout.Button(new GUIContent("×", "검색어 지우기"), EditorStyles.toolbarButton, GUILayout.Width(20f)))
            {
                searchTextsByType[dataType] = string.Empty;
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 아이콘, 표시 이름 및 삭제 버튼으로 구성된 에셋 목록을 그립니다.
        /// </summary>
        private void DrawAssetList(Type dataType)
        {
            List<SWIdentifiedObject> assets = assetsByType[dataType];
            string searchText = searchTextsByType[dataType];

            SWIdentifiedObject deleteTarget = null;
            int visibleCount = 0;

            scrollPositionsByType[dataType] = EditorGUILayout.BeginScrollView(
                scrollPositionsByType[dataType], EditorStyles.helpBox);

            float rowHeight = GetListDrawRowHeight();

            foreach (var asset in assets)
            {
                if (asset == null)
                {
                    continue;
                }

                string label = GetListLabel(asset);

                if (!MatchesSearch(asset, searchText))
                {
                    continue;
                }

                ++visibleCount;

                Rect rowRect = GUILayoutUtility.GetRect(0f, rowHeight, GUILayout.ExpandWidth(true));
                bool isSelected = selectedObjectsByType[dataType] == asset;

                bool deleteClicked = DrawListRow(rowRect, label, isSelected, asset, out Rect deleteRect);

                if (deleteClicked)
                {
                    deleteTarget = asset;
                    continue;
                }

                // 행 클릭 = 선택 토글 (삭제 버튼 영역 제외)
                Rect clickRect = new(rowRect.x, rowRect.y, deleteRect.x - rowRect.x, rowRect.height);

                if (GUI.Button(clickRect, GUIContent.none, GUIStyle.none))
                {
                    selectedObjectsByType[dataType] = isSelected ? null : asset;
                    inspectorScrollPosition = Vector2.zero;
                    GUI.FocusControl(null);
                }
            }

            if (visibleCount == 0)
            {
                string message = string.IsNullOrEmpty(searchText)
                    ? "에셋이 없습니다. 상단 생성 버튼으로 만들 수 있습니다."
                    : "검색 결과가 없습니다.";
                EditorGUILayout.HelpBox(message, MessageType.Info);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField($"총 {assets.Count}개", EditorStyles.miniLabel);

            if (deleteTarget != null)
            {
                DeleteAsset(dataType, deleteTarget);
            }
        }

        /// <summary>
        /// 아이콘과 삭제 버튼의 크기를 반영하여 목록 행 높이를 계산합니다.
        /// </summary>
        private float GetListDrawRowHeight()
        {
            return Mathf.Max(listRowHeight, listIconSize + ListRowPadding * 2f, deleteButtonHeight + ListRowPadding * 2f);
        }

        /// <summary>
        /// 선택 강조, 아이콘, 이름 및 삭제 버튼을 포함하는 목록 행을 그립니다.
        /// </summary>
        /// <returns>삭제 버튼 클릭 여부입니다.</returns>
        private bool DrawListRow(Rect rowRect, string label, bool isSelected, SWIdentifiedObject asset, out Rect deleteRect)
        {
            if (isSelected)
            {
                GUI.Box(rowRect, GUIContent.none, selectedBoxStyle);
            }

            // 아이콘
            Rect iconRect = new(
                rowRect.x + ListRowPadding,
                rowRect.y + (rowRect.height - listIconSize) * 0.5f,
                listIconSize,
                listIconSize);
            DrawAssetIcon(iconRect, asset);

            // X 삭제 버튼 영역
            deleteRect = new(
                rowRect.xMax - ListRowRightSafePadding - deleteButtonWidth - ListRowPadding,
                rowRect.y + (rowRect.height - deleteButtonHeight) * 0.5f,
                deleteButtonWidth,
                deleteButtonHeight);

            // 라벨
            Rect labelRect = new(
                iconRect.xMax + 4f,
                rowRect.y + ListRowPadding,
                Mathf.Max(1f, deleteRect.x - iconRect.xMax - 8f),
                rowRect.height - ListRowPadding * 2f);

            GUIStyle listLabelStyle = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = listLabelFontSize,
            };
            EditorGUI.LabelField(labelRect, new GUIContent(label, GetRowTooltip(asset)), listLabelStyle);

            using (new SWEditorUtils.GUIBgColorScope(new Color(1f, 0.6f, 0.6f)))
            {
                GUIStyle deleteButtonStyle = new(GUI.skin.button)
                {
                    fontSize = listLabelFontSize,
                };
                return GUI.Button(deleteRect, new GUIContent("x", "이 에셋을 삭제합니다"), deleteButtonStyle);
            }
        }

        /// <summary>
        /// 스프라이트 아이콘이 있으면 해당 이미지를 그리고, 없으면 스크립터블 오브젝트의 기본 아이콘을 그립니다.
        /// </summary>
        private static void DrawAssetIcon(Rect iconRect, SWIdentifiedObject asset)
        {
            Sprite sprite = asset.SpriteIcon;

            if (sprite != null && sprite.texture != null)
            {
                Rect texCoords = new(
                    sprite.rect.x / sprite.texture.width,
                    sprite.rect.y / sprite.texture.height,
                    sprite.rect.width / sprite.texture.width,
                    sprite.rect.height / sprite.texture.height);

                GUI.DrawTextureWithTexCoords(iconRect, sprite.texture, texCoords);
                return;
            }

            Texture defaultIcon = EditorGUIUtility.IconContent("d_ScriptableObject Icon").image;

            if (defaultIcon != null)
            {
                GUI.DrawTexture(iconRect, defaultIcon, ScaleMode.ScaleToFit);
            }
        }

        /// <summary>
        /// 표시 이름 모드에 따른 행 라벨을 반환합니다.
        /// </summary>
        private string GetListLabel(SWIdentifiedObject asset)
        {
            return labelMode switch
            {
                0 => string.IsNullOrEmpty(asset.CodeName) ? asset.name : asset.CodeName,
                1 => asset.DisplayName,
                _ => asset.name,
            };
        }

        /// <summary>
        /// 에셋의 식별자, 코드명 및 표시 이름을 요약한 도움말을 반환합니다.
        /// </summary>
        private static string GetRowTooltip(SWIdentifiedObject asset)
        {
            return $"ID: {asset.ID}\n코드명: {asset.CodeName}\n표시명: {asset.DisplayName}\n에셋: {asset.name}";
        }

        /// <summary>
        /// 에셋 이름, 코드명 또는 표시 이름이 검색어와 일치하는지 대소문자를 구분하지 않고 확인합니다.
        /// </summary>
        private static bool MatchesSearch(SWIdentifiedObject asset, string searchText)
        {
            if (string.IsNullOrEmpty(searchText))
            {
                return true;
            }

            return ContainsIgnoreCase(asset.name, searchText)
                || ContainsIgnoreCase(asset.CodeName, searchText)
                || ContainsIgnoreCase(asset.DisplayName, searchText);
        }

        private static bool ContainsIgnoreCase(string source, string value)
            => !string.IsNullOrEmpty(source) && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// 선택한 에셋의 이름 변경 및 프로젝트 창 위치 표시 도구를 그립니다.
        /// </summary>
        private void DrawSelectedObjectHeader(SWIdentifiedObject selectedObject)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            string changedName = EditorGUILayout.DelayedTextField(
                new GUIContent("에셋 이름", "프로젝트 파일 이름을 변경합니다 (표시명과는 별개)"),
                selectedObject.name);

            if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(changedName) && changedName != selectedObject.name)
            {
                RenameAsset(selectedObject, changedName);
            }

            if (GUILayout.Button(new GUIContent("Ping", "프로젝트 창에서 위치를 표시합니다"), GUILayout.Width(45f)))
            {
                SWEditorUtils.PingAndSelect(selectedObject);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        #endregion // 목록형 탭

        #region 단일 에셋 탭
        /// <summary>
        /// 단일 에셋 탭(파티/밸런스)을 그립니다.
        /// </summary>
        private void DrawSingletonTab(int singletonIndex)
        {
            Type dataType = SingletonTypes[singletonIndex];
            ScriptableObject asset = singletonAssets[singletonIndex];

            SWEditorUtils.DrawHeader($"{SingletonTabNames[singletonIndex]} ({dataType.Name})");

            if (asset == null)
            {
                EditorGUILayout.HelpBox($"{dataType.Name} 에셋이 없습니다. 아래 버튼으로 생성하세요.", MessageType.Warning);

                using (new SWEditorUtils.GUIBgColorScope(new Color(0.6f, 1f, 0.6f)))
                {
                    if (GUILayout.Button($"{SingletonDefaultNames[singletonIndex]} 생성", GUILayout.Height(24f)))
                    {
                        CreateSingletonAsset(singletonIndex);
                    }
                }

                if (GUILayout.Button("새로고침", GUILayout.Height(20f)))
                {
                    RefreshSingletons();
                }

                return;
            }

            EditorGUILayout.BeginHorizontal();

            using (new SWEditorUtils.GUIEnabledScope(false))
            {
                EditorGUILayout.ObjectField(asset, dataType, false);
            }

            if (GUILayout.Button(new GUIContent("Ping", "프로젝트 창에서 위치를 표시합니다"), GUILayout.Width(45f)))
            {
                SWEditorUtils.PingAndSelect(asset);
            }

            if (GUILayout.Button(new GUIContent("새로고침", "에셋을 다시 찾습니다"), GUILayout.Width(70f)))
            {
                RefreshSingletons();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);

            singletonScrolls[singletonIndex] = EditorGUILayout.BeginScrollView(singletonScrolls[singletonIndex]);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Editor.CreateCachedEditor(asset, null, ref singletonEditors[singletonIndex]);
            singletonEditors[singletonIndex].OnInspectorGUI();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 프로젝트에서 단일 에셋을 다시 찾고, 여러 개가 있으면 첫 번째 에셋을 사용하면서 경고를 기록합니다.
        /// </summary>
        private void RefreshSingletons()
        {
            for (int index = 0; index < SingletonTypes.Length; ++index)
            {
                Type dataType = SingletonTypes[index];
                string[] guids = AssetDatabase.FindAssets($"t:{dataType.Name}");

                singletonAssets[index] = guids.Length > 0
                    ? AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guids[0]))
                    : null;

                if (guids.Length > 1)
                {
                    SWLog.LogWarning($"[EoA Data Editor] {dataType.Name} 에셋이 {guids.Length}개 발견되었습니다. 첫 번째 에셋을 표시합니다.");
                }
            }
        }

        /// <summary>
        /// 단일 에셋을 생성합니다.
        /// </summary>
        private void CreateSingletonAsset(int singletonIndex)
        {
            string folderPath = singletonPaths[singletonIndex];

            if (!IsValidProjectPath(folderPath))
            {
                EditorUtility.DisplayDialog("생성 실패", $"생성 경로가 올바르지 않습니다:\n{folderPath}\n\n설정 탭에서 Assets/ 로 시작하는 경로를 지정해주세요.", "확인");
                return;
            }

            EnsureFolderExists(folderPath);

            var asset = CreateInstance(SingletonTypes[singletonIndex]);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{folderPath.TrimEnd('/')}/{SingletonDefaultNames[singletonIndex]}.asset");

            AssetDatabase.CreateAsset(asset, assetPath);

            if (autoSaveAssets)
            {
                AssetDatabase.SaveAssets();
            }

            RefreshSingletons();
            SWEditorUtils.PingAndSelect(asset);
        }
        #endregion // 단일 에셋 탭

        #region 설정 탭
        /// <summary>
        /// 생성 경로/접두사/동작/표시 설정 탭을 그립니다.
        /// </summary>
        private void DrawSettingsTab()
        {
            settingsScrollPosition = EditorGUILayout.BeginScrollView(settingsScrollPosition);

            SWEditorUtils.DrawHeader("에셋 생성 설정");

            for (int index = 0; index < ManagedTypes.Length; ++index)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{ManagedTabNames[index]} ({ManagedTypes[index].Name})", EditorStyles.boldLabel);

                createPaths[index] = EditorGUILayout.TextField(
                    new GUIContent("생성 경로", "새 에셋이 생성될 프로젝트 폴더 (Assets/ 로 시작)"), createPaths[index]);

                if (!IsValidProjectPath(createPaths[index]))
                {
                    EditorGUILayout.HelpBox("경로는 Assets/ 로 시작해야 합니다.", MessageType.Warning);
                }

                namePrefixes[index] = EditorGUILayout.TextField(
                    new GUIContent("파일 이름 접두사", "생성 시 파일 이름 앞에 붙는 접두사 (예: Card_)"), namePrefixes[index]);

                EditorGUILayout.EndVertical();
            }

            for (int index = 0; index < SingletonTypes.Length; ++index)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{SingletonTabNames[index]} ({SingletonTypes[index].Name})", EditorStyles.boldLabel);

                singletonPaths[index] = EditorGUILayout.TextField(
                    new GUIContent("생성 경로", "에셋이 없을 때 생성될 프로젝트 폴더"), singletonPaths[index]);

                if (!IsValidProjectPath(singletonPaths[index]))
                {
                    EditorGUILayout.HelpBox("경로는 Assets/ 로 시작해야 합니다.", MessageType.Warning);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6f);
            SWEditorUtils.DrawHeader("동작 설정");

            useAutoId = EditorGUILayout.ToggleLeft(
                new GUIContent("새 에셋에 자동 ID 부여 (현재 최대 ID + 1)", "끄면 ID는 0으로 생성되며 인스펙터에서 직접 설정합니다"), useAutoId);
            autoSaveAssets = EditorGUILayout.ToggleLeft(
                new GUIContent("생성/삭제 시 즉시 저장 (SaveAssets)", "끄면 유니티가 저장 시점을 결정합니다"), autoSaveAssets);

            EditorGUILayout.Space(6f);
            SWEditorUtils.DrawHeader("표시 설정");

            listWidth = EditorGUILayout.Slider(new GUIContent("목록 넓이", "좌측 목록 패널의 넓이"), listWidth, 200f, 450f);
            listRowHeight = EditorGUILayout.Slider(new GUIContent("목록 행 높이", "목록 행 하나의 높이"), listRowHeight, 20f, 48f);
            listIconSize = EditorGUILayout.Slider(new GUIContent("목록 아이콘 크기", "행 좌측 아이콘 크기 (SpriteIcon 또는 기본 아이콘)"), listIconSize, 16f, 40f);
            deleteButtonWidth = EditorGUILayout.Slider(new GUIContent("삭제 버튼 넓이", "행 우측 x 버튼 넓이"), deleteButtonWidth, 20f, 44f);
            deleteButtonHeight = EditorGUILayout.Slider(new GUIContent("삭제 버튼 높이", "행 우측 x 버튼 높이"), deleteButtonHeight, 16f, 40f);
            listLabelFontSize = EditorGUILayout.IntSlider(new GUIContent("목록 글자 크기", "행 라벨 글자 크기"), listLabelFontSize, 10, 18);
            labelMode = EditorGUILayout.Popup(new GUIContent("목록 표시 이름", "행에 표시할 이름 기준 (코드명/표시명/에셋 이름)"), labelMode, LabelModeNames);

            int newSortMode = EditorGUILayout.Popup(new GUIContent("정렬 기준", "목록 정렬 기준"), sortMode, SortModeNames);

            if (newSortMode != sortMode)
            {
                SetSortMode(newSortMode);
            }

            EditorGUILayout.Space(10f);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("설정 저장", GUILayout.Height(24f)))
            {
                SaveSettings();
                ShowNotification(new GUIContent("설정 저장 완료"));
            }

            if (GUILayout.Button("기본값 복원", GUILayout.Height(24f)))
            {
                if (EditorUtility.DisplayDialog("설정 초기화", "모든 설정을 기본값으로 되돌릴까요?", "복원", "취소"))
                {
                    ResetSettings();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }
        #endregion // 설정 탭

        #region 에셋 관리
        /// <summary>
        /// 프로젝트에서 지정한 유형의 모든 에셋을 다시 수집하고 정렬합니다.
        /// </summary>
        private void RefreshAssets(Type dataType)
        {
            List<SWIdentifiedObject> assets = assetsByType[dataType];
            assets.Clear();

            string[] guids = AssetDatabase.FindAssets($"t:{dataType.Name}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<SWIdentifiedObject>(path);

                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            SortAssets(dataType);

            // 삭제된 에셋이 선택 상태로 남지 않도록 정리
            if (selectedObjectsByType[dataType] != null && !assets.Contains(selectedObjectsByType[dataType]))
            {
                selectedObjectsByType[dataType] = null;
            }
        }

        /// <summary>
        /// 현재 정렬 기준으로 목록을 정렬합니다.
        /// </summary>
        private void SortAssets(Type dataType)
        {
            List<SWIdentifiedObject> assets = assetsByType[dataType];

            switch (sortMode)
            {
                case 0: // 코드명순
                    assets.Sort((a, b) => string.CompareOrdinal(a.CodeName ?? string.Empty, b.CodeName ?? string.Empty));
                    break;
                case 1: // 표시명순
                    assets.Sort((a, b) => string.CompareOrdinal(a.DisplayName ?? string.Empty, b.DisplayName ?? string.Empty));
                    break;
                default: // ID순
                    assets.Sort((a, b) => a.ID.CompareTo(b.ID));
                    break;
            }
        }

        /// <summary>
        /// 새 에셋을 생성합니다.
        /// 임시 코드명에는 전역 고유 식별자를 사용하며, 숫자 식별자는 설정에 따라 자동으로 부여합니다.
        /// </summary>
        private void CreateNewAsset(Type dataType, int typeIndex)
        {
            string createPath = createPaths[typeIndex];

            if (!IsValidProjectPath(createPath))
            {
                EditorUtility.DisplayDialog("생성 실패", $"생성 경로가 올바르지 않습니다:\n{createPath}\n\n설정 탭에서 Assets/ 로 시작하는 경로를 지정해주세요.", "확인");
                return;
            }

            EnsureFolderExists(createPath);

            var guid = Guid.NewGuid();
            var newData = CreateInstance(dataType) as SWIdentifiedObject;

            // SerializedObject로 비공개 필드(codeName, id)를 설정
            SerializedObject serializedData = new(newData);
            serializedData.FindProperty("codeName").stringValue = guid.ToString();

            if (useAutoId)
            {
                serializedData.FindProperty("id").intValue = GetNextID(dataType);
            }

            serializedData.ApplyModifiedPropertiesWithoutUndo();

            string prefix = namePrefixes[typeIndex] ?? string.Empty;
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{createPath.TrimEnd('/')}/{prefix}New.asset");
            AssetDatabase.CreateAsset(newData, assetPath);

            if (autoSaveAssets)
            {
                AssetDatabase.SaveAssets();
            }

            RefreshAssets(dataType);
            selectedObjectsByType[dataType] = newData;
            inspectorScrollPosition = Vector2.zero;

            SWLog.Log($"[EoA Data Editor] 생성 완료: {assetPath}");
        }

        /// <summary>
        /// 현재 가장 큰 숫자 식별자보다 하나 큰 값을 반환합니다.
        /// </summary>
        private int GetNextID(Type dataType)
        {
            int maxId = 0;
            List<SWIdentifiedObject> assets = assetsByType[dataType];

            foreach (var asset in assets)
            {
                if (asset != null && asset.ID > maxId)
                {
                    maxId = asset.ID;
                }
            }

            return maxId + 1;
        }

        /// <summary>
        /// 선택 에셋을 복제합니다.
        /// 데이터베이스의 중복 오류를 방지하도록 복제본에 새 코드명과 숫자 식별자를 부여합니다.
        /// </summary>
        private void DuplicateAsset(Type dataType, SWIdentifiedObject source)
        {
            if (source == null)
            {
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);

            if (!AssetDatabase.CopyAsset(sourcePath, newPath))
            {
                EditorUtility.DisplayDialog("복제 실패", $"'{source.name}' 복제에 실패했습니다.", "확인");
                return;
            }

            var duplicated = AssetDatabase.LoadAssetAtPath<SWIdentifiedObject>(newPath);

            if (duplicated != null)
            {
                SerializedObject serializedData = new(duplicated);
                serializedData.FindProperty("codeName").stringValue = Guid.NewGuid().ToString();

                if (useAutoId)
                {
                    serializedData.FindProperty("id").intValue = GetNextID(dataType);
                }

                serializedData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(duplicated);
            }

            if (autoSaveAssets)
            {
                AssetDatabase.SaveAssets();
            }

            RefreshAssets(dataType);
            selectedObjectsByType[dataType] = duplicated;
            inspectorScrollPosition = Vector2.zero;

            SWLog.Log($"[EoA Data Editor] 복제 완료: {newPath}");
        }

        /// <summary>
        /// 확인 다이얼로그 후 에셋을 삭제합니다.
        /// </summary>
        private void DeleteAsset(Type dataType, SWIdentifiedObject data)
        {
            if (data == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(data);

            bool confirmed = EditorUtility.DisplayDialog(
                "에셋 삭제",
                $"'{data.name}' 을(를) 삭제할까요?\n{assetPath}\n\n이 작업은 되돌릴 수 없습니다.",
                "삭제", "취소");

            if (!confirmed)
            {
                return;
            }

            if (selectedObjectsByType[dataType] == data)
            {
                selectedObjectsByType[dataType] = null;
            }

            AssetDatabase.DeleteAsset(assetPath);

            if (autoSaveAssets)
            {
                AssetDatabase.SaveAssets();
            }

            RefreshAssets(dataType);
            SWLog.Log($"[EoA Data Editor] 삭제 완료: {assetPath}");
        }

        /// <summary>
        /// 에셋 이름을 변경합니다.
        /// </summary>
        private void RenameAsset(SWIdentifiedObject target, string newName)
        {
            string path = AssetDatabase.GetAssetPath(target);
            string error = AssetDatabase.RenameAsset(path, newName);

            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("이름 변경 실패", error, "확인");
                return;
            }

            if (autoSaveAssets)
            {
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// 경로가 Assets/ 하위인지 확인합니다.
        /// </summary>
        private static bool IsValidProjectPath(string path)
            => !string.IsNullOrEmpty(path) && path.StartsWith("Assets");

        /// <summary>
        /// 폴더가 없으면 단계적으로 생성합니다.
        /// </summary>
        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int index = 1; index < parts.Length; ++index)
            {
                string next = $"{current}/{parts[index]}";

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
        #endregion // 에셋 관리
    }
}
