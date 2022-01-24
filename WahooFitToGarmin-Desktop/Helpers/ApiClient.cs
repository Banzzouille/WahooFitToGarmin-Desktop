using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace WahooFitToGarmin_Desktop.Helpers
{
	public class ApiClient
	{
        private readonly string _email;
        private readonly string _password;
        private const string BASE_URL = "https://connect.garmin.com";
		private const string SSO_URL = "https://sso.garmin.com";
		private const string SIGNIN_URL = "https://sso.garmin.com/sso/signin";

		private static string PROFILE_URL = $"{BASE_URL}/modern/currentuser-service/user/info";
		private static string UPLOAD_URL = $"{BASE_URL}/modern/proxy/upload-service/upload";

		private const string ORIGIN = SSO_URL;
		private static string USERAGENT = "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:48.0) Gecko/20100101 Firefox/50.0";
		
		private CookieJar _jar;

		public ApiClient(string email, string password)
        {
            _email = email;
            _password = password;
        }

		/// <summary>
		/// Initialize authentication.
		/// https://github.com/cyberjunky/python-garminconnect/blob/master/garminconnect/__init__.py#L16
		/// </summary>
		public async Task<String> InitAuth()
		{
			object queryParams = new
			{
				clientId = "GarminConnect",
				consumeServiceTicket = "false",
				createAccountShown = "true",
				cssUrl = "https://static.garmincdn.com/com.garmin.connect/ui/css/gauth-custom-v1.2-min.css",
				displayNameShown = "false",
				embedWidget = "false",
				gauthHost = "https://sso.garmin.com/sso",
				generateExtraServiceTicket = "false",
				id = "gauth-widget",
				initialFocus = "true",
				locale = "en_US",
				openCreateAccount = "false",
				redirectAfterAccountCreationUrl = "https://connect.garmin.com/",
				redirectAfterAccountLoginUrl = "https://connect.garmin.com/",
				rememberMeChecked = "false",
				rememberMeShown = "true",
				service = "https://connect.garmin.com",
				source = "https://connect.garmin.com",
				usernameShow = "false",
				webhost = "https://connect.garmin.com"
			};

			string loginForm = null;
			try
			{
				loginForm = await SIGNIN_URL
							.WithHeader("User-Agent", USERAGENT)
							.WithHeader("origin", ORIGIN)
							.SetQueryParams(queryParams)
							.WithCookies(out _jar)
							.GetStringAsync();
			}
			catch (FlurlHttpException e)
			{
                Debug.WriteLine(e, "No login form.");
				throw;
			}

			object loginData = new
			{
				embed = "true",
				username = this._email,
				password = this._password,
				lt = "e1s1",
				_eventId = "submit",
				displayNameRequired = "false",
			};

			string authResponse = null;
			try
			{
				authResponse = await SIGNIN_URL
								.WithHeader("User-Agent", USERAGENT)
								.WithHeader("origin", ORIGIN)
								.SetQueryParams(queryParams)
								.WithCookies(_jar)
								.PostUrlEncodedAsync(loginData)
								.ReceiveString();
			}
			catch (FlurlHttpException e)
			{
				Debug.WriteLine(e, "Authentication Failed.");
                return "Authentication Failed.";
			}

			// Check we have SSO guid in the cookies
			if (!_jar.Any(c => c.Name == "GARMIN-SSO-GUID"))
			{
                Debug.WriteLine("Missing Garmin auth cookie.");
				return "Failed to find Garmin auth cookie.";
			}

			//Try to find the full post login url in response
			var regex2 = new Regex("var response_url(\\s+) = (\\\"|\\').*?ticket=(?<ticket>[\\w\\-]+)(\\\"|\\')");
			var match = regex2.Match(authResponse);
			if (!match.Success)
			{
                Debug.WriteLine("Missing service ticket.");
				return "Failed to find service ticket.";
			}

			var ticket = match.Groups.GetValueOrDefault("ticket").Value;
			if (string.IsNullOrEmpty(ticket))
			{
                Debug.WriteLine("Failed to parse service ticket.");
				return "Failed to parse service ticket.";
			}

			queryParams = new
			{
				ticket = ticket
			};

			// Second Auth Step
			// Needs a service ticket from the previous step
			try
			{
				var authResponse2 = await BASE_URL
							.WithCookies(_jar)
							.SetQueryParams(queryParams)
							.GetStringAsync();
			}
			catch (FlurlHttpException e)
			{
                return "Second auth step failed.";
			}

			// Check login
			try
			{
				var response = await PROFILE_URL
							.WithHeader("User-Agent", USERAGENT)
							.WithHeader("origin", ORIGIN)
							.WithCookies(_jar)
							.GetJsonAsync();
			}
			catch (FlurlHttpException e)
			{
                return "Login check failed.";
			}

            return string.Empty;
        }

        public async Task<UploadResponse> UploadActivity(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var response = await $"{UPLOAD_URL}/GPX"
                .WithCookies(_jar)
                .WithHeader("NK", "NT")
                .WithHeader("origin", ORIGIN)
                .WithHeader("User-Agent", USERAGENT)
                .AllowHttpStatus("2xx,409")
                .PostMultipartAsync((data) =>
                {
                    data.AddFile("\"file\"", path: filePath, contentType: "application/octet-stream", fileName: $"\"{fileName}\"");
                })
                .ReceiveJson<UploadResponse>();

            return response;
        }
	}

    public class UploadResponse
    {
        public DetailedImportResult DetailedImportResult { get; set; }
    }
    public class DetailedImportResult
    {
        public DateTime CreationDate { get; set; }
        public string FileName { get; set; }
        public int FileSize { get; set; }
        public string IpAddress { get; set; }
        public int ProcessingTime { get; set; }
        public string UploadId { get; set; }
        public ICollection<Failure> Failures { get; set; }
        public ICollection<Success> Successes { get; set; }
    }

    public class Success
    {
        public string ExternalId { get; set; }
        public string InternalId { get; set; }
        public ICollection<Messages> Messages { get; set; }
    }

    public class Failure
    {
        public string ExternalId { get; set; }
        public string InternalId { get; set; }
        public ICollection<Messages> Messages { get; set; }
    }


    public class Messages
    {
        public int Code { get; set; }
        public string Content { get; set; }
    }
}
