using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace otps.UnityConsole
{
    /// <summary>
    /// 콘솔 로그 항목을 나타내는 클래스
    /// </summary>
    [Serializable]
    public class ConsoleLogEntry
    {
        public string message;
        public string stackTrace;
        public LogType logType;
        public int frameCount;
        public float fixedTime;
        public DateTime timestamp;
        
        private List<string> channels;
        private static readonly Regex channelRegex = new Regex(@"\[([^\]]+)\]", RegexOptions.Compiled);

        public ConsoleLogEntry(string message, string stackTrace, LogType logType)
        {
            this.message = message;
            this.stackTrace = stackTrace;
            this.logType = logType;
            this.frameCount = Time.frameCount;
            this.fixedTime = Time.fixedTime;
            this.timestamp = DateTime.Now;
            
            // 메시지에서 채널 파싱
            ParseChannels();
        }
        
        /// <summary>
        /// 메시지에서 [채널명] 형식의 태그를 파싱
        /// </summary>
        private void ParseChannels()
        {
            channels = new List<string>();
            
            if (string.IsNullOrEmpty(message))
                return;
            
            MatchCollection matches = channelRegex.Matches(message);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    string channel = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(channel) && !channels.Contains(channel))
                    {
                        channels.Add(channel);
                    }
                }
            }
        }
        
        /// <summary>
        /// 이 로그 항목이 가지고 있는 채널 목록을 반환
        /// </summary>
        public List<string> GetChannels()
        {
            if (channels == null)
            {
                ParseChannels();
            }
            return channels;
        }
        
        /// <summary>
        /// 특정 채널을 포함하는지 확인
        /// </summary>
        public bool HasChannel(string channel)
        {
            if (channels == null)
            {
                ParseChannels();
            }
            return channels.Contains(channel);
        }
        
        /// <summary>
        /// 여러 채널 중 하나라도 포함하는지 확인
        /// </summary>
        public bool HasAnyChannel(List<string> channelList)
        {
            if (channels == null)
            {
                ParseChannels();
            }
            
            if (channelList == null || channelList.Count == 0)
                return true;
            
            foreach (string channel in channelList)
            {
                if (channels.Contains(channel))
                    return true;
            }
            
            return false;
        }
    }
}
