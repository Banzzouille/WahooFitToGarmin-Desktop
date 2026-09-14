using System.Text;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WahooFitToGarmin.Tests.Fakes;

using WahooFitToGarmin_Desktop.Core.Activities;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.Tests;

internal sealed class FakeWatcher : IFolderWatcher
{
    public event Action<string>? FileAppeared;

    public List<string> Watched { get; } = [];

    public int StopCalls { get; private set; }

    public string? CurrentFolder { get; private set; }

    public void Watch(string folderPath)
    {
        Watched.Add(folderPath);
        CurrentFolder = folderPath;
    }

    public void Stop()
    {
        StopCalls++;
        CurrentFolder = null;
    }

    public void Raise(string path) => FileAppeared?.Invoke(path);

    public void Dispose() => Stop();
}

[TestClass]
public sealed class ActivityDiscoveryTests
{
    private string _folder = null!;
    private FakeFileStore _files = null!;
    private FakeWatcher _watcher = null!;
    private FakeSettingsStore _settings = null!;

    [TestInitialize]
    public void Setup()
    {
        // A real folder is needed because discovery checks that the configured
        // path exists before watching it.
        _folder = Path.Combine(Path.GetTempPath(), "wftg-discovery", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_folder);

        _files = new FakeFileStore();
        _watcher = new FakeWatcher();
        _settings = new FakeSettingsStore(new UserSettings { WatchedFolder = _folder });
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private (ActivityDiscovery Discovery, ActivityPipeline Pipeline, FakeUploader Uploader) Create(
        IProcessedActivityRecord record)
    {
        var uploader = new FakeUploader(UploadOutcome.Success());
        var readiness = new FileReadinessWaiter(
            _files, NullLogger.Instance, TimeSpan.Zero, requiredStableChecks: 1);

        var pipeline = new ActivityPipeline(
            _settings, record, uploader, [], _files, readiness, _files,
            NullLogger.Instance, null, (_, _) => Task.CompletedTask);

        var discovery = new ActivityDiscovery(
            _settings, record, _files, _watcher, pipeline, NullLogger.Instance);

        return (discovery, pipeline, uploader);
    }

    private static async Task DrainAsync(ActivityPipeline pipeline, int expected)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var run = pipeline.RunAsync(cts.Token);

        while (pipeline.Counters.Processed + pipeline.Counters.Failed + pipeline.Counters.Duplicates < expected
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

    /// <summary>Puts a real file on disk and registers it with the fake store.</summary>
    private string AddFile(string name, string content)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllText(path, content);
        _files.Add(path, content);
        return path;
    }

    [TestMethod]
    public void FirstRun_BaselinesExistingFilesWithoutUploadingThem()
    {
        AddFile("old-1.fit", "one");
        AddFile("old-2.fit", "two");

        var record = new FakeRecord(isFirstRun: true);
        var (discovery, pipeline, uploader) = Create(record);

        discovery.Start();

        Assert.AreEqual(0, uploader.Attempts, "existing files were uploaded on the first run");
        Assert.AreEqual(2, record.Entries.Count);
        Assert.IsTrue(record.Entries.Values.All(o => o == ActivityOutcome.Baseline));
        Assert.AreEqual(0, pipeline.Counters.Processed);
    }

    [TestMethod]
    public async Task SecondRun_ProcessesAFileThatArrivedWhileClosed()
    {
        AddFile("new.fit", "new ride");

        var record = new FakeRecord(isFirstRun: false);
        var (discovery, pipeline, uploader) = Create(record);

        discovery.Start();
        await DrainAsync(pipeline, expected: 1);

        Assert.AreEqual(1, uploader.Attempts);
        Assert.AreEqual(1, pipeline.Counters.Processed);
    }

    [TestMethod]
    public async Task AFileSeenByBothTheScanAndTheWatcher_IsProcessedOnce()
    {
        var path = AddFile("ride.fit", "ride");

        var record = new FakeRecord(isFirstRun: false);
        var (discovery, pipeline, uploader) = Create(record);

        discovery.Start();
        _watcher.Raise(path);

        await DrainAsync(pipeline, expected: 1);
        await Task.Delay(50, CancellationToken.None);

        Assert.AreEqual(1, uploader.Attempts, "the same file was uploaded twice");
    }

    [TestMethod]
    public void TheWatcherStartsBeforeTheScan_SoNothingArrivingDuringItIsMissed()
    {
        AddFile("ride.fit", "ride");

        var (discovery, _, _) = Create(new FakeRecord(isFirstRun: false));
        discovery.Start();

        CollectionAssert.Contains(_watcher.Watched, _folder);
    }

    [TestMethod]
    public void NoConfiguredFolder_IsReportedAndDoesNotThrow()
    {
        _settings.Update(s => s with { WatchedFolder = null });

        var (discovery, _, _) = Create(new FakeRecord(isFirstRun: false));

        discovery.Start();

        Assert.AreEqual(0, _watcher.Watched.Count);
    }

    [TestMethod]
    public void AConfiguredFolderThatDoesNotExist_IsReportedAndDoesNotThrow()
    {
        _settings.Update(s => s with { WatchedFolder = Path.Combine(_folder, "gone") });

        var (discovery, _, _) = Create(new FakeRecord(isFirstRun: false));

        discovery.Start();

        Assert.AreEqual(0, _watcher.Watched.Count);
    }

    [TestMethod]
    public void ChangingTheWatchedFolder_MovesTheWatcherWithoutARestart()
    {
        var (discovery, _, _) = Create(new FakeRecord(isFirstRun: false));
        discovery.Start();

        var second = Path.Combine(Path.GetTempPath(), "wftg-discovery", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(second);

        try
        {
            _settings.Update(s => s with { WatchedFolder = second });

            Assert.AreEqual(second, _watcher.CurrentFolder);
            CollectionAssert.Contains(_watcher.Watched, second);
        }
        finally
        {
            Directory.Delete(second, recursive: true);
        }
    }

    [TestMethod]
    public void SupplyingAMissingFolderLater_StartsWatchingImmediately()
    {
        _settings.Update(s => s with { WatchedFolder = null });

        var (discovery, _, _) = Create(new FakeRecord(isFirstRun: false));
        discovery.Start();
        Assert.AreEqual(0, _watcher.Watched.Count);

        _settings.Update(s => s with { WatchedFolder = _folder });

        CollectionAssert.Contains(_watcher.Watched, _folder);
    }

    [TestMethod]
    public async Task AFileRaisedByTheWatcher_IsProcessed()
    {
        var record = new FakeRecord(isFirstRun: false);
        var (discovery, pipeline, uploader) = Create(record);
        discovery.Start();

        var path = AddFile("later.fit", "later ride");
        _watcher.Raise(path);

        await DrainAsync(pipeline, expected: 1);

        Assert.AreEqual(1, uploader.Attempts);
    }

    [TestMethod]
    public void BaselineIsEstablishedEvenWhenTheFolderIsEmpty_SoTheSecondRunScansNormally()
    {
        var record = new FakeRecord(isFirstRun: true);
        var (discovery, _, uploader) = Create(record);

        discovery.Start();

        Assert.AreEqual(0, uploader.Attempts);
        Assert.AreEqual(0, record.Entries.Count);
    }

    [TestMethod]
    public async Task AfterTheBaseline_ANewFileIsStillProcessed()
    {
        AddFile("old.fit", "old");

        var record = new FakeRecord(isFirstRun: true);
        var (discovery, pipeline, uploader) = Create(record);
        discovery.Start();

        // Arrives after the baseline was taken.
        var fresh = AddFile("fresh.fit", "fresh ride");
        _watcher.Raise(fresh);

        await DrainAsync(pipeline, expected: 1);

        Assert.AreEqual(1, uploader.Attempts);
        CollectionAssert.AreEqual(Encoding.UTF8.GetBytes("fresh ride"), uploader.Uploaded.Single());
    }
}
