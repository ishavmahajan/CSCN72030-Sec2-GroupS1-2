namespace kitchenOS_Scada.Api.Commands;

// Common contract for commands identified by a stable wire-level name.
public interface ICommand
{
    string CommandName { get; }

}
