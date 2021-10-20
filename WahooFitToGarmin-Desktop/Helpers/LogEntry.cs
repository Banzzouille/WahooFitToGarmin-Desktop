using System;
using System.IO;

namespace WahooFitToGarmin_Desktop.Helpers
{
    public class LogEntry : PropertyChangedBase
    {
        private DateTime _dateTime;
        private string _message;

        public DateTime LogDateTime => _dateTime;
        public string LogMessage => _message;

        public LogEntry( string message)
        {
            _dateTime = DateTime.Now;
            _message = message;
            using var w = File.AppendText("WahooFitToGarmin-Desktop.log");
            w.WriteLine($"{_dateTime:u}  : {_message}");
            
        }
        
    }
}
