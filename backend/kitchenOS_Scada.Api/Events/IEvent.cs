using kitchenOS_Scada.Api.Messaging;

namespace kitchenOS_Scada.Api.Events;

public interface IEvent
{

    string EventName { get; }
    string DeviceId { get; set; }
    IMessage Message { get; set; }
    string Timestamp { get; set; }

}