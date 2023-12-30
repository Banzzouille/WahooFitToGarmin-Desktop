using System.Threading.Tasks;
using WahooFitToGarmin_Desktop.Core.GARMIN.Dto;
using WahooFitToGarmin_Desktop.Core.GARMIN.Dto.Garmin;

namespace WahooFitToGarmin_Desktop.Core.GARMIN
{
    public interface IClient
    {
        public OAuth2Token OAuth2Token { get; }
        Task SetOAuth2Token(string accessToken, string tokenSecret);
        Task<GarminAuthenciationResult> Authenticate(string email, string password);
        Task<GarminAuthenciationResult> CompleteMFAAuthAsync(string mfaCode);
        Task<UploadResponse> UploadActivity(string format, byte[] file, string filePath);
    }
}
