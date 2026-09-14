using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WahooFitToGarmin_Desktop.Core.Activities;

namespace WahooFitToGarmin.Tests;

/// <summary>
/// A probe driven by a script of observations, so readiness can be exercised
/// without a file system and without waiting for real time to pass.
/// </summary>
internal sealed class ScriptedProbe : IFileSystemProbe
{
    private readonly Queue<(long Length, DateTime LastWriteUtc)?> _observations;

    public ScriptedProbe(IEnumerable<(long, DateTime)?> observations)
    {
        _observations = new Queue<(long, DateTime)?>(observations);
    }

    public bool CanOpen { get; set; } = true;

    public int OpenAttempts { get; private set; }

    public bool Exists(string path) => _observations.Count > 0;

    public (long Length, DateTime LastWriteUtc)? Stat(string path) =>
        _observations.Count > 0 ? _observations.Peek() : null;

    public void Advance()
    {
        if (_observations.Count > 1)
        {
            _observations.Dequeue();
        }
    }

    public bool CanOpenForRead(string path)
    {
        OpenAttempts++;
        return CanOpen;
    }
}

[TestClass]
public sealed class FileReadinessTests
{
    private static readonly DateTime T0 = new(2026, 9, 14, 7, 30, 0, DateTimeKind.Utc);

    private static FileReadinessWaiter Create(IFileSystemProbe probe, TimeSpan? timeout = null) =>
        new(probe,
            NullLogger.Instance,
            quietInterval: TimeSpan.Zero,
            requiredStableChecks: 2,
            timeout: timeout ?? TimeSpan.FromMinutes(2));

    /// <summary>Advances the scripted probe instead of sleeping.</summary>
    private static Func<TimeSpan, CancellationToken, Task> Advancing(ScriptedProbe probe) =>
        (_, _) =>
        {
            probe.Advance();
            return Task.CompletedTask;
        };

    [TestMethod]
    public async Task AFileStillGrowing_IsNotReportedReadyUntilItStops()
    {
        // Grows, grows, grows, then settles.
        var probe = new ScriptedProbe(
        [
            (1000, T0),
            (5000, T0.AddSeconds(1)),
            (9000, T0.AddSeconds(2)),
            (9000, T0.AddSeconds(2)),
            (9000, T0.AddSeconds(2)),
        ]);

        var result = await Create(probe).WaitAsync("ride.fit", Advancing(probe), CancellationToken.None);

        Assert.AreEqual(ReadinessResult.Ready, result);
    }

    [TestMethod]
    public async Task AFileCompleteOnArrival_BecomesReady()
    {
        var probe = new ScriptedProbe([(4096, T0), (4096, T0), (4096, T0)]);

        var result = await Create(probe).WaitAsync("ride.fit", Advancing(probe), CancellationToken.None);

        Assert.AreEqual(ReadinessResult.Ready, result);
    }

    [TestMethod]
    public async Task AFileThatNeverStabilises_IsAbandoned()
    {
        // Always changing: every observation differs from the last.
        var forever = Enumerable.Range(0, 500)
            .Select(i => ((long)(1000 + i * 10), T0.AddSeconds(i)))
            .Cast<(long, DateTime)?>();

        var probe = new ScriptedProbe(forever);

        var result = await Create(probe, timeout: TimeSpan.Zero)
            .WaitAsync("ride.fit", Advancing(probe), CancellationToken.None);

        Assert.AreEqual(ReadinessResult.TimedOut, result);
    }

    [TestMethod]
    public async Task AMomentaryPause_DoesNotCountAsSettled()
    {
        // One repeated observation, then growth resumes, then it truly settles.
        // With a single stable check this would have been reported ready during
        // the pause, and a partial file uploaded.
        var probe = new ScriptedProbe(
        [
            (1000, T0),
            (1000, T0),          // the pause
            (7000, T0.AddSeconds(3)),
            (7000, T0.AddSeconds(3)),
            (7000, T0.AddSeconds(3)),
        ]);

        var waiter = new FileReadinessWaiter(
            probe, NullLogger.Instance, TimeSpan.Zero, requiredStableChecks: 3, TimeSpan.FromMinutes(2));

        var result = await waiter.WaitAsync("ride.fit", Advancing(probe), CancellationToken.None);

        Assert.AreEqual(ReadinessResult.Ready, result);
    }

    [TestMethod]
    public async Task AFileThatDisappears_IsReportedVanished()
    {
        var probe = new ScriptedProbe(Array.Empty<(long, DateTime)?>());

        var result = await Create(probe).WaitAsync("ride.fit", Advancing(probe), CancellationToken.None);

        Assert.AreEqual(ReadinessResult.Vanished, result);
    }

    [TestMethod]
    public async Task ReadinessIsNotDecidedByAnExclusiveLock()
    {
        // The open is confirmation, never the primary signal: a file that is
        // stable but momentarily unopenable keeps waiting rather than being
        // declared ready or failed.
        var probe = new ScriptedProbe([(4096, T0), (4096, T0), (4096, T0)]) { CanOpen = false };

        // Long enough to reach the stable-check threshold, short enough to end.
        var result = await Create(probe, timeout: TimeSpan.FromMilliseconds(50))
            .WaitAsync("ride.fit", Advancing(probe), CancellationToken.None);

        Assert.AreEqual(ReadinessResult.TimedOut, result);
        Assert.IsTrue(probe.OpenAttempts > 0, "the read-only open was never attempted");
    }

    [TestMethod]
    public async Task Cancellation_IsObserved()
    {
        var probe = new ScriptedProbe([(1000, T0), (2000, T0.AddSeconds(1))]);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(
            () => Create(probe).WaitAsync("ride.fit", Advancing(probe), cts.Token));
    }
}
