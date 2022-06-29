namespace WahooFitToGarmin_Desktop.Models
{
    public class AppConfig
    {
        public string ConfigurationsFolder { get; set; }

        public string AppPropertiesFileName { get; set; }

        public string GithubUrl { get; set; }

        public string WahooDropBoxFolder { get; set; }

        public string GarminLogin { get; set; }

        public string GarminPwd { get; set; }

        public bool KeepUploadedActivityFile { get; set; }
    }
}
