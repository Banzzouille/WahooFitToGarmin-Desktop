using System;
using System.Collections.Generic;

namespace WahooFitToGarmin_Desktop.Helpers
{
    public class LogEntry : PropertyChangedBase
    {
        public DateTime DateTime { get; set; }

        public string Message { get; set; }
    }
}
