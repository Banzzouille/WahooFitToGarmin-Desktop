using Dynastream.Fit;

using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Core.Activities;

using IoFile = System.IO.File;

namespace WahooFitToGarmin_Desktop.Core.DeviceEmulation
{
    /// <summary>
    /// Presents an activity as recorded by a chosen Garmin device.
    /// </summary>
    /// <remarks>
    /// Only the manufacturer and the product change, in the file identification
    /// message and in every device information record. Everything else is passed
    /// through: serial numbers, timestamps, device indexes, device types, source
    /// types, records, laps, sessions, and the messages the decoder does not
    /// recognise — of which a real export carries hundreds.
    ///
    /// That shape is not invented. It reproduces a conversion of a real ride that
    /// the service accepted and credited with an exercise load. Two earlier
    /// design decisions said otherwise, and the file disproved them: it kept the
    /// recording unit's own serial number, and it rewrote every device record
    /// rather than only the head unit's.
    /// </remarks>
    public sealed class FitDeviceEmulation : IActivityTransformation
    {
        private readonly IEmulationSettings _settings;
        private readonly ILogger _logger;

        public FitDeviceEmulation(IEmulationSettings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public byte[] Apply(byte[] content, string fileName)
        {
            var device = _settings.SelectedDevice;

            if (!_settings.IsEnabled)
            {
                return content;
            }

            if (device is null)
            {
                // A stored selection naming a device this version no longer has.
                // Uploading unchanged is better than failing the activity.
                _logger.LogWarning(
                    "Device emulation is enabled but the selected device is unknown; {FileName} is uploaded unchanged",
                    fileName);

                return content;
            }

            List<Mesg> messages;
            try
            {
                messages = FitCodec.Decode(content);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"{fileName} could not be read as a FIT file", ex);
            }

            if (!IsActivity(messages))
            {
                _logger.LogInformation(
                    "{FileName} is not an activity; device emulation does not apply and it is uploaded unchanged",
                    fileName);

                return content;
            }

            var rewritten = 0;

            foreach (var message in messages)
            {
                if (message.Num == MesgNum.FileId)
                {
                    ApplyToFileId(message, device);
                    rewritten++;
                }
                else if (message.Num == MesgNum.DeviceInfo)
                {
                    ApplyToDeviceInfo(message, device);
                    rewritten++;
                }
            }

            var produced = FitCodec.Encode(messages);

            Verify(content, produced, device, fileName);

            _logger.LogInformation(
                "{FileName} presented as {Device}, {Count} identity records rewritten",
                fileName,
                device.DisplayName,
                rewritten);

            return produced;
        }

        private static bool IsActivity(IEnumerable<Mesg> messages)
        {
            var fileId = messages.FirstOrDefault(m => m.Num == MesgNum.FileId);

            return fileId is not null && new FileIdMesg(fileId).GetType() == Dynastream.Fit.File.Activity;
        }

        /// <remarks>
        /// The serial number and the creation timestamp are deliberately left
        /// alone. The timestamp is the activity's identity in the service, and
        /// the serial number is what the recording device wrote — the reference
        /// conversion kept it and the file was still credited.
        /// </remarks>
        private static void ApplyToFileId(Mesg message, EmulatedDevice device)
        {
            var fileId = new FileIdMesg(message);

            fileId.SetManufacturer(Manufacturer.Garmin);
            fileId.SetProduct(device.ProductId);
            fileId.SetGarminProduct(device.ProductId);

            CopyInto(message, fileId);
        }

        /// <remarks>
        /// Every record, sensors included. What identifies an individual sensor —
        /// its serial number, index, device type and source type — is untouched,
        /// so the file still says which power meter was present.
        /// </remarks>
        private static void ApplyToDeviceInfo(Mesg message, EmulatedDevice device)
        {
            var info = new DeviceInfoMesg(message);

            info.SetManufacturer(Manufacturer.Garmin);
            info.SetProduct(device.ProductId);
            info.SetGarminProduct(device.ProductId);

            CopyInto(message, info);
        }

        /// <summary>
        /// Copies the typed message's fields back onto the original, so that
        /// developer fields and anything the typed view does not model survive.
        /// </summary>
        private static void CopyInto(Mesg target, Mesg source)
        {
            foreach (var field in source.Fields)
            {
                target.SetField(field);
            }
        }

        /// <summary>
        /// Decodes what was produced and checks it before it goes anywhere.
        /// </summary>
        /// <remarks>
        /// Silent corruption is the characteristic failure of rewriting files, so
        /// this runs on every transformation rather than only in tests. The files
        /// are small enough that the cost does not matter.
        /// </remarks>
        private static void Verify(byte[] source, byte[] produced, EmulatedDevice device, string fileName)
        {
            List<Mesg> before, after;

            try
            {
                before = FitCodec.Decode(source);
                after = FitCodec.Decode(produced);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"the file produced for {fileName} could not be read back", ex);
            }

            if (before.Count != after.Count)
            {
                throw new InvalidOperationException(
                    $"the file produced for {fileName} has {after.Count} messages where the source had {before.Count}");
            }

            var lostUnknown = before.Count(m => m.Name == "unknown") - after.Count(m => m.Name == "unknown");
            if (lostUnknown != 0)
            {
                throw new InvalidOperationException(
                    $"the file produced for {fileName} lost {lostUnknown} messages the decoder does not recognise");
            }

            var identity = after.FirstOrDefault(m => m.Num == MesgNum.FileId);
            if (identity is null || new FileIdMesg(identity).GetProduct() != device.ProductId)
            {
                throw new InvalidOperationException(
                    $"the file produced for {fileName} does not report {device.DisplayName}");
            }
        }
    }
}
