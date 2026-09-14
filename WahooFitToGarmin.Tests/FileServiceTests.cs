using System.Text;
using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using WahooFitToGarmin_Desktop.Core.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Services;

namespace WahooFitToGarmin.Tests;

[TestClass]
public sealed class FileServiceTests
{
    private string _folder = null!;
    private IFileService _service = null!;

    /// <summary>The five keys version 1.1.0 stored.</summary>
    private static readonly string[] StoredKeys =
    [
        "WahooDropBoxFolder",
        "GarminLogin",
        "GarminPwd",
        "KeepUploadedActivityFile",
        "Theme",
    ];

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "wftg-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_folder);
        _service = new FileService();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [TestMethod]
    public void RoundTrip_PreservesEveryStoredKey()
    {
        var original = new Dictionary<string, string>
        {
            ["WahooDropBoxFolder"] = @"C:\Users\rider\Dropbox\Apps\WahooFitness",
            ["GarminLogin"] = "rider@example.com",
            ["GarminPwd"] = "correct horse battery staple",
            ["KeepUploadedActivityFile"] = "True",
            ["Theme"] = "Dark",
        };

        _service.Save(_folder, "AppProperties.json", original);
        var restored = _service.Read<Dictionary<string, string>>(_folder, "AppProperties.json");

        Assert.IsNotNull(restored);
        foreach (var key in StoredKeys)
        {
            Assert.AreEqual(original[key], restored[key], $"value for {key} changed through the round trip");
        }
    }

    [TestMethod]
    public void Read_AcceptsBase64WrappedFileFromPreviousVersion()
    {
        // How version 1.1.0 wrote it: the property bag serialized as JSON, then
        // base64 encoded. Note the boolean, which that version stored unquoted
        // because it serialized the bag's values as they came.
        const string legacyJson =
            """{"WahooDropBoxFolder":"D:\\Dropbox\\Wahoo","GarminLogin":"rider@example.com","GarminPwd":"secret","KeepUploadedActivityFile":true,"Theme":"Light"}""";

        var wrapped = Convert.ToBase64String(Encoding.UTF8.GetBytes(legacyJson));
        File.WriteAllText(Path.Combine(_folder, "AppProperties.json"), wrapped, Encoding.UTF8);

        var restored = _service.Read<Dictionary<string, JsonElement>>(_folder, "AppProperties.json");

        Assert.IsNotNull(restored);
        Assert.AreEqual(@"D:\Dropbox\Wahoo", restored["WahooDropBoxFolder"].GetString());
        Assert.AreEqual("rider@example.com", restored["GarminLogin"].GetString());
        Assert.AreEqual(JsonValueKind.True, restored["KeepUploadedActivityFile"].ValueKind);
        Assert.AreEqual("Light", restored["Theme"].GetString());
    }

    [TestMethod]
    public void Read_AcceptsPlainJsonFile()
    {
        const string plainJson = """{"GarminLogin":"rider@example.com","Theme":"Dark"}""";
        File.WriteAllText(Path.Combine(_folder, "AppProperties.json"), plainJson, Encoding.UTF8);

        var restored = _service.Read<Dictionary<string, string>>(_folder, "AppProperties.json");

        Assert.IsNotNull(restored);
        Assert.AreEqual("rider@example.com", restored["GarminLogin"]);
        Assert.AreEqual("Dark", restored["Theme"]);
    }

    [TestMethod]
    public void Read_ReturnsDefaultWhenFileIsAbsent()
    {
        var restored = _service.Read<Dictionary<string, string>>(_folder, "does-not-exist.json");

        Assert.IsNull(restored);
    }

    [TestMethod]
    public void Read_ReturnsDefaultWhenFileIsEmpty()
    {
        File.WriteAllText(Path.Combine(_folder, "AppProperties.json"), string.Empty);

        var restored = _service.Read<Dictionary<string, string>>(_folder, "AppProperties.json");

        Assert.IsNull(restored);
    }

    [TestMethod]
    public void Save_CreatesTheFolderWhenItIsMissing()
    {
        var nested = Path.Combine(_folder, "Configurations");

        _service.Save(nested, "AppProperties.json", new Dictionary<string, string> { ["Theme"] = "Dark" });

        Assert.IsTrue(File.Exists(Path.Combine(nested, "AppProperties.json")));
    }

    [TestMethod]
    public void Save_KeepsTheBase64Envelope()
    {
        // The wrapper is obfuscation rather than encryption, but it is kept
        // until no credential is left in the file to obscure.
        _service.Save(_folder, "AppProperties.json", new Dictionary<string, string> { ["Theme"] = "Dark" });

        var raw = File.ReadAllText(Path.Combine(_folder, "AppProperties.json"));

        Assert.IsFalse(raw.TrimStart().StartsWith('{'), "the file was written as plain JSON");
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(raw));
        Assert.IsTrue(decoded.Contains("\"Theme\""), "the decoded content is not the expected JSON");
    }

    [TestMethod]
    public void Delete_RemovesTheFile()
    {
        _service.Save(_folder, "AppProperties.json", new Dictionary<string, string> { ["Theme"] = "Dark" });

        _service.Delete(_folder, "AppProperties.json");

        Assert.IsFalse(File.Exists(Path.Combine(_folder, "AppProperties.json")));
    }
}
