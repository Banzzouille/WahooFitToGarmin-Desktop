using System.Threading.Tasks;
using WahooFitToGarmin_Desktop.Core.GARMIN.Dto;
using WahooFitToGarmin_Desktop.Core.GARMIN.Dto.Garmin;

namespace WahooFitToGarmin_Desktop.Core.GARMIN
{
    public interface IClient
    {
        public OAuth2Token? OAuth2Token { get; }

        /// <summary>
        /// True while the current session can still be used.
        /// </summary>
        /// <remarks>
        /// The implementation always had this, but it was not on the interface,
        /// so no caller could reach it. The check in use was whether a token
        /// object happened to be non-null, which stays true long after the token
        /// stops working.
        /// </remarks>
        bool IsOAuthValid { get; }
        Task SetOAuth2Token(string accessToken, string tokenSecret);
        Task<GarminAuthenciationResult> Authenticate(string email, string password);
        Task<GarminAuthenciationResult> CompleteMFAAuthAsync(string mfaCode);
        Task<UploadResponse?> UploadActivity(string format, byte[] file, string filePath);
    }
}
