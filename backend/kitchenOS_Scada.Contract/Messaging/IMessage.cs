using System.Text.Json.Serialization;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Contract.Messaging;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TelemetryBody), "telemetry")]
[JsonDerivedType(typeof(CommandResultBody), "command_result")]
public interface IMessage
{
    DateTime Timestamp { get; set; }
}
