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
            oddBackgroundStyle.normal.background = MakeTexture(2, 2, new Color(0.35f, 0.35f, 0.35f, 1f));
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

            DrawToolbar();
            DrawLogList();
            DrawDetailArea();
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

            GUILayout.FlexibleSpace();

            // Time.frameCount 표시 토글
            bool newShowFrameCount = GUILayout.Toggle(settings.ShowFrameCount, "Show Frame Count", EditorStyles.toolbarButton);
            if (newShowFrameCount != settings.ShowFrameCount)
            {
                settings.ShowFrameCount = newShowFrameCount;
                Repaint();
            }

            // Time.fixedTime 표시 토글
            bool newShowFixedTime = GUILayout.Toggle(settings.ShowFixedTime, "Show Fixed Time", EditorStyles.toolbarButton);
            if (newShowFixedTime != settings.ShowFixedTime)
            {
                settings.ShowFixedTime = newShowFixedTime;
                Repaint();
            }

            // DateTime.Now 표시 토글
            bool newShowTimestamp = GUILayout.Toggle(settings.ShowTimestamp, "Show Timestamp", EditorStyles.toolbarButton);
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

                EditorGUILayout.BeginHorizontal(backgroundStyle);

                // Frame Count 컬럼 (설정에 따라 표시)
                if (settings.ShowFrameCount)
                {
                    GUILayout.Label($"[{entry.frameCount}]", textStyle, GUILayout.Width(80));
                }

                // 로그 메시지
                string displayMessage = entry.message;
                if (GUILayout.Button(displayMessage, textStyle, GUILayout.ExpandWidth(true)))
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
                
                // 메시지 표시 (레이블 없이)
                EditorGUILayout.LabelField(selectedEntry.message, EditorStyles.wordWrappedLabel);
                
                EditorGUILayout.Space(5);
                
                // 선택적 시간 정보 표시
                if (settings.ShowFrameCount || settings.ShowFixedTime || settings.ShowTimestamp)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    if (settings.ShowFrameCount)
                    {
                        EditorGUILayout.LabelField($"Frame: {selectedEntry.frameCount}", GUILayout.Width(150));
                    }
                    
                    if (settings.ShowFixedTime)
                    {
                        EditorGUILayout.LabelField($"Fixed Time: {selectedEntry.fixedTime:F3}s", GUILayout.Width(150));
                    }
                    
                    if (settings.ShowTimestamp)
                    {
                        EditorGUILayout.LabelField($"Time: {selectedEntry.timestamp:HH:mm:ss.fff}", GUILayout.ExpandWidth(true));
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(5);
                }
                
                // Stack Trace 표시 (UnityEngine.Debug:Log 라인 제거)
                EditorGUILayout.LabelField("Stack Trace:", EditorStyles.boldLabel);
                detailScrollPosition = EditorGUILayout.BeginScrollView(detailScrollPosition);
                string filteredStackTrace = FilterStackTrace(selectedEntry.stackTrace);
                EditorGUILayout.TextArea(filteredStackTrace, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.EndVertical();
            }
        }

        private string FilterStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return stackTrace;

            var lines = stackTrace.Split('\n');
            var filteredLines = new List<string>();

            foreach (var line in lines)
            {
                // UnityEngine.Debug:Log로 시작하는 라인 스킵
                if (line.TrimStart().StartsWith("UnityEngine.Debug:Log"))
                    continue;
                
                filteredLines.Add(line);
            }

            return string.Join("\n", filteredLines);
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
            if (!collapse)
                return logEntries;

            List<ConsoleLogEntry> filtered = new List<ConsoleLogEntry>();
            HashSet<string> uniqueMessages = new HashSet<string>();

            foreach (var entry in logEntries)
            {
                if (uniqueMessages.Add(entry.message))
                {
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
