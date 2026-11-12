using kitchenOS_Scada.Contract.Enums;
using kitchenOS_Scada.Contract.Messaging;

namespace kitchenOS_Scada.Contract.Events;

public interface IEvent
{
    string EventName { get; }
    string DeviceId { get; set; }
    DeviceTypes DeviceType { get; set; }
    IMessage Message { get; set; }
    string Timestamp { get; set; }
}
