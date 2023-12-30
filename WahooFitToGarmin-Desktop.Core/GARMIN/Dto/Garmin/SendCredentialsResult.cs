namespace WahooFitToGarmin_Desktop.Core.GARMIN.Dto.Garmin
{
    public class SendCredentialsResult
    {
        public bool WasRedirected { get; set; }
        public string RedirectedTo { get; set; }
        public string RawResponseBody { get; set; }
    }
}
