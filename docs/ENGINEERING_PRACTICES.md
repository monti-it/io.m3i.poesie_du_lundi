# Engineering practices: DDD, TDD, Clean Code

Copied from `io.m3i.ledgy`'s `docs/ENGINEERING_PRACTICES.md` — the same reuse pattern as the
`k8s/` manifests and the README's "Reproducing this on the same VPS for another app" checklist.
Where ledgy's version is concrete it uses ledgy's names; this copy substitutes
`io.m3i.poesie_du_lundi`'s. **One section is a deliberate deviation — see "This app's layout"
below.**

## Domain-Driven Design

### Reference layout: modular monolith, four projects per module, plus SharedKernel

```
<App>.sln
  SharedKernel/            Entity/AggregateRoot base classes, ValueObject, DateRange,
                            Result/Result<T>, tenant/identity abstractions — no external dependencies
  Modules/
    <ModuleName>/
      Domain/               entities, value objects, domain services — no external dependencies
      Application/          use cases, orchestrates Domain, defines interfaces Infrastructure implements
      Infrastructure/        EF Core DbContext, repository implementations, external service clients
      Api/                  minimal API endpoint definitions, request/response DTOs — thin, no business logic
  Host/                     composition root — Program.cs, DI wiring, the actual runnable ASP.NET Core app
```

Dependencies point one way within a module: `Api → Application → Domain`,
`Infrastructure → Application, Domain`. `Domain` (and `SharedKernel`) reference nothing but the
.NET base class library — no EF Core, no ASP.NET Core, no Npgsql. The point is a
compiler-enforced boundary, not a folder-naming convention that drifts under deadline pressure.

### This app's layout: single-project API (deviation from the reference)

`io.m3i.poesie_du_lundi` is a poetry blog — **one bounded context** (publishing). The full
per-module four-project split is ceremony this app doesn't need yet, so the deviation, decided
at bootstrap:

- **One API project**, `api/src/PoesieDuLundi`, with `Domain/`, `Application/`,
  `Infrastructure/`, `Api/` as **folders / namespaces**, not separate `.csproj` files. `Host`'s
  responsibilities (composition root, `Program.cs`, DI wiring) fold into the same project.
- The layer-direction rule still holds and is **enforced by `Architecture.Tests`** (NetArchTest
  over namespaces): `Domain` touches only the BCL, `Api` handlers hold no business logic,
  `Infrastructure` types don't leak into `Api`.
- **The public read side and the admin authoring side are separated by namespace**
  (`PoesieDuLundi.Api.Public` vs `PoesieDuLundi.Api.Admin`) and by route prefix
  (`/api/...` anonymous, `/api/admin/...` SSO-gated — see "Public vs admin" below).
- **Adopt the module split the day a second bounded context appears** (e.g. a `Newsletter`
  context, roadmap issue) — retrofitting one context is cheap; the reference layout above is
  the target, not a rejected option.

`SharedKernel` still exists as a folder of dependency-free building blocks (`Entity`,
`AggregateRoot`, `ValueObject`, `Result`/`Result<T>`, `DateRange`, `Slug`), so extracting it to
its own project later is mechanical.

### Cross-module references: a port + a composition-root adapter

Not applicable while there is one context. When it becomes relevant: a module never references
another module — it declares a narrow **port** interface in its own `Application` and the
composition root wires an **adapter** that implements it against the other module. Keep each
consuming module's own id value type for the referenced aggregate.

### Layer responsibilities

- **SharedKernel** — `Entity`/`AggregateRoot` (identity, equality, domain-event collection),
  `ValueObject` (structural equality via `GetEqualityComponents()`), `Result`/`Result<T>`,
  `DateRange`, `Slug`. Dependency-free.
- **Domain** — entities (identity, via `SharedKernel.Entity` — e.g. a `Poem` with an `Id`),
  value objects (no identity, immutable, self-validating in their constructor — e.g. `Slug`,
  `PublicationDate`), domain services. Repository *interfaces* may live here or in Application;
  Infrastructure fulfills them.
- **Application** — use cases (`PublishPoem`, `SchedulePoemForMonday`, `ListPublishedPoems`),
  one class/method per use case. No HTTP concerns, no EF Core.
- **Infrastructure** — the `DbContext`, EF Core `IEntityTypeConfiguration<T>` per entity (not
  data annotations on domain classes), repository implementations, migrations.
- **Api** — minimal API endpoint definitions, DTO ↔ domain mapping. A handler calls one use
  case and translates the result to an HTTP response; no business logic.

### Read side vs. write side (CQRS-lite)

The **write side** goes through aggregate repositories: `GetAsync` by id (tracked), `AddAsync`,
`Remove`. A use case loads the aggregate, calls a domain method, saves.

The **read side** — the public poem list, archive, feeds — does *not* load tracked aggregates.
It uses **read-model query objects**: `Infrastructure`-layer classes that run `AsNoTracking()`
queries and project straight to a flat read record. New read paths follow this rather than
accreting `List*`/`Summarise*` methods onto the write-side repository contract.

### Result pattern for expected failures

Domain and Application code returns `Result`/`Result<T>` for *expected* failures — a business
rule violated ("can't publish a poem with an empty body", "that Monday already has a poem"), not
a bug. Reserve exceptions for genuinely exceptional / programmer-error conditions (invalid
value-object constructor arguments). A use case returning `Result.Failure("...")` is normal
control flow; the endpoint maps it to the right HTTP status, it isn't a 500.

### Domain events

Aggregates (`SharedKernel.AggregateRoot`) collect `IDomainEvent`s via `AddDomainEvent(...)` as
they change state (e.g. `PoemPublished`). Dispatch is an Infrastructure/Host concern — Domain
only records that something happened, with zero reference to any dispatch mechanism. Wire the
dispatch pipe (a `SaveChangesInterceptor` publishing post-commit) when the first handler is
actually needed (e.g. the newsletter reacting to `PoemPublished`); until then it's unjustified
moving parts.

**Delivery model when it lands:** in-process, synchronous, post-commit, inside the producing
request's DI scope. Handlers must be idempotent and best-effort. Anything that must-happen
-exactly-once (a newsletter send) belongs behind a transactional outbox, not directly in a
handler.

### Identity, tenancy, and public vs admin

This blog is **publicly readable without authentication** and has a **small number of trusted
authors** behind the shared SSO gate.

- **Public read endpoints** (`/api/...`, no `admin` segment) are anonymous. No `Remote-User`,
  no tenant filter — they serve published content to the world.
- **Admin endpoints** (`/api/admin/...`) sit behind Traefik's `forwardAuth` to Authelia
  (`auth.m3i.io`), same shared gate as ledgy. `Remote-User` identifies the author.
- **Tenancy model: single workspace.** Unlike ledgy (tenant-per-user), this app has one shared
  content space that every authorised author edits. `Remote-User` is recorded as the
  *authoring* `Author` / audit attribution, not as a data-isolation boundary. Revisit only if
  the blog ever needs isolated per-author spaces (not on the roadmap).
- A local `dotnet run` has no gate: in `Development` only, admin endpoints fall back to a fixed
  dev author (`Auth:DevSubject`, default `dev@localhost`); anywhere else a missing `Remote-User`
  on an admin route is 401, never a silent shared identity.
- Same-origin by design: `/api/*` → API, everything else → frontend, one domain. No CORS.

### Ubiquitous language

Name things with the vocabulary the domain uses — `Poem`, `Series`, `PublicationDate`,
`Draft`/`Scheduled`/`Published`, `Feed` — not generic `Item`/`Record`/`Post`/`Data`.

## Test-Driven Development

### Workflow

Red → Green → Refactor. Applies most strictly to `Domain` and `Application`, where the logic
worth protecting lives. Thin `Api` glue doesn't need line-by-line test-first development but
still needs at least one integration test per endpoint.

### Test project structure

```
tests/
  PoesieDuLundi.Tests/              Domain unit tests (no mocking framework — Domain has no deps),
                                    Application use-case tests (mocked repositories, NSubstitute),
                                    Api integration tests (WebApplicationFactory + EF Core InMemory,
                                    exercises Api+Infrastructure+Application+Domain over real HTTP),
                                    composition-root policies (migration startup, auth, API-docs).
  Architecture.Tests/               NetArchTest boundary checks over namespaces (see deviation above).
  Persistence.SmokeTests/           real-Postgres round-trip via Testcontainers — NOT in the .sln,
                                    its own CI job. See "Database strategy".
```

Split `PoesieDuLundi.Tests` into per-area projects (or the full per-module test split) when it
grows unwieldy or a second bounded context appears — mirroring whatever the `src/` layout does
at that point.

Framework: **xUnit**. Mocking: **NSubstitute**.

### Database strategy: EF Core InMemory provider (primary), real Postgres (gate)

Integration tests use `Microsoft.EntityFrameworkCore.InMemory` — no Docker dependency for the
suite, fast CI. Trade-off, explicitly accepted: InMemory doesn't enforce real Postgres
constraints or SQL semantics. In ledgy this bit three times (query filters on strongly-typed
IDs written as `.Value`; a `HasConversion`'d collection property; a native `text[]` column) —
each passed InMemory, each threw against Npgsql. So:

- **CI stays InMemory-primary.** The whole suite is not moving to Testcontainers.
- **A manual real-Postgres round-trip is mandatory** before merging any change that touches an
  EF Core migration, a `HasConversion` / value-object mapping, a `HasQueryFilter`, or a
  native-array / `jsonb` column. `docker run --rm -p 5433:5432 -e POSTGRES_PASSWORD=... postgres:18`,
  point `ConnectionStrings__Default` at it, `dotnet ef database update`, write+read one row back.
- **The automated version** — a `test-api-postgres` CI job doing that round-trip via
  Testcontainers — ships with the persistence bootstrap issue.

### What needs a test

- Every `Domain` entity/value object: construction validation, any behavior beyond property storage.
- Every `Application` use case: the happy path plus each domain rule it enforces.
- Every `Api` endpoint: at least one integration test proving the full HTTP → persistence → HTTP path.

Rule is "new domain logic doesn't merge without a test for it," not a coverage-tool gate.

### Frontend

Vitest + React Testing Library. Same TDD discipline for non-trivial logic (form validation,
Markdown rendering wrappers, feed builders); not mandated for pure layout components.

#### Frontend layout: feature modules, plus a public/admin split

```
frontend/src/
  app/         # App shell: App.tsx, routes, main.tsx, NotFoundPage — the composition root
  shared/      # nothing feature-specific — mirrors SharedKernel
    api/       #   client.ts = fetch wrapper + verb helpers; a public client (no redirect-on-401)
               #   and an admin client (session-expiry handling like ledgy's apiFetch)
    components/#   Layout, SeoHead, Prose (Markdown renderer), EmptyState
    lib/       #   slug.ts, formatDate.ts, useInlineEdit.ts
  modules/
    reading/   #   the public site — pages/HomePage, PoemPage, ArchivePage, SeriesPage; feeds
    authoring/ #   the SSO-gated admin — pages/PoemListPage, PoemEditorPage; mounted under /admin
  index.css
```

- Path aliases `@shared/*`, `@modules/*`, `@app/*`.
- `*.test.tsx` sits next to the file it covers.
- The public bundle must not import from `modules/authoring` — keep the admin editor out of the
  anonymous page weight. Enforce with an oxlint `no-restricted-imports` rule.

## Clean Code

- **SOLID**, applied mainly through the layering: Dependency Inversion (Domain defines
  interfaces, Infrastructure implements) and Single Responsibility (one use case per class).
- **No primitive obsession** — model domain concepts as value objects (`Slug`, not a bare
  `string`; `PublicationDate`, not a loose `DateOnly` passed around with rules re-checked
  everywhere). A value object validates itself once, at construction.
- **Small functions, one level of abstraction each.**
- **Comments**: none by default; only when the *why* isn't obvious (a non-obvious constraint, a
  workaround, a subtle invariant). Good naming carries the "what".
- **Nullable reference types stay enabled** (`<Nullable>enable</Nullable>`).
- **Static analysis**: `oxlint` for the frontend; the C# compiler's nullable/analyzer warnings
  for the API. Revisit a stricter analyzer package once there's enough domain code to tune it.

## Current status for io.m3i.poesie_du_lundi specifically

Bootstrapping. This scaffold commit carries the deployment/infra meta layer (this file,
`docs/ARCHITECTURE.md`, `k8s/` templates, CI/CD workflow skeletons, `NuGet.Config`,
`docker-compose.yml`). The `api/` solution and `frontend/` app are built issue-by-issue from
the GitHub backlog — see the "Foundation" milestone. Start from the single-project layout in the
deviation section above, with `Architecture.Tests` and `Persistence.SmokeTests` from day one.
