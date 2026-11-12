using System.Text.Json;

namespace kitchenOS_Scada.Contract.Messaging;

public class DeviceCommand
{
    public string CommandId { get; set; } = "";
    public string CommandName { get; set; } = "";
    public Guid DeviceId { get; set; } = Guid.Empty;
    public string Timestamp { get; set; } = "";
    public CommandArguments? Arguments { get; set; }
}

public class CommandArguments
{
    public string? Component { get; set; }
    public string? Name { get; set; }
    public JsonElement Value { get; set; }
    public string? Unit { get; set; }
    public string? Mode { get; set; }
}
