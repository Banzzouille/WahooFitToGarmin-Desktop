using Dynastream.Fit;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using WahooFitToGarmin_Desktop.Core.DeviceEmulation;

namespace WahooFitToGarmin.Tests;

[TestClass]
public sealed class DeviceCatalogueTests
{
    [TestMethod]
    public void EveryRowResolvesToAProductIdentifier()
    {
        foreach (var device in DeviceCatalogue.All)
        {
            Assert.AreNotEqual(0, device.ProductId, $"{device.DisplayName} has no product identifier");
            Assert.IsFalse(string.IsNullOrWhiteSpace(device.Id), "a row has no stable identifier");
            Assert.IsFalse(string.IsNullOrWhiteSpace(device.DisplayName), $"{device.Id} has no display name");
        }
    }

    [TestMethod]
    public void IdentifiersAreUnique()
    {
        var ids = DeviceCatalogue.All.Select(d => d.Id).ToList();

        CollectionAssert.AreEquivalent(ids, ids.Distinct().ToList(), "two rows share a stable identifier");
    }

    [TestMethod]
    public void ProductIdentifiersMatchTheSpecification()
    {
        // Guards against a row being transcribed by hand rather than referencing
        // the specification's own enumeration.
        Assert.AreEqual(GarminProduct.Fenix7, DeviceCatalogue.Find("fenix-7")!.ProductId);
        Assert.AreEqual(GarminProduct.Edge1040, DeviceCatalogue.Find("edge-1040")!.ProductId);
        Assert.AreEqual(GarminProduct.Fr965, DeviceCatalogue.Find("fr-965")!.ProductId);
    }

    [TestMethod]
    public void TheCatalogueCoversTheAnnouncedDevices()
    {
        string[] expected =
        [
            "fenix-7", "fenix-7s", "fenix-7x", "fenix-7-pro-solar",
            "fenix-8", "fr-965", "edge-1040", "edge-1050",
        ];

        foreach (var id in expected)
        {
            Assert.IsNotNull(DeviceCatalogue.Find(id), $"{id} is missing from the catalogue");
        }
    }

    [TestMethod]
    public void AnUnknownIdentifierResolvesToNothing()
    {
        // A stored setting naming a device a later version removed must not
        // throw; emulation simply does not apply.
        Assert.IsNull(DeviceCatalogue.Find("edge-9999"));
        Assert.IsNull(DeviceCatalogue.Find(null));
        Assert.IsNull(DeviceCatalogue.Find("  "));
    }

    [TestMethod]
    public void BikeComputersAndWatchesAreBothRepresented()
    {
        Assert.IsTrue(DeviceCatalogue.All.Any(d => d.Kind == EmulatedDeviceKind.Watch));
        Assert.IsTrue(DeviceCatalogue.All.Any(d => d.Kind == EmulatedDeviceKind.BikeComputer));
    }
}
