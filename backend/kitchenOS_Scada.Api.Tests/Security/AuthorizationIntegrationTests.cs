using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using kitchenOS_Scada.Api.Features.Devices;
using kitchenOS_Scada.Api.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kitchenOS_Scada.Api.Tests.Security;

[TestClass]
public sealed class AuthorizationIntegrationTests
{
    private readonly TestIdentityProvider _identity = new();
    private WebApplication _app = default!;
    private HttpClient _client = default!;

    [TestInitialize]
    public async Task Initialize()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _identity.Configure(builder.Services);
        builder.Services.AddControllers(options => options.Filters.Add(new AuthorizedRouteFilter()))
            .AddApplicationPart(typeof(DevicesController).Assembly);
        _app = builder.Build();
        _app.UseWebSockets();
        _app.UseKitchenOsSecurity();
        _app.MapControllers();
        _app.MapGet("/telemetry", () => Results.NoContent())
            .RequireAuthorization(AuthorizationPolicies.Monitor);
        _app.MapPost("/alarms/test/acknowledge", () => Results.NoContent())
            .RequireAuthorization(AuthorizationPolicies.Operate);
        _app.MapGet("/health", () => Results.Ok()).AllowAnonymous();
        _app.MapGet("/future-endpoint", () => Results.NoContent());
        _app.Map("/ws", async context =>
        {
            using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            byte[] buffer = new byte[1];
            await socket.ReceiveAsync(buffer, context.RequestAborted);
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Done", context.RequestAborted);
        }).RequireAuthorization(AuthorizationPolicies.Monitor);
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.Single()) };
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        _client?.Dispose();
        if (_app is not null) await _app.DisposeAsync();
        _identity.Dispose();
    }

    [TestMethod]
    [DataRow("GET", "/devices")]
    [DataRow("GET", "/devices/MIX-001")]
    [DataRow("POST", "/devices/MIX-001/commands")]
    [DataRow("POST", "/devices/commands")]
    [DataRow("POST", "/devices")]
    [DataRow("DELETE", "/devices/MIX-001")]
    [DataRow("GET", "/telemetry")]
    [DataRow("POST", "/alarms/test/acknowledge")]
    [DataRow("GET", "/future-endpoint")]
    public async Task MissingCredentials_Return401(string method, string path)
    {
        using HttpResponseMessage response = await Send(method, path);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    [DataRow(DeviceRoles.Viewer, "GET", "/devices", 204)]
    [DataRow(DeviceRoles.Operator, "GET", "/devices/MIX-001", 204)]
    [DataRow(DeviceRoles.Administrator, "GET", "/devices", 204)]
    [DataRow(DeviceRoles.Viewer, "GET", "/telemetry", 204)]
    [DataRow(DeviceRoles.Viewer, "POST", "/devices/MIX-001/commands", 403)]
    [DataRow(DeviceRoles.Viewer, "POST", "/devices/commands", 403)]
    [DataRow(DeviceRoles.Operator, "POST", "/devices/MIX-001/commands", 204)]
    [DataRow(DeviceRoles.Administrator, "POST", "/devices/MIX-001/commands", 204)]
    [DataRow(DeviceRoles.Viewer, "POST", "/alarms/test/acknowledge", 403)]
    [DataRow(DeviceRoles.Operator, "POST", "/alarms/test/acknowledge", 204)]
    [DataRow(DeviceRoles.Administrator, "POST", "/alarms/test/acknowledge", 204)]
    [DataRow(DeviceRoles.Viewer, "POST", "/devices", 403)]
    [DataRow(DeviceRoles.Operator, "POST", "/devices", 403)]
    [DataRow(DeviceRoles.Administrator, "POST", "/devices", 204)]
    [DataRow(DeviceRoles.Operator, "DELETE", "/devices/MIX-001", 403)]
    [DataRow(DeviceRoles.Administrator, "DELETE", "/devices/MIX-001", 204)]
    [DataRow("Unknown", "GET", "/devices", 403)]
    public async Task Roles_AreEnforcedOnRoutes(string role, string method, string path, int expected)
    {
        using HttpResponseMessage response = await Send(method, path, _identity.Token(role));
        Assert.AreEqual(expected, (int)response.StatusCode);
    }

    [TestMethod]
    public async Task InvalidTokens_Return401()
    {
        using TestIdentityProvider otherProvider = new();
        string[] tokens = ["not-a-jwt", _identity.Token(expired: true),
            _identity.Token(issuer: "https://untrusted.test"),
            _identity.Token(audience: "another-api"), _identity.Token(includeSubject: false),
            otherProvider.Token()];
        foreach (string token in tokens)
        {
            using HttpResponseMessage response = await Send("GET", "/devices", token);
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [TestMethod]
    public async Task NoRole_Returns403AndQueryCredentialsDoNotAuthenticateHttp()
    {
        using HttpResponseMessage noRole = await Send("GET", "/devices", _identity.Token(null));
        Assert.AreEqual(HttpStatusCode.Forbidden, noRole.StatusCode);
        using HttpResponseMessage query = await Send("GET", "/devices?access_token=" + _identity.Token());
        Assert.AreEqual(HttpStatusCode.Unauthorized, query.StatusCode);
        using HttpResponseMessage health = await Send("GET", "/health");
        Assert.AreEqual(HttpStatusCode.OK, health.StatusCode);
    }

    [TestMethod]
    public async Task ValidIdentity_IsAvailableToRequestHandlers()
    {
        using HttpResponseMessage response = await Send("GET", "/devices", _identity.Token());
        Assert.AreEqual("test-operator-01", response.Headers.GetValues("X-Test-Subject").Single());
    }

    [TestMethod]
    [DataRow(DeviceRoles.Viewer)]
    [DataRow(DeviceRoles.Operator)]
    [DataRow(DeviceRoles.Administrator)]
    public async Task WebSocket_ValidRolesConnectUsingBrowserQueryToken(string role)
    {
        using ClientWebSocket socket = new();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await socket.ConnectAsync(SocketUri(_identity.Token(role)), timeout.Token);
        Assert.AreEqual(WebSocketState.Open, socket.State);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", timeout.Token);
    }

    [TestMethod]
    public async Task WebSocket_HeaderTokenConnects()
    {
        using ClientWebSocket socket = new();
        socket.Options.SetRequestHeader("Authorization", "Bearer " + _identity.Token());
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await socket.ConnectAsync(SocketUri(null), timeout.Token);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", timeout.Token);
    }

    [TestMethod]
    public async Task WebSocket_MissingInvalidExpiredAndWrongRoleAreRejected()
    {
        foreach ((string? token, HttpStatusCode expected) in new[]
        {
            ((string?)null, HttpStatusCode.Unauthorized),
            ("bad-token", HttpStatusCode.Unauthorized),
            (_identity.Token(expired: true), HttpStatusCode.Unauthorized),
            (_identity.Token("Unknown"), HttpStatusCode.Forbidden)
        })
        {
            using ClientWebSocket socket = new();
            socket.Options.CollectHttpResponseDetails = true;
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
            await Assert.ThrowsExactlyAsync<WebSocketException>(
                () => socket.ConnectAsync(SocketUri(token), timeout.Token));
            Assert.AreEqual(expected, socket.HttpStatusCode);
        }
    }

    private Task<HttpResponseMessage> Send(string method, string path, string? token = null)
    {
        HttpRequestMessage request = new(new HttpMethod(method), path);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }

    private Uri SocketUri(string? token) => new UriBuilder(_client.BaseAddress!)
    {
        Scheme = "ws", Path = "/ws",
        Query = token is null ? "" : "access_token=" + Uri.EscapeDataString(token)
    }.Uri;

    private sealed class AuthorizedRouteFilter : IResourceFilter
    {
        // Runs after the real MVC endpoint authorization but before unfinished
        // inventory actions and database dependencies. These are security tests.
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            context.HttpContext.Response.Headers["X-Test-Subject"] = context.HttpContext.User.FindFirst("sub")?.Value;
            context.Result = new NoContentResult();
        }
        public void OnResourceExecuted(ResourceExecutedContext context) { }
    }
}
