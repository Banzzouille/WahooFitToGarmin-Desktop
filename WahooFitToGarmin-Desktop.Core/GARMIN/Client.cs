using System;
using System.IO;
using System.Threading.Tasks;
using Flurl.Http;

using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Core.GARMIN.Dto;
using WahooFitToGarmin_Desktop.Core.GARMIN.Dto.Garmin;

namespace WahooFitToGarmin_Desktop.Core.GARMIN
{
    internal partial class Client : IClient
    {
        private readonly string _consumerKey;
        private readonly string _consumerSecret;
        private readonly ILogger _logger;
        private AuthStatus _authStatus;
        private string _mfaCsrfToken = string.Empty;
        CookieJar? _cookieJar = null;

        private static readonly object _commonQueryParams = new
        {
            id = "gauth-widget",
            embedWidget = "true",
            gauthHost = URLs.SSO_EMBED_URL,
            redirectAfterAccountCreationUrl = URLs.SSO_EMBED_URL,
            redirectAfterAccountLoginUrl = URLs.SSO_EMBED_URL,
            service = URLs.SSO_EMBED_URL,
            source = URLs.SSO_EMBED_URL,
        };

        public OAuth2Token? OAuth2Token { get; private set; }
        public DateTime _oAuth2TokenValidUntil { get; private set; }


        internal Client(string consumerKey, string consumerSecret, ILogger logger)
        {
            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;
            _logger = logger;
        }

        public bool IsOAuthValid
        {
            get
            {
                if (this.OAuth2Token == null)
                {
                    return false;
                }

                return DateTime.UtcNow < _oAuth2TokenValidUntil;
            }
        }

        public async Task<UploadResponse?> UploadActivity(string format, byte[] file, string filePath)
        {
            UploadResponse? response = null;
            var fileName = Path.GetFileName(filePath);
            try
            {
                using (var stream = new MemoryStream(file))
                {
                    response = await $"{URLs.UPLOAD_URL}/{format}"
                 .WithOAuthBearerToken(OAuth2Token!.Access_Token)
                 .WithHeader("NK", "NT")
                 .WithHeader("origin", URLs.ORIGIN)
                 .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                 .AllowHttpStatus("2xx,409")
                 .PostMultipartAsync((data) =>
                 {
                     data.AddFile("\"file\"", stream, contentType: "application/octet-stream", fileName: $"\"{fileName}\"");
                     //data.AddFile("\"file\"", path: filePath, contentType: "application/octet-stream", fileName: $"\"{fileName}\"");

                 })
                 .ReceiveJson<UploadResponse>()
                 ;
                }
            }


            catch (FlurlHttpException ex)
            {
                // `throw ex;` here reset the stack trace to this line, hiding
                // where the failure actually came from. `throw;` preserves it.
                _logger.LogError(
                    ex,
                    "Garmin upload failed for {FileName} with status {Status}",
                    fileName,
                    ex.StatusCode);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Garmin upload failed for {FileName}", fileName);
                throw;
            }

            return response;
        }

    }
}
