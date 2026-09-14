using System.Text;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WahooFitToGarmin.Tests.Fakes;

using WahooFitToGarmin_Desktop.Core.Activities;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.Tests;

[TestClass]
public sealed class ActivityPipelineTests
{
    private const string RidePath = "/watched/ride.fit";

    private FakeFileStore _files = null!;
    private FakeRecord _record = null!;
    private FakeSettingsStore _settings = null!;
    private List<TimeSpan> _delays = null!;

    [TestInitialize]
    public void Setup()
    {
        _files = new FakeFileStore();
        _record = new FakeRecord();
        _settings = new FakeSettingsStore();
        _delays = [];
    }

    private ActivityPipeline Create(
        IActivityUploader uploader,
        IEnumerable<IActivityTransformation>? transformations = null,
        PipelineOptions? options = null)
    {
        var readiness = new FileReadinessWaiter(
            _files, NullLogger.Instance, quietInterval: TimeSpan.Zero, requiredStableChecks: 1);

        return new ActivityPipeline(
            _settings,
            _record,
            uploader,
            transformations ?? [],
            _files,
            readiness,
            _files,
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

        // Let the worker settle, then stop it.
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

    // ---------------------------------------------------------------- outcomes

    [TestMethod]
    public async Task ASuccessfulUpload_IsCountedAndRecorded()
    {
        _files.Add(RidePath, "ride");
        var pipeline = Create(new FakeUploader(UploadOutcome.Success()));

        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(1, pipeline.Counters.Processed);
        Assert.AreEqual(0, pipeline.Counters.Failed);
        Assert.IsTrue(_record.Contains(ActivityHash.Compute(Encoding.UTF8.GetBytes("ride"))));
    }

    [TestMethod]
    public async Task ADuplicate_IsItsOwnOutcome_NotAFailure()
    {
        _files.Add(RidePath, "ride");
        var pipeline = Create(new FakeUploader(UploadOutcome.Duplicate("already present")));

        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(1, pipeline.Counters.Duplicates);
        Assert.AreEqual(0, pipeline.Counters.Failed);
        Assert.AreEqual(0, pipeline.Counters.Processed);
    }

    [TestMethod]
    public async Task ADuplicate_IsNotRetried()
    {
        _files.Add(RidePath, "ride");
        var uploader = new FakeUploader(UploadOutcome.Duplicate());
        var pipeline = Create(uploader);

        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(1, uploader.Attempts);
    }

    // ----------------------------------------------------------------- retries

    [TestMethod]
    public async Task ATransientFailureThenSuccess_CountsAsProcessed()
    {
        _files.Add(RidePath, "ride");
        var uploader = new FakeUploader(UploadOutcome.Transient("network"), UploadOutcome.Success());
        var pipeline = Create(uploader);

        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(2, uploader.Attempts);
        Assert.AreEqual(1, pipeline.Counters.Processed);
        Assert.AreEqual(0, pipeline.Counters.Failed);
    }

    [TestMethod]
    public async Task RetriesAreBounded()
    {
        _files.Add(RidePath, "ride");
        var uploader = new FakeUploader(UploadOutcome.Transient("network"));
        var pipeline = Create(uploader, options: new PipelineOptions { MaxAttempts = 3 });

        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(3, uploader.Attempts);
        Assert.AreEqual(1, pipeline.Counters.Failed);
    }

    [TestMethod]
    public async Task BackoffGrowsBetweenAttempts()
    {
        _files.Add(RidePath, "ride");
        var pipeline = Create(
            new FakeUploader(UploadOutcome.Transient()),
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
            new FakeUploader(UploadOutcome.Transient("rate limited", stated), UploadOutcome.Success()),
            options: new PipelineOptions { InitialBackoff = TimeSpan.FromSeconds(2) });

        await DrainAsync(pipeline, RidePath);

        CollectionAssert.Contains(_delays, stated);
    }

    [TestMethod]
    public async Task APermanentFailure_IsNotRetried()
    {
        _files.Add(RidePath, "ride");
        var uploader = new FakeUploader(UploadOutcome.Permanent("rejected"));
        var pipeline = Create(uploader);

        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(1, uploader.Attempts);
        Assert.AreEqual(1, pipeline.Counters.Failed);
    }

    [TestMethod]
    public async Task AnUnauthorisedResponse_TriggersOneRenewalThenOneRetry()
    {
        _files.Add(RidePath, "ride");
        var uploader = new FakeUploader(UploadOutcome.Unauthorised(), UploadOutcome.Success());
        var pipeline = Create(uploader);

        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(2, uploader.Attempts);
        Assert.AreEqual(1, pipeline.Counters.Processed);
    }

    [TestMethod]
    public async Task RepeatedUnauthorised_StopsRatherThanLooping()
    {
        _files.Add(RidePath, "ride");
        var uploader = new FakeUploader(UploadOutcome.Unauthorised());
        var pipeline = Create(uploader);

        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(2, uploader.Attempts, "the session was renewed more than once");
        Assert.AreEqual(1, pipeline.Counters.Failed);
    }

    // --------------------------------------------------------------- retention

    [TestMethod]
    public async Task TheSourceFileIsDeletedAfterSuccess_WhenRetentionIsOff()
    {
        _files.Add(RidePath, "ride");
        _settings.Update(s => s with { KeepUploadedActivityFile = false });

        await DrainAsync(Create(new FakeUploader(UploadOutcome.Success())), RidePath);

        CollectionAssert.Contains(_files.Deleted, RidePath);
    }

    [TestMethod]
    public async Task TheSourceFileIsKeptAfterSuccess_WhenRetentionIsOn()
    {
        _files.Add(RidePath, "ride");
        _settings.Update(s => s with { KeepUploadedActivityFile = true });

        await DrainAsync(Create(new FakeUploader(UploadOutcome.Success())), RidePath);

        Assert.AreEqual(0, _files.Deleted.Count);
        Assert.IsTrue(_files.Has(RidePath));
    }

    [TestMethod]
    public async Task TheSourceFileIsKeptOnDuplicate_WhateverTheRetentionSetting()
    {
        _files.Add(RidePath, "ride");
        _settings.Update(s => s with { KeepUploadedActivityFile = false });

        await DrainAsync(Create(new FakeUploader(UploadOutcome.Duplicate())), RidePath);

        Assert.AreEqual(0, _files.Deleted.Count, "a duplicate must not delete the user's file");
    }

    [TestMethod]
    public async Task TheSourceFileIsKeptOnFailure()
    {
        _files.Add(RidePath, "ride");
        _settings.Update(s => s with { KeepUploadedActivityFile = false });

        await DrainAsync(Create(new FakeUploader(UploadOutcome.Permanent())), RidePath);

        Assert.AreEqual(0, _files.Deleted.Count, "a failed activity must not be lost");
    }

    [TestMethod]
    public async Task AFailedDeletion_DoesNotTurnASuccessIntoAFailure()
    {
        _files.Add(RidePath, "ride");
        _files.DeleteThrows = new IOException("file in use");

        var pipeline = Create(new FakeUploader(UploadOutcome.Success()));
        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(1, pipeline.Counters.Processed);
        Assert.AreEqual(0, pipeline.Counters.Failed);
    }

    // ---------------------------------------------------------- transformation

    [TestMethod]
    public async Task AnEmptyTransformationSet_UploadsTheOriginalBytes()
    {
        _files.Add(RidePath, "ride");
        var uploader = new FakeUploader(UploadOutcome.Success());

        await DrainAsync(Create(uploader), RidePath);

        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("ride"), uploader.Uploaded.Single());
    }

    [TestMethod]
    public async Task AConfiguredTransformation_IsAppliedBeforeUpload()
    {
        _files.Add(RidePath, "ride");
        var uploader = new FakeUploader(UploadOutcome.Success());

        await DrainAsync(Create(uploader, [new MarkerTransformation()]), RidePath);

        Assert.AreEqual("ride" + MarkerTransformation.Marker, Encoding.UTF8.GetString(uploader.Uploaded.Single()));
        Assert.IsTrue(_files.Has(RidePath) || _files.Deleted.Contains(RidePath),
            "the transformation must not have replaced the source file");
    }

    [TestMethod]
    public async Task AFailingTransformation_FailsTheActivityRatherThanUploadingTheOriginal()
    {
        _files.Add(RidePath, "ride");
        var uploader = new FakeUploader(UploadOutcome.Success());

        var pipeline = Create(uploader, [new ThrowingTransformation()]);
        await DrainAsync(pipeline, RidePath);

        Assert.AreEqual(0, uploader.Attempts, "the untransformed file was uploaded as a fallback");
        Assert.AreEqual(1, pipeline.Counters.Failed);
        Assert.AreEqual(0, _files.Deleted.Count);
    }

    // ---------------------------------------------------------------- sequence

    [TestMethod]
    public async Task AnActivityAlreadyInTheRecord_IsNotUploadedAgain()
    {
        _files.Add(RidePath, "ride");
        _record.Mark(ActivityHash.Compute(Encoding.UTF8.GetBytes("ride")), "ride.fit", ActivityOutcome.Uploaded);

        var uploader = new FakeUploader(UploadOutcome.Success());
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

        Assert.AreEqual(0, uploader.Attempts);
    }

    [TestMethod]
    public async Task AnUnexpectedFailure_DoesNotStopTheNextActivity()
    {
        _files.Add("/watched/first.fit", "first");
        _files.Add("/watched/second.fit", "second");
        _files.ReadThrows = null;

        var uploader = new FakeUploader(UploadOutcome.Success());
        var pipeline = Create(uploader);

        // The first read throws, the second must still be processed.
        var throwingOnce = new ThrowOnceFileStore(_files, "/watched/first.fit");
        var readiness = new FileReadinessWaiter(
            _files, NullLogger.Instance, TimeSpan.Zero, requiredStableChecks: 1);

        pipeline = new ActivityPipeline(
            _settings, _record, uploader, [], _files, readiness, throwingOnce,
            NullLogger.Instance, null, (_, _) => Task.CompletedTask);

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

        var uploader = new OrderRecordingUploader();
        var pipeline = Create(uploader);

        await DrainAsync(pipeline, "/watched/a.fit", "/watched/b.fit", "/watched/c.fit");

        CollectionAssert.AreEqual(
            new[] { "/watched/a.fit", "/watched/b.fit", "/watched/c.fit" },
            uploader.Order);
        Assert.AreEqual(0, uploader.MaxConcurrent - 1, "uploads overlapped");
    }

    private sealed class ThrowOnceFileStore : IActivityFileStore
    {
        private readonly IActivityFileStore _inner;
        private readonly string _failingPath;

        public ThrowOnceFileStore(IActivityFileStore inner, string failingPath)
        {
            _inner = inner;
            _failingPath = failingPath;
        }

        public Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken) =>
            path == _failingPath
                ? throw new IOException("unreadable")
                : _inner.ReadAsync(path, cancellationToken);

        public Task DeleteAsync(string path) => _inner.DeleteAsync(path);

        public IReadOnlyList<string> Enumerate(string folderPath) => _inner.Enumerate(folderPath);
    }

    private sealed class OrderRecordingUploader : IActivityUploader
    {
        private int _concurrent;

        public List<string> Order { get; } = [];

        public int MaxConcurrent { get; private set; }

        public int Attempts { get; private set; }

        public async Task<UploadOutcome> UploadAsync(byte[] content, string filePath, CancellationToken cancellationToken)
        {
            var now = Interlocked.Increment(ref _concurrent);
            MaxConcurrent = Math.Max(MaxConcurrent, now);

            Attempts++;
            Order.Add(filePath);
            await Task.Delay(5, cancellationToken);

            Interlocked.Decrement(ref _concurrent);
            return UploadOutcome.Success();
        }
    }
}
