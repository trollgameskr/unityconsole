using System;
using UnityEngine;

namespace TrollGames.UnityConsole
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
        public DateTime timestamp;

        public ConsoleLogEntry(string message, string stackTrace, LogType logType)
        {
            this.message = message;
            this.stackTrace = stackTrace;
            this.logType = logType;
            this.frameCount = Time.frameCount;
            this.timestamp = DateTime.Now;
        }
    }
}
