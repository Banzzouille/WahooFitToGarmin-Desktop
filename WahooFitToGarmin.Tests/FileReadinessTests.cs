using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using WahooFitToGarmin_Desktop.Core.Activities;

namespace WahooFitToGarmin.Tests;

[TestClass]
public sealed class FileReadinessTests
{
    private const string Path = "ride.fit";
    private static readonly DateTime T0 = new(2026, 9, 14, 7, 30, 0, DateTimeKind.Utc);

    /// <summary>
    /// A probe that walks a script of observations, one step per delay, so the
    /// waiter can be exercised without a file system and without waiting for
    /// real time to pass.
    /// </summary>
    private sealed class Script
    {
        private readonly List<(long Length, DateTime LastWriteUtc)?> _observations;
        private int _index;

        public Script(params (long, DateTime)?[] observations) =>
            _observations = observations.Cast<(long, DateTime)?>().ToList();

        public bool CanOpen { get; set; } = true;

        public int OpenAttempts { get; private set; }

        public Mock<IFileSystemProbe> AsMock()
        {
            var mock = new Mock<IFileSystemProbe>(MockBehavior.Strict);

            mock.Setup(x => x.Exists(It.IsAny<string>())).Returns(() => _observations.Count > 0);
            mock.Setup(x => x.Stat(It.IsAny<string>())).Returns(Current);
            mock.Setup(x => x.CanOpenForRead(It.IsAny<string>()))
                .Returns(() =>
                {
                    OpenAttempts++;
                    return CanOpen;
                });

            return mock;
        }

        private (long Length, DateTime LastWriteUtc)? Current() =>
            _observations.Count == 0 ? null : _observations[Math.Min(_index, _observations.Count - 1)];

        /// <summary>Stands in for the wait between checks.</summary>
        public Func<TimeSpan, CancellationToken, Task> Delay => (_, _) =>
        {
            _index++;
            return Task.CompletedTask;
        };
    }

    private static FileReadinessWaiter Create(
        Mock<IFileSystemProbe> probe,
        int stableChecks = 2,
        TimeSpan? timeout = null) =>
        new(probe.Object,
            NullLogger.Instance,
            quietInterval: TimeSpan.Zero,
            requiredStableChecks: stableChecks,
            timeout: timeout ?? TimeSpan.FromMinutes(2));

    [TestMethod]
    public async Task AFileStillGrowing_IsNotReportedReadyUntilItStops()
    {
        // Grows, grows, grows, then settles.
        var script = new Script(
            (1000, T0),
            (5000, T0.AddSeconds(1)),
            (9000, T0.AddSeconds(2)),
            (9000, T0.AddSeconds(2)),
            (9000, T0.AddSeconds(2)));

        var probe = script.AsMock();

        var result = await Create(probe).WaitAsync(Path, script.Delay, CancellationToken.None);

        Assert.AreEqual(ReadinessResult.Ready, result);
    }

    [TestMethod]
    public async Task AFileCompleteOnArrival_BecomesReady()
    {
        var script = new Script((4096, T0), (4096, T0), (4096, T0));

        var result = await Create(script.AsMock()).WaitAsync(Path, script.Delay, CancellationToken.None);

        Assert.AreEqual(ReadinessResult.Ready, result);
    }

    [TestMethod]
    public async Task AFileThatNeverStabilises_IsAbandoned()
    {
        // Always changing: every observation differs from the last.
        var forever = Enumerable.Range(0, 500)
            .Select(i => ((long)(1000 + i * 10), T0.AddSeconds(i)))
            .Cast<(long, DateTime)?>()
            .ToArray();

        var script = new Script(forever);

        var result = await Create(script.AsMock(), timeout: TimeSpan.Zero)
            .WaitAsync(Path, script.Delay, CancellationToken.None);

        Assert.AreEqual(ReadinessResult.TimedOut, result);
    }

    [TestMethod]
    public async Task AMomentaryPause_DoesNotCountAsSettled()
    {
        // One repeated observation, then growth resumes, then it truly settles.
        // With a single stable check this would have been reported ready during
        // the pause, and a partial file uploaded.
        var script = new Script(
            (1000, T0),
            (1000, T0),                 // the pause
            (7000, T0.AddSeconds(3)),
            (7000, T0.AddSeconds(3)),
            (7000, T0.AddSeconds(3)));

        var result = await Create(script.AsMock(), stableChecks: 3)
            .WaitAsync(Path, script.Delay, CancellationToken.None);

        Assert.AreEqual(ReadinessResult.Ready, result);
    }

    [TestMethod]
    public async Task AFileThatDisappears_IsReportedVanished()
    {
        var script = new Script();

        var result = await Create(script.AsMock()).WaitAsync(Path, script.Delay, CancellationToken.None);

        Assert.AreEqual(ReadinessResult.Vanished, result);
    }

    [TestMethod]
    public async Task ReadinessIsNotDecidedByAnExclusiveLock()
    {
        // The open is confirmation, never the primary signal: a file that is
        // stable but momentarily unopenable keeps waiting rather than being
        // declared ready or failed.
        var script = new Script((4096, T0), (4096, T0), (4096, T0)) { CanOpen = false };
        var probe = script.AsMock();

        // Long enough to reach the stable-check threshold, short enough to end.
        var result = await Create(probe, timeout: TimeSpan.FromMilliseconds(50))
            .WaitAsync(Path, script.Delay, CancellationToken.None);

        Assert.AreEqual(ReadinessResult.TimedOut, result);
        probe.Verify(x => x.CanOpenForRead(Path), Times.AtLeastOnce);
    }

    [TestMethod]
    public async Task Cancellation_IsObserved()
    {
        var script = new Script((1000, T0), (2000, T0.AddSeconds(1)));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => Create(script.AsMock()).WaitAsync(Path, script.Delay, cts.Token));
    }
}
