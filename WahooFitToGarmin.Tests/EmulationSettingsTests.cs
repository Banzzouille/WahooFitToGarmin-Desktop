using System.Text;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WahooFitToGarmin_Desktop.Core.DeviceEmulation;
using WahooFitToGarmin_Desktop.Core.Services;
using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin.Tests;

[TestClass]
public sealed class EmulationSettingsTests
{
    private const string SettingsFileName = "Settings.json";

    private string _folder = null!;

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "wftg-emulation", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private SettingsStore Store() =>
        new(new FileService(), NullLogger<SettingsStore>.Instance, _folder, SettingsFileName);

    [TestMethod]
    public void EmulationIsOffOnAFreshInstallation()
    {
        var settings = new EmulationSettings(Store());

        Assert.IsFalse(settings.IsEnabled);
        Assert.IsNull(settings.SelectedDevice);
    }

    [TestMethod]
    public void ASettingsFileWrittenBeforeTheFeatureExistedYieldsItDisabled()
    {
        // Version 1.1.0's format, which knows nothing about emulation. Somebody
        // upgrading must not have their uploads silently change.
        const string legacy =
            """{"WahooDropBoxFolder":"D:\\Dropbox\\Wahoo","KeepUploadedActivityFile":true,"Theme":"Light"}""";

        File.WriteAllText(
            Path.Combine(_folder, LegacySettingsMigration.LegacyFileName),
            Convert.ToBase64String(Encoding.UTF8.GetBytes(legacy)),
            Encoding.UTF8);

        var settings = new EmulationSettings(Store());

        Assert.IsFalse(settings.IsEnabled);
        Assert.IsNull(settings.SelectedDevice);
    }

    [TestMethod]
    public void ASelectedDeviceIsPersistedAndResolvedOnRestart()
    {
        var store = Store();
        store.Update(s => s with { EmulateDevice = true, EmulatedDeviceId = "edge-1040" });

        var reopened = new EmulationSettings(Store());

        Assert.IsTrue(reopened.IsEnabled);
        Assert.AreEqual("Garmin Edge 1040", reopened.SelectedDevice!.DisplayName);
    }

    [TestMethod]
    public void AnUnknownDeviceIdentifierResolvesToNoDeviceRatherThanThrowing()
    {
        // A catalogue entry removed by a later version, or a hand-edited file.
        var store = Store();
        store.Update(s => s with { EmulateDevice = true, EmulatedDeviceId = "edge-9999" });

        var settings = new EmulationSettings(Store());

        Assert.IsTrue(settings.IsEnabled);
        Assert.IsNull(settings.SelectedDevice, "an unknown identifier should resolve to nothing");
    }

    [TestMethod]
    public void ChangingTheSelectionTakesEffectWithoutARestart()
    {
        var store = Store();
        var settings = new EmulationSettings(store);

        store.Update(s => s with { EmulateDevice = true, EmulatedDeviceId = "fenix-7" });
        Assert.AreEqual("Garmin Fenix 7", settings.SelectedDevice!.DisplayName);

        store.Update(s => s with { EmulatedDeviceId = "edge-1050" });
        Assert.AreEqual("Garmin Edge 1050", settings.SelectedDevice!.DisplayName,
            "the setting was read once and cached");
    }

    [TestMethod]
    public void TurningEmulationOffLeavesTheSelectionAlone()
    {
        var store = Store();
        store.Update(s => s with { EmulateDevice = true, EmulatedDeviceId = "edge-1040" });
        store.Update(s => s with { EmulateDevice = false });

        var settings = new EmulationSettings(store);

        Assert.IsFalse(settings.IsEnabled);
        Assert.IsNotNull(settings.SelectedDevice, "the choice should survive being switched off");
    }
}
