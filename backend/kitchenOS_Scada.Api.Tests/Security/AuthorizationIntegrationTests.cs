using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using kitchenOS_Scada.Api.Features.Devices;
using kitchenOS_Scada.Api.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace kitchenOS_Scada.Api.Tests.Security;

[TestClass]
public sealed class AuthorizationIntegrationTests
{
    private readonly TestIdentityProvider _identity = new();
    private readonly CookieContainer _cookies = new();
    private WebApplication _app = default!;
    private HttpClient _client = default!;

    [TestInitialize]
    public async Task Initialize()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _identity.Configure(builder.Services);
        builder.Services.AddControllers(options => options.Filters.Add(new AuthorizedRouteFilter()))
            .AddApplicationPart(typeof(DevicesController).Assembly);
        _app = builder.Build();
        await _identity.InitializeAsync(_app.Services);
        _app.UseWebSockets();
        _app.UseKitchenOsSecurity();
        _app.MapKitchenOsIdentity();
        _app.MapControllers();
        _app.MapGet("/telemetry", () => Results.NoContent()).RequireAuthorization(AuthorizationPolicies.Monitor);
        _app.MapPost("/alarms/test/acknowledge", () => Results.NoContent()).RequireAuthorization(AuthorizationPolicies.Operate);
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
        _client = new HttpClient(new HttpClientHandler { CookieContainer = _cookies, AllowAutoRedirect = false })
            { BaseAddress = new Uri(_app.Urls.Single()) };
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
    [DataRow("GET", "/auth/me")]
    [DataRow("POST", "/auth/users")]
    public async Task MissingCredentials_Return401(string method, string path)
    {
        using HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
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
    [DataRow("NoRole", "GET", "/devices", 403)]
    public async Task Roles_AreEnforcedOnRoutes(string role, string method, string path, int expected)
    {
        await _identity.LoginAsync(_client, role);
        using HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));
        Assert.AreEqual(expected, (int)response.StatusCode);
    }

    [TestMethod]
    public async Task MissingCsrf_IsRejectedForLoginAndAuthenticatedWrites()
    {
        using HttpResponseMessage login = await _client.PostAsJsonAsync("/auth/login",
            new { email = TestIdentityProvider.Email(DeviceRoles.Operator), password = TestIdentityProvider.Password });
        Assert.AreEqual(HttpStatusCode.BadRequest, login.StatusCode);
        await _identity.LoginAsync(_client);
        _client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        using HttpResponseMessage mutation = await _client.PostAsJsonAsync("/devices/MIX-001/commands", new { commandName = "start" });
        Assert.AreEqual(HttpStatusCode.BadRequest, mutation.StatusCode);
    }

    [TestMethod]
    public async Task InvalidCredentialsAndLockout_Return401()
    {
        await TestIdentityProvider.RefreshCsrfAsync(_client);
        for (int attempt = 0; attempt < 5; attempt++)
        {
            using HttpResponseMessage failed = await _client.PostAsJsonAsync("/auth/login",
                new { email = TestIdentityProvider.Email(DeviceRoles.Operator), password = "Wrong_password_42!" });
            Assert.AreEqual(HttpStatusCode.Unauthorized, failed.StatusCode);
        }
        using HttpResponseMessage locked = await _client.PostAsJsonAsync("/auth/login",
            new { email = TestIdentityProvider.Email(DeviceRoles.Operator), password = TestIdentityProvider.Password });
        Assert.AreEqual(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    [TestMethod]
    public async Task ExpiredAndTamperedCookies_Return401()
    {
        await _identity.LoginAsync(_client);
        _identity.Clock.Advance(TimeSpan.FromHours(1));
        using HttpResponseMessage expired = await _client.GetAsync("/devices");
        Assert.AreEqual(HttpStatusCode.Unauthorized, expired.StatusCode);
        _cookies.Add(_client.BaseAddress!, new Cookie("KitchenOS.Session", "tampered", "/"));
        using HttpResponseMessage tampered = await _client.GetAsync("/devices");
        Assert.AreEqual(HttpStatusCode.Unauthorized, tampered.StatusCode);
    }

    [TestMethod]
    public async Task LogoutInvalidatesCopiedSessionCookie()
    {
        await _identity.LoginAsync(_client);
        Cookie original = _cookies.GetCookies(_client.BaseAddress!)["KitchenOS.Session"]!;
        string value = original.Value;
        using HttpResponseMessage logout = await _client.PostAsync("/auth/logout", null);
        Assert.AreEqual(HttpStatusCode.NoContent, logout.StatusCode);
        _cookies.Add(_client.BaseAddress!, new Cookie("KitchenOS.Session", value, "/"));
        using HttpResponseMessage replay = await _client.GetAsync("/devices");
        Assert.AreEqual(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [TestMethod]
    public async Task AdministratorCreatesAccountsWithHashedPasswordsAndControlledRoles()
    {
        await _identity.LoginAsync(_client, DeviceRoles.Administrator);
        using HttpResponseMessage created = await _client.PostAsJsonAsync("/auth/users",
            new { email = "new-user@kitchenos.test", password = TestIdentityProvider.Password, role = DeviceRoles.Viewer });
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);
        using IServiceScope scope = _app.Services.CreateScope();
        UserManager<IdentityUser> users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        IdentityUser user = (await users.FindByEmailAsync("new-user@kitchenos.test"))!;
        Assert.AreNotEqual(TestIdentityProvider.Password, user.PasswordHash);
        Assert.IsTrue(await users.CheckPasswordAsync(user, TestIdentityProvider.Password));
        Assert.IsTrue(await users.IsInRoleAsync(user, DeviceRoles.Viewer));
        using HttpResponseMessage invalidRole = await _client.PostAsJsonAsync("/auth/users",
            new { email = "bad-role@kitchenos.test", password = TestIdentityProvider.Password, role = "Superuser" });
        Assert.AreEqual(HttpStatusCode.BadRequest, invalidRole.StatusCode);
        using HttpResponseMessage weak = await _client.PostAsJsonAsync("/auth/users",
            new { email = "weak@kitchenos.test", password = "123", role = DeviceRoles.Viewer });
        Assert.AreEqual(HttpStatusCode.BadRequest, weak.StatusCode);
    }

    [TestMethod]
    [DataRow(DeviceRoles.Viewer)]
    [DataRow(DeviceRoles.Operator)]
    public async Task NonAdministrators_CannotCreateAccounts(string role)
    {
        await _identity.LoginAsync(_client, role);
        using HttpResponseMessage response = await _client.PostAsJsonAsync("/auth/users",
            new { email = "escalation@kitchenos.test", password = TestIdentityProvider.Password, role = DeviceRoles.Administrator });
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task IdentityIsAvailableAndHealthIsAnonymous()
    {
        using HttpResponseMessage health = await _client.GetAsync("/health");
        Assert.AreEqual(HttpStatusCode.OK, health.StatusCode);
        await _identity.LoginAsync(_client);
        using HttpResponseMessage response = await _client.GetAsync("/devices");
        Assert.IsFalse(string.IsNullOrWhiteSpace(response.Headers.GetValues("X-Test-Subject").Single()));
        string account = await _client.GetStringAsync("/auth/me");
        StringAssert.Contains(account, TestIdentityProvider.Email(DeviceRoles.Operator));
    }

    [TestMethod]
    [DataRow(DeviceRoles.Viewer)]
    [DataRow(DeviceRoles.Operator)]
    [DataRow(DeviceRoles.Administrator)]
    public async Task WebSocket_ValidRolesConnectUsingSessionCookie(string role)
    {
        await _identity.LoginAsync(_client, role);
        using ClientWebSocket socket = new();
        socket.Options.Cookies = _cookies;
        socket.Options.SetRequestHeader("Origin", "http://localhost:3020");
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await socket.ConnectAsync(SocketUri(), timeout.Token);
        Assert.AreEqual(WebSocketState.Open, socket.State);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", timeout.Token);
    }

    [TestMethod]
    public async Task WebSocket_MissingExpiredWrongRoleAndUntrustedOriginsAreRejected()
    {
        await RejectSocket(HttpStatusCode.Unauthorized);
        await _identity.LoginAsync(_client, "NoRole");
        await RejectSocket(HttpStatusCode.Forbidden);
        await _identity.LoginAsync(_client);
        await RejectSocket(HttpStatusCode.Forbidden, "https://untrusted.test");
        _identity.Clock.Advance(TimeSpan.FromHours(1));
        await RejectSocket(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task BootstrapCreatesOnlyTheInitialAdministratorWithoutResettingItsPassword()
    {
        using IServiceScope scope = _app.Services.CreateScope();
        UserManager<IdentityUser> users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        foreach (IdentityUser administrator in await users.GetUsersInRoleAsync(DeviceRoles.Administrator))
            Assert.IsTrue((await users.DeleteAsync(administrator)).Succeeded);
        BootstrapAdministratorOptions bootstrap = scope.ServiceProvider.GetRequiredService<IOptions<BootstrapAdministratorOptions>>().Value;
        bootstrap.Email = "bootstrap@kitchenos.test";
        bootstrap.Password = TestIdentityProvider.Password;
        await IdentityInitializationExtensions.InitializeAccountsAsync(scope.ServiceProvider);
        IdentityUser account = (await users.FindByEmailAsync(bootstrap.Email))!;
        Assert.IsTrue(await users.IsInRoleAsync(account, DeviceRoles.Administrator));
        string? originalHash = account.PasswordHash;
        bootstrap.Password = "Different_Password_43!";
        await IdentityInitializationExtensions.InitializeAccountsAsync(scope.ServiceProvider);
        Assert.AreEqual(originalHash, (await users.FindByEmailAsync(bootstrap.Email))!.PasswordHash);
        Assert.IsTrue(await users.CheckPasswordAsync(account, TestIdentityProvider.Password));
    }

    [TestMethod]
    public void ProductionCookies_RequireHttpsAndAreHttpOnly()
    {
        using TestIdentityProvider identity = new();
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        identity.Configure(builder.Services);
        using ServiceProvider services = builder.Services.BuildServiceProvider();
        CookieAuthenticationOptions cookie = services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.AreEqual(CookieSecurePolicy.Always, cookie.Cookie.SecurePolicy);
        Assert.IsTrue(cookie.Cookie.HttpOnly);
        Assert.AreEqual(SameSiteMode.Strict, cookie.Cookie.SameSite);
        Assert.IsFalse(cookie.SlidingExpiration);
    }

    private async Task RejectSocket(HttpStatusCode status, string? origin = null)
    {
        using ClientWebSocket socket = new();
        socket.Options.Cookies = _cookies;
        socket.Options.CollectHttpResponseDetails = true;
        if (origin is not null) socket.Options.SetRequestHeader("Origin", origin);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await Assert.ThrowsExactlyAsync<WebSocketException>(() => socket.ConnectAsync(SocketUri(), timeout.Token));
        Assert.AreEqual(status, socket.HttpStatusCode);
    }
    private Uri SocketUri() => new UriBuilder(_client.BaseAddress!) { Scheme = "ws", Path = "/ws" }.Uri;

    private sealed class AuthorizedRouteFilter : IResourceFilter
    {
        // Stop after real route authorization, before unrelated unfinished inventory actions.
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            context.HttpContext.Response.Headers["X-Test-Subject"] = context.HttpContext.User.FindFirst("sub")?.Value;
            context.Result = new NoContentResult();
        }
        public void OnResourceExecuted(ResourceExecutedContext context) { }
    }
}
