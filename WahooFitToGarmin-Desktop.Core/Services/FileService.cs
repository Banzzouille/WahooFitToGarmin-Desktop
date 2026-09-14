using System.Text;
using System.Text.Json;

using WahooFitToGarmin_Desktop.Core.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Helpers;

namespace WahooFitToGarmin_Desktop.Core.Services
{
    public class FileService : IFileService
    {
        /// <summary>
        /// UTF-8 without a byte order mark. <c>Encoding.UTF8</c> emits one, which
        /// is pointless on a base64 payload and trips any tool that reads the
        /// file without expecting it.
        /// </summary>
        private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// Reads a settings file written by this or any previous version.
        /// </summary>
        /// <remarks>
        /// Both stored forms are accepted: the base64-wrapped form written by
        /// version 1.1.0 and earlier, and plain JSON. The wrapper is obfuscation
        /// rather than encryption, and it is kept on write until there is no
        /// credential left in the file to obscure.
        /// </remarks>
        public T? Read<T>(string folderPath, string fileName)
        {
            var path = Path.Combine(folderPath, fileName);
            if (!File.Exists(path))
            {
                return default;
            }

            var fileContent = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                return default;
            }

            var json = fileContent.TrimStart().StartsWith('{')
                ? fileContent
                : StringExtensions.DecodeBase64(fileContent.Trim('\uFEFF'), Encoding.UTF8);

            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, Options);
        }

        public void Save<T>(string folderPath, string fileName, T content)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileContent = JsonSerializer.Serialize(content, Options);
            var obfuscatedSettings = StringExtensions.EncodeBase64(fileContent, Encoding.UTF8);

            File.WriteAllText(Path.Combine(folderPath, fileName), obfuscatedSettings, Utf8NoBom);
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
