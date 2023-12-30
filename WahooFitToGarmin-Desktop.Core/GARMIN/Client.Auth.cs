using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Flurl.Http;
using Flurl.Util;
using OAuth;
using WahooFitToGarmin_Desktop.Core.GARMIN.Dto;
using WahooFitToGarmin_Desktop.Core.GARMIN.Dto.Garmin;

namespace WahooFitToGarmin_Desktop.Core.GARMIN
{
    internal partial class Client : IClient
    {
        public async Task<GarminAuthenciationResult> Authenticate(string email, string password)
        {
            var result = new GarminAuthenciationResult();
            _cookieJar = null;

            _authStatus = AuthStatus.PreInitCookies;
            // Set cookies
            try
            {
                await this.InitCookieJarAsync();
            }
            catch (FlurlHttpException ex)
            {
                _authStatus = AuthStatus.InitCookiesError;
                throw new GarminClientException(_authStatus, ex.Message, ex);
            }

            _authStatus = AuthStatus.InitCookiesSuccessful;

            // get CSRF token
            var csrfRequest = new
            {
                id = "gauth-widget",
                embedWidget = "true",
                gauthHost = URLs.SSO_EMBED_URL,
                service = URLs.SSO_EMBED_URL,
                source = URLs.SSO_EMBED_URL,
                redirectAfterAccountLoginUrl = URLs.SSO_EMBED_URL,
                redirectAfterAccountCreationUrl = URLs.SSO_EMBED_URL,
            };

            string csrfToken = string.Empty;
            try
            {
                var tokenResult = await this.GetCsrfTokenAsync(csrfRequest);
                _authStatus = AuthStatus.CSRFTokenRequestSent;
                csrfToken = this.FindCsrfToken(tokenResult);

            }
            catch (FlurlHttpException ex)
            {
                throw new GarminClientException(_authStatus, ex.Message, ex);
            }

            _authStatus = AuthStatus.CSRFReceivedSuccessful;

            // send credencials
            var sendCredentialsRequest = new
            {
                username = email,
                password = password,
                embed = "true",
                _csrf = csrfToken
            };

            SendCredentialsResult sendCredentialsResult = null;
            try
            {
                sendCredentialsResult = await this.SendCredentialsAsync(csrfRequest, sendCredentialsRequest);
            }
            catch (FlurlHttpException ex) when (ex.StatusCode is (int)HttpStatusCode.Forbidden)
            {
                var responseContent = (await ex.GetResponseStringAsync()) ?? string.Empty;

                if (responseContent == "error code: 1020")
                {
                    _authStatus = AuthStatus.AuthBlockedByCloudFlare;
                    var errorMessage = "Garmin Authentication Failed. Blocked by CloudFlare.";
                    throw new GarminClientException(_authStatus, ex.Message, ex);
                }
                _authStatus = AuthStatus.AuthenticationFailed;
                throw new GarminClientException(_authStatus, ex.Message, ex);
            }
            catch (FlurlHttpException ex)
            {
                _authStatus = AuthStatus.AuthenticationFailedCheckCredencials;
                throw new GarminClientException(_authStatus, ex.Message, ex);
            }

            if (sendCredentialsResult.WasRedirected && sendCredentialsResult.RedirectedTo.Contains(URLs.SSO_ENTER_MFA_URL))
            {
                result.MFACodeRequested = true;

                _authStatus = AuthStatus.MFARedirected;
                try
                {
                    var mfaCsrfToken = FindCsrfToken(sendCredentialsResult.RawResponseBody);
                    _mfaCsrfToken = mfaCsrfToken;
                    return result;

                }
                catch (FlurlHttpException ex)
                {
                    _authStatus = AuthStatus.MFACSRFTokenNotFound;
                    throw new GarminClientException(_authStatus, ex.Message, ex);
                }


            }

            var loginResult = sendCredentialsResult?.RawResponseBody;

            return await FinishAuthenticate(loginResult);

        }

        public async Task<GarminAuthenciationResult> FinishAuthenticate(string loginResult)
        {
            GarminAuthenciationResult result = new GarminAuthenciationResult();
            var ticketRegex = new Regex(MagicStrings.TICKET_REGEX);
            var ticketMatch = ticketRegex.Match(loginResult);
            if (!ticketMatch.Success)
            {
                _authStatus = AuthStatus.SuccessButCouldNotFindServiceTicket;
                var errorMessage = "Auth appeared successful but failed to find regex match for service ticket.";
                throw new GarminClientException(_authStatus, errorMessage);
            }

            var ticket = ticketMatch.Groups.ToKeyValuePairs().FirstOrDefault(t=>string.Equals(t.Key,"ticket")).Value.ToString();
           

            if (string.IsNullOrWhiteSpace(ticket))
            {
                _authStatus = AuthStatus.SuccessButTicketIsEmpty;
                var errorMessage = "Auth appeared successful, and found service ticket, but ticket was null or empty.";
                throw new GarminClientException(_authStatus, errorMessage);
            }
            _authStatus = AuthStatus.InitialAuthSuccessful;


            // get Oauth1
            var oAuth1 = await GetOAuth1Async(ticket);
            if (string.IsNullOrEmpty(oAuth1.oAuthToken) || string.IsNullOrEmpty(oAuth1.oAuthTokenSecret))
            {
                _authStatus = AuthStatus.OAuth1TokensAreEmpty;
                var errorMessage = "Auth appeared successful but failed to get the OAuth1 token.";
                throw new GarminClientException(_authStatus, errorMessage);
            }

            // set OAuth2
            await SetOAuth2Token(oAuth1.oAuthToken, oAuth1.oAuthTokenSecret);

            if (OAuth2Token == null)
            {
                _authStatus = AuthStatus.OAuthToken2IsNull;
                var errorMessage = "Auth appeared successful but failed to get the OAuth2 token.";
                throw new GarminClientException(_authStatus, errorMessage);
            }
            _authStatus = AuthStatus.Authenticated;
            result.IsSuccess = true;
            return result;
        }

        public async Task<GarminAuthenciationResult> CompleteMFAAuthAsync(string mfaCode)
        {

            if (_authStatus != AuthStatus.MFARedirected)
            {
                var message = "Not Redirected to MFA";
                throw new GarminClientException(_authStatus, message);
            }


            var mfaData = new List<KeyValuePair<string, string>>()
        {
            new KeyValuePair<string, string>("embed", "true"),
            new KeyValuePair<string, string>("mfa-code", mfaCode),
            new KeyValuePair<string, string>("fromPage", "setupEnterMfaCode"),
            new KeyValuePair<string, string>("_csrf",_mfaCsrfToken)
        };

            // Send the MFA Code to Garmin
            try
            {
                var mfaResponseBody = await SendMfaCodeAsync(mfaData);
                return await FinishAuthenticate(mfaResponseBody);
            }
            catch (FlurlHttpException ex) when (ex.StatusCode is (int)HttpStatusCode.Forbidden)
            {

                var responseContent = (await ex.GetResponseStringAsync()) ?? string.Empty;

                if (responseContent == "error code: 1020")
                {
                    _authStatus = AuthStatus.MFAAuthBlockedByCloudFlare;
                    var errorMessage = "MFA: Garmin Authentication Failed. Blocked by CloudFlare.";
                    throw new GarminClientException(_authStatus, ex.Message, ex);
                }
                _authStatus = AuthStatus.InvalidMFACode;
                throw new GarminClientException(_authStatus, ex.Message, ex);
            }
        }



        private async Task InitCookieJarAsync()
        {
            await URLs.SSO_EMBED_URL
                        .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                        .WithHeader("origin", URLs.ORIGIN)
                        .SetQueryParams(Client._commonQueryParams)
                        .WithCookies(out var jar)
                        .GetStringAsync();

            _cookieJar = jar;
        }

        public async Task<string> GetCsrfTokenAsync(object queryParams)
        {

            var result = await CookieExtensions.WithCookies(URLs.SSO_SIGNIN_URL
                            .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                            .WithHeader("origin", URLs.ORIGIN)
                            .SetQueryParams(queryParams), (CookieJar)_cookieJar)
                        .GetAsync()
                        .ReceiveString();

            return result;
        }
        private async Task<SendCredentialsResult> SendCredentialsAsync(object queryParams, object loginData)
        {
            var result = new SendCredentialsResult();
            result.RawResponseBody = await CookieExtensions.WithCookies(URLs.SSO_SIGNIN_URL
                            .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                            .WithHeader("origin", URLs.ORIGIN)
                            .WithHeader("referer", URLs.REFERER)
                            .WithHeader("NK", "NT")
                            .SetQueryParams(queryParams), (CookieJar)_cookieJar)
                        .OnRedirect((r) => { result.WasRedirected = true; result.RedirectedTo = r.Redirect.Url; })
                        .PostUrlEncodedAsync(loginData)
                        .ReceiveString();

            return result;
        }

        private Task<string> SendMfaCodeAsync(object mfaData)
        {
            return CookieExtensions.WithCookies("https://sso.garmin.com/sso/verifyMFA/loginEnterMfaCode"
                            .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                            .WithHeader("origin", URLs.ORIGIN)
                            .SetQueryParams(Client._commonQueryParams), (CookieJar)_cookieJar)
                        .OnRedirect(redir => CookieExtensions.WithCookies(redir.Request, (CookieJar)_cookieJar))
                        .PostUrlEncodedAsync(mfaData)
                        .ReceiveString();
        }

        private async Task<(string oAuthToken, string oAuthTokenSecret)> GetOAuth1Async(string ticket)
        {

            OAuthRequest oauthRequest = OAuthRequest.ForRequestToken(_consumerKey, _consumerSecret);
            oauthRequest.RequestUrl = URLs.OAUTH1_URL(ticket);
            string authHeader = oauthRequest.GetAuthorizationHeader();
            try
            {
                var tokens = await oauthRequest.RequestUrl
                              .WithHeader("User-Agent", MagicStrings.USER_AGENT)
                              .WithHeader("Authorization", authHeader)
                              .GetStringAsync();

                var splittedTokens = HttpUtility.ParseQueryString(tokens);

                if (splittedTokens.Count < 2)
                    throw new Exception($"OAuth1 response length did not match expected: {tokens.Length}");

                var oAuthToken = splittedTokens.Get("oauth_token");
                var oAuthTokenSecret = splittedTokens.Get("oauth_token_secret");

                if (string.IsNullOrWhiteSpace(oAuthToken))
                    throw new Exception("OAuth1 token is null");

                if (string.IsNullOrWhiteSpace(oAuthTokenSecret))
                    throw new Exception("OAuth1 token secret is null");

                return (oAuthToken, oAuthTokenSecret);
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseStringAsync();
            }
            catch (Exception ex)
            {
                _authStatus = AuthStatus.OAuth1TokensProblem;
                throw new GarminClientException(_authStatus, ex.Message, ex);
            }

            return ("", "");

        }

        public async Task SetOAuth2Token(string accessToken, string tokenSecret)
        {
            OAuth2Token = await this.GetOAuth2Token(accessToken, tokenSecret);

            if (OAuth2Token != null)
            {
                this._oAuth2TokenValidUntil = DateTime.UtcNow.AddSeconds(OAuth2Token.Expires_In);
            }
        }

        private async Task<OAuth2Token?> GetOAuth2Token(string accessToken, string tokenSecret)
        {

            OAuthRequest oauthRequest = OAuthRequest.ForProtectedResource("POST", _consumerKey, _consumerSecret, accessToken, tokenSecret);

            oauthRequest.RequestUrl = URLs.OAUTH_EXCHANGE_URL;
            string authHeader = oauthRequest.GetAuthorizationHeader();

            try
            {
                var token = await oauthRequest.RequestUrl
              .WithHeader("User-Agent", MagicStrings.USER_AGENT)
              .WithHeader("Authorization", authHeader)
              .WithHeader("Content-Type", "application/x-www-form-urlencoded")
              .WithTimeout(10)
              .PostUrlEncodedAsync(new object())
              .ReceiveJson<OAuth2Token>();

                return token;
            }
            catch (FlurlHttpException ex)
            {
                var error = await ex.GetResponseStringAsync();
            }
            catch (Exception ex)
            {
                _authStatus = AuthStatus.OAuth2TokensProblem;
                throw new GarminClientException(_authStatus, ex.Message, ex);

            }

            return null;

        }

        private string FindCsrfToken(string rawResponseBody)
        {
            try
            {
                var tokenRegex = new Regex(MagicStrings.CSRF_REGEX);
                var match = tokenRegex.Match(rawResponseBody);
                if (!match.Success)
                {
                    _authStatus = AuthStatus.CSRFTokenNotFound;
                    throw new GarminClientException(_authStatus, $"Failed to find regex match for csrf token. tokenResult: {rawResponseBody}");
                }

                var csrfToken = match.Groups.ToKeyValuePairs().FirstOrDefault(t => string.Equals(t.Key, "csrf")).Value.ToString();


                if (string.IsNullOrWhiteSpace(csrfToken))
                {
                    _authStatus = AuthStatus.CSRFTokenNotFound;
                    throw new GarminClientException(_authStatus, "Found csrfToken but its null.");
                }


                return csrfToken;
            }
            catch (Exception e)
            {
                _authStatus = AuthStatus.CSRFTokenCannotParse;
                throw new GarminClientException(_authStatus, "Failed to parse csrf token.", e);
            }
        }
    }
}
