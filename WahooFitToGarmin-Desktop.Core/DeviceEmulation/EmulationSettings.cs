using WahooFitToGarmin_Desktop.Core.Settings;

namespace WahooFitToGarmin_Desktop.Core.DeviceEmulation
{
    /// <summary>
    /// Reads the emulation choice out of the settings store.
    /// </summary>
    /// <remarks>
    /// Reads through on every access rather than caching, so a change on the
    /// settings page applies to the next upload without a restart, like every
    /// other setting.
    /// </remarks>
    public sealed class EmulationSettings : IEmulationSettings
    {
        private readonly ISettingsStore _settings;

        public EmulationSettings(ISettingsStore settings) => _settings = settings;

        public bool IsEnabled => _settings.Current.EmulateDevice;

        public EmulatedDevice? SelectedDevice => DeviceCatalogue.Find(_settings.Current.EmulatedDeviceId);
    }
}
