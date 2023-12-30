namespace WahooFitToGarmin_Desktop.Core.GARMIN
{
    public class MagicStrings
    {
        public static string USER_AGENT = "com.garmin.android.apps.connectmobile";
        public static string CSRF_REGEX = "name=\"_csrf\"\\s+value=\"(?<csrf>.+?)\"";
        public static string TICKET_REGEX = "embed\\?ticket=(?<ticket>[^\"]+)\"";
    }
}
