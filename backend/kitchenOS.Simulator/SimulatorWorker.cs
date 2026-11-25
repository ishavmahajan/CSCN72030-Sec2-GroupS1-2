using Microsoft.Extensions.Hosting;
using kitchenOS_Scada.Contract.Infrastructure.Bus;

namespace kitchenOS.Simulator;

internal sealed class SimulatorWorker(
    DeviceSimulator simulator,
    IMessageBus messageBus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string devicesPath = Path.Combine(
            AppContext.BaseDirectory,
            "SimulatorFiles",
            "devices.json");
        string simulationPath = Path.Combine(
            AppContext.BaseDirectory,
            "SimulatorFiles",
            "simulation.json");
        string alarmsPath = Path.Combine(AppContext.BaseDirectory, "SimulatorFiles", "alarm.json");

        string devicesJson = await File.ReadAllTextAsync(devicesPath, stoppingToken);
        string simulationJson = await File.ReadAllTextAsync(simulationPath, stoppingToken);
        string alarmsJson = await File.ReadAllTextAsync(alarmsPath, stoppingToken);

        simulator.Load(devicesJson, simulationJson, messageBus);
        simulator.LoadAlarms(alarmsJson);
        await simulator.Start(stoppingToken);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is shutting down.
        }
        finally
        {
            await simulator.StopAsync();
        }
    }
}
