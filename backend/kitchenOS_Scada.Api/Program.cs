using kitchenOS_Scada.Api.Infrastructure.Bus;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
builder.Services.AddSingleton<RedisMessageBus>();
builder.Services.AddSingleton<IMessageBus>(sp => sp.GetRequiredService<RedisMessageBus>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    ""
};

app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "Healthy" });
})
.WithName("GetHealth");

app.Run();


