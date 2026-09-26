using kitchenOS_Scada.Api.Messaging;
using kitchenOS_Scada.Api.Enums;

namespace kitchenOS_Scada.Api.Events;

public interface IEvent
{

    string EventName { get; }
    string DeviceId { get; set; }
    DeviceTypes DeviceType { get; set; }
    IMessage Message { get; set; }
    string Timestamp { get; set; }

}