using kitchenOS_Scada.Api.Enums;
namespace kitchenOS_Scada.Api.Messaging;
public class TelemetryMessage: IMessage
{
    public string DeviceId { get; set; }
    public DateTime Timestamp { get; set; }
    public List<PointValues> Values { get; set; }
    public string? unit { get; set; }
}

public class PointValues
{
    public PointNames Name { get; set; }
    public string Component { get; set; }
    public TelemetryValue Value { get; set; }
    public string Unit { get; set; }
}


public class TelemetryValue
{
    public string? Text { get; set; }
    public double? Number { get; set; }
    public bool? Flag { get; set; }
}