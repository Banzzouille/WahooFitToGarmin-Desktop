namespace WahooFitToGarmin_Desktop.Core.GARMIN.Dto
{
    public enum AuthStatus : byte
    {
        PreInitCookies = 0,
        InitCookiesSuccessful = 1,
        InitCookiesError = 2,
        CSRFTokenRequestSent = 3,
        CSRFTokenNotFound = 4,
        CSRFTokenEmpty = 5,
        CSRFTokenCannotParse = 6,
        CSRFReceivedSuccessful = 7,
        AuthBlockedByCloudFlare = 8,
        AuthenticationFailed = 9,
        AuthenticationFailedCheckCredencials = 10,
        SuccessButCouldNotFindServiceTicket = 11,
        SuccessButTicketIsEmpty = 12,
        InitialAuthSuccessful = 13,
        OAuth1TokensProblem = 14,
        OAuth1TokensAreEmpty = 15,
        OAuth2TokensProblem = 16,
        OAuthToken2IsNull = 17,
        Authenticated = 18,
        MFARedirected = 19,
        MFACSRFTokenNotFound = 20,
        MFAAuthBlockedByCloudFlare = 21,
        InvalidMFACode = 22,
        

    }
}
