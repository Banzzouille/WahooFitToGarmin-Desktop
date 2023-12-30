namespace WahooFitToGarmin_Desktop.Core.GARMIN.Dto
{
    public class GarminAuthenciationResult
    {
        public bool IsSuccess { get; set; }
        public bool Response { get; set; }
        public string Error { get; set; }
        public bool MFACodeRequested { get; set; }
    }
}
