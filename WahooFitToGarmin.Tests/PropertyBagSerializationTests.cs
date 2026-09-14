using System.Collections;
using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using WahooFitToGarmin_Desktop.Core.Helpers;

namespace WahooFitToGarmin.Tests;

/// <summary>
/// Covers task 8.9: content held in the application's non-generic property bag
/// must survive serialization, which it would not if it were handed to
/// <c>System.Text.Json</c> directly.
/// </summary>
[TestClass]
public sealed class PropertyBagSerializationTests
{
    [TestMethod]
    public void Project_KeepsEveryEntryOfANonGenericBag()
    {
        // Hashtable stands in for App.Current.Properties, which is a
        // non-generic IDictionary.
        IDictionary bag = new Hashtable
        {
            ["WahooDropBoxFolder"] = @"C:\Dropbox\Wahoo",
            ["GarminLogin"] = "rider@example.com",
            ["GarminPwd"] = "secret",
            ["KeepUploadedActivityFile"] = true,
            ["Theme"] = "Dark",
        };

        var projected = PropertyBagSerialization.Project(bag);

        Assert.AreEqual(5, projected.Count, "entries were lost in the projection");
        Assert.AreEqual(@"C:\Dropbox\Wahoo", projected["WahooDropBoxFolder"]);
        Assert.AreEqual("rider@example.com", projected["GarminLogin"]);
        Assert.AreEqual("secret", projected["GarminPwd"]);
        Assert.AreEqual("Dark", projected["Theme"]);
    }

    [TestMethod]
    public void Project_RendersNonStringValuesAsStrings()
    {
        IDictionary bag = new Hashtable { ["KeepUploadedActivityFile"] = true };

        var projected = PropertyBagSerialization.Project(bag);

        Assert.IsTrue(bool.TryParse(projected["KeepUploadedActivityFile"], out var parsed),
            "the stored value is not parseable as the boolean the application reads back");
        Assert.IsTrue(parsed);
    }

    [TestMethod]
    public void Project_SurvivesNullValues()
    {
        IDictionary bag = new Hashtable { ["WahooDropBoxFolder"] = null };

        var projected = PropertyBagSerialization.Project(bag);

        Assert.AreEqual(string.Empty, projected["WahooDropBoxFolder"]);
    }

    [TestMethod]
    public void ProjectThenSerialize_ProducesAReadableDocument()
    {
        IDictionary bag = new Hashtable
        {
            ["GarminLogin"] = "rider@example.com",
            ["KeepUploadedActivityFile"] = false,
        };

        var json = JsonSerializer.Serialize(PropertyBagSerialization.Project(bag));

        Assert.IsTrue(json.Contains("GarminLogin"), "the serialized document lost its keys");
        Assert.IsTrue(json.Contains("rider@example.com"), "the serialized document lost its values");
    }

    [TestMethod]
    public void SerializingTheBagDirectly_Works_ButYieldsUntypedValuesOnTheWayBack()
    {
        // Records why the normalisation exists, which is not what the design
        // originally claimed. System.Text.Json handles the non-generic bag in
        // both directions on this runtime — an earlier version of this test
        // asserted the opposite and failed, which is how that was found.
        //
        // What it does not do is give back what was put in: values return as
        // JsonElement, so a bag restored from disk holds a different value type
        // than the same bag after the settings page writes to it. Normalising
        // to strings removes that split.
        IDictionary bag = new Hashtable { ["GarminLogin"] = "rider@example.com" };

        var json = JsonSerializer.Serialize(bag);
        Assert.IsTrue(json.Contains("rider@example.com"), "the bag did not serialize");

        var restored = JsonSerializer.Deserialize<IDictionary>(json);
        Assert.IsNotNull(restored);

        var value = restored["GarminLogin"];
        Assert.IsInstanceOfType<JsonElement>(value,
            "values now round-trip as their original type; the normalisation could be revisited");
    }

    [TestMethod]
    public void Flatten_ReadsAStringValue()
    {
        var element = JsonDocument.Parse("""{"v":"Dark"}""").RootElement.GetProperty("v");

        Assert.AreEqual("Dark", PropertyBagSerialization.Flatten(element));
    }

    [TestMethod]
    public void Flatten_ReadsABooleanWrittenByThePreviousVersion()
    {
        // Version 1.1.0 wrote this key unquoted; reading it as a string fails.
        var element = JsonDocument.Parse("""{"v":true}""").RootElement.GetProperty("v");

        var flattened = PropertyBagSerialization.Flatten(element);

        Assert.IsTrue(bool.TryParse(flattened, out var parsed), $"'{flattened}' is not parseable as a boolean");
        Assert.IsTrue(parsed);
    }

    [TestMethod]
    public void Flatten_TurnsNullIntoAnEmptyString()
    {
        var element = JsonDocument.Parse("""{"v":null}""").RootElement.GetProperty("v");

        Assert.AreEqual(string.Empty, PropertyBagSerialization.Flatten(element));
    }

    [TestMethod]
    public void RoundTrip_FromLegacyBagToRestoredValues()
    {
        // The full path a settings file takes across the upgrade: written by the
        // previous version with an unquoted boolean, read back through Flatten.
        const string legacyJson =
            """{"WahooDropBoxFolder":"D:\\Dropbox\\Wahoo","KeepUploadedActivityFile":true,"Theme":"Light"}""";

        var stored = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(legacyJson);
        Assert.IsNotNull(stored);

        var restored = stored.ToDictionary(x => x.Key, x => PropertyBagSerialization.Flatten(x.Value));

        Assert.AreEqual(@"D:\Dropbox\Wahoo", restored["WahooDropBoxFolder"]);
        Assert.AreEqual("Light", restored["Theme"]);
        Assert.IsTrue(bool.TryParse(restored["KeepUploadedActivityFile"], out var keep) && keep);
    }
}
