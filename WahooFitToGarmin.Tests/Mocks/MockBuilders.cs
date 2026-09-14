using System.Text;

using Moq;

using WahooFitToGarmin_Desktop.Core.Activities;
using WahooFitToGarmin_Desktop.Core.Platform;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.Tests.Mocks;

/// <summary>
/// Activity content held in memory for a test.
/// </summary>
/// <remarks>
/// This is test data, not a test double: the doubles are the Moq mocks built
/// over it by <see cref="MockBuilders"/>. Keeping the data separate is what lets
/// the file store and the readiness probe — two interfaces a test usually needs
/// to agree with each other — be backed by the same content.
/// </remarks>
internal sealed class ActivityFiles
{
    private readonly Dictionary<string, byte[]> _content = new(StringComparer.Ordinal);

    public List<string> Deleted { get; } = [];

    public IReadOnlyCollection<string> Paths => _content.Keys;

    public void Add(string path, string content) => _content[path] = Encoding.UTF8.GetBytes(content);

    public bool Has(string path) => _content.ContainsKey(path);

    public byte[] Read(string path) => _content[path];

    public void Remove(string path)
    {
        _content.Remove(path);
        Deleted.Add(path);
    }

    public long? Length(string path) => _content.TryGetValue(path, out var bytes) ? bytes.LongLength : null;
}

internal static class MockBuilders
{
    // ------------------------------------------------------------------ files

    /// <summary>
    /// A file store over in-memory content. <paramref name="readThrows"/> and
    /// <paramref name="deleteThrows"/> make the store fail on demand.
    /// </summary>
    public static Mock<IActivityFileStore> FileStore(
        ActivityFiles files,
        Exception? readThrows = null,
        Exception? deleteThrows = null,
        string? readThrowsForPath = null)
    {
        var mock = new Mock<IActivityFileStore>(MockBehavior.Strict);

        mock.Setup(x => x.ReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string path, CancellationToken _) =>
            {
                if (readThrows is not null && (readThrowsForPath is null || readThrowsForPath == path))
                {
                    throw readThrows;
                }

                return Task.FromResult(files.Read(path));
            });

        mock.Setup(x => x.DeleteAsync(It.IsAny<string>()))
            .Returns((string path) =>
            {
                if (deleteThrows is not null)
                {
                    throw deleteThrows;
                }

                files.Remove(path);
                return Task.CompletedTask;
            });

        mock.Setup(x => x.Enumerate(It.IsAny<string>()))
            .Returns(() => files.Paths.ToList());

        return mock;
    }

    /// <summary>
    /// A readiness probe over the same content, reporting every known file as
    /// immediately stable.
    /// </summary>
    public static Mock<IFileSystemProbe> StableProbe(ActivityFiles files)
    {
        var mock = new Mock<IFileSystemProbe>(MockBehavior.Strict);

        mock.Setup(x => x.Exists(It.IsAny<string>())).Returns((string path) => files.Has(path));
        mock.Setup(x => x.CanOpenForRead(It.IsAny<string>())).Returns((string path) => files.Has(path));
        mock.Setup(x => x.Stat(It.IsAny<string>()))
            .Returns((string path) =>
            {
                var length = files.Length(path);
                return length is null ? null : (length.Value, DateTime.UnixEpoch);
            });

        return mock;
    }

    // --------------------------------------------------------------- uploader

    /// <summary>
    /// An uploader returning the given outcomes in order. The last one repeats,
    /// so a test only states what changes.
    /// </summary>
    public static Mock<IActivityUploader> Uploader(params UploadOutcome[] outcomes)
    {
        var mock = new Mock<IActivityUploader>(MockBehavior.Strict);
        var remaining = new Queue<UploadOutcome>(outcomes);

        mock.Setup(x => x.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => remaining.Count > 1 ? remaining.Dequeue() : remaining.Peek());

        return mock;
    }

    // --------------------------------------------------------------- settings

    /// <summary>
    /// A settings store whose value can be replaced, raising
    /// <see cref="ISettingsStore.Changed"/> as the real one does.
    /// </summary>
    public static Mock<ISettingsStore> SettingsStore(UserSettings? initial = null)
    {
        var mock = new Mock<ISettingsStore>(MockBehavior.Strict);
        var current = initial ?? new UserSettings();

        mock.SetupGet(x => x.Current).Returns(() => current);
        mock.SetupAdd(x => x.Changed += It.IsAny<EventHandler<UserSettings>>());
        mock.SetupRemove(x => x.Changed -= It.IsAny<EventHandler<UserSettings>>());

        mock.Setup(x => x.Update(It.IsAny<Func<UserSettings, UserSettings>>()))
            .Callback((Func<UserSettings, UserSettings> change) =>
            {
                current = change(current);
                mock.Raise(x => x.Changed += null, mock.Object, current);
            });

        return mock;
    }

    // ----------------------------------------------------------------- record

    /// <summary>
    /// An idempotence record backed by a set the test can inspect.
    /// </summary>
    public static Mock<IProcessedActivityRecord> Record(
        bool isFirstRun = false,
        IDictionary<string, ActivityOutcome>? entries = null)
    {
        var mock = new Mock<IProcessedActivityRecord>(MockBehavior.Strict);
        var known = entries ?? new Dictionary<string, ActivityOutcome>(StringComparer.Ordinal);

        mock.SetupGet(x => x.IsFirstRun).Returns(isFirstRun);
        mock.Setup(x => x.Contains(It.IsAny<string>())).Returns((string hash) => known.ContainsKey(hash));

        mock.Setup(x => x.Mark(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ActivityOutcome>()))
            .Callback((string hash, string _, ActivityOutcome outcome) => known[hash] = outcome);

        mock.Setup(x => x.MarkBaseline(It.IsAny<IReadOnlyCollection<(string, string)>>()))
            .Callback((IReadOnlyCollection<(string Hash, string Name)> baseline) =>
            {
                foreach (var (hash, _) in baseline)
                {
                    known[hash] = ActivityOutcome.Baseline;
                }
            });

        return mock;
    }

    // --------------------------------------------------------- transformation

    /// <summary>Appends a marker, so a test can tell transformed bytes apart.</summary>
    public const string TransformationMarker = "-transformed";

    public static Mock<IActivityTransformation> MarkerTransformation()
    {
        var mock = new Mock<IActivityTransformation>(MockBehavior.Strict);

        mock.Setup(x => x.Apply(It.IsAny<byte[]>(), It.IsAny<string>()))
            .Returns((byte[] content, string _) =>
                Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(content) + TransformationMarker));

        return mock;
    }

    public static Mock<IActivityTransformation> FailingTransformation()
    {
        var mock = new Mock<IActivityTransformation>(MockBehavior.Strict);

        mock.Setup(x => x.Apply(It.IsAny<byte[]>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("the file could not be prepared"));

        return mock;
    }

    // ------------------------------------------------------- watcher, notifier

    public static Mock<IFolderWatcher> Watcher()
    {
        var mock = new Mock<IFolderWatcher>(MockBehavior.Strict);

        mock.Setup(x => x.Watch(It.IsAny<string>()));
        mock.Setup(x => x.Stop());
        mock.Setup(x => x.Dispose());
        mock.SetupAdd(x => x.FileAppeared += It.IsAny<Action<string>>());
        mock.SetupRemove(x => x.FileAppeared -= It.IsAny<Action<string>>());

        return mock;
    }

    public static Mock<INotifier> Notifier()
    {
        var mock = new Mock<INotifier>(MockBehavior.Strict);
        mock.Setup(x => x.Notify(It.IsAny<string>(), It.IsAny<string>()));
        return mock;
    }
}
