using System.Text;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using WahooFitToGarmin.Tests.Mocks;

using WahooFitToGarmin_Desktop.Core.Activities;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.Tests;

[TestClass]
public sealed class ActivityPipelineTests
{
    private const string RidePath = "/watched/ride.fit";

    private ActivityFiles _files = null!;
    private Mock<IProcessedActivityRecord> _record = null!;
    private Mock<ISettingsStore> _settings = null!;
    private List<TimeSpan> _delays = null!;

    [TestInitialize]
    public void Setup()
    {
        _files = new ActivityFiles();
        _record = MockBuilders.Record();
        _settings = MockBuilders.SettingsStore();
        _delays = [];
    }

    private ActivityPipeline Create(
        Mock<IActivityUploader> uploader,
        IEnumerable<IActivityTransformation>? transformations = null,
        PipelineOptions? options = null,
        Mock<IActivityFileStore>? fileStore = null)
    {
        var probe = MockBuilders.StableProbe(_files);
        var readiness = new FileReadinessWaiter(
            probe.Object, NullLogger.Instance, quietInterval: TimeSpan.Zero, requiredStableChecks: 1);

        return new ActivityPipeline(
            _settings.Object,
            _record.Object,
            uploader.Object,
            transformations ?? [],
            probe.Object,
            readiness,
            (fileStore ?? MockBuilders.FileStore(_files)).Object,
            NullLogger.Instance,
            options,
            delay: (d, _) =>
            {
                _delays.Add(d);
                return Task.CompletedTask;
            });
    }

    /// <summary>Queues the given paths, runs the worker until the queue drains.</summary>
    private static async Task DrainAsync(ActivityPipeline pipeline, params string[] paths)
    {
        foreach (var path in paths)
        {
            pipeline.Enqueue(path);
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var run = pipeline.RunAsync(cts.Token);

        while (pipeline.Counters.Processed + pipeline.Counters.Failed + pipeline.Counters.Duplicates < paths.Length
               && !cts.IsCancellationRequested)
        {
            await Task.Delay(5, CancellationToken.None);
        }

        await cts.CancelAsync();
        try
        {
            await run;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static string HashOf(string content) => ActivityHash.Compute(Encoding.UTF8.GetBytes(content));

    // ---------------------------------------------------------------- outcomes

    [TestMethod]
    public async Task ASuccessfulUpload_IsCountedAndRecorded()
    {
        _files.Add(RidePath, "ride");
        var pipeline = Create(MockBuilders.Uploader(UploadOutcome.Success()));

        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(1, pipeline.Counters.Processed);
        Assert.AreEqual(0, pipeline.Counters.Failed);
        _record.Verify(x => x.Mark(HashOf("ride"), "ride.fit", ActivityOutcome.Uploaded), Times.Once);
    }

    [TestMethod]
    public async Task ADuplicate_IsItsOwnOutcome_NotAFailure()
    {
        _files.Add(RidePath, "ride");
        var pipeline = Create(MockBuilders.Uploader(UploadOutcome.Duplicate("already present")));

        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(1, pipeline.Counters.Duplicates);
        Assert.AreEqual(0, pipeline.Counters.Failed);
        Assert.AreEqual(0, pipeline.Counters.Processed);
        _record.Verify(x => x.Mark(It.IsAny<string>(), It.IsAny<string>(), ActivityOutcome.Duplicate), Times.Once);
    }

    [TestMethod]
    public async Task ADuplicate_IsNotRetried()
    {
        _files.Add(RidePath, "ride");
        var uploader = MockBuilders.Uploader(UploadOutcome.Duplicate());

        await DrainAsync(Create(uploader), RidePath);

        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ----------------------------------------------------------------- retries

    [TestMethod]
    public async Task ATransientFailureThenSuccess_CountsAsProcessed()
    {
        _files.Add(RidePath, "ride");
        var uploader = MockBuilders.Uploader(UploadOutcome.Transient("network"), UploadOutcome.Success());
        var pipeline = Create(uploader);

        await DrainAsync(pipeline, RidePath);

        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        Assert.AreEqual(1, pipeline.Counters.Processed);
        Assert.AreEqual(0, pipeline.Counters.Failed);
    }

    [TestMethod]
    public async Task RetriesAreBounded()
    {
        _files.Add(RidePath, "ride");
        var uploader = MockBuilders.Uploader(UploadOutcome.Transient("network"));
        var pipeline = Create(uploader, options: new PipelineOptions { MaxAttempts = 3 });

        await DrainAsync(pipeline, RidePath);

        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        Assert.AreEqual(1, pipeline.Counters.Failed);
    }

    [TestMethod]
    public async Task BackoffGrowsBetweenAttempts()
    {
        _files.Add(RidePath, "ride");
        var pipeline = Create(
            MockBuilders.Uploader(UploadOutcome.Transient()),
            options: new PipelineOptions
            {
                MaxAttempts = 3,
                InitialBackoff = TimeSpan.FromSeconds(2),
                BackoffFactor = 4,
            });

        await DrainAsync(pipeline, RidePath);

        // The readiness waiter shares the same delay function and contributes
        // zero-length waits; only the retry backoff is non-zero.
        var backoffs = _delays.Where(d => d > TimeSpan.Zero).ToList();

        Assert.AreEqual(2, backoffs.Count, "expected one wait between each of three attempts");
        Assert.IsTrue(backoffs[1] > backoffs[0], $"delays did not grow: {backoffs[0]} then {backoffs[1]}");
    }

    [TestMethod]
    public async Task AStatedRetryDelay_WinsOverTheBackoffSchedule()
    {
        _files.Add(RidePath, "ride");
        var stated = TimeSpan.FromSeconds(42);
        var pipeline = Create(
            MockBuilders.Uploader(UploadOutcome.Transient("rate limited", stated), UploadOutcome.Success()),
            options: new PipelineOptions { InitialBackoff = TimeSpan.FromSeconds(2) });

        await DrainAsync(pipeline, RidePath);

        CollectionAssert.Contains(_delays, stated);
    }

    [TestMethod]
    public async Task APermanentFailure_IsNotRetried()
    {
        _files.Add(RidePath, "ride");
        var uploader = MockBuilders.Uploader(UploadOutcome.Permanent("rejected"));
        var pipeline = Create(uploader);

        await DrainAsync(pipeline, RidePath);

        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.AreEqual(1, pipeline.Counters.Failed);
    }

    [TestMethod]
    public async Task AnUnauthorisedResponse_TriggersOneRenewalThenOneRetry()
    {
        _files.Add(RidePath, "ride");
        var uploader = MockBuilders.Uploader(UploadOutcome.Unauthorised(), UploadOutcome.Success());
        var pipeline = Create(uploader);

        await DrainAsync(pipeline, RidePath);

        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        Assert.AreEqual(1, pipeline.Counters.Processed);
    }

    [TestMethod]
    public async Task RepeatedUnauthorised_StopsRatherThanLooping()
    {
        _files.Add(RidePath, "ride");
        var uploader = MockBuilders.Uploader(UploadOutcome.Unauthorised());
        var pipeline = Create(uploader);

        await DrainAsync(pipeline, RidePath);

        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2),
            "the session was renewed more than once");
        Assert.AreEqual(1, pipeline.Counters.Failed);
    }

    // --------------------------------------------------------------- retention

    [TestMethod]
    public async Task TheSourceFileIsDeletedAfterSuccess_WhenRetentionIsOff()
    {
        _files.Add(RidePath, "ride");
        _settings.Object.Update(s => s with { KeepUploadedActivityFile = false });

        var fileStore = MockBuilders.FileStore(_files);
        await DrainAsync(Create(MockBuilders.Uploader(UploadOutcome.Success()), fileStore: fileStore), RidePath);

        fileStore.Verify(x => x.DeleteAsync(RidePath), Times.Once);
    }

    [TestMethod]
    public async Task TheSourceFileIsKeptAfterSuccess_WhenRetentionIsOn()
    {
        _files.Add(RidePath, "ride");
        _settings.Object.Update(s => s with { KeepUploadedActivityFile = true });

        var fileStore = MockBuilders.FileStore(_files);
        await DrainAsync(Create(MockBuilders.Uploader(UploadOutcome.Success()), fileStore: fileStore), RidePath);

        fileStore.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        Assert.IsTrue(_files.Has(RidePath));
    }

    [TestMethod]
    public async Task TheSourceFileIsKeptOnDuplicate_WhateverTheRetentionSetting()
    {
        _files.Add(RidePath, "ride");
        _settings.Object.Update(s => s with { KeepUploadedActivityFile = false });

        var fileStore = MockBuilders.FileStore(_files);
        await DrainAsync(Create(MockBuilders.Uploader(UploadOutcome.Duplicate()), fileStore: fileStore), RidePath);

        fileStore.Verify(
            x => x.DeleteAsync(It.IsAny<string>()),
            Times.Never,
            "a duplicate must not delete the user's file");
    }

    [TestMethod]
    public async Task TheSourceFileIsKeptOnFailure()
    {
        _files.Add(RidePath, "ride");
        _settings.Object.Update(s => s with { KeepUploadedActivityFile = false });

        var fileStore = MockBuilders.FileStore(_files);
        await DrainAsync(Create(MockBuilders.Uploader(UploadOutcome.Permanent()), fileStore: fileStore), RidePath);

        fileStore.Verify(
            x => x.DeleteAsync(It.IsAny<string>()),
            Times.Never,
            "a failed activity must not be lost");
    }

    [TestMethod]
    public async Task AFailedDeletion_DoesNotTurnASuccessIntoAFailure()
    {
        _files.Add(RidePath, "ride");
        var fileStore = MockBuilders.FileStore(_files, deleteThrows: new IOException("file in use"));

        var pipeline = Create(MockBuilders.Uploader(UploadOutcome.Success()), fileStore: fileStore);
        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(1, pipeline.Counters.Processed);
        Assert.AreEqual(0, pipeline.Counters.Failed);
    }

    // ---------------------------------------------------------- transformation

    [TestMethod]
    public async Task AnEmptyTransformationSet_UploadsTheOriginalBytes()
    {
        _files.Add(RidePath, "ride");
        var uploader = MockBuilders.Uploader(UploadOutcome.Success());

        await DrainAsync(Create(uploader), RidePath);

        uploader.Verify(
            x => x.UploadAsync(
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "ride"),
                RidePath,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task AConfiguredTransformation_IsAppliedBeforeUpload()
    {
        _files.Add(RidePath, "ride");
        var uploader = MockBuilders.Uploader(UploadOutcome.Success());
        var transformation = MockBuilders.MarkerTransformation();

        await DrainAsync(Create(uploader, [transformation.Object]), RidePath);

        uploader.Verify(
            x => x.UploadAsync(
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "ride" + MockBuilders.TransformationMarker),
                RidePath,
                It.IsAny<CancellationToken>()),
            Times.Once);
        transformation.Verify(x => x.Apply(It.IsAny<byte[]>(), "ride.fit"), Times.Once);
    }

    [TestMethod]
    public async Task AFailingTransformation_FailsTheActivityRatherThanUploadingTheOriginal()
    {
        _files.Add(RidePath, "ride");
        var uploader = MockBuilders.Uploader(UploadOutcome.Success());
        var fileStore = MockBuilders.FileStore(_files);

        var pipeline = Create(uploader, [MockBuilders.FailingTransformation().Object], fileStore: fileStore);
        await DrainAsync(pipeline, RidePath);

        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "the untransformed file was uploaded as a fallback");
        Assert.AreEqual(1, pipeline.Counters.Failed);
        fileStore.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
    }

    // ---------------------------------------------------------------- sequence

    [TestMethod]
    public async Task AnActivityAlreadyInTheRecord_IsNotUploadedAgain()
    {
        _files.Add(RidePath, "ride");
        _record = MockBuilders.Record(
            entries: new Dictionary<string, ActivityOutcome> { [HashOf("ride")] = ActivityOutcome.Uploaded });

        var uploader = MockBuilders.Uploader(UploadOutcome.Success());
        var pipeline = Create(uploader);

        pipeline.Enqueue(RidePath);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        try
        {
            await pipeline.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task AnUnexpectedFailure_DoesNotStopTheNextActivity()
    {
        _files.Add("/watched/first.fit", "first");
        _files.Add("/watched/second.fit", "second");

        // Reading the first file throws; the second must still be processed.
        var fileStore = MockBuilders.FileStore(
            _files,
            readThrows: new IOException("unreadable"),
            readThrowsForPath: "/watched/first.fit");

        var pipeline = Create(MockBuilders.Uploader(UploadOutcome.Success()), fileStore: fileStore);

        await DrainAsync(pipeline, "/watched/first.fit", "/watched/second.fit");

        Assert.AreEqual(1, pipeline.Counters.Failed);
        Assert.AreEqual(1, pipeline.Counters.Processed);
    }

    [TestMethod]
    public async Task ABurstIsProcessedOneAtATime_InOrder()
    {
        _files.Add("/watched/a.fit", "a");
        _files.Add("/watched/b.fit", "b");
        _files.Add("/watched/c.fit", "c");

        var order = new List<string>();
        var concurrent = 0;
        var maxConcurrent = 0;

        var uploader = new Mock<IActivityUploader>(MockBehavior.Strict);
        uploader
            .Setup(x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (byte[] _, string path, CancellationToken token) =>
            {
                maxConcurrent = Math.Max(maxConcurrent, Interlocked.Increment(ref concurrent));
                order.Add(path);
                await Task.Delay(5, token);
                Interlocked.Decrement(ref concurrent);

                return UploadOutcome.Success();
            });

        var pipeline = Create(uploader);

        await DrainAsync(pipeline, "/watched/a.fit", "/watched/b.fit", "/watched/c.fit");

        CollectionAssert.AreEqual(new[] { "/watched/a.fit", "/watched/b.fit", "/watched/c.fit" }, order);
        Assert.AreEqual(1, maxConcurrent, "uploads overlapped");
    }
}
