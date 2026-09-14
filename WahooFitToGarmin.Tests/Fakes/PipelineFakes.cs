using System.Text;

using WahooFitToGarmin_Desktop.Core.Activities;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.Tests.Fakes;

/// <summary>In-memory activity files, so nothing touches a disk.</summary>
internal sealed class FakeFileStore : IActivityFileStore, IFileSystemProbe
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public List<string> Deleted { get; } = [];

    public Exception? ReadThrows { get; set; }

    public Exception? DeleteThrows { get; set; }

    public void Add(string path, string content) => _files[path] = Encoding.UTF8.GetBytes(content);

    public bool Has(string path) => _files.ContainsKey(path);

    public Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken)
    {
        if (ReadThrows is not null)
        {
            throw ReadThrows;
        }

        return Task.FromResult(_files[path]);
    }

    public Task DeleteAsync(string path)
    {
        if (DeleteThrows is not null)
        {
            throw DeleteThrows;
        }

        _files.Remove(path);
        Deleted.Add(path);
        return Task.CompletedTask;
    }

    public IReadOnlyList<string> Enumerate(string folderPath) => _files.Keys.ToList();

    // The probe side: every known file is immediately stable.
    public bool Exists(string path) => _files.ContainsKey(path);

    public (long Length, DateTime LastWriteUtc)? Stat(string path) =>
        _files.TryGetValue(path, out var bytes) ? (bytes.LongLength, DateTime.UnixEpoch) : null;

    public bool CanOpenForRead(string path) => _files.ContainsKey(path);
}

/// <summary>An uploader driven by a scripted sequence of outcomes.</summary>
internal sealed class FakeUploader : IActivityUploader
{
    private readonly Queue<UploadOutcome> _outcomes;

    public FakeUploader(params UploadOutcome[] outcomes) => _outcomes = new Queue<UploadOutcome>(outcomes);

    public int Attempts { get; private set; }

    public List<byte[]> Uploaded { get; } = [];

    public Task<UploadOutcome> UploadAsync(byte[] content, string filePath, CancellationToken cancellationToken)
    {
        Attempts++;
        Uploaded.Add(content);

        // The last scripted outcome repeats, so a test only states what changes.
        var outcome = _outcomes.Count > 1 ? _outcomes.Dequeue() : _outcomes.Peek();
        return Task.FromResult(outcome);
    }
}

/// <summary>A settings store with no file behind it.</summary>
internal sealed class FakeSettingsStore : ISettingsStore
{
    public FakeSettingsStore(UserSettings? initial = null) => Current = initial ?? new UserSettings();

    public UserSettings Current { get; private set; }

    public event EventHandler<UserSettings>? Changed;

    public void Update(Func<UserSettings, UserSettings> change)
    {
        Current = change(Current);
        Changed?.Invoke(this, Current);
    }
}

/// <summary>An in-memory idempotence record.</summary>
internal sealed class FakeRecord : IProcessedActivityRecord
{
    private readonly Dictionary<string, ActivityOutcome> _entries = new(StringComparer.Ordinal);

    public FakeRecord(bool isFirstRun = false) => IsFirstRun = isFirstRun;

    public bool IsFirstRun { get; }

    public IReadOnlyDictionary<string, ActivityOutcome> Entries => _entries;

    public bool Contains(string contentHash) => _entries.ContainsKey(contentHash);

    public void Mark(string contentHash, string fileName, ActivityOutcome outcome) =>
        _entries[contentHash] = outcome;

    public void MarkBaseline(IReadOnlyCollection<(string ContentHash, string FileName)> entries)
    {
        foreach (var (hash, _) in entries)
        {
            _entries[hash] = ActivityOutcome.Baseline;
        }
    }
}

/// <summary>Appends a marker, so a test can tell transformed bytes apart.</summary>
internal sealed class MarkerTransformation : IActivityTransformation
{
    public const string Marker = "-transformed";

    public byte[] Apply(byte[] content, string fileName) =>
        Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(content) + Marker);
}

internal sealed class ThrowingTransformation : IActivityTransformation
{
    public byte[] Apply(byte[] content, string fileName) =>
        throw new InvalidOperationException("the file could not be prepared");
}
