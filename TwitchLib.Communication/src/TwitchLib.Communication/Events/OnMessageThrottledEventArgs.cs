using System;

namespace TwitchLib.Communication.Events
{
    public class OnMessageThrottledEventArgs : EventArgs
    {
        public string Message { get; set; }
        public int SentMessageCount { get; set; }
        public TimeSpan Period { get; set; }
        public int AllowedInPeriod { get; set; }
    }
}
