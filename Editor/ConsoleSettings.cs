using UnityEngine;

namespace TrollGames.UnityConsole.Editor
{
    /// <summary>
    /// 콘솔 설정을 관리하는 ScriptableObject
    /// </summary>
    public class ConsoleSettings : ScriptableObject
    {
        private const string SettingsPath = "Assets/Editor/ConsoleSettings.asset";
        private const string PrefsKey = "UnityConsole_ShowFrameCount";

        [SerializeField]
        private bool showFrameCount = true;

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
            showFrameCount = UnityEditor.EditorPrefs.GetBool(PrefsKey, true);
        }

        private void SaveToPrefs()
        {
            UnityEditor.EditorPrefs.SetBool(PrefsKey, showFrameCount);
        }
    }
}
