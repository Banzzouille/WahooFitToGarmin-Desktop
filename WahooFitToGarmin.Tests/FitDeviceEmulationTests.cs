using Dynastream.Fit;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using WahooFitToGarmin_Desktop.Core.DeviceEmulation;

using IoFile = System.IO.File;
using SysDateTime = System.DateTime;

namespace WahooFitToGarmin.Tests;

/// <summary>
/// Exercised against real Wahoo exports. The fixtures are an ELEMNT BOLT ride
/// and the same ride already converted to a Forerunner 910XT by another tool —
/// a conversion the service accepted and credited with an exercise load, which
/// is what this transformation reproduces.
/// </summary>
[TestClass]
public sealed class FitDeviceEmulationTests
{
    private const string WahooExport = "2022-06-21-193152-ELEMNT BOLT 9635-282-0.fit";
    private const string ReferenceConversion = "22 june 2022 fit file changed to xt910.fit";

    private static string FixturePath(string name)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Wahoo")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory!.FullName, "Wahoo", name);
    }

    private static byte[] Fixture(string name) => IoFile.ReadAllBytes(FixturePath(name));

    private static Mock<IEmulationSettings> Settings(string? deviceId = "edge-1040", bool enabled = true)
    {
        var mock = new Mock<IEmulationSettings>(MockBehavior.Strict);
        mock.SetupGet(x => x.IsEnabled).Returns(enabled);
        mock.SetupGet(x => x.SelectedDevice).Returns(DeviceCatalogue.Find(deviceId));
        return mock;
    }

    private static FitDeviceEmulation Create(Mock<IEmulationSettings> settings) =>
        new(settings.Object, NullLogger.Instance);

    // ------------------------------------------------------------- round trip

    [TestMethod]
    public void RoundTrip_PreservesTheMessageInventory()
    {
        var source = Fixture(WahooExport);

        var messages = FitCodec.Decode(source);
        var back = FitCodec.Decode(FitCodec.Encode(messages));

        Assert.AreEqual(messages.Count, back.Count, "messages were lost or gained");
        CollectionAssert.AreEqual(
            messages.GroupBy(m => m.Num).OrderBy(g => g.Key).Select(g => g.Count()).ToList(),
            back.GroupBy(m => m.Num).OrderBy(g => g.Key).Select(g => g.Count()).ToList(),
            "the mix of message types changed");
    }

    [TestMethod]
    public void RoundTrip_PreservesMessagesTheDecoderDoesNotRecognise()
    {
        // A real export carries hundreds of Wahoo's own message types. A decoder
        // that drops what it does not understand would discard them silently.
        var messages = FitCodec.Decode(Fixture(WahooExport));
        var unknownBefore = messages.Count(m => m.Name == "unknown");

        var back = FitCodec.Decode(FitCodec.Encode(messages));

        Assert.IsTrue(unknownBefore > 0, "the fixture no longer exercises this path");
        Assert.AreEqual(unknownBefore, back.Count(m => m.Name == "unknown"));
    }

    [TestMethod]
    public void RoundTrip_PreservesDeveloperFields()
    {
        var source = Fixture(WahooExport);
        var before = FitCodec.Decode(source).Sum(m => m.DeveloperFields.Count());

        var after = FitCodec.Decode(FitCodec.Encode(FitCodec.Decode(source)))
            .Sum(m => m.DeveloperFields.Count());

        Assert.IsTrue(before > 10_000, "the fixture no longer exercises this path");
        Assert.IsTrue(after >= before, $"developer fields were lost: {before} became {after}");
    }

    // --------------------------------------------------------------- identity

    [TestMethod]
    public void TheProducedFileReportsTheSelectedDevice()
    {
        var produced = Create(Settings("edge-1040")).Apply(Fixture(WahooExport), WahooExport);

        var fileId = new FileIdMesg(FitCodec.Decode(produced).First(m => m.Num == MesgNum.FileId));

        Assert.AreEqual(Manufacturer.Garmin, fileId.GetManufacturer());
        Assert.AreEqual(GarminProduct.Edge1040, fileId.GetProduct());
    }

    [TestMethod]
    public void EveryDeviceRecordReportsTheSelectedDevice()
    {
        var produced = Create(Settings("edge-1040")).Apply(Fixture(WahooExport), WahooExport);

        var records = FitCodec.Decode(produced)
            .Where(m => m.Num == MesgNum.DeviceInfo)
            .Select(m => new DeviceInfoMesg(m))
            .ToList();

        Assert.IsTrue(records.Count > 1, "the fixture no longer exercises this path");
        Assert.IsTrue(
            records.All(r => r.GetManufacturer() == Manufacturer.Garmin),
            "a device record still reports its original manufacturer");
    }

    [TestMethod]
    public void SerialNumbersAreTheOnesTheRecordingDeviceWrote()
    {
        var source = Fixture(WahooExport);
        var produced = Create(Settings("edge-1040")).Apply(source, WahooExport);

        static List<uint?> Serials(byte[] content) => FitCodec.Decode(content)
            .Where(m => m.Num == MesgNum.DeviceInfo)
            .Select(m => new DeviceInfoMesg(m).GetSerialNumber())
            .ToList();

        CollectionAssert.AreEqual(Serials(source), Serials(produced), "a serial number was rewritten");
    }

    [TestMethod]
    public void SensorsKeepWhatIdentifiesThem()
    {
        var source = Fixture(WahooExport);
        var produced = Create(Settings("edge-1040")).Apply(source, WahooExport);

        static List<(byte? Index, byte? Type, byte? Source)> Sensors(byte[] content) =>
            FitCodec.Decode(content)
                .Where(m => m.Num == MesgNum.DeviceInfo)
                .Select(m => new DeviceInfoMesg(m))
                .Select(d => (d.GetDeviceIndex(), d.GetDeviceType(), (byte?)d.GetSourceType()))
                .ToList();

        CollectionAssert.AreEqual(Sensors(source), Sensors(produced),
            "a device index, device type or source type changed");
    }

    [TestMethod]
    public void TheActivityTimestampIsUntouched()
    {
        var source = Fixture(WahooExport);
        var produced = Create(Settings("edge-1040")).Apply(source, WahooExport);

        static SysDateTime? Created(byte[] content) =>
            new FileIdMesg(FitCodec.Decode(content).First(m => m.Num == MesgNum.FileId))
                .GetTimeCreated()?.GetDateTime();

        Assert.AreEqual(Created(source), Created(produced), "the activity was moved in time");
    }

    [TestMethod]
    public void NoCreatorRecordIsInvented()
    {
        // The conversion the service credited carried none. Adding one would be
        // inventing a requirement.
        var source = Fixture(WahooExport);
        Assert.AreEqual(0, FitCodec.Decode(source).Count(m => m.Num == MesgNum.FileCreator),
            "the fixture no longer exercises this path");

        var produced = Create(Settings("edge-1040")).Apply(source, WahooExport);

        Assert.AreEqual(0, FitCodec.Decode(produced).Count(m => m.Num == MesgNum.FileCreator));
    }

    // ------------------------------------------------------------- preserving

    [TestMethod]
    public void TheRideItselfSurvivesTheTransformation()
    {
        var source = Fixture(WahooExport);
        var produced = Create(Settings("edge-1040")).Apply(source, WahooExport);

        var before = FitCodec.Decode(source);
        var after = FitCodec.Decode(produced);

        Assert.AreEqual(before.Count, after.Count);
        Assert.AreEqual(
            before.Count(m => m.Num == MesgNum.Record),
            after.Count(m => m.Num == MesgNum.Record),
            "data records were lost");
        Assert.AreEqual(
            before.Count(m => m.Name == "unknown"),
            after.Count(m => m.Name == "unknown"),
            "unrecognised messages were lost");
    }

    [TestMethod]
    public void TheSourceContentIsNotModified()
    {
        var source = Fixture(WahooExport);
        var copy = source.ToArray();

        Create(Settings("edge-1040")).Apply(source, WahooExport);

        CollectionAssert.AreEqual(copy, source, "the transformation mutated its input");
    }

    // ----------------------------------------------------------- pass-through

    [TestMethod]
    public void DisabledEmulationReturnsTheOriginalBytes()
    {
        var source = Fixture(WahooExport);

        var produced = Create(Settings(enabled: false)).Apply(source, WahooExport);

        Assert.AreSame(source, produced);
    }

    [TestMethod]
    public void AnUnknownSelectedDeviceReturnsTheOriginalBytes()
    {
        var source = Fixture(WahooExport);

        var produced = Create(Settings("edge-9999")).Apply(source, WahooExport);

        Assert.AreSame(source, produced);
    }

    // ------------------------------------------------------------- comparison

    [TestMethod]
    public void OurOutputMatchesTheShapeOfTheConversionTheServiceAccepted()
    {
        // The reference file was uploaded and credited with an exercise load.
        // Ours should differ from it only in which device is named.
        var ours = Create(Settings("edge-1040")).Apply(Fixture(WahooExport), WahooExport);

        var reference = FitCodec.Decode(Fixture(ReferenceConversion));
        var produced = FitCodec.Decode(ours);

        Assert.AreEqual(reference.Count, produced.Count, "message count differs from the accepted conversion");

        var referenceFileId = new FileIdMesg(reference.First(m => m.Num == MesgNum.FileId));
        var producedFileId = new FileIdMesg(produced.First(m => m.Num == MesgNum.FileId));

        Assert.AreEqual(referenceFileId.GetManufacturer(), producedFileId.GetManufacturer());
        Assert.AreEqual(referenceFileId.GetSerialNumber(), producedFileId.GetSerialNumber(),
            "the accepted conversion kept the original serial number and so should we");
        Assert.AreEqual(
            referenceFileId.GetTimeCreated()?.GetDateTime(),
            producedFileId.GetTimeCreated()?.GetDateTime());

        Assert.AreEqual(
            reference.Count(m => m.Num == MesgNum.DeviceInfo),
            produced.Count(m => m.Num == MesgNum.DeviceInfo));
    }
}
