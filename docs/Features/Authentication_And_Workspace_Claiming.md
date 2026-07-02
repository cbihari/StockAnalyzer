# Authentication And Workspace Claiming

## Purpose

Allow anonymous use first, then let users create an account and attach their current browser workspace.

## Business Flow

1. Guest user accumulates watchlist, alerts, portfolio, and prediction history.
2. User signs up/logs in.
3. Frontend prompts to claim workspace.
4. Backend links the `GuestWorkspace` and eligible predictions to the authenticated user.

## UI Flow

- Routes: `/login`, `/signup`, `/auth/callback`.
- Components: auth forms and profile component.
- App shell checks whether workspace claim prompt should appear after login.

## Backend Flow

- `AuthController` handles config, signup, login, Google challenge/callback, and `me`.
- `AccountController.ClaimWorkspace` requires JWT.
- `GuestWorkspaceService.ClaimAsync` links workspace to user.

## API Endpoints

- `GET /api/auth/config`
- `POST /api/auth/signup`
- `POST /api/auth/login`
- `GET /api/auth/google`
- `GET /api/auth/google/callback`
- `GET /api/auth/me`
- `POST /api/account/claim-workspace`

## Models

- `SignupRequestDto`, `LoginRequestDto`, `AuthResponseDto`, `AuthUserDto`, `AuthConfigDto`, `ClaimWorkspaceRequestDto`
- Domain/infrastructure: `StockAnalyzerUser`, `GuestWorkspace`

## Validation

- Display name: 2-80 characters.
- Password: Identity minimum 8 characters, non-alphanumeric not required.
- Google is enabled only with non-placeholder client ID and secret.

## Database Tables

- `AspNetUsers`
- `AspNetUserLogins`
- `GuestWorkspaces`
- `Predictions`

## Error Handling

- Invalid credentials return unauthorized with plain detail.
- Missing Google config returns 503 for Google login.
- Invalid claim requests return unauthorized or validation errors.

## Future Improvements

- Email verification.
- Password reset.
- Role-based admin authorization for affiliate stats.
