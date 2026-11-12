using kitchenOS_Scada.Contract.Enums;
using kitchenOS_Scada.Contract.Messaging;

namespace kitchenOS_Scada.Contract.Events;

public class EventEnvelope : IEvent
{
    public string EventName { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public DeviceTypes DeviceType { get; set; }
    public string Timestamp { get; set; } = "";
    public IMessage Message { get; set; } = default!;
}

public class TelemetryBody : IMessage
{
    public DateTime Timestamp { get; set; }
    public List<TelemetryPoint> Values { get; set; } = [];
}

public class TelemetryPoint
{
    public string? Component { get; set; }
    public string Name { get; set; } = "";
    public object Value { get; set; } = "";
    public string? Unit { get; set; }
}

public class CommandResultBody : IMessage
{
    public DateTime Timestamp { get; set; }
    public string CommandId { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
}
