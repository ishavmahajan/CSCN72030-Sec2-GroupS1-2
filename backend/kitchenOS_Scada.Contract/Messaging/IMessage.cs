using System.Text.Json.Serialization;
using kitchenOS_Scada.Contract.Events;

namespace kitchenOS_Scada.Contract.Messaging;

// Base contract for polymorphic messages serialized on the shared bus.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TelemetryBody), "telemetry")]
[JsonDerivedType(typeof(CommandResultBody), "command_result")]
[JsonDerivedType(typeof(AlarmBody), "alarm")]
public interface IMessage
{
    DateTime Timestamp { get; set; }
}
