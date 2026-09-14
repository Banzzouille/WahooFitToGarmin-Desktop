using Dynastream.Fit;

namespace WahooFitToGarmin_Desktop.Core.DeviceEmulation
{
    /// <summary>What kind of device is being emulated.</summary>
    public enum EmulatedDeviceKind
    {
        Watch,

        BikeComputer,
    }

    /// <summary>
    /// One device the application can present an activity as coming from.
    /// </summary>
    /// <param name="Id">Stable key used in stored settings. Never the display
    /// name, which is free to change.</param>
    /// <param name="DisplayName">What the user picks from.</param>
    /// <param name="ProductId">The FIT product identifier.</param>
    /// <param name="SoftwareVersion">Written into the file creator message.</param>
    /// <param name="HardwareVersion">Written into the file creator message.</param>
    /// <param name="Kind">Watch or bike computer.</param>
    public sealed record EmulatedDevice(
        string Id,
        string DisplayName,
        ushort ProductId,
        ushort SoftwareVersion,
        byte HardwareVersion,
        EmulatedDeviceKind Kind);

    /// <summary>
    /// The devices that can be emulated.
    /// </summary>
    /// <remarks>
    /// Data, not code branches: adding a device is adding a row.
    ///
    /// Product identifiers reference the FIT specification's own product
    /// enumeration by name and are never transcribed as numeric literals, so a
    /// mistake is a compilation error rather than the wrong device appearing in
    /// the service.
    ///
    /// The software and hardware versions are cosmetic. A real device writes
    /// them, so the file carries plausible values, but they have no known effect
    /// on how the service processes an activity. Nobody should invest effort in
    /// tracking real firmware releases here.
    /// </remarks>
    public static class DeviceCatalogue
    {
        public static IReadOnlyList<EmulatedDevice> All { get; } =
        [
            new("fenix-7", "Garmin Fenix 7", GarminProduct.Fenix7, 1600, 0, EmulatedDeviceKind.Watch),
            new("fenix-7s", "Garmin Fenix 7S", GarminProduct.Fenix7s, 1600, 0, EmulatedDeviceKind.Watch),
            new("fenix-7x", "Garmin Fenix 7X", GarminProduct.Fenix7x, 1600, 0, EmulatedDeviceKind.Watch),
            new("fenix-7-pro-solar", "Garmin Fenix 7 Pro Solar", GarminProduct.Fenix7ProSolar, 1600, 0, EmulatedDeviceKind.Watch),
            new("fenix-8", "Garmin Fenix 8", GarminProduct.Fenix8, 1300, 0, EmulatedDeviceKind.Watch),
            new("fr-965", "Garmin Forerunner 965", GarminProduct.Fr965, 1500, 0, EmulatedDeviceKind.Watch),
            new("edge-1040", "Garmin Edge 1040", GarminProduct.Edge1040, 1400, 0, EmulatedDeviceKind.BikeComputer),
            new("edge-1050", "Garmin Edge 1050", GarminProduct.Edge1050, 1200, 0, EmulatedDeviceKind.BikeComputer),
        ];

        /// <summary>
        /// Resolves a stored identifier, or null when it names nothing — a
        /// catalogue entry removed in a later version, for instance.
        /// </summary>
        public static EmulatedDevice? Find(string? id) =>
            string.IsNullOrWhiteSpace(id)
                ? null
                : All.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.Ordinal));
    }
}
