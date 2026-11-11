using Microsoft.VisualStudio.TestTools.UnitTesting;
using kitchenOS_Scada.Api.Simulator;

namespace kitchenOS_Scada.Api.Tests.Simulator;

/// <summary>
/// Verifies lifecycle behavior for the device simulator.
/// </summary>
[TestClass]
public class DeviceSimulatorLifecycleTests
{
    [TestMethod]
    public void Start_WithoutLoad_Completes()
    {
        var simulator = new DeviceSimulator();

        simulator.Start().GetAwaiter().GetResult();
    }
}
