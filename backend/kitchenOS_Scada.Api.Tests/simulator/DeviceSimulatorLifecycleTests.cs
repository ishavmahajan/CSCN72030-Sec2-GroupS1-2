using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Api.Simulator;

namespace kitchenOS_Scada.Api.Tests.Simulator;

/// <summary>
/// Verifies lifecycle validation for the device simulator.
/// </summary>
[TestClass]
public class DeviceSimulatorLifecycleTests
{
    [TestMethod]
    public void Start_WithoutLoad_Throws()
    {
        var simulator = new DeviceSimulator();

        Assert.ThrowsException<InvalidOperationException>(() => simulator.Start());
    }
}
