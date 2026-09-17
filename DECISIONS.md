# Decisions

Notes on how I built this and where AI fit in.

## 1. What AI generated vs. what I wrote

I used Claude heavily on this. To be straight about it: most of the first-draft code came out of
the model, and my job was reviewing it, deciding what was actually correct, and fixing what wasn't.

Generated and kept more or less as-is:

- the four-project Clean Architecture skeleton and the csproj/sln wiring
- the entities and enums
- the EF Core DbContext and Fluent API configurations
- first drafts of `ArtistService`, `TrackService`, and the controllers
- the JWT setup in `Program.cs` and `AuthController`
- the seed data
- the Angular side: `ApiService`, `AuthService`, the auth interceptor, both components, routing, CSS
- the `@ngrx/signals` stores, which I added later to replace the local `signal()` state that was
  originally sitting in the components

What I actually worked on:

The model invented business rules that weren't in the spec, so I went through each one and decided
whether to keep it.

Re-submitting a track to a DSP it's already on resets that row to `pending` rather than erroring or
creating a duplicate. I kept that, since it's what you'd want after a rejection, and added a unique
index on `(TrackId, DspId)` so the database enforces it instead of just the code.

Distributing a track flips it from `draft` to `submitted`. I kept that too. It deliberately does not
flip to `distributed`, because that's the DSP's outcome, not something the API should assume just
because a submission went out.

The status endpoint originally let you go from `distributed` straight back to `draft`. I left it
permissive at first and flagged it, but looking again that's a real gap, not a scope decision.
`AllowedStatusTransitions` in `TrackService` now only allows `draft → submitted → distributed`, with
no skipping and no going backwards. Anything else returns a 400 that names the allowed next status.

I rewrote the error handling. The first version had try/catch duplicated in every controller action.
I moved it into `ExceptionHandlingMiddleware`, which also meant deciding what the error contract
should be: `{ "message": ..., "errors": [...] }`.

I added validation the model skipped. An unknown `artistId` or `dspId`, a duplicate ISRC, and an
invalid `?status=` filter value now all return a 400 with a specific message instead of an empty
list or a 500.

I pulled the JWT key and admin password out of `appsettings.json` entirely and added a startup check
that fails fast if the signing key is missing or under 32 characters.

The database engine was actually the model's suggestion, not something the brief asked for. The
task just says EF Core with proper migrations, it doesn't name a provider. Claude picked SQLite so
anyone pulling the repo can run it with zero setup, no SQL Server instance, no Docker, no connection
string to fill in first. I went along with it after checking that it doesn't cost anything real:
everything goes through EF Core and `IApplicationDbContext`, so switching to SQL Server or Postgres
later is a one-line change (`UseSqlite` to `UseSqlServer`) plus a new connection string, nothing in
the entities, DTOs or services would need to touch. For a take-home that has to run on someone
else's machine with no setup instructions beyond a README, that trade-off made sense to keep.

On migrations: I wrote the migration files by hand initially, because I didn't have the .NET SDK
available to run `dotnet ef migrations add`. I've since verified them properly. They apply to a
clean database, the app starts and seeds with no errors, and running `dotnet ef migrations add` on
top produces an empty migration, which confirms the snapshot matches the model.

One tooling note worth recording. `dotnet ef` failed for me with
`Could not load file or assembly 'System.Runtime, Version=10.0.0.0'`. That isn't a problem with the
project. My global `dotnet-ef` tool was version 10 while this project targets net8.0. Pinning EF
tools to 8.0.8 fixed it. It's in the README because anyone on newer tooling will hit the same wall
and could easily assume the project is broken.

I also ran `npm install` and `ng build` in both configurations rather than assuming the generated
TypeScript compiled. That's how I found the bug in question 3.

## 2. Security issues I found or introduced

**Secrets committed in `appsettings.json`.** The first pass put a real JWT signing key and admin
password straight into a file that gets committed by default. Renaming them to something obviously
fake isn't a fix. `appsettings.json` now ships with `Jwt:Key`, `AdminUser:Username` and
`AdminUser:Password` blank, `Program.cs` refuses to start with an actionable message if `Jwt:Key` is
missing or shorter than 32 characters, and the real values live in user-secrets locally or in
environment variables anywhere else. Nothing that signs a token or logs anyone in is in this repo.

**Hardcoded admin, plaintext comparison.** `AuthController` compares the password with `==`. That's
fine for one demo account and explicitly not how I'd ship real auth: no user table, no hashing, no
lockout on repeated failures, and `==` isn't constant-time. I left a comment in the controller
flagging it rather than presenting it as production-ready.

**No rate limiting on login.** As it stands the login endpoint can be brute-forced for free. I
didn't add it given the scope, but it's the first thing I'd do before this touched a real network.
ASP.NET Core's built-in rate limiting middleware would cover it cheaply.

**Token in `localStorage`.** Readable by any script on the page, so it's an XSS exposure. I judged
it acceptable for a demo. For anything real I'd use an httpOnly `SameSite=Strict` cookie issued by
the API and deal with CSRF instead.

**CORS was `AllowAnyOrigin`.** Narrowed to the Angular dev server origin the front-end actually runs
on. Wide-open CORS combined with bearer tokens is needless attack surface.

**Over-posting, avoided by design rather than luck.** Every write endpoint takes its own request DTO
instead of binding to EF entities, so a client can't set `Track.Id` or flip `Status` through the
wrong endpoint. This was already the pattern in the generated code; I kept it and checked that no
controller action accepts an entity type directly.

**Leaky 500s.** Unhandled exceptions originally bubbled out with the default developer exception
behaviour, which is easy to leave switched on by accident. The middleware now returns a generic
message for anything unexpected and logs the real exception server-side.

## 3. Something the AI got wrong

Both Angular environment files pointed at `http://localhost:5080/api`, and `angular.json` had no
`fileReplacements` block on the production configuration. So `ng build` produced a production bundle
hardcoded to talk to localhost.

What makes this worth calling out is that nothing catches it. Both files exist, both compile, the
types are fine, and it works perfectly on my machine. It only breaks once it's deployed somewhere,
which is the worst possible time to find out. I caught it by running the production build and
checking what the bundle actually resolved to, not by reading the diff.

The lesson generalises. "It compiles" and "it's configured for where it will actually run" are
different claims, and the model is far more reliable at the first than the second. Build config,
environment wiring and deployment assumptions are where I stopped trusting it and started checking.

A smaller one from the NGRX work. It injected the stores as constructor parameters,
`constructor(private store: TrackListStore)`, which is the natural thing to reach for in Angular.
It doesn't compile. `signalStore()` returns a const, not a class, so TypeScript won't accept it as a
type annotation (TS2749). The correct form is `readonly store = inject(TrackListStore)`. In the same
pass the compiler also caught `changeStatus` calling `loadTrack` from inside the same
`withMethods()` block that defines it, since a method can only see siblings from an earlier block,
so that had to be split in two.

Neither is a deep bug. Both are library-specific conventions the model got wrong while producing
code that looked completely plausible, which is exactly why running it matters more than reading it.