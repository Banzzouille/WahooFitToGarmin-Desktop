using System;

namespace WahooFitToGarmin_Desktop.Core.GARMIN.Dto
{

    public class GarminClientException : Exception
    {
        public AuthStatus AuthStatus { get; set; }

        public GarminClientException(AuthStatus curentStatus, string message) : base(message) {
            AuthStatus = curentStatus;
        }
        public GarminClientException(AuthStatus curentStatus, string message, Exception innerException) : base(message, innerException) {
            AuthStatus = curentStatus;
        }
    }
}
