namespace WahooFitToGarmin_Desktop.Core.DeviceEmulation
{
    /// <summary>
    /// What the transformation needs to know, without depending on where the
    /// settings live.
    /// </summary>
    public interface IEmulationSettings
    {
        bool IsEnabled { get; }

        /// <summary>
        /// The device to present, or null when the stored selection names
        /// nothing this version knows.
        /// </summary>
        EmulatedDevice? SelectedDevice { get; }
    }
}
