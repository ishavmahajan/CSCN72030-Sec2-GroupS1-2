using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace kitchenOS_Scada.Api.Security;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapKitchenOsIdentity(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder auth = endpoints.MapGroup("/auth");
        auth.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
            Results.Ok(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }))
            .AllowAnonymous();
        auth.MapPost("/login", async (LoginRequest request, SignInManager<IdentityUser> signIn,
            UserManager<IdentityUser> users) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Email and password are required." });
            IdentityUser? user = await users.FindByEmailAsync(request.Email);
            if (user is null) return Results.Unauthorized();
            SignInResult result = await signIn.PasswordSignInAsync(user, request.Password,
                isPersistent: false, lockoutOnFailure: true);
            return result.Succeeded ? Results.NoContent() : Results.Unauthorized();
        }).AllowAnonymous().RequireRateLimiting("IdentityLogin");

        AuthorizationPolicy signedIn = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        auth.MapGet("/me", async (HttpContext context, UserManager<IdentityUser> users) =>
        {
            IdentityUser? user = await users.GetUserAsync(context.User);
            return user is null ? Results.Unauthorized() : Results.Ok(new
            {
                id = user.Id, email = user.Email, roles = await users.GetRolesAsync(user)
            });
        }).RequireAuthorization(signedIn);
        auth.MapPost("/logout", async (HttpContext context, UserManager<IdentityUser> users,
            SignInManager<IdentityUser> signIn) =>
        {
            IdentityUser? user = await users.GetUserAsync(context.User);
            if (user is not null)
            {
                IdentityResult revoked = await users.UpdateSecurityStampAsync(user);
                if (!revoked.Succeeded) return Results.Problem("Could not invalidate the session.");
            }
            await signIn.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization(signedIn);
        auth.MapPost("/users", async (CreateUserRequest request, UserManager<IdentityUser> users) =>
        {
            string role = request.Role ?? DeviceRoles.Viewer;
            if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email)
                || string.IsNullOrWhiteSpace(request.Password)
                || !new[] { DeviceRoles.Viewer, DeviceRoles.Operator, DeviceRoles.Administrator }.Contains(role))
                return Results.BadRequest(new { error = "A valid email, password, and supported role are required." });
            IdentityUser user = new() { UserName = request.Email, Email = request.Email };
            IdentityResult created = await users.CreateAsync(user, request.Password);
            if (!created.Succeeded) return Results.BadRequest(new { errors = created.Errors.Select(error => error.Code) });
            IdentityResult assigned = await users.AddToRoleAsync(user, role);
            if (!assigned.Succeeded)
            {
                await users.DeleteAsync(user);
                return Results.Problem("Could not assign the account role.");
            }
            return Results.Json(new { id = user.Id, email = user.Email, role }, statusCode: StatusCodes.Status201Created);
        }).RequireAuthorization(AuthorizationPolicies.Administer);
        return endpoints;
    }

    public sealed record LoginRequest(string? Email, string? Password);
    public sealed record CreateUserRequest(string? Email, string? Password, string? Role);
}
