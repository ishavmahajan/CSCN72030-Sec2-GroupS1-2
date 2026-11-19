using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS.Simulator.Bus;
using StackExchange.Redis;
using kitchenOS.Simulator;
Console.WriteLine("Starting kitchenOS Simulator...");

var builder = Host.CreateApplicationBuilder(args);

// Resolve the Redis endpoint from configuration, with a local default for
// development and standalone simulator runs.
string redisConnection =
    builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

// Share one Redis connection and message-bus implementation with the worker.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddSingleton<RedisMessageBus>();
builder.Services.AddSingleton<IMessageBus>(services =>
    services.GetRequiredService<RedisMessageBus>());

// The worker owns the simulator lifecycle and publishes its device events.
builder.Services.AddSingleton<DeviceSimulator>();
builder.Services.AddHostedService<SimulatorWorker>();

// Build and run until the host receives a shutdown request.
await builder.Build().RunAsync();

