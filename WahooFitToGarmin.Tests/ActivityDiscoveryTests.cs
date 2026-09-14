using System.Text;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using WahooFitToGarmin.Tests.Mocks;

using WahooFitToGarmin_Desktop.Core.Activities;
using WahooFitToGarmin_Desktop.Core.Platform;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.Tests;

[TestClass]
public sealed class ActivityDiscoveryTests
{
    private string _folder = null!;
    private ActivityFiles _files = null!;
    private Mock<IFolderWatcher> _watcher = null!;
    private Mock<ISettingsStore> _settings = null!;
    private Mock<INotifier> _notifier = null!;

    [TestInitialize]
    public void Setup()
    {
        // A real folder is needed because discovery checks that the configured
        // path exists before watching it.
        _folder = Path.Combine(Path.GetTempPath(), "wftg-discovery", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_folder);

        _files = new ActivityFiles();
        _watcher = MockBuilders.Watcher();
        _settings = MockBuilders.SettingsStore(new UserSettings { WatchedFolder = _folder });
        _notifier = MockBuilders.Notifier();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private (ActivityDiscovery Discovery, ActivityPipeline Pipeline, Mock<IActivityUploader> Uploader) Create(
        Mock<IProcessedActivityRecord> record)
    {
        var uploader = MockBuilders.Uploader(UploadOutcome.Success());
        var probe = MockBuilders.StableProbe(_files);
        var fileStore = MockBuilders.FileStore(_files);

        var readiness = new FileReadinessWaiter(
            probe.Object, NullLogger.Instance, TimeSpan.Zero, requiredStableChecks: 1);

        var pipeline = new ActivityPipeline(
            _settings.Object, record.Object, uploader.Object, [], probe.Object, readiness,
            fileStore.Object, NullLogger.Instance, null, (_, _) => Task.CompletedTask);

        var discovery = new ActivityDiscovery(
            _settings.Object, record.Object, fileStore.Object, _watcher.Object, pipeline,
            _notifier.Object, NullLogger.Instance);

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

    /// <summary>Puts a real file on disk and registers its content in memory.</summary>
    private string AddFile(string name, string content)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllText(path, content);
        _files.Add(path, content);
        return path;
    }

    /// <summary>Raises the watcher's event, as the real watcher would.</summary>
    private void RaiseFileAppeared(string path) => _watcher.Raise(x => x.FileAppeared += null, path);

    private static void VerifyNoUpload(Mock<IActivityUploader> uploader, string because) =>
        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            because);

    [TestMethod]
    public void FirstRun_BaselinesExistingFilesWithoutUploadingThem()
    {
        AddFile("old-1.fit", "one");
        AddFile("old-2.fit", "two");

        var record = MockBuilders.Record(isFirstRun: true);
        var (discovery, pipeline, uploader) = Create(record);

        discovery.Start();

        VerifyNoUpload(uploader, "existing files were uploaded on the first run");
        record.Verify(
            x => x.MarkBaseline(It.Is<IReadOnlyCollection<(string, string)>>(e => e.Count == 2)),
            Times.Once);
        Assert.AreEqual(0, pipeline.Counters.Processed);
    }

    [TestMethod]
    public async Task SecondRun_ProcessesAFileThatArrivedWhileClosed()
    {
        AddFile("new.fit", "new ride");

        var (discovery, pipeline, uploader) = Create(MockBuilders.Record(isFirstRun: false));

        discovery.Start();
        await DrainAsync(pipeline, expected: 1);

        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.AreEqual(1, pipeline.Counters.Processed);
    }

    [TestMethod]
    public async Task AFileSeenByBothTheScanAndTheWatcher_IsProcessedOnce()
    {
        var path = AddFile("ride.fit", "ride");

        var (discovery, pipeline, uploader) = Create(MockBuilders.Record(isFirstRun: false));

        discovery.Start();
        RaiseFileAppeared(path);

        await DrainAsync(pipeline, expected: 1);
        await Task.Delay(50, CancellationToken.None);

        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "the same file was uploaded twice");
    }

    [TestMethod]
    public void TheWatcherStartsBeforeTheScan_SoNothingArrivingDuringItIsMissed()
    {
        AddFile("ride.fit", "ride");

        var (discovery, _, _) = Create(MockBuilders.Record(isFirstRun: false));
        discovery.Start();

        _watcher.Verify(x => x.Watch(_folder), Times.Once);
    }

    [TestMethod]
    public void NoConfiguredFolder_IsReportedAndDoesNotThrow()
    {
        _settings.Object.Update(s => s with { WatchedFolder = null });

        var (discovery, _, _) = Create(MockBuilders.Record(isFirstRun: false));

        discovery.Start();

        _watcher.Verify(x => x.Watch(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void AConfiguredFolderThatDoesNotExist_IsReportedAndDoesNotThrow()
    {
        _settings.Object.Update(s => s with { WatchedFolder = Path.Combine(_folder, "gone") });

        var (discovery, _, _) = Create(MockBuilders.Record(isFirstRun: false));

        discovery.Start();

        _watcher.Verify(x => x.Watch(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void ChangingTheWatchedFolder_MovesTheWatcherWithoutARestart()
    {
        var (discovery, _, _) = Create(MockBuilders.Record(isFirstRun: false));
        discovery.Start();

        var second = Path.Combine(Path.GetTempPath(), "wftg-discovery", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(second);

        try
        {
            _settings.Object.Update(s => s with { WatchedFolder = second });

            _watcher.Verify(x => x.Watch(second), Times.Once);
        }
        finally
        {
            Directory.Delete(second, recursive: true);
        }
    }

    [TestMethod]
    public void SupplyingAMissingFolderLater_StartsWatchingImmediately()
    {
        _settings.Object.Update(s => s with { WatchedFolder = null });

        var (discovery, _, _) = Create(MockBuilders.Record(isFirstRun: false));
        discovery.Start();
        _watcher.Verify(x => x.Watch(It.IsAny<string>()), Times.Never);

        _settings.Object.Update(s => s with { WatchedFolder = _folder });

        _watcher.Verify(x => x.Watch(_folder), Times.Once);
    }

    [TestMethod]
    public async Task AFileRaisedByTheWatcher_IsProcessed()
    {
        var (discovery, pipeline, uploader) = Create(MockBuilders.Record(isFirstRun: false));
        discovery.Start();

        var path = AddFile("later.fit", "later ride");
        RaiseFileAppeared(path);

        await DrainAsync(pipeline, expected: 1);

        uploader.Verify(
            x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public void BaselineIsEstablishedEvenWhenTheFolderIsEmpty_SoTheSecondRunScansNormally()
    {
        var record = MockBuilders.Record(isFirstRun: true);
        var (discovery, _, uploader) = Create(record);

        discovery.Start();

        VerifyNoUpload(uploader, "an empty folder produced an upload");
        record.Verify(
            x => x.MarkBaseline(It.Is<IReadOnlyCollection<(string, string)>>(e => e.Count == 0)),
            Times.Once);
    }

    [TestMethod]
    public async Task AfterTheBaseline_ANewFileIsStillProcessed()
    {
        AddFile("old.fit", "old");

        var (discovery, pipeline, uploader) = Create(MockBuilders.Record(isFirstRun: true));
        discovery.Start();

        // Arrives after the baseline was taken.
        var fresh = AddFile("fresh.fit", "fresh ride");
        RaiseFileAppeared(fresh);

        await DrainAsync(pipeline, expected: 1);

        uploader.Verify(
            x => x.UploadAsync(
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "fresh ride"),
                fresh,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public void AFileDetectedByTheWatcher_RaisesANotification()
    {
        var (discovery, _, _) = Create(MockBuilders.Record(isFirstRun: false));
        discovery.Start();

        var path = AddFile("ride.fit", "ride");
        RaiseFileAppeared(path);

        _notifier.Verify(x => x.Notify("A new file is coming", "ride.fit"), Times.Once);
    }

    [TestMethod]
    public void TheStartupScan_DoesNotNotify()
    {
        // Restarting with files waiting must not produce a burst of
        // notifications for activities the user already knows about.
        AddFile("one.fit", "one");
        AddFile("two.fit", "two");

        var (discovery, _, _) = Create(MockBuilders.Record(isFirstRun: false));
        discovery.Start();

        _notifier.Verify(x => x.Notify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void TheBaselineScan_DoesNotNotify()
    {
        AddFile("old.fit", "old");

        var (discovery, _, _) = Create(MockBuilders.Record(isFirstRun: true));
        discovery.Start();

        _notifier.Verify(x => x.Notify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
