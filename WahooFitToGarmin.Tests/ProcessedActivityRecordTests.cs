using System.Text;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using WahooFitToGarmin_Desktop.Core.Activities;

namespace WahooFitToGarmin.Tests;

[TestClass]
public sealed class ProcessedActivityRecordTests
{
    private string _folder = null!;
    private string _recordPath = null!;

    [TestInitialize]
    public void Setup()
    {
        _folder = Path.Combine(Path.GetTempPath(), "wftg-record", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_folder);
        _recordPath = Path.Combine(_folder, "processed.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    private ProcessedActivityRecord Create() =>
        new(_recordPath, NullLogger<ProcessedActivityRecord>.Instance);

    private static byte[] Content(string text) => Encoding.UTF8.GetBytes(text);

    [TestMethod]
    public void Hash_IsTheSameForTheSameContent_RegardlessOfName()
    {
        var first = ActivityHash.Compute(Content("ride"));
        var second = ActivityHash.Compute(Content("ride"));

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void Hash_DiffersForDifferentContent()
    {
        Assert.AreNotEqual(ActivityHash.Compute(Content("ride")), ActivityHash.Compute(Content("run")));
    }

    [TestMethod]
    public void SameContentUnderADifferentName_IsRecognisedAsAlreadyProcessed()
    {
        var record = Create();
        var hash = ActivityHash.Compute(Content("ride"));
        record.Mark(hash, "2026-09-14-073000.fit", ActivityOutcome.Uploaded);

        // The sync client re-downloads the same activity under another name.
        var sameContentAgain = ActivityHash.Compute(Content("ride"));

        Assert.IsTrue(record.Contains(sameContentAgain));
    }

    [TestMethod]
    public void Record_SurvivesARestart()
    {
        var hash = ActivityHash.Compute(Content("ride"));
        Create().Mark(hash, "ride.fit", ActivityOutcome.Uploaded);

        var reopened = Create();

        Assert.IsTrue(reopened.Contains(hash));
        Assert.IsFalse(reopened.IsFirstRun);
    }

    [TestMethod]
    public void FirstRun_IsDetectedByTheAbsenceOfTheRecord()
    {
        Assert.IsTrue(Create().IsFirstRun, "a machine with no record should report a first run");

        Create().Mark(ActivityHash.Compute(Content("ride")), "ride.fit", ActivityOutcome.Uploaded);

        Assert.IsFalse(Create().IsFirstRun);
    }

    [TestMethod]
    public void Baseline_RecordsManyEntriesWithoutUploadingThem()
    {
        var record = Create();
        var entries = Enumerable.Range(0, 50)
            .Select(i => (ActivityHash.Compute(Content($"ride-{i}")), $"ride-{i}.fit"))
            .ToList();

        record.MarkBaseline(entries);

        foreach (var (hash, _) in entries)
        {
            Assert.IsTrue(record.Contains(hash));
        }
    }

    [TestMethod]
    public void Record_StaysBounded()
    {
        var record = Create();

        // Well past the cap.
        for (var i = 0; i < 2500; i++)
        {
            record.Mark(ActivityHash.Compute(Content($"ride-{i}")), $"ride-{i}.fit", ActivityOutcome.Uploaded);
        }

        var stored = File.ReadAllText(_recordPath);
        var count = System.Text.Json.JsonDocument.Parse(stored).RootElement.GetArrayLength();

        Assert.IsTrue(count <= 2000, $"the record grew to {count} entries");
        // The most recent must still be there; the oldest are the ones dropped.
        Assert.IsTrue(record.Contains(ActivityHash.Compute(Content("ride-2499"))));
    }

    [TestMethod]
    public void UnreadableRecord_DoesNotThrow_AndReportsNothingAsProcessed()
    {
        File.WriteAllText(_recordPath, "{ this is not the record you are looking for");

        var record = Create();

        // Degrades to re-offering files, which the service reports as duplicates.
        Assert.IsFalse(record.Contains(ActivityHash.Compute(Content("ride"))));
    }
}
