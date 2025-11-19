using UnityEngine;

namespace TrollGames.UnityConsole.Editor
{
    /// <summary>
    /// 콘솔 설정을 관리하는 ScriptableObject
    /// </summary>
    public class ConsoleSettings : ScriptableObject
    {
        private const string SettingsPath = "Assets/Editor/ConsoleSettings.asset";
        private const string PrefsKey_ShowFrameCount = "UnityConsole_ShowFrameCount";
        private const string PrefsKey_ShowFixedTime = "UnityConsole_ShowFixedTime";
        private const string PrefsKey_ShowTimestamp = "UnityConsole_ShowTimestamp";

        [SerializeField]
        private bool showFrameCount = true;
        
        [SerializeField]
        private bool showFixedTime = false;
        
        [SerializeField]
        private bool showTimestamp = false;

        public bool ShowFrameCount
        {
            get => showFrameCount;
            set
            {
                if (showFrameCount != value)
                {
                    showFrameCount = value;
                    SaveToPrefs();
                }
            }
        }

        public bool ShowFixedTime
        {
            get => showFixedTime;
            set
            {
                if (showFixedTime != value)
                {
                    showFixedTime = value;
                    SaveToPrefs();
                }
            }
        }

        public bool ShowTimestamp
        {
            get => showTimestamp;
            set
            {
                if (showTimestamp != value)
                {
                    showTimestamp = value;
                    SaveToPrefs();
                }
            }
        }

        private static ConsoleSettings instance;

        public static ConsoleSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = CreateInstance<ConsoleSettings>();
                    instance.LoadFromPrefs();
                }
                return instance;
            }
        }

        private void LoadFromPrefs()
        {
            showFrameCount = UnityEditor.EditorPrefs.GetBool(PrefsKey_ShowFrameCount, true);
            showFixedTime = UnityEditor.EditorPrefs.GetBool(PrefsKey_ShowFixedTime, false);
            showTimestamp = UnityEditor.EditorPrefs.GetBool(PrefsKey_ShowTimestamp, false);
        }

        private void SaveToPrefs()
        {
            UnityEditor.EditorPrefs.SetBool(PrefsKey_ShowFrameCount, showFrameCount);
            UnityEditor.EditorPrefs.SetBool(PrefsKey_ShowFixedTime, showFixedTime);
            UnityEditor.EditorPrefs.SetBool(PrefsKey_ShowTimestamp, showTimestamp);
        }
    }
}
