# Authentication

StockAnalyzer supports anonymous guest workspaces and optional user accounts.

## Auth Modes

| Mode | Behavior |
| --- | --- |
| Guest | Browser receives a generated `X-Client-ID`; workspace can sync without login. |
| Email/password | `/api/auth/signup` and `/api/auth/login` create/return JWT sessions. |
| Google OAuth | Enabled only when valid Google client ID/secret are configured. |

## Backend Implementation

- ASP.NET Core Identity stores users in PostgreSQL (`AspNet*` tables).
- JWT bearer authentication is configured in `backend/Program.cs`.
- Tokens are created by `JwtTokenService` with 7-day expiry.
- `StockAnalyzerUser` extends Identity user with `DisplayName` and `CreatedAt`.
- `/api/auth/me` and `/api/account/claim-workspace` require authentication.
- Most product APIs can operate anonymously through `X-Client-ID`.

## Frontend Implementation

- `AuthService` calls auth endpoints and holds current user signal state.
- `AuthTokenStore` stores JWT in `sessionStorage`.
- `authInterceptor` attaches bearer token to API requests.
- Google callback receives token in the URL fragment and redirects into the app.

## Configuration

| Variable/setting | Purpose |
| --- | --- |
| `JWT_SECRET` | Required outside development. |
| `Jwt:Issuer`, `Jwt:Audience` | Token validation identifiers. |
| `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET` | Enables Google OAuth when non-placeholder. |
| `FRONTEND_URL` | OAuth callback redirect target. |

## Authorization

There is currently no role-based feature gate for normal product flows. Affiliate stats are protected by configuration (`AFFILIATE_ADMIN_ENABLED`) rather than Identity roles.
