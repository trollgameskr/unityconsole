using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace otps.UnityConsole.Editor
{
    /// <summary>
    /// Time.frameCount를 표시하는 확장된 Unity 콘솔 창
    /// </summary>
    public class EnhancedConsoleWindow : EditorWindow
    {
        private List<ConsoleLogEntry> logEntries = new List<ConsoleLogEntry>();
        private bool collapse = false;
        private bool clearOnPlay = false;
        private bool errorPause = false;
        
        private bool showLog = true;
        private bool showWarning = true;
        private bool showError = true;

        private int selectedIndex = -1;
        
        private double lastClickTime;
        private int lastClickedIndex = -1;

        private ConsoleSettings settings;
        
        private bool wasScrollAtBottom = true;
        
        private string newTagInput = "";

        // 성능 최적화: 캐싱
        private const int MaxLogEntries = 10000;
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
        
        // UI Elements
        private ListView logListView;
        private ScrollView detailScrollView;
        private Label logCountLabel;
        private Label warningCountLabel;
        private Label errorCountLabel;
        private TextField searchField;
        private TextField tagInputField;
        private VisualElement tagContainer;
        private Label activeTagLabel;
        private Button clearActiveTagButton;
        private Toolbar tagBar;

        [MenuItem("Window/Enhanced Console %#&c")]
        public static void ShowWindow()
        {
            var window = GetWindow<EnhancedConsoleWindow>("Enhanced Console");
            window.Show();
        }

        private void OnEnable()
        {
            settings = ConsoleSettings.Instance;
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();

            // 인라인 스타일 적용
            ApplyInlineStyles(root);

            // 메인 컨테이너
            var mainContainer = new VisualElement();
            mainContainer.name = "main-container";
            mainContainer.style.flexGrow = 1;
            root.Add(mainContainer);

            // 툴바
            var toolbar = CreateToolbar();
            mainContainer.Add(toolbar);

            // 태그 바
            tagBar = CreateTagBar();
            mainContainer.Add(tagBar);

            // 분할 뷰 (로그 리스트 + 디테일)
            var splitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Vertical);
            splitView.style.flexGrow = 1;
            mainContainer.Add(splitView);

            // 로그 리스트
            var logListContainer = new VisualElement();
            logListContainer.style.flexGrow = 1;
            logListView = CreateLogListView();
            logListContainer.Add(logListView);
            splitView.Add(logListContainer);

            // 디테일 영역
            detailScrollView = new ScrollView(ScrollViewMode.Vertical);
            detailScrollView.name = "detail-scroll-view";
            detailScrollView.style.flexGrow = 1;
            splitView.Add(detailScrollView);

            // 키보드 이벤트 핸들러 등록
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);

            RefreshLogListView();
        }

        private void ApplyInlineStyles(VisualElement root)
        {
            // 스타일은 각 요소에 직접 적용됨
        }

        private Toolbar CreateToolbar()
        {
            var toolbar = new Toolbar();
            toolbar.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f);
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.12f, 0.12f, 0.12f);

            // Clear 버튼
            var clearButton = new ToolbarButton(() =>
            {
                logEntries.Clear();
                selectedIndex = -1;
                InvalidateCache();
                RefreshLogListView();
            }) { text = "Clear" };
            toolbar.Add(clearButton);

            // 공백
            toolbar.Add(new VisualElement { style = { width = 5 } });

            // Collapse 토글
            var collapseToggle = new ToolbarToggle { text = "Collapse", value = collapse };
            collapseToggle.RegisterValueChangedCallback(evt =>
            {
                collapse = evt.newValue;
                InvalidateCache();
                RefreshLogListView();
            });
            toolbar.Add(collapseToggle);

            // Clear on Play 토글
            var clearOnPlayToggle = new ToolbarToggle { text = "Clear on Play", value = clearOnPlay };
            clearOnPlayToggle.RegisterValueChangedCallback(evt => clearOnPlay = evt.newValue);
            toolbar.Add(clearOnPlayToggle);

            // Error Pause 토글
            var errorPauseToggle = new ToolbarToggle { text = "Error Pause", value = errorPause };
            errorPauseToggle.RegisterValueChangedCallback(evt => errorPause = evt.newValue);
            toolbar.Add(errorPauseToggle);

            // 공백
            toolbar.Add(new VisualElement { style = { width = 5 } });

            // 검색 레이블
            var searchLabel = new Label("검색:");
            searchLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            searchLabel.style.width = 35;
            toolbar.Add(searchLabel);

            // 검색 필드
            searchField = new TextField { value = settings.SearchFilter };
            searchField.style.width = 150;
            searchField.RegisterValueChangedCallback(evt =>
            {
                settings.SearchFilter = evt.newValue;
                InvalidateCache();
                RefreshLogListView();
            });
            toolbar.Add(searchField);

            // Flexible space
            var flexSpace = new VisualElement();
            flexSpace.style.flexGrow = 1;
            toolbar.Add(flexSpace);

            // Frame 토글
            var frameToggle = new ToolbarToggle { text = "Frame", value = settings.ShowFrameCount };
            frameToggle.RegisterValueChangedCallback(evt =>
            {
                settings.ShowFrameCount = evt.newValue;
                RefreshLogListView();
            });
            toolbar.Add(frameToggle);

            // GameTime 토글
            var gameTimeToggle = new ToolbarToggle { text = "GameTime", value = settings.ShowFixedTime };
            gameTimeToggle.RegisterValueChangedCallback(evt =>
            {
                settings.ShowFixedTime = evt.newValue;
                RefreshLogListView();
            });
            toolbar.Add(gameTimeToggle);

            // Time 토글
            var timeToggle = new ToolbarToggle { text = "Time", value = settings.ShowTimestamp };
            timeToggle.RegisterValueChangedCallback(evt =>
            {
                settings.ShowTimestamp = evt.newValue;
                RefreshLogListView();
            });
            toolbar.Add(timeToggle);

            // 공백
            toolbar.Add(new VisualElement { style = { width = 5 } });

            // 로그 카운트 토글 (아이콘 포함)
            var logToggle = CreateLogCountToggle(LogType.Log, "console.infoicon.sml");
            toolbar.Add(logToggle);

            var warningToggle = CreateLogCountToggle(LogType.Warning, "console.warnicon.sml");
            toolbar.Add(warningToggle);

            var errorToggle = CreateLogCountToggle(LogType.Error, "console.erroricon.sml");
            toolbar.Add(errorToggle);

            return toolbar;
        }

        private VisualElement CreateLogCountToggle(LogType logType, string iconName)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.paddingLeft = 4;
            container.style.paddingRight = 4;
            container.style.paddingTop = 2;
            container.style.paddingBottom = 2;
            container.style.minWidth = 40;

            var toggle = new Toggle();
            toggle.style.marginRight = 2;
            
            if (logType == LogType.Log)
            {
                toggle.value = showLog;
                toggle.RegisterValueChangedCallback(evt =>
                {
                    showLog = evt.newValue;
                    InvalidateCache();
                    RefreshLogListView();
                });
            }
            else if (logType == LogType.Warning)
            {
                toggle.value = showWarning;
                toggle.RegisterValueChangedCallback(evt =>
                {
                    showWarning = evt.newValue;
                    InvalidateCache();
                    RefreshLogListView();
                });
            }
            else if (logType == LogType.Error)
            {
                toggle.value = showError;
                toggle.RegisterValueChangedCallback(evt =>
                {
                    showError = evt.newValue;
                    InvalidateCache();
                    RefreshLogListView();
                });
            }

            container.Add(toggle);

            var icon = new Image();
            var iconContent = EditorGUIUtility.IconContent(iconName);
            icon.image = iconContent.image;
            icon.style.width = 16;
            icon.style.height = 16;
            icon.style.marginRight = 2;
            container.Add(icon);

            Label countLabel = new Label(GetLogCount(logType).ToString());
            
            if (logType == LogType.Log)
                logCountLabel = countLabel;
            else if (logType == LogType.Warning)
                warningCountLabel = countLabel;
            else if (logType == LogType.Error)
                errorCountLabel = countLabel;
                
            container.Add(countLabel);

            return container;
        }

        private Toolbar CreateTagBar()
        {
            var toolbar = new Toolbar();
            toolbar.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f);
            toolbar.style.borderBottomWidth = 1;
            toolbar.style.borderBottomColor = new Color(0.12f, 0.12f, 0.12f);

            // 태그 레이블
            var tagLabel = new Label("태그:");
            tagLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            tagLabel.style.width = 35;
            toolbar.Add(tagLabel);

            // 태그 입력 필드
            tagInputField = new TextField { value = newTagInput };
            tagInputField.style.width = 100;
            tagInputField.RegisterValueChangedCallback(evt => newTagInput = evt.newValue);
            toolbar.Add(tagInputField);

            // 태그 추가 버튼
            var addTagButton = new ToolbarButton(() =>
            {
                if (!string.IsNullOrEmpty(newTagInput.Trim()))
                {
                    settings.AddTag(newTagInput.Trim());
                    newTagInput = "";
                    tagInputField.value = "";
                    RefreshTagBar();
                }
            }) { text = "+" };
            addTagButton.style.width = 25;
            toolbar.Add(addTagButton);

            // 공백
            toolbar.Add(new VisualElement { style = { width = 5 } });

            // 활성 태그 레이블 및 클리어 버튼 컨테이너
            var activeTagContainer = new VisualElement();
            activeTagContainer.name = "active-tag-container";
            activeTagContainer.style.flexDirection = FlexDirection.Row;
            toolbar.Add(activeTagContainer);

            if (!string.IsNullOrEmpty(settings.ActiveTag))
            {
                activeTagLabel = new Label($"필터: {settings.ActiveTag}");
                activeTagLabel.style.color = new Color(0, 1, 1);
                activeTagLabel.style.width = 100;
                activeTagLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                activeTagContainer.Add(activeTagLabel);

                clearActiveTagButton = new ToolbarButton(() =>
                {
                    settings.ActiveTag = "";
                    InvalidateCache();
                    RefreshTagBar();
                    RefreshLogListView();
                }) { text = "X" };
                clearActiveTagButton.style.width = 25;
                activeTagContainer.Add(clearActiveTagButton);
                
                toolbar.Add(new VisualElement { style = { width = 5 } });
            }

            // 태그 컨테이너
            tagContainer = new VisualElement();
            tagContainer.style.flexDirection = FlexDirection.Row;
            tagContainer.style.flexGrow = 1;
            toolbar.Add(tagContainer);

            RefreshTagButtons();

            return toolbar;
        }

        private void RefreshTagBar()
        {
            if (tagBar == null)
                return;

            // 활성 태그 컨테이너 찾기
            var activeTagContainer = tagBar.Q<VisualElement>("active-tag-container");
            if (activeTagContainer != null)
            {
                activeTagContainer.Clear();

                if (!string.IsNullOrEmpty(settings.ActiveTag))
                {
                    activeTagLabel = new Label($"필터: {settings.ActiveTag}");
                    activeTagLabel.style.color = new Color(0, 1, 1);
                    activeTagLabel.style.width = 100;
                    activeTagLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                    activeTagContainer.Add(activeTagLabel);

                    clearActiveTagButton = new ToolbarButton(() =>
                    {
                        settings.ActiveTag = "";
                        InvalidateCache();
                        RefreshTagBar();
                        RefreshLogListView();
                    }) { text = "X" };
                    clearActiveTagButton.style.width = 25;
                    activeTagContainer.Add(clearActiveTagButton);
                }
            }

            RefreshTagButtons();
        }

        private void RefreshTagButtons()
        {
            if (tagContainer == null)
                return;

            tagContainer.Clear();

            foreach (var tag in settings.Tags)
            {
                bool isActive = settings.ActiveTag == tag;
                
                var tagButton = new ToolbarButton(() =>
                {
                    if (settings.ActiveTag == tag)
                        settings.ActiveTag = "";
                    else
                        settings.ActiveTag = tag;
                    
                    InvalidateCache();
                    RefreshTagBar();
                    RefreshLogListView();
                }) { text = tag };
                
                tagButton.style.minWidth = 50;
                
                if (isActive)
                {
                    tagButton.style.color = new Color(0, 1, 1);
                }

                tagContainer.Add(tagButton);

                // 삭제 버튼
                var removeButton = new ToolbarButton(() =>
                {
                    settings.RemoveTag(tag);
                    InvalidateCache();
                    RefreshTagBar();
                    RefreshLogListView();
                }) { text = "-" };
                removeButton.style.width = 20;
                tagContainer.Add(removeButton);

                tagContainer.Add(new VisualElement { style = { width = 2 } });
            }
        }

        private ListView CreateLogListView()
        {
            var listView = new ListView
            {
                selectionType = SelectionType.Single,
                fixedItemHeight = 20,
                style = { flexGrow = 1 }
            };

            listView.makeItem = () =>
            {
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Row;
                container.style.alignItems = Align.Center;
                container.style.paddingLeft = 2;
                container.style.paddingRight = 5;
                container.style.paddingTop = 2;
                container.style.paddingBottom = 2;
                container.style.minHeight = 20;

                // 아이콘
                var icon = new Image();
                icon.style.width = 16;
                icon.style.height = 16;
                icon.style.marginRight = 4;
                container.Add(icon);

                // Frame Count
                var frameLabel = new Label();
                frameLabel.name = "frame-label";
                frameLabel.style.width = 60;
                frameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                frameLabel.style.color = Color.white;
                container.Add(frameLabel);

                // Fixed Time
                var timeLabel = new Label();
                timeLabel.name = "time-label";
                timeLabel.style.width = 70;
                timeLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                timeLabel.style.color = Color.white;
                container.Add(timeLabel);

                // Timestamp
                var timestampLabel = new Label();
                timestampLabel.name = "timestamp-label";
                timestampLabel.style.width = 75;
                timestampLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                timestampLabel.style.color = Color.white;
                container.Add(timestampLabel);

                // Message
                var messageLabel = new Label();
                messageLabel.name = "message-label";
                messageLabel.style.flexGrow = 1;
                messageLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                messageLabel.style.color = Color.white;
                container.Add(messageLabel);

                return container;
            };

            listView.bindItem = (element, index) =>
            {
                var filteredEntries = GetFilteredEntries();
                if (index < 0 || index >= filteredEntries.Count)
                    return;

                var entry = filteredEntries[index];
                int actualIndex = logEntries.IndexOf(entry);
                
                // 배경색 설정
                if (actualIndex == selectedIndex)
                {
                    element.style.backgroundColor = new Color(0.24f, 0.37f, 0.54f);
                }
                else if (index % 2 == 0)
                {
                    element.style.backgroundColor = new Color(0.17f, 0.17f, 0.17f);
                }
                else
                {
                    element.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
                }

                // 아이콘
                var icon = element.Q<Image>();
                var iconContent = GetIconContentForLogType(entry.logType);
                icon.image = iconContent.image;

                // Frame Count
                var frameLabel = element.Q<Label>("frame-label");
                if (settings.ShowFrameCount)
                {
                    frameLabel.text = $"[{entry.frameCount}]";
                    frameLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    frameLabel.style.display = DisplayStyle.None;
                }

                // Fixed Time
                var timeLabel = element.Q<Label>("time-label");
                if (settings.ShowFixedTime)
                {
                    timeLabel.text = $"[{entry.fixedTime:F2}s]";
                    timeLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    timeLabel.style.display = DisplayStyle.None;
                }

                // Timestamp
                var timestampLabel = element.Q<Label>("timestamp-label");
                if (settings.ShowTimestamp)
                {
                    timestampLabel.text = $"[{entry.timestamp:HH:mm:ss}]";
                    timestampLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    timestampLabel.style.display = DisplayStyle.None;
                }

                // Message
                var messageLabel = element.Q<Label>("message-label");
                messageLabel.text = entry.message;
                
                // 텍스트 색상
                if (entry.logType == LogType.Warning)
                {
                    messageLabel.style.color = Color.yellow;
                }
                else if (entry.logType == LogType.Error || entry.logType == LogType.Exception || entry.logType == LogType.Assert)
                {
                    messageLabel.style.color = Color.red;
                }
                else
                {
                    messageLabel.style.color = Color.white;
                }
            };

            listView.selectionChanged += (items) =>
            {
                var selectedItems = new List<object>(items);
                if (selectedItems.Count > 0)
                {
                    var filteredEntries = GetFilteredEntries();
                    int index = listView.selectedIndex;
                    if (index >= 0 && index < filteredEntries.Count)
                    {
                        selectedIndex = logEntries.IndexOf(filteredEntries[index]);
                        UpdateDetailView();
                        RefreshLogListView(); // 선택 상태 반영
                    }
                }
            };

            // 더블 클릭 핸들러
            listView.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && selectedIndex >= 0 && selectedIndex < logEntries.Count)
                {
                    OpenScriptFromStackTrace(logEntries[selectedIndex].stackTrace);
                }
            });

            return listView;
        }

        private void RefreshLogListView()
        {
            if (logListView == null)
                return;

            var filteredEntries = GetFilteredEntries();
            logListView.itemsSource = filteredEntries;
            logListView.Rebuild();

            // 카운트 레이블 업데이트
            if (logCountLabel != null)
                logCountLabel.text = GetLogCount(LogType.Log).ToString();
            if (warningCountLabel != null)
                warningCountLabel.text = GetLogCount(LogType.Warning).ToString();
            if (errorCountLabel != null)
                errorCountLabel.text = GetLogCount(LogType.Error).ToString();
        }

        private void UpdateDetailView()
        {
            if (detailScrollView == null)
                return;

            detailScrollView.Clear();

            if (selectedIndex >= 0 && selectedIndex < logEntries.Count)
            {
                var selectedEntry = logEntries[selectedIndex];

                // 메시지
                var messageLabel = new Label(selectedEntry.message);
                messageLabel.style.paddingLeft = 5;
                messageLabel.style.paddingRight = 5;
                messageLabel.style.paddingTop = 5;
                messageLabel.style.paddingBottom = 5;
                messageLabel.style.whiteSpace = WhiteSpace.Normal;
                messageLabel.style.color = Color.white;
                detailScrollView.Add(messageLabel);

                // 공백
                detailScrollView.Add(new VisualElement { style = { height = 5 } });

                // 스택 트레이스
                DrawStackTraceLines(selectedEntry.stackTrace);
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

                var lineLabel = new Label(line);
                lineLabel.style.paddingLeft = 5;
                lineLabel.style.paddingRight = 5;
                lineLabel.style.paddingTop = 2;
                lineLabel.style.paddingBottom = 2;
                lineLabel.style.whiteSpace = WhiteSpace.Normal;
                lineLabel.style.color = hasFileReference ? new Color(0.37f, 0.62f, 1.0f) : new Color(0.8f, 0.8f, 0.8f);

                if (hasFileReference)
                {
                    // 클릭 이벤트
                    lineLabel.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        OpenScriptFromStackTraceLine(line);
                    });
                    
                    // 커서 변경
                    lineLabel.RegisterCallback<MouseEnterEvent>(evt =>
                    {
                        lineLabel.style.color = new Color(0.56f, 0.78f, 1.0f);
                    });
                    
                    lineLabel.RegisterCallback<MouseLeaveEvent>(evt =>
                    {
                        lineLabel.style.color = new Color(0.37f, 0.62f, 1.0f);
                    });
                }

                detailScrollView.Add(lineLabel);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            bool handled = false;

            if (evt.keyCode == KeyCode.DownArrow)
            {
                // 다음 에러로 이동
                if (selectedIndex >= 0)
                {
                    int nextErrorIndex = FindNextError(selectedIndex);
                    if (nextErrorIndex >= 0)
                    {
                        selectedIndex = nextErrorIndex;
                        ScrollToSelectedLog();
                        UpdateDetailView();
                        handled = true;
                    }
                }
            }
            else if (evt.keyCode == KeyCode.UpArrow)
            {
                // 이전 에러로 이동
                if (selectedIndex >= 0)
                {
                    int prevErrorIndex = FindPreviousError(selectedIndex);
                    if (prevErrorIndex >= 0)
                    {
                        selectedIndex = prevErrorIndex;
                        ScrollToSelectedLog();
                        UpdateDetailView();
                        handled = true;
                    }
                }
            }

            if (handled)
            {
                evt.StopPropagation();
            }
        }

        private void ScrollToSelectedLog()
        {
            if (selectedIndex < 0 || selectedIndex >= logEntries.Count || logListView == null)
                return;

            // 선택된 로그를 filteredEntries에서 찾기
            List<ConsoleLogEntry> filteredEntries = GetFilteredEntries();
            ConsoleLogEntry selectedEntry = logEntries[selectedIndex];
            int filteredIndex = filteredEntries.IndexOf(selectedEntry);

            if (filteredIndex >= 0)
            {
                logListView.selectedIndex = filteredIndex;
                logListView.ScrollToItem(filteredIndex);
            }
        }

        private GUIContent GetIconContentForLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return EditorGUIUtility.IconContent("console.warnicon.sml");
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return EditorGUIUtility.IconContent("console.erroricon.sml");
                default:
                    return EditorGUIUtility.IconContent("console.infoicon.sml");
            }
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

            logEntries.Add(new ConsoleLogEntry(logString, stackTrace, type));
            InvalidateCache();

            if (errorPause && type == LogType.Error)
            {
                Debug.Break();
            }

            // UI 업데이트
            EditorApplication.delayCall += () =>
            {
                RefreshLogListView();
                
                // 스크롤이 하단에 있으면 자동으로 스크롤
                if (wasScrollAtBottom && logListView != null)
                {
                    var filteredEntries = GetFilteredEntries();
                    if (filteredEntries.Count > 0)
                    {
                        logListView.ScrollToItem(filteredEntries.Count - 1);
                    }
                }
            };
        }
        
        private void InvalidateCache()
        {
            cachedFilteredEntries = null;
            cachedLogCount = -1;
            cachedWarningCount = -1;
            cachedErrorCount = -1;
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
            if (cachedFilteredEntries != null && 
                lastSearchFilter == settings.SearchFilter &&
                lastActiveTag == settings.ActiveTag &&
                lastCollapse == collapse &&
                lastShowLog == showLog &&
                lastShowWarning == showWarning &&
                lastShowError == showError)
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
            
            List<ConsoleLogEntry> filtered = new List<ConsoleLogEntry>();
            
            // 검색 필터 적용
            bool hasSearchFilter = !string.IsNullOrEmpty(settings.SearchFilter);
            bool hasTagFilter = !string.IsNullOrEmpty(settings.ActiveTag);
            
            if (collapse)
            {
                HashSet<string> uniqueMessages = new HashSet<string>();

                foreach (var entry in logEntries)
                {
                    // 로그 타입 필터 확인
                    if (!ShouldShowLog(entry.logType))
                        continue;
                    
                    // 검색 필터 확인 (대소문자 구분 없이)
                    if (hasSearchFilter && entry.message.IndexOf(settings.SearchFilter, StringComparison.OrdinalIgnoreCase) == -1)
                        continue;
                    
                    // 태그 필터 확인 (대소문자 구분 없이)
                    if (hasTagFilter && entry.message.IndexOf(settings.ActiveTag, StringComparison.OrdinalIgnoreCase) == -1)
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
                    // 로그 타입 필터 확인
                    if (!ShouldShowLog(entry.logType))
                        continue;
                    
                    // 검색 필터 확인 (대소문자 구분 없이)
                    if (hasSearchFilter && entry.message.IndexOf(settings.SearchFilter, StringComparison.OrdinalIgnoreCase) == -1)
                        continue;
                    
                    // 태그 필터 확인 (대소문자 구분 없이)
                    if (hasTagFilter && entry.message.IndexOf(settings.ActiveTag, StringComparison.OrdinalIgnoreCase) == -1)
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
