using System.Text;

using Newtonsoft.Json;

using WahooFitToGarmin_Desktop.Core.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Helpers;

namespace WahooFitToGarmin_Desktop.Core.Services
{
    public class FileService : IFileService
    {
        public T? Read<T>(string folderPath, string fileName)
        {
            var path = Path.Combine(folderPath, fileName);
            if (!File.Exists(path))
            {
                return default;
            }

            var fileContent = File.ReadAllText(path);
            if (fileContent.StartsWith('{'))
            {
                return JsonConvert.DeserializeObject<T>(fileContent);
            }

            var json = StringExtensions.DecodeBase64(fileContent, Encoding.UTF8);
            return json is null ? default : JsonConvert.DeserializeObject<T>(json);
        }

        public void Save<T>(string folderPath, string fileName, T content)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileContent = JsonConvert.SerializeObject(content);
            var obfuscatedSettings = StringExtensions.EncodeBase64(fileContent, Encoding.UTF8);
            File.WriteAllText(Path.Combine(folderPath, fileName), obfuscatedSettings, Encoding.UTF8);
        }

        public void Delete(string folderPath, string fileName)
        {
            if (fileName != null && File.Exists(Path.Combine(folderPath, fileName)))
            {
                File.Delete(Path.Combine(folderPath, fileName));
            }
        }
    }
}
