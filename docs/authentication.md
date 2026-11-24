# API authentication and authorization

KitchenOS validates access tokens issued by an external OpenID Connect provider.
It does not issue passwords, tokens, or local user accounts. The team must select
the provider and register the API before enabling this branch in a shared environment.

## Configuration

Set these values in the local `.env` used by Docker Compose:

```dotenv
AUTH_AUTHORITY=https://your-provider.example/your-issuer
AUTH_AUDIENCE=your-kitchenos-api-audience
AUTH_ROLE_CLAIM_TYPE=roles
```

Use the provider's actual issuer and access-token audience. For a host outside
Docker, the equivalent ASP.NET Core settings are `Authentication__Authority`,
`Authentication__Audience`, and `Authentication__RoleClaimType`.
Missing issuer/audience configuration fails startup; there is no anonymous development bypass.
Signing keys come from HTTPS OIDC discovery, with normal key rotation support.
Issuer, audience, signature, expiration, and a nonempty `sub` are required.
The API uses zero clock skew, so expired tokens are rejected immediately.

Create these exact role values in the provider and assign users explicitly:

| Role | Monitoring and `/ws` | Commands and future alarm acknowledgement | Register/remove devices |
| --- | --- | --- | --- |
| Viewer | Yes | No | No |
| Operator | Yes | Yes | No |
| Administrator | Yes | Yes | Yes |

The default role claim is `roles`; configure a different claim name if your provider
uses one. Unknown/missing roles receive 403 even with a valid token.
New endpoints default to the monitoring policy. New mutation endpoints must
explicitly use `AuthorizationPolicies.Operate` or `AuthorizationPolicies.Administer`.
Only `/health` is anonymous. Database readiness and development OpenAPI require a role.

## HTTP and WebSocket clients

Send an **access token intended for this API**, not an ID token:

```http
Authorization: Bearer <access-token>
```

Non-browser WebSocket clients can use the same header. Browser clients may use
`wss://your-api/ws?access_token=<URL-encoded-access-token>` because the browser
WebSocket constructor cannot set an Authorization header. URL tokens are accepted
only on an actual `/ws` WebSocket upgrade, never on ordinary HTTP operations.
Use HTTPS/WSS in deployed environments. Keep ASP.NET Core hosting logs at Warning
(already configured), and redact query strings in reverse-proxy/access logs.
Do not persist tokens in browser local storage or log them. Obtain/refresh tokens
through the chosen provider's supported client or a backend-for-frontend.
The current frontend has no login/token acquisition integration; that must be wired
to the chosen provider before its existing anonymous calls can work with this API.

Authentication is checked at the WebSocket handshake. Existing connections are not
revalidated on every message; clients should reconnect with fresh credentials when
their token expires. Server-enforced live-session revocation is outside this change.

## Identity and audit

Controllers can read validated `User` claims, and scoped services can obtain them
through `IHttpContextAccessor`. Mutation audit logs include the stable issuer and
subject, request trace ID, method, path, and response status. No token, password,
query string, or command argument body is logged. No schema or device/event contract
changes are needed.

## Scope and verification

The current inventory GET/register/remove implementations are placeholders, and no
alarm acknowledgement HTTP endpoint exists yet. Their business implementation is
owned by the device/alarm features. Authorization is attached to the existing device
actions; the Operator policy is ready for the future acknowledgement endpoint.

Run:

```powershell
docker compose run --rm --no-deps --build backend dotnet test /app/kitchenOS_Scada.Api.Tests/kitchenOS_Scada.Api.Tests.csproj
```

`Security/AuthorizationIntegrationTests` hosts the real controller routes with a
resource filter that stops after authorization, so unfinished inventory actions do
not require PostgreSQL. It tests HTTP 401/403, each permitted role, invalid issuer,
audience/signature/expiry/subject, fallback protection, identity propagation, and real
WebSocket handshakes. The alarm/telemetry test routes are policy fixtures, not new API
features. Existing command tests now run with signed Operator tokens and the real
authentication middleware, continuing to exercise publication and result delivery.

References:
- https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0
- https://learn.microsoft.com/aspnet/core/signalr/authn-and-authz
