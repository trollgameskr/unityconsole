using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace otps.UnityConsole.Editor
{
    /// <summary>
    /// Time.frameCount를 표시하는 확장된 Unity 콘솔 창
    /// </summary>
    public class EnhancedConsoleWindow : EditorWindow
    {
        private List<ConsoleLogEntry> logEntries = new List<ConsoleLogEntry>();
        private Vector2 scrollPosition;
        private bool collapse = false;
        private bool clearOnPlay = false;
        private bool errorPause = false;
        
        private bool showLog = true;
        private bool showWarning = true;
        private bool showError = true;

        private int selectedIndex = -1;
        private Vector2 detailScrollPosition;
        
        private double lastClickTime;
        private int lastClickedIndex = -1;

        private GUIStyle logStyle;
        private GUIStyle warningStyle;
        private GUIStyle errorStyle;
        private GUIStyle evenBackgroundStyle;
        private GUIStyle oddBackgroundStyle;
        private GUIStyle selectedBackgroundStyle;

        // 로그 레벨별 아이콘
        private GUIContent logIcon;
        private GUIContent warningIcon;
        private GUIContent errorIcon;

        private ConsoleSettings settings;
        
        private bool wasScrollAtBottom = true;
        
        private string newTagInput = "";

        // 성능 최적화: 캐싱
        private const int MaxLogEntries = 10000; // 최대 로그 개수 제한
        private List<ConsoleLogEntry> cachedFilteredEntries = null;
        private int cachedLogCount = -1;
        private int cachedWarningCount = -1;
        private int cachedErrorCount = -1;
        private string lastSearchFilter = null;
        private string lastActiveTag = null;
        private bool lastCollapse = false;
        private bool lastShowLog = true;
        private bool lastShowWarning = true;
        private bool lastShowError = true;
        private HashSet<string> lastActiveChannels = null;
        
        // 가상 스크롤링을 위한 변수
        private const float LogEntryHeight = 20f;
        
        // GUIStyle 재사용을 위한 캐시
        private Dictionary<int, GUIStyle> styledTextStyleCache = new Dictionary<int, GUIStyle>();
        private Dictionary<int, GUIStyle> buttonStyleCache = new Dictionary<int, GUIStyle>();

        [MenuItem("Window/Enhanced Console")]
        public static void ShowWindow()
        {
            var window = GetWindow<EnhancedConsoleWindow>("Enhanced Console");
            window.Show();
        }

        private void OnEnable()
        {
            settings = ConsoleSettings.Instance;
            Application.logMessageReceived += HandleLog;
            InitializeStyles();
            
            // 키보드 이벤트를 받을 수 있도록 설정
            wantsMouseMove = true;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
            
            // 메모리 정리
            styledTextStyleCache?.Clear();
            buttonStyleCache?.Clear();
        }

        private void InitializeStyles()
        {
            logStyle = new GUIStyle();
            logStyle.normal.textColor = Color.white;
            logStyle.fontSize = 12;
            logStyle.padding = new RectOffset(5, 5, 2, 2);

            warningStyle = new GUIStyle(logStyle);
            warningStyle.normal.textColor = Color.yellow;

            errorStyle = new GUIStyle(logStyle);
            errorStyle.normal.textColor = Color.red;

            evenBackgroundStyle = new GUIStyle();
            evenBackgroundStyle.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.3f, 1f));

            oddBackgroundStyle = new GUIStyle();
            oddBackgroundStyle.normal.background = MakeTexture(2, 2, new Color(0.25f, 0.25f, 0.25f, 1f));

            selectedBackgroundStyle = new GUIStyle();
            selectedBackgroundStyle.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.5f, 0.7f, 1f)); // 연한 파랑색
            
            // 로그 레벨별 아이콘 로드
            logIcon = EditorGUIUtility.IconContent("console.infoicon.sml");
            warningIcon = EditorGUIUtility.IconContent("console.warnicon.sml");
            errorIcon = EditorGUIUtility.IconContent("console.erroricon.sml");
            
            // 스타일 캐시 초기화
            styledTextStyleCache?.Clear();
            buttonStyleCache?.Clear();
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            // 에디터 재생 시 텍스처가 파괴되지 않도록 설정
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return texture;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (clearOnPlay && !EditorApplication.isPlaying)
            {
                logEntries.Clear();
                InvalidateCache();
            }

            // 최대 로그 개수 제한 (성능 최적화)
            if (logEntries.Count >= MaxLogEntries)
            {
                logEntries.RemoveAt(0);
                if (selectedIndex > 0)
                {
                    selectedIndex--;
                }
            }

            ConsoleLogEntry newEntry = new ConsoleLogEntry(logString, stackTrace, type);
            logEntries.Add(newEntry);
            
            // 발견된 채널을 자동으로 등록
            List<string> discoveredChannels = newEntry.GetChannels();
            if (discoveredChannels.Count > 0)
            {
                settings.RegisterChannelsIfNeeded(discoveredChannels);
            }
            
            InvalidateCache();

            if (errorPause && type == LogType.Error)
            {
                Debug.Break();
            }

            // 스크롤이 하단에 있으면 자동으로 스크롤
            if (wasScrollAtBottom)
            {
                // 다음 프레임에서 스크롤을 맨 아래로 설정
                EditorApplication.delayCall += () =>
                {
                    scrollPosition.y = float.MaxValue;
                    Repaint();
                };
            }

            Repaint();
        }
        
        private void InvalidateCache()
        {
            cachedFilteredEntries = null;
            cachedLogCount = -1;
            cachedWarningCount = -1;
            cachedErrorCount = -1;
        }

        private void OnGUI()
        {
            // 스타일 또는 텍스처가 손실된 경우 재초기화
            if (logStyle == null || evenBackgroundStyle == null || oddBackgroundStyle == null || 
                selectedBackgroundStyle == null || evenBackgroundStyle.normal.background == null ||
                oddBackgroundStyle.normal.background == null || selectedBackgroundStyle.normal.background == null)
            {
                InitializeStyles();
            }

            HandleKeyboardInput();
            
            DrawToolbar();
            DrawChannelBar();
            DrawTagBar();
            DrawLogList();
            DrawDetailArea();
        }
        
        private void HandleKeyboardInput()
        {
            Event e = Event.current;
            
            // KeyDown 이벤트를 처리
            if (e.type == EventType.KeyDown)
            {
                bool handled = false;
                
                if (e.keyCode == KeyCode.DownArrow)
                {
                    // 다음 에러로 이동
                    if (selectedIndex >= 0)
                    {
                        int nextErrorIndex = FindNextError(selectedIndex);
                        if (nextErrorIndex >= 0)
                        {
                            selectedIndex = nextErrorIndex;
                            handled = true;
                        }
                    }
                }
                else if (e.keyCode == KeyCode.UpArrow)
                {
                    // 이전 에러로 이동
                    if (selectedIndex >= 0)
                    {
                        int prevErrorIndex = FindPreviousError(selectedIndex);
                        if (prevErrorIndex >= 0)
                        {
                            selectedIndex = prevErrorIndex;
                            handled = true;
                        }
                    }
                }
                
                if (handled)
                {
                    e.Use();
                    Repaint();
                }
            }
        }
        
        private int FindNextError(int currentIndex)
        {
            for (int i = currentIndex + 1; i < logEntries.Count; i++)
            {
                if (IsErrorLog(logEntries[i].logType))
                {
                    return i;
                }
            }
            return -1;
        }
        
        private int FindPreviousError(int currentIndex)
        {
            for (int i = currentIndex - 1; i >= 0; i--)
            {
                if (IsErrorLog(logEntries[i].logType))
                {
                    return i;
                }
            }
            return -1;
        }
        
        private bool IsErrorLog(LogType type)
        {
            return type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                logEntries.Clear();
                selectedIndex = -1;
                InvalidateCache();
            }

            GUILayout.Space(5);

            bool newCollapse = GUILayout.Toggle(collapse, "Collapse", EditorStyles.toolbarButton);
            if (newCollapse != collapse)
            {
                collapse = newCollapse;
                InvalidateCache();
            }
            clearOnPlay = GUILayout.Toggle(clearOnPlay, "Clear on Play", EditorStyles.toolbarButton);
            errorPause = GUILayout.Toggle(errorPause, "Error Pause", EditorStyles.toolbarButton);

            GUILayout.Space(5);

            // 검색 필터
            GUILayout.Label("검색:", GUILayout.Width(35));
            string newSearchFilter = EditorGUILayout.TextField(settings.SearchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));
            if (newSearchFilter != settings.SearchFilter)
            {
                settings.SearchFilter = newSearchFilter;
                InvalidateCache();
                Repaint();
            }

            GUILayout.FlexibleSpace();

            // Time.frameCount 표시 토글
            bool newShowFrameCount = GUILayout.Toggle(settings.ShowFrameCount, "Frame", EditorStyles.toolbarButton);
            if (newShowFrameCount != settings.ShowFrameCount)
            {
                settings.ShowFrameCount = newShowFrameCount;
                Repaint();
            }

            // Time.fixedTime 표시 토글
            bool newShowFixedTime = GUILayout.Toggle(settings.ShowFixedTime, "GameTime", EditorStyles.toolbarButton);
            if (newShowFixedTime != settings.ShowFixedTime)
            {
                settings.ShowFixedTime = newShowFixedTime;
                Repaint();
            }

            // DateTime.Now 표시 토글
            bool newShowTimestamp = GUILayout.Toggle(settings.ShowTimestamp, "Time", EditorStyles.toolbarButton);
            if (newShowTimestamp != settings.ShowTimestamp)
            {
                settings.ShowTimestamp = newShowTimestamp;
                Repaint();
            }

            GUILayout.Space(5);

            // 로그 타입별 필터 버튼 (아이콘과 개수 표시)
            GUIContent logContent = new GUIContent(GetLogCount(LogType.Log).ToString(), logIcon.image);
            GUIContent warningContent = new GUIContent(GetLogCount(LogType.Warning).ToString(), warningIcon.image);
            GUIContent errorContent = new GUIContent(GetLogCount(LogType.Error).ToString(), errorIcon.image);
            
            bool newShowLog = GUILayout.Toggle(showLog, logContent, EditorStyles.toolbarButton, GUILayout.Width(40));
            bool newShowWarning = GUILayout.Toggle(showWarning, warningContent, EditorStyles.toolbarButton, GUILayout.Width(40));
            bool newShowError = GUILayout.Toggle(showError, errorContent, EditorStyles.toolbarButton, GUILayout.Width(40));
            
            if (newShowLog != showLog || newShowWarning != showWarning || newShowError != showError)
            {
                showLog = newShowLog;
                showWarning = newShowWarning;
                showError = newShowError;
                InvalidateCache();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawChannelBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("채널:", GUILayout.Width(35));
            
            // 채널 필터 상태 표시
            int activeChannelCount = settings.ActiveChannels.Count;
            int totalChannelCount = settings.Channels.Count;
            
            if (totalChannelCount > 0)
            {
                GUILayout.Label($"({activeChannelCount}/{totalChannelCount})", GUILayout.Width(50));
            }
            
            GUILayout.Space(5);
            
            // 모두 활성화 버튼
            if (GUILayout.Button("전체선택", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                foreach (var channel in settings.Channels)
                {
                    settings.SetChannelActive(channel, true);
                }
                InvalidateCache();
                Repaint();
            }
            
            // 모두 비활성화 버튼
            if (GUILayout.Button("전체해제", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                foreach (var channel in settings.Channels)
                {
                    settings.SetChannelActive(channel, false);
                }
                InvalidateCache();
                Repaint();
            }
            
            GUILayout.Space(5);
            
            // 채널 목록 표시 (토글 버튼으로) - 복사본으로 순회하여 안전하게 삭제
            string channelToRemove = null;
            List<string> channelsCopy = new List<string>(settings.Channels);
            
            foreach (var channel in channelsCopy)
            {
                bool isActive = settings.IsChannelActive(channel);
                
                // 활성/비활성에 따라 색상 변경
                Color originalColor = GUI.backgroundColor;
                if (isActive)
                {
                    GUI.backgroundColor = new Color(0.5f, 0.8f, 0.5f); // 녹색 톤
                }
                else
                {
                    GUI.backgroundColor = new Color(0.6f, 0.6f, 0.6f); // 회색 톤
                }
                
                // 채널 토글 버튼
                bool newActive = GUILayout.Toggle(isActive, channel, EditorStyles.toolbarButton, GUILayout.MinWidth(50));
                if (newActive != isActive)
                {
                    settings.SetChannelActive(channel, newActive);
                    InvalidateCache();
                    Repaint();
                }
                
                GUI.backgroundColor = originalColor;
                
                // 채널 삭제 버튼
                if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
                {
                    channelToRemove = channel;
                }
                
                GUILayout.Space(2);
            }
            
            // 루프 밖에서 채널 삭제
            if (channelToRemove != null)
            {
                settings.RemoveChannel(channelToRemove);
                InvalidateCache();
                Repaint();
            }
            
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTagBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("태그:", GUILayout.Width(35));
            
            // 새 태그 입력 필드
            newTagInput = EditorGUILayout.TextField(newTagInput, EditorStyles.toolbarTextField, GUILayout.Width(100));
            
            // 태그 추가 버튼
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(25)))
            {
                if (!string.IsNullOrEmpty(newTagInput.Trim()))
                {
                    settings.AddTag(newTagInput.Trim());
                    newTagInput = "";
                    Repaint();
                }
            }
            
            GUILayout.Space(5);
            
            // 활성 태그를 표시하고 비활성화 버튼
            if (!string.IsNullOrEmpty(settings.ActiveTag))
            {
                GUILayout.Label($"필터: {settings.ActiveTag}", GUILayout.Width(100));
                if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    settings.ActiveTag = "";
                    InvalidateCache();
                    Repaint();
                }
                GUILayout.Space(5);
            }
            
            // 태그 목록 표시 (버튼으로) - 복사본으로 순회하여 안전하게 삭제
            string tagToRemove = null;
            foreach (var tag in settings.Tags)
            {
                // 활성 태그는 다른 색상으로 표시
                bool isActive = settings.ActiveTag == tag;
                Color originalColor = GUI.contentColor;
                if (isActive)
                {
                    GUI.contentColor = Color.cyan;
                }
                
                if (GUILayout.Button(tag, EditorStyles.toolbarButton, GUILayout.MinWidth(50)))
                {
                    // 태그 클릭 시 필터 활성화/비활성화 토글
                    if (settings.ActiveTag == tag)
                    {
                        settings.ActiveTag = "";
                    }
                    else
                    {
                        settings.ActiveTag = tag;
                    }
                    InvalidateCache();
                    Repaint();
                }
                
                GUI.contentColor = originalColor;
                
                // 태그 삭제 버튼
                if (GUILayout.Button("-", EditorStyles.toolbarButton, GUILayout.Width(20)))
                {
                    tagToRemove = tag;
                }
                
                GUILayout.Space(2);
            }
            
            // 루프 밖에서 태그 삭제
            if (tagToRemove != null)
            {
                settings.RemoveTag(tagToRemove);
                InvalidateCache();
                Repaint();
            }
            
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLogList()
        {
            float listHeight = position.height * 0.6f;
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(listHeight));

            List<ConsoleLogEntry> filteredEntries = GetFilteredEntries();
            
            // 가상 스크롤링: 보이는 영역만 렌더링
            int totalLogs = filteredEntries.Count;
            float totalHeight = totalLogs * LogEntryHeight;
            
            // 보이는 영역 계산
            int firstVisibleIndex = Mathf.Max(0, (int)(scrollPosition.y / LogEntryHeight));
            int lastVisibleIndex = Mathf.Min(totalLogs - 1, (int)((scrollPosition.y + listHeight) / LogEntryHeight) + 1);
            
            // 상단 여백
            if (firstVisibleIndex > 0)
            {
                GUILayout.Space(firstVisibleIndex * LogEntryHeight);
            }
            
            for (int i = firstVisibleIndex; i <= lastVisibleIndex && i < totalLogs; i++)
            {
                ConsoleLogEntry entry = filteredEntries[i];
                
                if (!ShouldShowLog(entry.logType))
                    continue;

                int actualIndex = logEntries.IndexOf(entry);
                bool isSelected = (actualIndex == selectedIndex);
                
                // 선택된 로그는 연한 파랑색 배경, 아니면 짝수/홀수 배경
                GUIStyle backgroundStyle;
                if (isSelected)
                {
                    backgroundStyle = selectedBackgroundStyle;
                }
                else
                {
                    backgroundStyle = (i % 2 == 0) ? evenBackgroundStyle : oddBackgroundStyle;
                }
                
                GUIStyle textStyle = GetStyleForLogType(entry.logType);
                
                // 스타일 캐싱으로 재사용 (성능 최적화)
                int styleKey = (isSelected ? 1000000 : 0) + (i % 2 == 0 ? 100000 : 0) + (int)entry.logType;
                
                if (!styledTextStyleCache.TryGetValue(styleKey, out GUIStyle styledTextStyle))
                {
                    styledTextStyle = new GUIStyle(textStyle);
                    styledTextStyle.normal.background = backgroundStyle.normal.background;
                    styledTextStyleCache[styleKey] = styledTextStyle;
                }
                
                if (!buttonStyleCache.TryGetValue(styleKey, out GUIStyle buttonStyle))
                {
                    buttonStyle = new GUIStyle(textStyle);
                    buttonStyle.normal.background = backgroundStyle.normal.background;
                    buttonStyle.alignment = TextAnchor.MiddleLeft;
                    buttonStyleCache[styleKey] = buttonStyle;
                }

                EditorGUILayout.BeginHorizontal(backgroundStyle, GUILayout.Height(LogEntryHeight));

                // 로그 타입 아이콘 표시
                GUIContent icon = GetIconForLogType(entry.logType);
                GUILayout.Label(icon, styledTextStyle, GUILayout.Width(20));

                // Frame Count 컬럼 (설정에 따라 표시)
                if (settings.ShowFrameCount)
                {
                    GUILayout.Label($"[{entry.frameCount}]", styledTextStyle, GUILayout.Width(60));
                }

                // Fixed Time 컬럼 (설정에 따라 표시)
                if (settings.ShowFixedTime)
                {
                    GUILayout.Label($"[{entry.fixedTime:F2}s]", styledTextStyle, GUILayout.Width(70));
                }

                // Timestamp 컬럼 (설정에 따라 표시)
                if (settings.ShowTimestamp)
                {
                    GUILayout.Label($"[{entry.timestamp:HH:mm:ss}]", styledTextStyle, GUILayout.Width(75));
                }

                // 로그 메시지
                string displayMessage = entry.message;
                if (GUILayout.Button(displayMessage, buttonStyle, GUILayout.ExpandWidth(true)))
                {
                    // 더블 클릭 감지
                    double currentTime = EditorApplication.timeSinceStartup;
                    if (actualIndex == lastClickedIndex && (currentTime - lastClickTime) < 0.3)
                    {
                        // 더블 클릭됨 - IDE에서 스크립트 열기
                        OpenScriptFromStackTrace(entry.stackTrace);
                        lastClickedIndex = -1;
                        lastClickTime = 0;
                    }
                    else
                    {
                        // 싱글 클릭 - 선택만
                        selectedIndex = actualIndex;
                        lastClickedIndex = actualIndex;
                        lastClickTime = currentTime;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            
            // 하단 여백
            if (lastVisibleIndex < totalLogs - 1)
            {
                GUILayout.Space((totalLogs - lastVisibleIndex - 1) * LogEntryHeight);
            }

            EditorGUILayout.EndScrollView();
            
            // 스크롤 위치가 하단에 있는지 확인 (여유를 두고 10픽셀 이내)
            Rect scrollViewRect = GUILayoutUtility.GetLastRect();
            float maxScrollY = Mathf.Max(0, totalHeight - scrollViewRect.height);
            wasScrollAtBottom = (maxScrollY == 0) || (scrollPosition.y >= maxScrollY - 10f);
        }

        private void DrawDetailArea()
        {
            if (selectedIndex >= 0 && selectedIndex < logEntries.Count)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                ConsoleLogEntry selectedEntry = logEntries[selectedIndex];
                
                // 메시지와 Stack Trace를 함께 스크롤
                detailScrollPosition = EditorGUILayout.BeginScrollView(detailScrollPosition);
                
                // 메시지 표시 (선택 가능)
                EditorGUILayout.SelectableLabel(selectedEntry.message, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(false));
                
                EditorGUILayout.Space(5);
                
                // Stack Trace 표시 (각 라인을 클릭 가능하게)
                DrawStackTraceLines(selectedEntry.stackTrace);
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void DrawStackTraceLines(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return;

            var lines = stackTrace.Split('\n');

            foreach (var line in lines)
            {
                // UnityEngine.Debug:Log로 시작하는 라인 스킵
                if (line.TrimStart().StartsWith("UnityEngine.Debug:Log"))
                    continue;
                
                // 파일 경로와 라인 번호가 있는지 확인
                bool hasFileReference = line.Contains(" (at ") && line.Contains(")");
                
                if (hasFileReference)
                {
                    // 클릭 가능한 버튼으로 표시
                    GUIStyle buttonStyle = new GUIStyle(EditorStyles.label);
                    buttonStyle.wordWrap = true;
                    buttonStyle.normal.textColor = new Color(0.5f, 0.7f, 1.0f); // 파란색 톤
                    
                    if (GUILayout.Button(line, buttonStyle))
                    {
                        OpenScriptFromStackTraceLine(line);
                    }
                    
                    // 마우스 오버 시 커서 변경
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    if (lastRect.Contains(Event.current.mousePosition))
                    {
                        EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.Link);
                    }
                }
                else
                {
                    // 파일 참조가 없는 라인은 선택 가능한 레이블로 표시
                    EditorGUILayout.SelectableLabel(line, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(false));
                }
            }
        }
        
        private void OpenScriptFromStackTraceLine(string line)
        {
            // 스택 트레이스 형식: "ClassName:MethodName() (at Assets/...path.cs:lineNumber)"
            int atIndex = line.IndexOf(" (at ");
            if (atIndex > 0)
            {
                int endIndex = line.LastIndexOf(')');
                if (endIndex > atIndex)
                {
                    string pathAndLine = line.Substring(atIndex + 5, endIndex - atIndex - 5);
                    int colonIndex = pathAndLine.LastIndexOf(':');
                    
                    if (colonIndex > 0)
                    {
                        string filePath = pathAndLine.Substring(0, colonIndex);
                        string lineNumberStr = pathAndLine.Substring(colonIndex + 1);
                        
                        if (int.TryParse(lineNumberStr, out int lineNumber))
                        {
                            // Unity API를 사용하여 파일 열기
                            var scriptAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
                            if (scriptAsset != null)
                            {
                                AssetDatabase.OpenAsset(scriptAsset, lineNumber);
                            }
                        }
                    }
                }
            }
        }

        private void OpenScriptFromStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return;

            var lines = stackTrace.Split('\n');
            
            foreach (var line in lines)
            {
                // UnityEngine.Debug:Log 라인은 스킵
                if (line.TrimStart().StartsWith("UnityEngine.Debug:Log"))
                    continue;

                // 스택 트레이스 형식: "ClassName:MethodName() (at Assets/...path.cs:lineNumber)"
                int atIndex = line.IndexOf(" (at ");
                if (atIndex > 0)
                {
                    int endIndex = line.LastIndexOf(')');
                    if (endIndex > atIndex)
                    {
                        string pathAndLine = line.Substring(atIndex + 5, endIndex - atIndex - 5);
                        int colonIndex = pathAndLine.LastIndexOf(':');
                        
                        if (colonIndex > 0)
                        {
                            string filePath = pathAndLine.Substring(0, colonIndex);
                            string lineNumberStr = pathAndLine.Substring(colonIndex + 1);
                            
                            if (int.TryParse(lineNumberStr, out int lineNumber))
                            {
                                // Unity API를 사용하여 파일 열기
                                var scriptAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(filePath);
                                if (scriptAsset != null)
                                {
                                    AssetDatabase.OpenAsset(scriptAsset, lineNumber);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private List<ConsoleLogEntry> GetFilteredEntries()
        {
            // 캐시가 유효한지 확인
            bool channelsChanged = lastActiveChannels == null || 
                                   !lastActiveChannels.SetEquals(settings.ActiveChannels);
            
            if (cachedFilteredEntries != null && 
                lastSearchFilter == settings.SearchFilter &&
                lastActiveTag == settings.ActiveTag &&
                lastCollapse == collapse &&
                lastShowLog == showLog &&
                lastShowWarning == showWarning &&
                lastShowError == showError &&
                !channelsChanged)
            {
                return cachedFilteredEntries;
            }
            
            // 캐시 업데이트
            lastSearchFilter = settings.SearchFilter;
            lastActiveTag = settings.ActiveTag;
            lastCollapse = collapse;
            lastShowLog = showLog;
            lastShowWarning = showWarning;
            lastShowError = showError;
            lastActiveChannels = new HashSet<string>(settings.ActiveChannels);
            
            List<ConsoleLogEntry> filtered = new List<ConsoleLogEntry>();
            
            // 채널 필터 적용 여부 확인
            bool hasChannelFilter = settings.Channels.Count > 0 && settings.ActiveChannels.Count > 0;
            bool hasSearchFilter = !string.IsNullOrEmpty(settings.SearchFilter);
            bool hasTagFilter = !string.IsNullOrEmpty(settings.ActiveTag);
            
            if (collapse)
            {
                HashSet<string> uniqueMessages = new HashSet<string>();

                foreach (var entry in logEntries)
                {
                    // 1. 채널 필터 먼저 적용 (채널이 등록되어 있을 때만)
                    if (hasChannelFilter)
                    {
                        List<string> entryChannels = entry.GetChannels();
                        
                        // 로그에 채널이 없으면 제외
                        if (entryChannels.Count == 0)
                            continue;
                        
                        // 활성 채널 중 하나라도 포함하는지 확인
                        bool hasActiveChannel = false;
                        foreach (string channel in entryChannels)
                        {
                            if (settings.IsChannelActive(channel))
                            {
                                hasActiveChannel = true;
                                break;
                            }
                        }
                        
                        if (!hasActiveChannel)
                            continue;
                    }
                    
                    // 2. 검색 필터 확인 (대소문자 구분 없이)
                    if (hasSearchFilter && entry.message.IndexOf(settings.SearchFilter, System.StringComparison.OrdinalIgnoreCase) == -1)
                        continue;
                    
                    // 3. 태그 필터 확인 (대소문자 구분 없이)
                    if (hasTagFilter && entry.message.IndexOf(settings.ActiveTag, System.StringComparison.OrdinalIgnoreCase) == -1)
                        continue;
                    
                    if (uniqueMessages.Add(entry.message))
                    {
                        filtered.Add(entry);
                    }
                }
            }
            else
            {
                foreach (var entry in logEntries)
                {
                    // 1. 채널 필터 먼저 적용 (채널이 등록되어 있을 때만)
                    if (hasChannelFilter)
                    {
                        List<string> entryChannels = entry.GetChannels();
                        
                        // 로그에 채널이 없으면 제외
                        if (entryChannels.Count == 0)
                            continue;
                        
                        // 활성 채널 중 하나라도 포함하는지 확인
                        bool hasActiveChannel = false;
                        foreach (string channel in entryChannels)
                        {
                            if (settings.IsChannelActive(channel))
                            {
                                hasActiveChannel = true;
                                break;
                            }
                        }
                        
                        if (!hasActiveChannel)
                            continue;
                    }
                    
                    // 2. 검색 필터 확인 (대소문자 구분 없이)
                    if (hasSearchFilter && entry.message.IndexOf(settings.SearchFilter, System.StringComparison.OrdinalIgnoreCase) == -1)
                        continue;
                    
                    // 3. 태그 필터 확인 (대소문자 구분 없이)
                    if (hasTagFilter && entry.message.IndexOf(settings.ActiveTag, System.StringComparison.OrdinalIgnoreCase) == -1)
                        continue;
                    
                    filtered.Add(entry);
                }
            }

            cachedFilteredEntries = filtered;
            return filtered;
        }

        private bool ShouldShowLog(LogType type)
        {
            switch (type)
            {
                case LogType.Log:
                    return showLog;
                case LogType.Warning:
                    return showWarning;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return showError;
                default:
                    return true;
            }
        }

        private GUIStyle GetStyleForLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return warningStyle;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return errorStyle;
                default:
                    return logStyle;
            }
        }

        private GUIContent GetIconForLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return warningIcon;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return errorIcon;
                default:
                    return logIcon;
            }
        }

        private int GetLogCount(LogType type)
        {
            // 캐시 확인
            switch (type)
            {
                case LogType.Log:
                    if (cachedLogCount >= 0)
                        return cachedLogCount;
                    break;
                case LogType.Warning:
                    if (cachedWarningCount >= 0)
                        return cachedWarningCount;
                    break;
                case LogType.Error:
                    if (cachedErrorCount >= 0)
                        return cachedErrorCount;
                    break;
            }
            
            // 캐시되지 않은 경우 계산
            int count = 0;
            foreach (var entry in logEntries)
            {
                if (entry.logType == type)
                    count++;
            }
            
            // 캐시에 저장
            switch (type)
            {
                case LogType.Log:
                    cachedLogCount = count;
                    break;
                case LogType.Warning:
                    cachedWarningCount = count;
                    break;
                case LogType.Error:
                    cachedErrorCount = count;
                    break;
            }
            
            return count;
        }
    }
}
