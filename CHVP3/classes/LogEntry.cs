using System;

namespace Wasabi
{
    public class LogEntry : PropertyChangedBase
    {

        protected static int IndexCount;

        public DateTime DateTime { get; set; }
        public int Index { get; set; }
        public string Message { get; set; }

        public LogEntry() { }

        public LogEntry(string message)
        {
            this.Index = IndexCount++;
            this.DateTime = DateTime.Now;
            this.Message = message;
        }

    }

}