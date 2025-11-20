using System.Collections.Generic;
using UnityEngine;

namespace otps.UnityConsole.Editor
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
        private const string PrefsKey_SearchFilter = "UnityConsole_SearchFilter";
        private const string PrefsKey_Tags = "UnityConsole_Tags";
        private const string PrefsKey_ActiveTag = "UnityConsole_ActiveTag";
        private const string PrefsKey_ActiveChannels = "UnityConsole_ActiveChannels";

        [SerializeField]
        private bool showFrameCount = true;
        
        [SerializeField]
        private bool showFixedTime = false;
        
        [SerializeField]
        private bool showTimestamp = false;
        
        [SerializeField]
        private string searchFilter = "";

        [SerializeField]
        private List<string> tags = new List<string>();
        
        [SerializeField]
        private string activeTag = "";
        
        [SerializeField]
        private HashSet<string> activeChannels = new HashSet<string>();

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

        public string SearchFilter
        {
            get => searchFilter;
            set
            {
                if (searchFilter != value)
                {
                    searchFilter = value;
                    SaveToPrefs();
                }
            }
        }

        public List<string> Tags
        {
            get => tags;
        }

        public string ActiveTag
        {
            get => activeTag;
            set
            {
                if (activeTag != value)
                {
                    activeTag = value;
                    SaveToPrefs();
                }
            }
        }

        public void AddTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag) && !tags.Contains(tag))
            {
                tags.Add(tag);
                SaveToPrefs();
            }
        }

        public void RemoveTag(string tag)
        {
            if (tags.Remove(tag))
            {
                if (activeTag == tag)
                {
                    activeTag = "";
                }
                SaveToPrefs();
            }
        }

        public HashSet<string> ActiveChannels
        {
            get => activeChannels;
        }

        public bool IsChannelActive(string channel)
        {
            return activeChannels.Contains(channel);
        }

        public void ToggleChannel(string channel)
        {
            if (activeChannels.Contains(channel))
            {
                activeChannels.Remove(channel);
            }
            else
            {
                activeChannels.Add(channel);
            }
            SaveToPrefs();
        }

        public void SetChannelActive(string channel, bool active)
        {
            if (active)
            {
                if (activeChannels.Add(channel))
                {
                    SaveToPrefs();
                }
            }
            else
            {
                if (activeChannels.Remove(channel))
                {
                    SaveToPrefs();
                }
            }
        }

        public void AddChannel(string channel)
        {
            if (!string.IsNullOrEmpty(channel) && !tags.Contains(channel))
            {
                tags.Add(channel);
                SaveToPrefs();
            }
        }

        public void RemoveChannel(string channel)
        {
            if (tags.Remove(channel))
            {
                activeChannels.Remove(channel);
                SaveToPrefs();
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
            searchFilter = UnityEditor.EditorPrefs.GetString(PrefsKey_SearchFilter, "");
            
            // 태그 목록 로드 (JSON으로 저장)
            string tagsJson = UnityEditor.EditorPrefs.GetString(PrefsKey_Tags, "");
            if (!string.IsNullOrEmpty(tagsJson))
            {
                tags = new List<string>(JsonUtility.FromJson<TagListWrapper>(tagsJson).tags ?? new string[0]);
            }
            else
            {
                tags = new List<string>();
            }
            
            activeTag = UnityEditor.EditorPrefs.GetString(PrefsKey_ActiveTag, "");
            
            // 활성 채널 목록 로드 (JSON으로 저장)
            string activeChannelsJson = UnityEditor.EditorPrefs.GetString(PrefsKey_ActiveChannels, "");
            if (!string.IsNullOrEmpty(activeChannelsJson))
            {
                var channelArray = JsonUtility.FromJson<TagListWrapper>(activeChannelsJson).tags;
                activeChannels = channelArray != null ? new HashSet<string>(channelArray) : new HashSet<string>();
            }
            else
            {
                activeChannels = new HashSet<string>();
            }
        }

        private void SaveToPrefs()
        {
            UnityEditor.EditorPrefs.SetBool(PrefsKey_ShowFrameCount, showFrameCount);
            UnityEditor.EditorPrefs.SetBool(PrefsKey_ShowFixedTime, showFixedTime);
            UnityEditor.EditorPrefs.SetBool(PrefsKey_ShowTimestamp, showTimestamp);
            UnityEditor.EditorPrefs.SetString(PrefsKey_SearchFilter, searchFilter);
            
            // 태그 목록 저장 (JSON으로 저장)
            TagListWrapper wrapper = new TagListWrapper { tags = tags.ToArray() };
            string tagsJson = JsonUtility.ToJson(wrapper);
            UnityEditor.EditorPrefs.SetString(PrefsKey_Tags, tagsJson);
            
            UnityEditor.EditorPrefs.SetString(PrefsKey_ActiveTag, activeTag);
            
            // 활성 채널 목록 저장 (JSON으로 저장)
            TagListWrapper activeChannelsWrapper = new TagListWrapper { tags = new List<string>(activeChannels).ToArray() };
            string activeChannelsJson = JsonUtility.ToJson(activeChannelsWrapper);
            UnityEditor.EditorPrefs.SetString(PrefsKey_ActiveChannels, activeChannelsJson);
        }
        
        [System.Serializable]
        private class TagListWrapper
        {
            public string[] tags;
        }
    }
}
