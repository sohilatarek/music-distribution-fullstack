# Music Distribution — Track Management API

A Track Management API for a music distribution company, plus a small Angular front-end.
Backend is a .NET 8 Web API built with Clean Architecture (Domain / Application / Infrastructure / Api),
EF Core + SQLite, and JWT authentication.

```
music-distribution/
├── backend/
│   ├── MusicDistribution.sln
│   └── src/
│       ├── MusicDistribution.Domain          # Entities, enums — no dependencies
│       ├── MusicDistribution.Application     # DTOs, service interfaces/implementations, validation, exceptions
│       ├── MusicDistribution.Infrastructure  # EF Core DbContext, configurations, migrations, seeding
│       └── MusicDistribution.Api             # Controllers, JWT auth, Swagger, composition root (Program.cs)
├── frontend/                                  # Angular 18 (standalone components)
├── DECISIONS.md
└── README.md
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/) and npm
- Angular CLI (only needed if you want to run `ng` commands directly instead of via `npm run`):
  ```bash
  npm install -g @angular/cli@18
  ```
- `dotnet-ef` CLI tool (only needed if you want to add new migrations):
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## 1. Run the backend

```bash
cd backend
dotnet restore
```

### Configuring secrets (required before first run)

No JWT signing key or admin password is committed to source control — `appsettings.json` ships
with them blank on purpose, and the API fails fast at startup with a clear error if they're not
set. Configure them once, locally, via the .NET Secret Manager (already wired up via
`UserSecretsId` in the Api project):

```bash
cd backend/src/MusicDistribution.Api
dotnet user-secrets set "Jwt:Key" "this-is-a-demo-only-signing-key-change-it-32chars-min"
dotnet user-secrets set "AdminUser:Username" "admin"
dotnet user-secrets set "AdminUser:Password" "ChangeMe123!"
```

(Those exact values are placeholders for local demo purposes only — swap in your own if you like.
The point is they live in your machine's user-secrets store, not in this repo.)

User Secrets only apply when running locally in `Development`. If you're running this anywhere
else (CI, a container, a real deployment), set the equivalent environment variables instead —
ASP.NET Core maps `__` to a config section separator:

```bash
export Jwt__Key="a-long-random-production-value"
export AdminUser__Username="admin"
export AdminUser__Password="a-real-password-not-this-one"
```

The signing key must be at least 32 characters (HMAC-SHA256's minimum recommended key size) —
the API checks this at startup and refuses to start with a shorter one.

### Database & migrations

The database is SQLite (file `musicdistribution.db`, created next to the API project). Migrations
live in `src/MusicDistribution.Infrastructure/Persistence/Migrations`.

You do **not** need to run anything manually for a first run — `Program.cs` calls
`db.Database.MigrateAsync()` on startup, which creates the SQLite file and applies all
pending migrations automatically, then seeds sample data if the database is empty.

If you prefer to apply migrations yourself (e.g. before running, or in a CI pipeline):

```bash
cd backend
dotnet ef database update \
  --project src/MusicDistribution.Infrastructure \
  --startup-project src/MusicDistribution.Api
```

If you change the data model and want to add a new migration:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/MusicDistribution.Infrastructure \
  --startup-project src/MusicDistribution.Api
```

### Run the API

```bash
cd backend
dotnet run --project src/MusicDistribution.Api
```

The API starts on `http://localhost:5080` and opens Swagger UI at `http://localhost:5080/swagger`,
where you can browse and try every endpoint. On first run it seeds:

- 4 artists
- 9 tracks across multiple genres and statuses (draft / submitted / distributed)
- 3 DSPs (Spotify, Apple Music, YouTube Music)
- a realistic set of track ↔ DSP distributions (pending / live / rejected)

### Getting a JWT token

`POST /api/tracks/{id}/distribute` and `PATCH /api/tracks/{id}/status` are protected with
`[Authorize]` and require a bearer token. Use whichever `AdminUser` credentials you set via
user-secrets above:

Get a token:

```bash
curl -X POST http://localhost:5080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"ChangeMe123!"}'
```

Response:

```json
{ "token": "eyJhbGciOi...", "expiresAt": "2026-09-15T13:00:00Z" }
```

Use it on protected calls:

```bash
curl -X POST http://localhost:5080/api/tracks/1/distribute \
  -H "Authorization: Bearer eyJhbGciOi..." \
  -H "Content-Type: application/json" \
  -d '{"dspIds":[1,2]}'
```

In Swagger UI, click **Authorize** and paste the token (no `Bearer ` prefix needed — Swashbuckle
adds it) to call protected endpoints from the browser.

## 2. Run the front-end

```bash
cd frontend
npm install
npm start          # ng serve, defaults to http://localhost:4200
```

The API base URL is set in `src/environments/environment.ts` (defaults to
`http://localhost:5080/api`) and `src/environments/environment.production.ts` for prod builds
(`ng build`, which uses the `fileReplacements` in `angular.json` to swap it in) — edit these if
your API runs elsewhere.

Open `http://localhost:4200`. The front-end has two views:

- **Track list** (`/`) — every track with artist name, genre, status, and a status filter dropdown.
- **Track detail** (`/tracks/:id`) — full track info, its DSP distribution statuses, a form to
  submit the track to any DSP it hasn't reached yet, and buttons to change its status.

Log in with the same admin credentials via the bar in the top-right corner to unlock the
distribute/status-change actions (they call the JWT-protected endpoints). A `authInterceptor`
(`src/app/core/auth.interceptor.ts`) attaches the stored JWT to every outgoing request
automatically once you're logged in.

### State management: NGRX Signal Store

Each view's state lives in an `@ngrx/signals` store rather than component-local `signal()`s:

- `core/stores/track-list.store.ts` — the track list, the active status filter, loading/error
  state. `loadTracks` is an `rxMethod` bound directly to the `statusFilter` signal in the store's
  `onInit` hook, so it re-fetches automatically whenever the filter changes; `setStatusFilter`
  only has to patch state, not orchestrate a reload.
- `core/stores/track-detail.store.ts` — the track, the DSP catalog, the in-progress "distribute to
  these DSPs" selection, and the distribute/status-change actions, each as its own `rxMethod`
  using `tapResponse` for the success/error split.

Both stores are provided in their component's `providers` array (route-scoped), not
`providedIn: 'root'`, so state resets cleanly every time you navigate to a different track rather
than leaking between visits.

## API reference

| Method | Route                        | Auth | Description                                            |
|--------|------------------------------|------|----------------------------------------------------------|
| POST   | `/api/artists`                | No   | Create an artist                                        |
| GET    | `/api/artists`                | No   | List all artists                                         |
| POST   | `/api/tracks`                  | No   | Create a track for an artist                             |
| GET    | `/api/tracks`                  | No   | List tracks, filters: `?artistId=&genre=&status=`        |
| GET    | `/api/tracks/{id}`             | No   | Track details incl. DSP distribution statuses            |
| POST   | `/api/tracks/{id}/distribute`  | **Yes**  | Submit a track to one or more DSPs                    |
| PATCH  | `/api/tracks/{id}/status`      | **Yes**  | Update a track's status                               |
| GET    | `/api/dsps`                    | No   | List available DSPs (used by the front-end)               |
| POST   | `/api/auth/login`              | No   | Exchange admin credentials for a JWT                       |

All error responses use the shape `{ "message": "..." }` (validation errors also include an
`errors` array).

## Running tests

No automated test project was included given the 5-day scope — see `DECISIONS.md` for what was
prioritized instead. The Api project is left `partial` (`public partial class Program {}`) so a
`WebApplicationFactory<Program>`-based integration test project can be dropped in later without
changes to `Program.cs`.

## Design notes

- **Clean Architecture**: `Domain` has zero dependencies; `Application` depends only on `Domain`
  and defines `IApplicationDbContext` so it never references EF Core directly; `Infrastructure`
  implements that interface with EF Core/SQLite; `Api` wires everything together in `Program.cs`
  and only talks to `Application` interfaces.
- **Validation**: DTOs use Data Annotations (`[Required]`, `[EmailAddress]`, `[RegularExpression]`)
  for shape validation, returned as a consistent 400 payload by a custom
  `InvalidModelStateResponseFactory`. Business-rule validation (duplicate ISRC, unknown
  artist/DSP id, invalid status filter) is enforced in the Application services and surfaced via a
  small exception-to-HTTP-status middleware (`NotFoundException` → 404, `ValidationAppException` → 400).
- **Idempotent re-distribution**: submitting a track to a DSP it's already been submitted to resets
  that distribution to `pending` rather than creating a duplicate row (there's a unique index on
  `(TrackId, DspId)`).
- **Status lifecycle is enforced, not just suggested**: `PATCH /api/tracks/{id}/status` only allows
  `draft → submitted → distributed`, strictly forward and non-skipping. `distributed` is terminal
  through this endpoint. Any other requested transition (backward, skipping a step, or "changing"
  to the current status) returns `400` with a message naming the allowed next status. See
  `TrackService.AllowedStatusTransitions`.
