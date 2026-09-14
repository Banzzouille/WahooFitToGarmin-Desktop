namespace WahooFitToGarmin_Desktop.Core.Contracts.Services
{
    public interface IFileService
    {
        // Returns null when the file does not exist or holds no usable content.
        T? Read<T>(string folderPath, string fileName);

        void Save<T>(string folderPath, string fileName, T content);

        void Delete(string folderPath, string fileName);
    }
}
