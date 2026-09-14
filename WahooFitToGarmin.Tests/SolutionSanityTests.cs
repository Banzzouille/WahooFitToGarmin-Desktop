using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using WahooFitToGarmin_Desktop.Core.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Services;

namespace WahooFitToGarmin.Tests;

// Placeholder for task 4.4: proves the test project runs, that the core library
// is reachable from a platform-neutral target, and that mocking works. Replaced
// by the real FileService tests in task group 8.
[TestClass]
public class SolutionSanityTests
{
    [TestMethod]
    public void CoreLibraryIsReachable()
    {
        IFileService service = new FileService();

        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void CoreAbstractionsCanBeMocked()
    {
        var mock = new Mock<IFileService>();
        mock.Setup(x => x.Read<string>("folder", "file")).Returns("value");

        var result = mock.Object.Read<string>("folder", "file");

        Assert.AreEqual("value", result);
        mock.Verify(x => x.Read<string>("folder", "file"), Times.Once);
    }
}
