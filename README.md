# io.m3i.poesie_du_lundi

**La poésie du lundi** — a poetry blog. A new poem every Monday.

Stack: PostgreSQL + .NET 10 + React, containerized with Docker, deployed to **Kubernetes (k3s)**
on the shared OVH VPS — the same infrastructure as [`io.m3i.ledgy`](https://github.com/monti-it/io.m3i.ledgy),
which is app #1 on that cluster. This is app #2, following ledgy's
[README's "Reproducing this on the same VPS for another app"](https://github.com/monti-it/io.m3i.ledgy/blob/master/README.md)
checklist. Engineering practices (DDD, TDD, Clean Code) are in
[docs/ENGINEERING_PRACTICES.md](docs/ENGINEERING_PRACTICES.md) — **including the one deliberate
deviation from ledgy: a single-project API**, since a blog is one bounded context. The system
shape is in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Status

**Bootstrapping.** This repo currently holds only the infra/meta scaffold (docs, `k8s/`
templates, CI/CD workflow skeletons, `NuGet.Config`, `docker-compose.yml`). The `api/` solution
and `frontend/` app are built issue-by-issue from the [GitHub backlog](../../issues) — see the
**Foundation** and **MVP** milestones. Nothing is deployed yet.

Target: live at `https://poesie-du-lundi.m3i.io`, public and anonymously readable, with a small
SSO-gated authoring area at `/admin`.

## What makes this different from ledgy

| | ledgy | poesie_du_lundi |
| --- | --- | --- |
| API layout | modular monolith, 4 projects × N modules + `Host` | **one project**, DDD layering by namespace (adopt the split when a 2nd context appears) |
| Access | entirely behind the SSO gate | **public read**, SSO only on `/admin` + `/api/admin` |
| Tenancy | tenant-per-user (isolated data) | **single shared content space**; SSO identity = authoring attribution only |
| Ingress | one Ingress, gate on all paths | **two Ingress resources** — public (anonymous) + admin (forwardAuth) |

Everything else — k3s, Traefik, cert-manager, the `deploy` OS user, ufw, GHCR, the CI→VPS
`kubectl set image` rollout with a `migrate` init container, EF Core InMemory tests + a
real-Postgres smoke job — is the same pattern, deliberately.

## Structure

- `frontend/` — React + TypeScript (Vite), nginx container. `modules/reading` = the public
  site, `modules/authoring` = the `/admin` editor. *(built from issues)*
- `api/` — .NET 10, single project `src/PoesieDuLundi` (`PoesieDuLundi.slnx`), Kestrel container.
  `Domain/` `Application/` `Infrastructure/` `Api/` as namespaces; `Api/Public` anonymous,
  `Api/Admin` SSO-gated. *(built from issues)*
- `k8s/` — Deployments+Services for `api`/`frontend`, a StatefulSet+headless Service for
  Postgres, **two** Ingress resources, namespace-scoped CI RBAC. Namespace `poesie`. Applied
  manually via `sudo kubectl apply` — CI only patches image tags.
- `docker-compose.yml` — **local dev only**, not the deployment mechanism.
- `.env` / `.env.example` — Postgres creds for the *local* Compose stack only.
- `.github/workflows/` — `ci.yml` (test on PR, reusable) + `deploy.yml` (build → GHCR → rollout).
- `NuGet.Config` — pins restore to nuget.org only (this machine's global config points at an
  unrelated private feed that 401s).

## Local development

Frontend:

```bash
cd frontend
npm install
npm run dev        # proxies /api to http://localhost:5080
```

API (needs Postgres reachable — see the Compose stack below):

```bash
cd api
ConnectionStrings__Default="Host=localhost;Database=poesie;Username=poesie;Password=devpassword" \
  dotnet run --project src/PoesieDuLundi --urls http://localhost:5080
```

`dotnet build PoesieDuLundi.slnx` builds everything (app + test projects).

Full stack in containers:

```bash
cp .env.example .env    # then set a real POSTGRES_PASSWORD
docker compose up --build
```

Frontend on `:8080`, API on `:5080` (`/api/hello`, `/healthz`). Postgres has no host port.

## Deployment

The CI/CD shape is identical to ledgy's — see
[ledgy's README "Deploying to the OVH VPS"](https://github.com/monti-it/io.m3i.ledgy/blob/master/README.md)
for the full story (CI/CD jobs, the `migrate` init container, migration review at PR time,
rollback). The differences for this app:

- **Namespace**: `poesie` (not `ledgy`).
- **Images**: `ghcr.io/monti-it/io.m3i.poesie_du_lundi-{api,frontend}`.
- **Domain**: `poesie-du-lundi.m3i.io`.
- **kubeconfig context**: `poesie-ci` — CI passes `--context=poesie-ci` explicitly on every
  `kubectl` call (the shared `/home/deploy/.kube/config` has one context per app; never rely on
  `current-context`).
- **Two Ingress resources** instead of one — `k8s/ingress.yaml` — so the public site is
  anonymous and only `/admin` + `/api/admin` go through `auth-forward-auth@kubernetescrd`.

### Repository secrets required (Settings → Secrets and variables → Actions)

| Secret | Value |
|---|---|
| `VPS_HOST` | VPS public IP — same as ledgy |
| `VPS_USER` | `deploy` — the shared deploy user, reused as-is |
| `VPS_SSH_KEY` | the **same** Actions→VPS private key as ledgy (authenticates to an OS account, not repo-scoped) |

### One-time cluster provisioning for this app

Already shared cluster-wide, **don't redo**: k3s, Traefik, cert-manager + the `letsencrypt-prod`
`ClusterIssuer`, the `deploy` OS user, ufw. Per-app steps (from ledgy's "Per-app — repeat this
pattern"), tracked as the infra issues in the backlog:

1. Sanity-check node headroom before adding a second app's Postgres instance:
   `free -h` and `sudo kubectl describe nodes` (the node has 3.7GB total; ledgy alone already
   uses a slice of it).
2. `sudo kubectl create namespace poesie`
3. New GitHub **deploy key** on the VPS + an SSH `Host github.com-poesie` alias in
   `/home/deploy/.ssh/config` (do **not** overwrite ledgy's `Host github.com` block). Clone
   with `git clone git@github.com-poesie:monti-it/io.m3i.poesie_du_lundi.git`.
4. `poesie-db-credentials` + `poesie-api-config` Secrets in the `poesie` namespace, generated
   inline so the password is never printed to a terminal or log:

   ```bash
   PW=$(openssl rand -base64 24)
   sudo kubectl create secret generic poesie-db-credentials -n poesie \
     --from-literal=POSTGRES_DB=poesie \
     --from-literal=POSTGRES_USER=poesie \
     --from-literal=POSTGRES_PASSWORD="$PW"
   sudo kubectl create secret generic poesie-api-config -n poesie \
     --from-literal=ConnectionStrings__Default="Host=poesie-db;Database=poesie;Username=poesie;Password=$PW" \
     --from-literal=ASPNETCORE_ENVIRONMENT=Production
   ```

   `RunMigrationsOnStartup` is deliberately left unset on `poesie-api-config` — production
   relies solely on the `migrate` init container in `k8s/api-deployment.yaml`, same as ledgy.
5. `ghcr-pull` image-pull Secret + the `imagePullSecrets` ServiceAccount patch, `-n poesie`
   (same GHCR PAT reused; Secrets are namespace-scoped so the step can't be skipped).
6. `sudo kubectl apply -f k8s/` — `db-statefulset.yaml`, `api-deployment.yaml`,
   `frontend-deployment.yaml`, `deploy-rbac.yaml`, `ingress.yaml`. Resource `requests`/`limits`
   are set from day one (this is app #2 — the node's 3.7GB RAM is now shared).
7. Add the `poesie-ci` credential + context into `/home/deploy/.kube/config` (reuse the
   existing `ledgy-k3s` cluster entry — same physical cluster).
8. DNS: `poesie-du-lundi.m3i.io` A record → the VPS IP. cert-manager issues the certificate on first
   `Ingress` apply.

Verified live (2026-09-14): `curl https://poesie-du-lundi.m3i.io/api/hello` → `200` with no
auth; `curl -I https://poesie-du-lundi.m3i.io/api/admin/` → `302` to
`https://auth.m3i.io/?rd=...`, confirming Traefik matches the more specific `/api/admin` prefix
ahead of the public `/api` rule; `openssl s_client -connect poesie-du-lundi.m3i.io:443` shows a
Let's Encrypt certificate (`poesie-tls`) for the host, issued the same day.

## Feeds

The public site publishes RSS 2.0, Atom, and JSON Feed of published poems, plus `sitemap.xml`
and `robots.txt`. These are MVP, not a follow-up.
