using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Core.GARMIN;
using WahooFitToGarmin_Desktop.Core.GARMIN.Dto;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// Creates Garmin clients. Behind an interface so the session can be tested
    /// without a network, and so <c>garmin-di-oauth2-core</c> can replace the
    /// authentication flow in one place.
    /// </summary>
    public interface IGarminClientFactory
    {
        Task<IClient> CreateAsync(CancellationToken cancellationToken);
    }

    /// <inheritdoc cref="IGarminClientFactory"/>
    public sealed class GarminClientFactory : IGarminClientFactory
    {
        private readonly ILogger _logger;

        public GarminClientFactory(ILogger logger) => _logger = logger;

        public Task<IClient> CreateAsync(CancellationToken cancellationToken) => ClientFactory.Create(_logger);
    }

    /// <inheritdoc cref="IGarminSession"/>
    public sealed class GarminSession : IGarminSession
    {
        private readonly ISettingsStore _settings;
        private readonly IGarminClientFactory _clientFactory;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);

        private IClient? _client;

        public GarminSession(ISettingsStore settings, IGarminClientFactory clientFactory, ILogger logger)
        {
            _settings = settings;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<SessionResult> GetAsync(CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // The defect this fixes: the previous code asked whether a token
                // object existed, not whether it was still usable, so a session
                // that expired during a long-running instance produced failed
                // uploads until the application was restarted.
                if (_client is { IsOAuthValid: true })
                {
                    _logger.LogInformation("Already logged.");
                    return SessionResult.Success(_client);
                }

                var settings = _settings.Current;

                if (string.IsNullOrWhiteSpace(settings.GarminLogin)
                    || string.IsNullOrWhiteSpace(settings.GarminPassword))
                {
                    _logger.LogInformation("Please enter your Garmin login and password in settings");
                    return SessionResult.Failed(SessionFailure.SignInRequired, "no credentials configured");
                }

                _logger.LogInformation("Connection to Garmin Connect server");

                IClient client;
                GarminAuthenciationResult authentication;
                try
                {
                    client = await _clientFactory.CreateAsync(cancellationToken).ConfigureAwait(false);
                    authentication = await client
                        .Authenticate(settings.GarminLogin, settings.GarminPassword)
                        .ConfigureAwait(false);
                }
                catch (GarminClientException ex)
                {
                    // The service answered and refused. Retrying the same
                    // credentials will not change that.
                    _logger.LogError(ex, "Garmin authentication failed at {AuthStatus}", ex.AuthStatus);
                    return SessionResult.Failed(SessionFailure.SignInRequired, ex.Message);
                }
                catch (Exception ex)
                {
                    // Anything else is treated as worth retrying: the network
                    // being unavailable must not be reported as a credential
                    // problem.
                    _logger.LogError(ex, "Could not reach Garmin to authenticate");
                    return SessionResult.Failed(SessionFailure.Transient, ex.Message);
                }

                if (!authentication.IsSuccess)
                {
                    return SessionResult.Failed(
                        SessionFailure.SignInRequired,
                        authentication.Error ?? "authentication did not succeed");
                }

                _logger.LogInformation("Connection success.");
                _client = client;

                return SessionResult.Success(client);
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Invalidate() => _client = null;
    }
}
