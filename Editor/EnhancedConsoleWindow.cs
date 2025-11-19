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

        private ConsoleSettings settings;
        
        private string newTagInput = "";

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
            return texture;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (clearOnPlay && !EditorApplication.isPlaying)
            {
                logEntries.Clear();
            }

            logEntries.Add(new ConsoleLogEntry(logString, stackTrace, type));

            if (errorPause && type == LogType.Error)
            {
                Debug.Break();
            }

            Repaint();
        }

        private void OnGUI()
        {
            if (logStyle == null)
            {
                InitializeStyles();
            }

            HandleKeyboardInput();
            
            DrawToolbar();
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
            }

            GUILayout.Space(5);

            collapse = GUILayout.Toggle(collapse, "Collapse", EditorStyles.toolbarButton);
            clearOnPlay = GUILayout.Toggle(clearOnPlay, "Clear on Play", EditorStyles.toolbarButton);
            errorPause = GUILayout.Toggle(errorPause, "Error Pause", EditorStyles.toolbarButton);

            GUILayout.Space(5);

            // 검색 필터
            GUILayout.Label("검색:", GUILayout.Width(35));
            string newSearchFilter = EditorGUILayout.TextField(settings.SearchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));
            if (newSearchFilter != settings.SearchFilter)
            {
                settings.SearchFilter = newSearchFilter;
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

            showLog = GUILayout.Toggle(showLog, GetLogCount(LogType.Log).ToString(), EditorStyles.toolbarButton, GUILayout.Width(40));
            showWarning = GUILayout.Toggle(showWarning, GetLogCount(LogType.Warning).ToString(), EditorStyles.toolbarButton, GUILayout.Width(40));
            showError = GUILayout.Toggle(showError, GetLogCount(LogType.Error).ToString(), EditorStyles.toolbarButton, GUILayout.Width(40));

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
            
            for (int i = 0; i < filteredEntries.Count; i++)
            {
                ConsoleLogEntry entry = filteredEntries[i];
                
                if (!ShouldShowLog(entry.logType))
                    continue;

                GUIStyle backgroundStyle = (i % 2 == 0) ? evenBackgroundStyle : oddBackgroundStyle;
                GUIStyle textStyle = GetStyleForLogType(entry.logType);
                
                // 배경색이 적용된 텍스트 스타일 생성
                GUIStyle styledTextStyle = new GUIStyle(textStyle);
                styledTextStyle.normal.background = backgroundStyle.normal.background;
                
                // 배경색이 적용된 버튼 스타일 생성
                GUIStyle buttonStyle = new GUIStyle(textStyle);
                buttonStyle.normal.background = backgroundStyle.normal.background;
                buttonStyle.alignment = TextAnchor.MiddleLeft;

                EditorGUILayout.BeginHorizontal(backgroundStyle);

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
                    int actualIndex = logEntries.IndexOf(entry);
                    
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

            EditorGUILayout.EndScrollView();
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
            List<ConsoleLogEntry> filtered = new List<ConsoleLogEntry>();
            
            // 검색 필터 적용
            bool hasSearchFilter = !string.IsNullOrEmpty(settings.SearchFilter);
            bool hasTagFilter = !string.IsNullOrEmpty(settings.ActiveTag);
            
            if (collapse)
            {
                HashSet<string> uniqueMessages = new HashSet<string>();

                foreach (var entry in logEntries)
                {
                    // 검색 필터 확인 (대소문자 구분 없이)
                    if (hasSearchFilter && entry.message.IndexOf(settings.SearchFilter, System.StringComparison.OrdinalIgnoreCase) == -1)
                        continue;
                    
                    // 태그 필터 확인 (대소문자 구분 없이)
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
                    // 검색 필터 확인 (대소문자 구분 없이)
                    if (hasSearchFilter && entry.message.IndexOf(settings.SearchFilter, System.StringComparison.OrdinalIgnoreCase) == -1)
                        continue;
                    
                    // 태그 필터 확인 (대소문자 구분 없이)
                    if (hasTagFilter && entry.message.IndexOf(settings.ActiveTag, System.StringComparison.OrdinalIgnoreCase) == -1)
                        continue;
                    
                    filtered.Add(entry);
                }
            }

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

        private int GetLogCount(LogType type)
        {
            int count = 0;
            foreach (var entry in logEntries)
            {
                if (entry.logType == type)
                    count++;
            }
            return count;
        }
    }
}
