using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using kitchenOS_Scada.Contract.Infrastructure.Bus;
using kitchenOS.Simulator.Bus;
using StackExchange.Redis;
using kitchenOS.Simulator;

var builder = Host.CreateApplicationBuilder(args);

string redisConnection =
    builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnection));

builder.Services.AddSingleton<RedisMessageBus>();
builder.Services.AddSingleton<IMessageBus>(services =>
    services.GetRequiredService<RedisMessageBus>());
builder.Services.AddSingleton<DeviceSimulator>();
builder.Services.AddHostedService<SimulatorWorker>();

await builder.Build().RunAsync();

