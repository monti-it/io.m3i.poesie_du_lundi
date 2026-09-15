# Architecture

How `io.m3i.poesie_du_lundi`'s system is put together. The **why** behind the layout is in
[ENGINEERING_PRACTICES.md](ENGINEERING_PRACTICES.md); deployment — k3s, CI/CD, TLS, SSO wiring —
is in [README.md](../README.md). This document is a skeleton, filled in as the app is built.

## Shape

A poetry blog: a **public, anonymously-readable site** plus a **small SSO-gated authoring
area**. Same infrastructure story as `io.m3i.ledgy` (PostgreSQL + .NET 10 + React, Docker, k3s
on the shared OVH VPS), a lighter API.

```
frontend/   Vite + React, built to static files, served by nginx.
              modules/reading   — the public site (/, /poems/:slug, /archive, /series/:slug)
              modules/authoring — the admin editor, mounted under /admin (SSO-gated)
api/        ASP.NET Core, ONE project (PoesieDuLundi), DDD layering by namespace:
              Domain/         aggregates, value objects            — BCL only
              Application/    one use case per operation           — → Domain
              Infrastructure/ PoesieDuLundiDbContext, EF configs, migrations, read models
              Api/            minimal-API endpoints + DTOs
                Api/Public/   anonymous  — GET published content, feeds
                Api/Admin/    SSO-gated  — CRUD drafts, schedule, publish
k8s/        production deployment — Deployments+Service for api/frontend, StatefulSet for
              Postgres, two Ingress resources (public anonymous + /admin gated), namespace `poesie`.
docker-compose.yml   local dev only — not what's deployed.
```

Dependencies point one way: `Api → Application → Domain`, `Infrastructure → Application, Domain`.
`Domain` has no `PackageReference` to EF Core / ASP.NET Core / Npgsql. Enforced by
`Architecture.Tests` (NetArchTest over namespaces, since there's one project — see
ENGINEERING_PRACTICES.md "This app's layout").

## Domain (planned — see the Foundation + MVP issues)

| Aggregate | Owns |
| --- | --- |
| `Poem` | title, body (Markdown), `Slug`, `Series` link, status (`Draft`/`Scheduled`/`Published`), `PublicationDate`, `Tags` (slugged, jsonb) |
| `Series` | a themed cycle / "saison" grouping poems — title, slug, description, order |
| `Author` | display name, bio, avatar; keyed by the SSO subject (`Remote-User`) |

One `PoesieDuLundiDbContext`, its own `__EFMigrationsHistory` table. `DatabaseMigrator` runs
migrations as the `migrate` init container on every rollout.

## Request flow

```mermaid
flowchart LR
    B[Browser] --> T[Traefik Ingress]
    T -->|"/ , /poems/* , /archive"| FE[poesie-frontend]
    T -->|"/api/* (public)"| API[poesie-api]
    T -->|"/admin/* , /api/admin/* — forwardAuth to Authelia"| G{SSO gate}
    G -->|Remote-User| API
    G --> FE
    API --> DB[(Postgres)]
```

- Public paths reach the API/frontend with no auth middleware.
- `/admin` and `/api/admin` go through the shared `auth-forward-auth@kubernetescrd` Middleware
  (owned by `io.m3i.auth`, consumed cross-namespace). `Remote-User` → the `Author` making the
  edit. Single shared content space (not tenant-per-user) — see ENGINEERING_PRACTICES.md.
- Same-origin: one domain, no CORS.

## Publishing model

Poems are written as drafts, then **scheduled for a Monday** ("la poésie du lundi"). A
scheduled poem becomes visible when its `PublicationDate` arrives — resolved on read
(`status == Scheduled && PublicationDate <= today` counts as published in the public query) so
no background scheduler is required for correctness; a lightweight recurring job only
materialises the transition and triggers side effects (feeds cache bust, later: newsletter).

## Feeds & SEO

RSS 2.0 + Atom + JSON Feed of published poems, `sitemap.xml`, `robots.txt`, per-poem
OpenGraph/meta tags. These are first-class MVP, not an afterthought — the public site's reach
depends on them.
