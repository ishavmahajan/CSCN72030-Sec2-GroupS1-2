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

        string devicesJson = await File.ReadAllTextAsync(devicesPath, stoppingToken);
        string simulationJson = await File.ReadAllTextAsync(simulationPath, stoppingToken);

        simulator.Load(devicesJson, simulationJson, messageBus);
        await simulator.Start(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is shutting down.
        }
        finally
        {
            simulator.Stop();
        }
    }
}
