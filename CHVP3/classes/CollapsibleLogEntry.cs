using System;
using System.Collections.Generic;

namespace Wasabi
{
    public class CollapsibleLogEntry : LogEntry
    {
        public List<LogEntry> Contents { get; set; }

        public CollapsibleLogEntry() { }

        public CollapsibleLogEntry(string message, List<LogEntry> contents)
        {
            this.Index = IndexCount++;
            this.DateTime = DateTime.Now;
            this.Message = message;
            this.Contents = contents;
        }
    }

}