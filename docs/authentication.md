# ASP.NET Core Identity

KitchenOS stores local user accounts, password hashes and roles in PostgreSQL using
ASP.NET Core Identity. No external login provider, JWT signing secret, public
registration, or preset passwords are required. The browser signs in with an
HTTP-only session cookie, including for `/ws`.

## First startup

Keep the PostgreSQL values from the team's `.env`. Add these entries to that same
local file at the repository root (beside `docker-compose.yml`):

```dotenv
IDENTITY_ADMIN_EMAIL=your-email@example.com
IDENTITY_ADMIN_PASSWORD=choose-a-unique-strong-password
FRONTEND_ORIGIN=http://localhost:3020
```

Choose a real password of at least 12 characters with uppercase, lowercase, digit
and non-alphanumeric characters. The example is a placeholder, not a working
credential. Never commit `.env` or paste its contents into a PR.

```powershell
docker compose up -d --build
```

Startup applies the Identity migration in its own `identity` schema, with a
separate migration-history table. Existing device entities and migrations are
unchanged. It creates the three roles and the initial administrator only when
there is no administrator already. Startup never resets an existing account's
password or promotes an existing user. Remove `IDENTITY_ADMIN_PASSWORD` and
`IDENTITY_ADMIN_EMAIL` from `.env` after the first successful startup and recreate
the backend container so those values are removed from its environment.

Open http://localhost:3020 and sign in. The dashboard sends cookies and CSRF tokens
on API requests and uses the same session cookie for WebSockets. Viewers see
monitoring information without command controls. Sign out closes the dashboard's
socket and invalidates that user's existing cookies through the security stamp.

## Accounts and roles

| Role | Monitoring and WebSockets | Commands / future alarm acknowledgement | Register or remove devices | Create accounts |
| --- | --- | --- | --- | --- |
| Viewer | Yes | No | No | No |
| Operator | Yes | Yes | No | No |
| Administrator | Yes | Yes | Yes | Yes |

There is no public self-registration or account-creation form. To create another
account, an administrator can run this in the signed-in dashboard's browser console
(the password below is a placeholder; supply the new user's chosen password):

```javascript
const csrf = await fetch('http://localhost:8080/auth/csrf', {
  credentials: 'include'
}).then(response => response.json());
const result = await fetch('http://localhost:8080/auth/users', {
  method: 'POST', credentials: 'include',
  headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf.token },
  body: JSON.stringify({
    email: 'operator@example.com', password: '<new-user-password>', role: 'Operator'
  })
});
console.log(result.status); // 201 when created
```

`role` defaults to `Viewer` if omitted. Unsupported roles and weak passwords return
400; unauthenticated requests return 401, and non-administrators receive 403.
An account/role administration UI, password-recovery email and MFA UI are not part
of this change. Do not enable account email confirmation until an email sender and
confirmation flow are configured.

## Endpoints

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/auth/csrf` | Obtain an identity-bound CSRF token and anti-forgery cookie |
| POST | `/auth/login` | Sign in with `{ "email": "...", "password": "..." }`; returns 204 |
| GET | `/auth/me` | Return the signed-in user's ID, email and roles |
| POST | `/auth/logout` | Invalidate sessions and clear the cookie; returns 204 |
| POST | `/auth/users` | Administrator creates an account with a supported role |

All unsafe HTTP methods, including login/logout, require `X-CSRF-TOKEN` from
`/auth/csrf` plus the matching cookie. Obtain a fresh token after changing identity.
Session cookies are HTTP-only, SameSite Strict, and expire after 30 minutes without
sliding renewal. Password failures lock the account after five attempts for 15
minutes. The login endpoint also limits each remote IP to ten requests per minute.
Cookies are revalidated against Identity's security stamp on every request.
Monitoring sockets are cut off at the authentication ticket's expiry; stamp changes
are checked on subsequent HTTP requests or new socket handshakes, not continuously
inside an already-open socket.

WebSockets accept cookies only, with no token query parameter. Handshakes reject
missing, expired or invalid cookies. Browser origins must match the configured
frontend origin or the API's own origin. HTTP CORS also allows only the configured
frontend origin and credentials. `/health` remains anonymous; other unannotated
endpoints require the monitoring policy.

## Deployment

HTTP cookies are permitted for local Development only. Outside Development, use
HTTPS for the frontend and API and WSS for sockets; cookies require Secure.
SameSite Strict requires frontend and API to be on the same site. Use one trusted
reverse proxy/origin or same-site subdomains, rather than unrelated hosting domains.
Set `Security__FrontendOrigin` to the actual UI origin. Configure trusted forwarded
headers at the deployment boundary if TLS terminates at a reverse proxy.

Docker persists Data Protection keys in the `identity-keys` volume. Retain that
volume across restarts, share it between API instances if scaling, and protect it
as credential material. Production needs appropriate disk/key encryption and
restricted filesystem access. Losing the keys invalidates existing cookies.

For non-Docker hosting, use `ConnectionStrings__Postgres`,
`Identity__BootstrapAdministrator__Email`, `Identity__BootstrapAdministrator__Password`,
`Security__FrontendOrigin` and `Security__KeyRingPath` through secure configuration.

## Tests and feature boundaries

```powershell
docker compose run --rm --no-deps --build backend dotnet test /app/kitchenOS_Scada.Api.Tests/kitchenOS_Scada.Api.Tests.csproj
```

Security tests use real Identity managers, EF Core SQLite stores, password hashing,
antiforgery and session cookies, without requiring Redis/PostgreSQL. They exercise
401/403, roles, login, password lockout, account creation, CSRF rejection, expired
and tampered cookies, logout revocation and WebSocket handshakes. Existing command
tests sign in as an Operator before exercising publication and result delivery.
The production migration targets PostgreSQL and keeps Identity separate from the
device persistence context.

Inventory GET/register/remove business implementations in the current base branch
are placeholders, and there is no alarm acknowledgement endpoint yet. Their owners
must complete those business features. Authorization tests stop after route policy
checks for those actions; fixture telemetry/acknowledgement routes test policies,
not new business functionality. Apply `AuthorizationPolicies.Operate` when the alarm
acknowledgement action lands. Mutation audit logs contain a stable user ID and
request trace information, without passwords, cookies or command argument bodies.

References:
- https://learn.microsoft.com/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0
- https://learn.microsoft.com/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
