using System.Text;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using WahooFitToGarmin_Desktop.Core.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Services;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.Tests;

[TestClass]
public sealed class SettingsStoreTests
{
    private const string SettingsFileName = "Settings.json";

    private string _folder = null!;
    private IFileService _fileService = null!;

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "wftg-settings", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_folder);
        _fileService = new FileService();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private SettingsStore CreateStore() =>
        new(_fileService, NullLogger<SettingsStore>.Instance, _folder, SettingsFileName);

    /// <summary>Writes a settings file in the shape version 1.1.0 produced.</summary>
    private void WriteLegacyFile(string json)
    {
        var wrapped = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        File.WriteAllText(
            Path.Combine(_folder, LegacySettingsMigration.LegacyFileName),
            wrapped,
            Encoding.UTF8);
    }

    [TestMethod]
    public void NewInstallation_StartsWithDefaults_AndReportsNoFailure()
    {
        var store = CreateStore();

        Assert.IsNull(store.Current.WatchedFolder);
        Assert.IsFalse(store.Current.KeepUploadedActivityFile);
        Assert.IsFalse(store.Current.IsComplete);
    }

    [TestMethod]
    public void Update_PersistsImmediately_SoAnAbruptTerminationLosesNothing()
    {
        var store = CreateStore();
        store.Update(s => s with { WatchedFolder = @"D:\Dropbox\Wahoo" });

        // No clean shutdown, no flush: a brand new store reads what is on disk.
        var reopened = CreateStore();

        Assert.AreEqual(@"D:\Dropbox\Wahoo", reopened.Current.WatchedFolder);
    }

    [TestMethod]
    public void Update_RaisesChanged()
    {
        var store = CreateStore();
        UserSettings? observed = null;
        store.Changed += (_, s) => observed = s;

        store.Update(s => s with { Theme = "Dark" });

        Assert.IsNotNull(observed);
        Assert.AreEqual("Dark", observed.Theme);
    }

    [TestMethod]
    public void Update_DoesNotRaiseChanged_WhenNothingChanged()
    {
        var store = CreateStore();
        store.Update(s => s with { Theme = "Dark" });

        var raised = 0;
        store.Changed += (_, _) => raised++;
        store.Update(s => s with { Theme = "Dark" });

        Assert.AreEqual(0, raised, "an update that changes nothing should not notify");
    }

    [TestMethod]
    public void Migration_CarriesEveryValueWorthCarrying()
    {
        // Note the unquoted boolean: that is how version 1.1.0 wrote it.
        WriteLegacyFile(
            """{"WahooDropBoxFolder":"D:\\Dropbox\\Wahoo","GarminLogin":"rider@example.com","GarminPwd":"secret","KeepUploadedActivityFile":true,"Theme":"Light"}""");

        var store = CreateStore();

        Assert.AreEqual(@"D:\Dropbox\Wahoo", store.Current.WatchedFolder);
        Assert.AreEqual("rider@example.com", store.Current.GarminLogin);
        Assert.AreNotEqual(
            "secret",
            store.Current.GarminPassword,
            "the stored password was carried forward, which this version must not do");
        Assert.IsTrue(store.Current.KeepUploadedActivityFile);
        Assert.AreEqual("Light", store.Current.Theme);
    }

    [TestMethod]
    public void Migration_DeletesThePreviousFile()
    {
        WriteLegacyFile("""{"GarminLogin":"rider@example.com","GarminPwd":"secret"}""");

        CreateStore();

        Assert.IsFalse(
            File.Exists(Path.Combine(_folder, LegacySettingsMigration.LegacyFileName)),
            "the previous settings file survived, and it holds a password in clear text");
    }

    [TestMethod]
    public void Migration_LeavesNoCopyOfThePasswordAnywhereInTheFolder()
    {
        // The narrow assertion above would pass if the file were merely renamed.
        // This one fails if the password survives under any name at all.
        WriteLegacyFile("""{"GarminLogin":"rider@example.com","GarminPwd":"secret"}""");

        CreateStore();

        foreach (var file in Directory.EnumerateFiles(_folder))
        {
            var content = File.ReadAllText(file).Trim('\uFEFF');

            // The store obfuscates with base64, so the readable form has to be
            // recovered before searching it. A file that is not base64 is
            // searched as it stands.
            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(content));
            }
            catch (FormatException)
            {
                decoded = content;
            }

            StringAssert.DoesNotMatch(
                decoded,
                new System.Text.RegularExpressions.Regex("secret"),
                $"{Path.GetFileName(file)} still contains the password");
        }
    }

    [TestMethod]
    public void Migration_RunsOnce()
    {
        WriteLegacyFile("""{"Theme":"Light"}""");
        CreateStore();

        // A second legacy file appearing later must not overwrite settings that
        // have been used since.
        WriteLegacyFile("""{"Theme":"Dark"}""");
        var second = CreateStore();

        Assert.AreEqual("Light", second.Current.Theme, "migration ran a second time");
    }

    [TestMethod]
    public void Migration_IsNotAttemptedWhenThereIsNoPreviousFile()
    {
        var store = CreateStore();

        Assert.IsNull(store.Current.Theme);
    }

    [TestMethod]
    public void UnparseableSettingsFile_FallsBackToDefaultsRatherThanThrowing()
    {
        File.WriteAllText(Path.Combine(_folder, SettingsFileName), "{ this is not json", Encoding.UTF8);

        var store = CreateStore();

        Assert.IsNull(store.Current.WatchedFolder);
    }

    [TestMethod]
    public void UnwritableStore_KeepsTheValueInMemory()
    {
        var failing = new Mock<IFileService>();
        failing.Setup(x => x.Read<UserSettings>(It.IsAny<string>(), It.IsAny<string>()))
               .Returns((UserSettings?)null);
        failing.Setup(x => x.Save(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserSettings>()))
               .Throws(new UnauthorizedAccessException("read-only"));

        var store = new SettingsStore(
            failing.Object, NullLogger<SettingsStore>.Instance, _folder, SettingsFileName);

        store.Update(s => s with { Theme = "Dark" });

        Assert.AreEqual("Dark", store.Current.Theme, "the application should keep running with the new value");
    }

    [TestMethod]
    public void IsComplete_RequiresFolderAndCredentials()
    {
        var settings = new UserSettings();
        Assert.IsFalse(settings.IsComplete);

        settings = settings with { WatchedFolder = @"D:\Wahoo" };
        Assert.IsFalse(settings.IsComplete);

        settings = settings with { GarminLogin = "rider@example.com", GarminPassword = "secret" };
        Assert.IsTrue(settings.IsComplete);
    }
}
