using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using WahooFitToGarmin.Tests.Mocks;

using WahooFitToGarmin_Desktop.Core.Activities;
using WahooFitToGarmin_Desktop.Core.GARMIN;
using WahooFitToGarmin_Desktop.Core.GARMIN.Dto;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.Tests;

[TestClass]
public sealed class GarminSessionTests
{
    private static ISettingsStore Configured() =>
        MockBuilders.SettingsStore(new UserSettings
        {
            WatchedFolder = "/watched",
            GarminLogin = "rider@example.com",
            GarminPassword = "secret",
        }).Object;

    private static Mock<IClient> ClientThat(bool valid, bool authenticates = true)
    {
        var client = new Mock<IClient>();
        client.SetupGet(x => x.IsOAuthValid).Returns(valid);
        client.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new GarminAuthenciationResult { IsSuccess = authenticates });
        return client;
    }

    private static (GarminSession Session, Mock<IGarminClientFactory> Factory) Create(
        ISettingsStore settings,
        IClient client)
    {
        var factory = new Mock<IGarminClientFactory>();
        factory.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(client);

        return (new GarminSession(settings, factory.Object, NullLogger.Instance), factory);
    }

    [TestMethod]
    public async Task AValidSession_IsReusedWithoutAuthenticatingAgain()
    {
        var client = ClientThat(valid: true);
        var (session, factory) = Create(Configured(), client.Object);

        var first = await session.GetAsync(CancellationToken.None);
        var second = await session.GetAsync(CancellationToken.None);

        Assert.IsTrue(first.IsSuccess);
        Assert.IsTrue(second.IsSuccess);
        factory.Verify(x => x.CreateAsync(It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task AnExpiredSession_IsRenewedBeforeTheNextUpload()
    {
        // Valid at first, then expired: the object is still there, which is
        // exactly the state the previous implementation mistook for usable.
        var client = new Mock<IClient>();
        var validity = new Queue<bool>([true, false]);
        client.SetupGet(x => x.IsOAuthValid).Returns(() => validity.Count > 0 ? validity.Dequeue() : false);
        client.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new GarminAuthenciationResult { IsSuccess = true });

        var (session, factory) = Create(Configured(), client.Object);

        await session.GetAsync(CancellationToken.None);
        await session.GetAsync(CancellationToken.None);
        await session.GetAsync(CancellationToken.None);

        factory.Verify(
            x => x.CreateAsync(It.IsAny<CancellationToken>()),
            Times.AtLeast(2),
            "an expired session was reused instead of being renewed");
    }

    [TestMethod]
    public async Task ValidityIsJudgedOnTheSession_NotOnATokenObjectBeingPresent()
    {
        var client = new Mock<IClient>();
        client.SetupGet(x => x.IsOAuthValid).Returns(false);
        // A token object exists, and is stale. Presence must not imply usable.
        client.SetupGet(x => x.OAuth2Token).Returns(new OAuth2Token { Access_Token = "stale" });
        client.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
              .ReturnsAsync(new GarminAuthenciationResult { IsSuccess = true });

        var (session, _) = Create(Configured(), client.Object);

        await session.GetAsync(CancellationToken.None);
        await session.GetAsync(CancellationToken.None);

        client.Verify(
            x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(2),
            "a stale token object was treated as a usable session");
    }

    [TestMethod]
    public async Task MissingCredentials_ReportSignInRequired_NotATransientFailure()
    {
        var settings = MockBuilders.SettingsStore(new UserSettings { WatchedFolder = "/watched" }).Object;
        var (session, factory) = Create(settings, ClientThat(valid: false).Object);

        var result = await session.GetAsync(CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(SessionFailure.SignInRequired, result.Failure);
        factory.Verify(x => x.CreateAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task RejectedCredentials_ReportSignInRequired()
    {
        var client = new Mock<IClient>();
        client.SetupGet(x => x.IsOAuthValid).Returns(false);
        client.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
              .ThrowsAsync(new GarminClientException(AuthStatus.AuthenticationFailedCheckCredencials, "rejected"));

        var (session, _) = Create(Configured(), client.Object);

        var result = await session.GetAsync(CancellationToken.None);

        Assert.AreEqual(SessionFailure.SignInRequired, result.Failure);
    }

    [TestMethod]
    public async Task AnUnreachableService_ReportsATransientFailure()
    {
        // The network being down must not be reported as a credential problem.
        var client = new Mock<IClient>();
        client.SetupGet(x => x.IsOAuthValid).Returns(false);
        client.Setup(x => x.Authenticate(It.IsAny<string>(), It.IsAny<string>()))
              .ThrowsAsync(new HttpRequestException("no route to host"));

        var (session, _) = Create(Configured(), client.Object);

        var result = await session.GetAsync(CancellationToken.None);

        Assert.AreEqual(SessionFailure.Transient, result.Failure);
    }

    [TestMethod]
    public async Task AuthenticationThatDoesNotSucceed_ReportsSignInRequired()
    {
        var client = ClientThat(valid: false, authenticates: false);
        var (session, _) = Create(Configured(), client.Object);

        var result = await session.GetAsync(CancellationToken.None);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(SessionFailure.SignInRequired, result.Failure);
    }

    [TestMethod]
    public async Task Invalidate_ForcesTheNextRequestToEstablishANewSession()
    {
        var client = ClientThat(valid: true);
        var (session, factory) = Create(Configured(), client.Object);

        await session.GetAsync(CancellationToken.None);
        session.Invalidate();
        await session.GetAsync(CancellationToken.None);

        factory.Verify(x => x.CreateAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
