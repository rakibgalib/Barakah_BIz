# Barakah Platform — Implementation Progress

**Last updated:** 2026-08-11

## How this file works

Claude's in-session task list does not carry over between conversations, so this file is the
actual cross-session progress tracker — updated at the end of each implementation session.
Checkboxes mean code is written **and verified** (build/test passing), not just written.
"Deferred" items are intentional, explicit decisions — not silently skipped work.

Phases follow the roadmap in [doc.html](doc.html#roadmap).

## Phase 1: Foundation (Month 1-2) — code complete, Docker verification deferred

### Shared libraries (`src/Shared/`)
- [x] `Barakah.SharedKernel` — Entity/AggregateRoot/ValueObject/Result
- [x] `Barakah.Persistence` — tenant-schema `search_path` switching, generic repository
- [x] `Barakah.TenantContext` — tenant resolution middleware (wired into Catalog/Inventory in Phase 2 slice 1, see below)
- [x] `Barakah.EventBus` — Kafka-backed `IEventBus`, best-effort publish. Producer-only as of Phase 2 slice 2: Order Service publishes `OrderCreatedIntegrationEvent`, no consumer exists yet (Notification/Analytics services aren't built)

### Database
- [x] `scripts/init-db.sql` — added `public.users`, `public.refresh_tokens` (were missing; `user_roles` referenced a table that didn't exist)

### Identity Service (`src/Modules/IdentityService/`)
- [x] `POST /api/auth/{register,login,refresh,logout}`, `GET /api/users/me`, `GET /health`
- [x] JWT (access + refresh) + BCrypt password hashing
- [x] `dotnet build` — 0 errors
- [x] Unit tests passing (password hashing round-trip, JWT generation, refresh-token hashing)
- [ ] Live round-trip against a running Postgres/Citus instance — **deferred, see below**

### Tenant Service (`src/Modules/TenantService/`)
- [x] `POST /api/tenants` (calls the existing `create_tenant_schema()` Postgres function), `GET`/`PATCH` endpoints, `GET /health`
- [x] `dotnet build` — 0 errors
- [ ] Live round-trip against a running Postgres/Citus instance — **deferred, see below**

### Infra
- [x] `BarakahPlatform.sln`, `docker-compose.yml`, Dockerfiles, `.env.example`
- [x] GitHub Actions CI (`dotnet restore/build/test`)
- [x] xUnit test projects — **10/10 tests passing**
- [x] `.gitignore` bug fixed (root-anchored `/bin/`/`/obj/` patterns never matched per-project build output — that's why the old scaffold's `obj/` folders got committed)

### Verification status
- [x] `dotnet restore` / `dotnet build` / `dotnet test` — all clean (see "Bonus fixes" below for one real bug this caught: an EF Core package downgrade conflict)
- [ ] **Docker Desktop install + `docker compose up`** — explicitly deferred by user on 2026-08-09
- [ ] End-to-end: `POST /api/tenants` → `POST /api/auth/register` → `POST /api/auth/login` against real Postgres — blocked on the above

### Bonus fixes found along the way
- Fixed `NU1605` package downgrade: `Microsoft.EntityFrameworkCore` was pinned to `9.0.0`, but `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4` requires `>=9.0.1` — bumped to `9.0.4`.
- Fixed `.gitignore`'s rooted `/bin/`, `/obj/` patterns (now `bin/`, `obj/` so they match at any depth).
- Removed a stray literal `EOF` line at the end of `.gitignore` (heredoc leftover).

## Phase 2, slice 1: Catalog + Inventory Services — code complete, Docker verification deferred

Month 3-4 ("Core Services") is Catalog, Inventory, Order, Payment, plus an Admin dashboard —
scoped down the same way Phase 1 was scoped to "Foundation." This slice is Catalog + Inventory
only; Order needs both to exist first (product lookup + stock deduction) and Payment needs Order,
so those are the next slice. The Admin dashboard (React/Next.js) is a separate stack, out of
scope for a backend pass.

**This is also the first time multi-tenancy is actually exercised** — Identity/Tenant only ever
touched the shared `public` schema. Catalog/Inventory read/write the per-tenant `tenant_{id}`
schema, so `Barakah.TenantContext`'s `TenantResolutionMiddleware` and `Barakah.Persistence`'s
`UseTenantSchemaAsync` (built in Phase 1, left unwired) are wired in here for the first time.

### Catalog Service (`src/Modules/CatalogService/`), port 5004
- [x] `Product` entity mapped to the per-tenant `products` table (schema resolved at runtime via
  `search_path`, not Fluent API — unlike Identity/Tenant's `public`-schema mapping)
- [x] `POST/GET/GET{id}/GET by-sku/{sku}/PUT{id}/DELETE{id} /api/products`, `GET /health`
- [x] `dotnet build` — 0 errors

### Inventory Service (`src/Modules/InventoryService/`), port 5005
- [x] `InventoryItem` entity mapped to the per-tenant `inventory` table (`product_id` as a plain
  FK `Guid`, no cross-service EF navigation — Inventory doesn't own the `products` table)
- [x] `GET /api/inventory/branch/{branchId}`, `GET /api/inventory/{productId}/{branchId}`,
  `POST /api/inventory`, `PATCH /api/inventory/{id}/adjust` (core stock-adjustment op),
  `GET /api/inventory/low-stock`, `GET /health`
- [x] `dotnet build` — 0 errors

### Tenant resolution (shared, both services)
- [x] `Barakah.TenantContext.HttpTenantResolver` — new shared `ITenantResolver` implementation
  (HTTP call to Tenant Service's `GET /api/tenants/by-subdomain/{subdomain}`), added to the
  shared library instead of duplicating a client class per service as originally planned
- [x] `TenantResolutionMiddleware` wired into both services' `Program.cs` pipelines
- [x] `docker-compose.yml`: both services added (ports 5004/5005), `Services__TenantService`
  pointing at the `tenant` container

### Verification status
- [x] `dotnet restore` / `dotnet build` / `dotnet test` — all clean, 10/10 pre-existing tests
  still passing (no regressions)
- [ ] No new unit tests this slice — Catalog/Inventory are CRUD over EF Core against a schema
  that doesn't exist without a running Postgres; meaningful coverage here is integration tests,
  blocked on the same deferred Docker verification as below
- [ ] **Docker Desktop install + `docker compose up`** — deferred until the end of the project
  (see below)
- [ ] End-to-end: `POST /api/products` → `POST /api/inventory` → `PATCH .../adjust` with a real
  `X-Tenant-Subdomain` header resolving through the live Tenant Service — blocked on the above

### Docker verification timeline (2026-08-11)
Deliberately deferred to the end of the project, not just "later" — the dev machine has 8GB RAM,
and running Docker Desktop alongside everything else would interfere with other work on this
machine. All services are being built and verified via `dotnet build`/`dotnet test` only until
then; live multi-container round trips (task #8) wait until Docker is installed near project end.

## Phase 2, slice 2: Order + Payment Services — code complete, Docker verification deferred

Finishes Month 3-4 ("Core Services"). This is where `Barakah.EventBus` finally gets used
(producer side only — see below) and Kafka joins `docker-compose.yml`.

**Schema gap closed:** `scripts/init-db.sql`'s `create_tenant_schema()` had `orders`/`order_items`
but no `payments` table — added one (`id`, `tenant_id`, `order_id`, `amount`, `method`, `status`,
`transaction_reference`, timestamps) plus an index on `order_id`.

### Order Service (`src/Modules/OrderService/`), port 5006
- [x] `Order`/`OrderItem` entities mapped to the existing `orders`/`order_items` tables (no schema
  change needed — they were already created by Phase 1's `create_tenant_schema()`)
- [x] `POST /api/orders` — the real orchestration: fetches current price from Catalog (not
  client-supplied, to prevent order-total manipulation), validates + deducts stock via Inventory's
  existing `/adjust` endpoint (its negative-stock guard is the real stock check — no separate
  reservation system), creates the order, then publishes `OrderCreatedIntegrationEvent`
- [x] `GET /api/orders/{id}`, `GET /api/orders/branch/{branchId}`, `PATCH /api/orders/{id}/status`,
  `PATCH /api/orders/{id}/payment-status` (the last one added specifically for Payment Service to
  call back into), `GET /health`
- [x] `CatalogClient`/`InventoryClient` — typed `HttpClient` wrappers, same pattern as
  `HttpTenantResolver`
- [x] `dotnet build` — 0 errors

### Payment Service (`src/Modules/PaymentService/`), port 5007
- [x] `Payment` entity mapped to the new `payments` table — a transaction ledger, not a real
  Stripe integration (that needs API keys and a business decision this wasn't the moment for;
  this builds the data model/endpoints Stripe would plug into later)
- [x] `POST /api/payments` (records a payment, then calls Order Service's new
  `payment-status` endpoint to sync — best-effort, not transactional: a failed sync doesn't roll
  back the payment record), `GET /api/payments/{id}`, `GET /api/payments/order/{orderId}`,
  `GET /health`
- [x] `dotnet build` — 0 errors

### EventBus wiring — producer-only, honestly incomplete
`OrderCreatedIntegrationEvent` (in `Barakah.EventBus`) is published after every successful order.
**Nothing consumes it yet** — Notification and Analytics services, which `doc.html` describes as
the actual consumers (order confirmation → inventory update → notification), aren't built. This
is a deliberate half-step, not a finished pipeline — don't read "EventBus wired in" as "the
event-driven flow works end-to-end."

### Infra
- [x] Both services added to `BarakahPlatform.sln` and `docker-compose.yml` (ports 5006/5007)
- [x] `kafka` service added to `docker-compose.yml` — single-node KRaft broker
  (`bitnami/kafka:3.7`, no separate Zookeeper container)
- [x] Dockerfiles matching the existing pattern

### Verification status
- [x] `dotnet restore` / `dotnet build` / `dotnet test` — all clean, 10/10 pre-existing tests
  still passing (no regressions)
- [ ] No new unit tests — same reasoning as slice 1: these are orchestration/CRUD endpoints that
  need a live Postgres + running Catalog/Inventory services to test meaningfully
- [ ] **Docker Desktop** — still deferred to end of project (8GB RAM constraint, see above)
- [ ] End-to-end: create order → verify stock deducted → record payment → verify order's
  `payment_status` updated, with Kafka actually running and the event actually publishing —
  blocked on Docker, folded into task #8

## Admin Dashboard + Notification/Analytics Services (2026-08-11) — code complete, live-data verification deferred

Closes out the rest of Phase 2 "Core Services" (Admin dashboard) and adds the two services that
finally consume `OrderCreatedIntegrationEvent`, which Order Service has been publishing to Kafka
since Phase 2 slice 2 with no consumer until now.

**Assumption made and not yet confirmed by the user** (two `AskUserQuestion` prompts went
unanswered mid-session): the dashboard calls the 6 backend services directly with CORS enabled,
rather than building the documented-but-never-implemented Ocelot API Gateway first. Revisit if
that's not the right call — no gateway exists in code, only in `doc.html`'s architecture diagram.

### Admin Dashboard (`src/Web/Barakah.Admin`) — Next.js 16 + TypeScript + Tailwind
- [x] Login/refresh/logout against Identity Service's real contract (15-min access token, 30-day
  refresh, no cookies — token stored client-side, `apiFetch` auto-refreshes before expiry)
- [x] Tenant switcher (`X-Tenant-Subdomain` header on every tenant-scoped call, matching how the
  backend already expects any HTTP client to behave — no new backend auth concept introduced)
- [x] Pages: Dashboard overview (low-stock + recent-orders cards + sales-summary widget + CSV
  export), Tenants (list/create), Products, Inventory (+ adjust), Orders (+ detail + status
  update), Payments (view-only), Notifications (view-only)
- [x] `npm run build` and `npm run lint` — both clean, 0 errors
- [x] Verified in-browser via the dev server: login form validation works, unauthenticated visits
  to protected routes correctly redirect to `/login`, failed API calls show a clean "Failed to
  reach the X service — is it running?" message instead of crashing (there is no live backend to
  test real data against — see Docker note below)
- **Known backend-contract gaps surfaced while building this** (not something to silently paper
  over): Tenant Service has no "list all tenants" endpoint (only create/get-by-id/by-subdomain);
  Inventory and Order services have no tenant-wide list, only "by branch" (there's no Branch
  entity/service in this backend, so the dashboard asks the admin to type in a Branch Id); Payment
  Service only lists payments by order, not by tenant. The dashboard works around all three by
  scoping its UI to what the API actually offers rather than assuming endpoints that don't exist.

### Notification Service (`src/Modules/NotificationService`), port 5011
- [x] `Barakah.EventBus` gained `IEventSubscriber`/`KafkaEventSubscriber` (purely additive — first
  consumer-side capability in a library that was publish-only since Phase 2 slice 2)
- [x] `OrderCreatedConsumer` (BackgroundService) subscribes to the `order-created` Kafka topic and
  writes a `Notification` row per order — **simulated send only** (marks `Sent` immediately, logs
  it; no real email/SMS provider call, same "ledger not a real integration" precedent Payment
  Service already set)
- [x] `GET/POST /api/notifications`, `GET /api/notifications/{id}`, `GET /health`
- [x] `notifications` table added to `create_tenant_schema()` in `scripts/init-db.sql`
- [x] `dotnet build` — 0 errors

### Analytics Service (`src/Modules/AnalyticsService`), port 5012
- [x] Read-only aggregation over the existing `orders`/`payments` tables (own local entity classes,
  no cross-service project reference — same "services never share entity types" convention as
  everywhere else in this codebase)
- [x] `GET /api/analytics/sales-summary?from&to`, `GET /api/analytics/export/orders.csv`
- [x] `dotnet build` — 0 errors
- **Scoped down from `doc.html`'s full description** (BI dashboards, cross-tenant aggregation,
  Elasticsearch) to what's buildable without new infra — no ES in `docker-compose.yml` today, and
  cross-tenant "anonymized" aggregation is a privacy-design question left open, not solved here

### Wiring
- [x] Both services registered in `BarakahPlatform.sln` and added to `docker-compose.yml`
  (following the exact block pattern every other service already uses)
- [x] CORS (`http://localhost:3000`) added to all 6 pre-existing services' `Program.cs` so the
  dashboard can call them directly — no `appsettings.json` changes needed, origin is hardcoded
- [x] Full solution `dotnet build` (0 errors) and `dotnet test` (10/10 passing, no regressions)
  after every change above

### Verification status
- [x] Everything above is `dotnet build`/`npm run build` verified, and the dashboard's static UI
  (routing, auth guard, form validation, error states) was click-tested in a real browser
- [ ] **No live data round-trip anywhere in this session's work** — same Docker-deferral boundary
  as every prior phase. Order Service publishing → Notification Service actually consuming from a
  real Kafka broker, Analytics actually summing real orders, the dashboard actually logging in
  against a real Identity Service — none of this can be exercised until Postgres/Kafka are running
- [ ] Gateway-vs-direct-CORS-calls decision (see Assumption above) — not yet confirmed by the user

## Open decisions carried forward
- Subscription pricing (Basic/Professional/Enterprise/Pharmacy/Restaurant/Clothing/SuperShop) is "TBD" in `doc.html` — set when ready.
- Citus-from-day-one vs. deferring sharding — flagged as an assumption in `doc.html`, not yet revisited.
- **Brand color mismatch** (found via graphify, see below): `brand/README.md` states Islamic Green (`#009246`) as the primary brand color, but `docs/doc.html` actually renders in navy (`#0f2b3d`) and gold (`#f3d37c`). Not yet reconciled — decide which is authoritative.
- **No API Gateway built** — `doc.html` documents an Ocelot gateway on port 5000, but it has never been implemented; the admin dashboard calls all 6 backend services directly instead (see Admin Dashboard section above). Revisit if a single ingress point becomes necessary.
- **Dashboard redesign (2026-08-11)** — applied a real design direction (refined Islamic-green brand palette, warm neutral tones, gold accent, dense/quiet SaaS layout) across the whole admin dashboard, replacing the default-Tailwind look. Tokens live in `src/Web/Barakah.Admin/src/app/globals.css`.
- **CORS opened to any origin (2026-08-11)** — all 8 backend services now use `SetIsOriginAllowed(_ => true)` instead of a hardcoded `localhost:3000` allowlist, so the dashboard works both locally and from a GitHub Codespaces forwarded URL (see below). No credentials/cookies are used (Bearer tokens only), so this is safe for the current dev-only stage — tighten before any real deployment.

## Tooling: GitHub Codespaces (2026-08-11)

Added `.devcontainer/` so the whole stack (Postgres/Citus, Redis, Kafka, all 8 backend services,
the dashboard) can run for free in the cloud without touching the user's 8GB RAM constraint — see
[[docker_deferred_ram]] in memory, this is testing infrastructure, not a reversal of the "Docker
deferred locally" decision.
- `.devcontainer/devcontainer.json` — dotnet 9 + Node 22 + Docker-in-Docker features, requests a
  4-core/16GB machine, forwards all 12 ports (dashboard, 8 services, Postgres, Redis, Kafka)
- `.devcontainer/post-create.sh` — one-time setup: `dotnet restore`, `npm install`, seeds `.env`/`.env.local` from the `.example` files
- `.devcontainer/post-start.sh` — every start: rewrites the dashboard's `NEXT_PUBLIC_*_URL` env vars
  to the Codespace's forwarded HTTPS URLs (`$CODESPACE_NAME`-derived) instead of `localhost` — a
  client-side fetch to `localhost:5001` from the viewer's real browser would hit their own machine,
  not the Codespace, so this had to be dynamic
- **Not yet verified end-to-end inside an actual Codespace** (only built/lint-checked locally) —
  first real Codespaces run should confirm `docker compose up -d` succeeds and the dashboard can
  actually reach all 8 services through their forwarded URLs

## Tooling: repo knowledge graph (2026-08-09)

Ran `/graphify` on the whole repo (44 files, ~9,700 words) — a navigable map of how the codebase, docs, and planning files connect. Outputs, not checked into git (regenerate anytime with `/graphify`):
- `graphify-out/graph.html` — interactive graph, open directly in a browser
- `graphify-out/GRAPH_REPORT.md` — plain-language audit (god nodes, communities, surprising connections)
- `graphify-out/graph.json` — raw graph data

375 nodes, 608 edges, 15 communities. Confirmed the codebase and the four planning docs (`README.md`, `brand/README.md`, `docs/doc.html`, `docs/PROGRESS.md`) are consistently cross-referenced, and surfaced the brand-color mismatch noted above as its one real finding.
