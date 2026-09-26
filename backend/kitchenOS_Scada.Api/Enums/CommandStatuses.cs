namespace kitchenOS_Scada.Api.Enums;

public enum CommandStatuses
{
   ACKNOWLEDGED,
   REJECTED
}

public static class CommandStatusesExtensions
{
    public static string GetCommandStatus(this CommandStatuses commandStatus)
    {
        return commandStatus switch
        {
            CommandStatuses.ACKNOWLEDGED => "acknowledged",
            CommandStatuses.REJECTED => "rejected",
            _ => throw new ArgumentOutOfRangeException(nameof(commandStatus), commandStatus, null)
        };
    }

    public static CommandStatuses GetCommandStatusEnum(string commandStatus)
    {
        return commandStatus switch
        {
            "acknowledged" => CommandStatuses.ACKNOWLEDGED,
            "rejected" => CommandStatuses.REJECTED,
            _ => throw new ArgumentOutOfRangeException(nameof(commandStatus), commandStatus, null)
        };
    }
}