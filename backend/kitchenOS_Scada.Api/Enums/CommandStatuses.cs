namespace kitchenOS_Scada.Api.Enums;

// Outcomes reported after a device command is processed.
public enum CommandStatuses
{
   ACKNOWLEDGED,
   REJECTED
}

public static class CommandStatusesExtensions
{
    // Keep API strings stable while allowing internal code to use an enum.
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
