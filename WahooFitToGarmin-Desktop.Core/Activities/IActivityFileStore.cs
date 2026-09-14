namespace WahooFitToGarmin_Desktop.Core.Activities
{
    /// <summary>
    /// Reads and removes activity files.
    /// </summary>
    /// <remarks>
    /// Behind an interface so the pipeline's behaviour — retention, deletion,
    /// failure handling — can be exercised without a file system, and so that a
    /// test can assert that nothing was deleted.
    /// </remarks>
    public interface IActivityFileStore
    {
        Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken);

        Task DeleteAsync(string path);

        /// <summary>Activity files currently in the folder.</summary>
        IReadOnlyList<string> Enumerate(string folderPath);
    }

    /// <inheritdoc cref="IActivityFileStore"/>
    public sealed class ActivityFileStore : IActivityFileStore
    {
        public const string ActivityFilePattern = "*.fit";

        public Task<byte[]> ReadAsync(string path, CancellationToken cancellationToken) =>
            File.ReadAllBytesAsync(path, cancellationToken);

        public Task DeleteAsync(string path)
        {
            File.Delete(path);
            return Task.CompletedTask;
        }

        public IReadOnlyList<string> Enumerate(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                return [];
            }

            // Top level only, matching the watcher: subfolders were never
            // processed and nothing here changes that.
            return Directory.GetFiles(folderPath, ActivityFilePattern, SearchOption.TopDirectoryOnly);
        }
    }
}
