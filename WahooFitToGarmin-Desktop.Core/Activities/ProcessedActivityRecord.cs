using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>One remembered activity.</summary>
    public sealed record ProcessedActivity
    {
        [JsonPropertyName("hash")]
        public string ContentHash { get; init; } = string.Empty;

        [JsonPropertyName("name")]
        public string FileName { get; init; } = string.Empty;

        [JsonPropertyName("outcome")]
        public ActivityOutcome Outcome { get; init; }

        [JsonPropertyName("at")]
        public DateTimeOffset RecordedAt { get; init; }
    }

    /// <inheritdoc cref="IProcessedActivityRecord"/>
    public sealed class ProcessedActivityRecord : IProcessedActivityRecord
    {
        /// <summary>
        /// Entries kept. Beyond this the oldest are dropped; an activity old
        /// enough to fall off the end is not one a sync client is still
        /// re-offering. The service's own duplicate detection is the backstop.
        /// </summary>
        private const int MaxEntries = 2000;

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = false,
            Converters = { new JsonStringEnumConverter() },
        };

        private readonly string _filePath;
        private readonly ILogger<ProcessedActivityRecord> _logger;
        private readonly object _gate = new();
        private readonly Dictionary<string, ProcessedActivity> _entries = new(StringComparer.Ordinal);

        public ProcessedActivityRecord(string filePath, ILogger<ProcessedActivityRecord> logger)
        {
            _filePath = filePath;
            _logger = logger;

            IsFirstRun = !File.Exists(_filePath);
            Load();
        }

        /// <inheritdoc/>
        public bool IsFirstRun { get; }

        public bool Contains(string contentHash)
        {
            lock (_gate)
            {
                return _entries.ContainsKey(contentHash);
            }
        }

        public void Mark(string contentHash, string fileName, ActivityOutcome outcome)
        {
            lock (_gate)
            {
                _entries[contentHash] = new ProcessedActivity
                {
                    ContentHash = contentHash,
                    FileName = fileName,
                    Outcome = outcome,
                    RecordedAt = DateTimeOffset.UtcNow,
                };

                Prune();
                Persist();
            }
        }

        public void MarkBaseline(IReadOnlyCollection<(string ContentHash, string FileName)> entries)
        {
            lock (_gate)
            {
                var now = DateTimeOffset.UtcNow;

                foreach (var (hash, name) in entries)
                {
                    _entries[hash] = new ProcessedActivity
                    {
                        ContentHash = hash,
                        FileName = name,
                        Outcome = ActivityOutcome.Baseline,
                        RecordedAt = now,
                    };
                }

                Prune();
                Persist();
            }
        }

        private void Prune()
        {
            if (_entries.Count <= MaxEntries)
            {
                return;
            }

            var surplus = _entries
                .OrderBy(x => x.Value.RecordedAt)
                .Take(_entries.Count - MaxEntries)
                .Select(x => x.Key)
                .ToList();

            foreach (var key in surplus)
            {
                _entries.Remove(key);
            }
        }

        private void Load()
        {
            if (IsFirstRun)
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var stored = JsonSerializer.Deserialize<List<ProcessedActivity>>(json, Options);

                if (stored is null)
                {
                    return;
                }

                foreach (var entry in stored.Where(e => !string.IsNullOrEmpty(e.ContentHash)))
                {
                    _entries[entry.ContentHash] = entry;
                }
            }
            catch (Exception ex)
            {
                // A record we cannot read degrades to the service's own duplicate
                // detection: files get re-offered, the service reports them as
                // duplicates, and nothing is deleted. Noise, not data loss.
                _logger.LogError(
                    ex,
                    "Could not read the processed-activity record at {FilePath}; previously uploaded files may be offered again",
                    _filePath);
            }
        }

        private void Persist()
        {
            try
            {
                var directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_entries.Values.ToList(), Options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Could not write the processed-activity record to {FilePath}; an activity may be offered again after a restart",
                    _filePath);
            }
        }
    }
}
