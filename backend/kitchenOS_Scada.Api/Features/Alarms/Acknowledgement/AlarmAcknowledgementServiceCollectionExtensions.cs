namespace kitchenOS_Scada.Api.Features.Alarms.Acknowledgement;

public static class AlarmAcknowledgementServiceCollectionExtensions
{
    public static IServiceCollection AddAlarmAcknowledgement(this IServiceCollection services)
    {
        services.AddSingleton<IAlarmAcknowledgementService, AlarmAcknowledgementService>();
        return services;
    }
}
