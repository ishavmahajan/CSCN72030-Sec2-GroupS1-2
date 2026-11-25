using System.Text.Json;
using kitchenOS_Scada.Contract.Enums;
namespace kitchenOS.Simulator.Devices;

// Defines the operations required by every simulated kitchen device.
public interface ISimulatedDevice
{
    string DeviceId { get; }
    DeviceTypes DeviceType { get; }
    string Description { get; }


    void UpdateProperties(Random random);
    ISimulatedDevice setJson(JsonElement json);
    ISimulatedDevice setDeviceId(string deviceId);
    List<PointReading> ReadPoints();
    bool TrySetValue<T>(string name, T value, string? component, out string? reason);
    bool StartComponent(string? component);
    bool StopComponent(string? component);
    bool TrySetMode(string mode, string? component, out string? reason);
    abstract public void refreshFromJson();


}

// Snapshot of one device point suitable for API and telemetry projection.
public class PointReading
{
    public PointReading(string name, object value, string? unit, string? component)
    {
        Name = name;
        Value = value;
        Unit = unit;
        Component = component;
    }

    public string Name { get; }
    public object Value { get; }
    public string? Unit { get; }
    public string? Component { get; }
}
