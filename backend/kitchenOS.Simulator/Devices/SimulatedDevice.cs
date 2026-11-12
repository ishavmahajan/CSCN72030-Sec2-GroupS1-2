using System.Text.Json;
using kitchenOS_Scada.Contract.Enums;

namespace kitchenOS_Scada.Api.Simulator.Devices;

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
public abstract class SimulatedDevice : ISimulatedDevice
{
    protected JsonElement _json;

    public string DeviceName { get; set; }
    public string Description { get; private set; }
    public virtual int RefreshRate { get; } = 50000;


    public SimulatedDevice(DeviceTypes deviceType)
    {
        DeviceType = deviceType;
        DeviceName = "N/A";
        Description = "No description available";
    }

    abstract public void refreshFromJson();
    public string DeviceId { get; set; }
    public DeviceTypes DeviceType { get; }

    public virtual void UpdateProperties(Random random)
    {
    }

    public virtual ISimulatedDevice setJson(JsonElement json)
    {
        _json = json;
        return this;
    }

    public virtual List<PointReading> ReadPoints()
    {
        return new List<PointReading>();
    }

    public virtual bool TrySetValue<T>(string name, T value, string? component, out string? reason)
    {
        reason = "unknown property";
        return false;
    }

    public virtual bool StartComponent(string? component)
    {
        return false;
    }

    public virtual bool StopComponent(string? component)
    {
        return false;
    }

    public virtual bool TrySetMode(string mode, string? component, out string? reason)
    {
        reason = "setMode is not accepted";
        return false;
    }

    public ISimulatedDevice setDeviceId(string deviceId)
    {
        DeviceId = deviceId;
        return this;
    }

    public static SimulatedDevice Create(string deviceType, JsonElement json)
    {
        if (deviceType == "WALK_IN_FREEZER") return WalkInFreezer.FromJson(json);
        if (deviceType == "STOVE") return Stove.FromJson(json);
        if (deviceType == "OVEN") return Oven.FromJson(json);
        if (deviceType == "MIXER") return Mixer.FromJson(json);
        if (deviceType == "EXHAUST_FAN") return ExhaustFan.FromJson(json);
        if (deviceType == "BLENDER") return Blender.FromJson(json);
        if (deviceType == "DISHWASHER") return Dishwasher.FromJson(json);
        if (deviceType == "SINK") return Sink.FromJson(json);

        throw new ArgumentException($"Unknown device type: {deviceType}");
    }

    public SimulatedDevice setDeviceName(string deviceName)
    {
        DeviceName = deviceName;
        return this;
    }

    public SimulatedDevice setDescription(string description)
    {
        Description = description;
        return this;
    }
    

}

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
