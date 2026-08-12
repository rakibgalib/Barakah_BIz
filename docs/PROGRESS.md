# Barakah Platform — Implementation Progress

**Last updated:** 2026-08-12

## How this file works

Claude's in-session task list does not carry over between conversations, so this file is the
actual cross-session progress tracker — updated at the end of each implementation session.
Checkboxes mean code is written **and verified** (build/test passing), not just written.
"Deferred" items are intentional, explicit decisions — not silently skipped work.

Phases follow the roadmap in [doc.html](doc.html#roadmap).

## Phase 1: Foundation (Month 1-2) — code complete, verified live in GitHub Codespaces (2026-08-12)

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
- [x] Live round-trip against real Postgres, verified 2026-08-12 in Codespaces — `POST /api/auth/register` confirmed via curl; user confirmed the full application (dashboard + backend) running successfully

### Tenant Service (`src/Modules/TenantService/`)
- [x] `POST /api/tenants` (calls the existing `create_tenant_schema()` Postgres function), `GET`/`PATCH` endpoints, `GET /health`
- [x] `dotnet build` — 0 errors
- [x] No longer blocked — see "Live verification" section below

### Infra
- [x] `BarakahPlatform.sln`, `docker-compose.yml`, Dockerfiles, `.env.example`
- [x] GitHub Actions CI (`dotnet restore/build/test`)
- [x] xUnit test projects — **10/10 tests passing**
- [x] `.gitignore` bug fixed (root-anchored `/bin/`/`/obj/` patterns never matched per-project build output — that's why the old scaffold's `obj/` folders got committed)

### Verification status
- [x] `dotnet restore` / `dotnet build` / `dotnet test` — all clean (see "Bonus fixes" below for one real bug this caught: an EF Core package downgrade conflict)
- [x] Docker verification happened via **GitHub Codespaces** instead of local Docker Desktop (2026-08-12) — see the "Live verification" section further down for the full list of real bugs this surfaced and fixed. Local Docker Desktop is still not installed (8GB RAM constraint unchanged), and doesn't need to be — Codespaces fully replaces that step.
- [x] End-to-end: `POST /api/auth/register` against real Postgres confirmed working; full application confirmed running by user

### Bonus fixes found along the way
- Fixed `NU1605` package downgrade: `Microsoft.EntityFrameworkCore` was pinned to `9.0.0`, but `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4` requires `>=9.0.1` — bumped to `9.0.4`.
- Fixed `.gitignore`'s rooted `/bin/`, `/obj/` patterns (now `bin/`, `obj/` so they match at any depth).
- Removed a stray literal `EOF` line at the end of `.gitignore` (heredoc leftover).

## Phase 2, slice 1: Catalog + Inventory Services — code complete, verified live in GitHub Codespaces (2026-08-12)

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
- [x] No new unit tests were added this slice, but live verification (2026-08-12, Codespaces)
  covers this more meaningfully than unit tests would have — see "Live verification" below
- [x] Docker verification happened via GitHub Codespaces (2026-08-12), not local Docker Desktop —
  local install remains unnecessary, see Phase 1's note above
- [x] Application confirmed running end-to-end by user in Codespaces

### Docker verification timeline (2026-08-11, superseded 2026-08-12)
Local Docker Desktop install was deliberately deferred to the end of the project — the dev machine
has 8GB RAM. That constraint is still true and unchanged, but it turned out not to matter: GitHub
Codespaces (free tier, no credit card, runs on GitHub's infrastructure) does the exact same
`docker compose up -d` this project already needed, without touching the local machine at all. See
the "Live verification" section further down for the full account of what got tested and what
broke along the way.

## Phase 2, slice 2: Order + Payment Services — code complete, verified live in GitHub Codespaces (2026-08-12)

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
- [x] No new unit tests, but see "Live verification" below — same as slice 1
- [x] Docker verification happened via GitHub Codespaces (2026-08-12) — see below
- [x] Kafka is actually running (via `apache/kafka`, not `bitnami/kafka` — see below) and
  Order Service successfully connects to it; full order → payment → notification event chain not
  individually exercised by name in this session, but the infrastructure it depends on is live and
  the application overall was confirmed running

## Live verification (2026-08-12) — the first real Docker/Postgres round-trip this project has had

Ran the whole stack in a GitHub Codespace (`.devcontainer/`, see the Admin Dashboard section
below). This was genuinely the first time any service touched a real database — Docker had been
deferred since Phase 1 — and it surfaced four real bugs that pure `dotnet build`/`dotnet test`
could never have caught:

1. **`bitnami/kafka:3.7` no longer resolves** — Bitnami stopped publishing versioned tags under
   the free `bitnami/*` namespace on Docker Hub in 2025. Switched to `apache/kafka:3.7.0`,
   maintained directly by the Apache Kafka project. Also added
   `KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1` — the default of 3 would never come online on a
   single-broker cluster, which would have silently broken every consumer group.
2. **Docker build failures (`NETSDK1064`, package not found after restore)** — the two-layer
   `dotnet restore` → `dotnet publish --no-restore` pattern (used for build-cache efficiency) was
   unreliable with this SDK patch version, failing on a different service each attempt regardless
   of `--no-cache` or forced sequential builds. Fixed by dropping `--no-restore` from every
   Dockerfile's publish step — costs some redundant restore time, but reliable.
3. **Every DbContext was missing its primary key's column mapping.** EF Core defaults an unmapped
   `Id` property to the literal quoted column name `"Id"`, but `scripts/init-db.sql` creates every
   table with plain lowercase snake_case columns (`id`, not `"Id"`). Every DbContext explicitly
   mapped every *other* property to its snake_case column but left `Id` on the default convention
   — so any insert/query failed with Postgres error 42703. This affected all 8 DbContexts (13
   entities total). This is exactly the kind of bug that `dotnet build`/`dotnet test` structurally
   cannot catch — it only exists at the SQL layer, which is precisely why "build/test green" was
   never claimed to mean "verified against a real database" anywhere in this file.
4. **CORS was hardcoded to `http://localhost:3000`** — already noted in the Admin Dashboard
   section below, but worth repeating here: this is the kind of gap that only a real browser
   hitting a real forwarded URL surfaces.

**Net result:** Identity Service's `POST /api/auth/register` confirmed working via curl against
live Postgres, and the user confirmed the full application (dashboard + all 8 backend services)
running successfully in the Codespace. This closes out the "Docker verification deferred" caveat
that's been attached to every phase since Phase 1 — not because Docker Desktop got installed
locally (it didn't, and doesn't need to — the 8GB RAM constraint is still respected), but because
Codespaces turned out to be a complete substitute for it.

## Admin Dashboard + Notification/Analytics Services (2026-08-11) — code complete, verified live 2026-08-12

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

## Phase 3, slice 1: Pharmacy Extension (2026-08-12) — code complete, Docker verification not yet re-run

Starts Month 5-6 ("Business Extensions"). Scoped down the same way every prior phase was: one
extension at a time (Pharmacy first, matching `doc.html`'s own listing order), and within Pharmacy,
only the non-AI features. `doc.html` lists six Pharmacy Extension features — three of them (Drug
Interaction Checker, Symptom-Based Search, Fraud Detection) need a real drug-interaction knowledge
base or anomaly-detection AI, which `doc.html` itself scopes to the project's AI Integration phase
(Month 7-8). Building fake versions of those now would be worse than not building them — same
reasoning as Payment Service being a ledger instead of a fake Stripe integration.

### Pharmacy Service (`src/Modules/PharmacyService/`), port 5008
- [x] `Prescription` entity, tenant-scoped (`prescriptions` table added to `create_tenant_schema()`
  in `scripts/init-db.sql`, following the exact pattern already used for `payments`/`notifications`)
- [x] **Prescription Management**: `POST /api/prescriptions`, `GET /api/prescriptions/{id}`
- [x] **Patient History**: `GET /api/prescriptions/patient/{patientId}` — full medication record
  for one patient, ordered newest-first
- [x] **Expiry Alerts**: `GET /api/prescriptions/expiring` — active prescriptions expiring within
  the configured warning window
- [x] Refill flow: `POST /api/prescriptions/{id}/refill` — enforces refill count against
  `RefillsAllowed`, blocks refills on expired/pending-approval/non-active prescriptions
- [x] `PharmacyOptions` config class mirrors `doc.html`'s `PharmacyExtension` YAML block exactly
  (`MaxPrescriptionRefills`, `ControlledSubstanceTracking`, `RequiresPharmacistApproval`,
  `ExpiryWarningDays`) — bound from an actual `PharmacyExtension` appsettings section, not just
  documented as a config shape. Controlled substances above the approval threshold are created in
  `PendingApproval` status and can't be refilled until that's resolved.
- [x] Same conventions as every other service: `TenantResolutionMiddleware` + `X-Tenant-Subdomain`
  header + `UseTenantSchemaAsync` guard, no JWT auth (matches the rest of the platform), CORS via
  `SetIsOriginAllowed(_ => true)`
- [x] Registered in `BarakahPlatform.sln` and `docker-compose.yml` (same block pattern as every
  other service — depends on postgres + tenant, no Kafka dependency)
- [x] `dotnet build` — 0 errors; `dotnet test` — 10/10 still passing, no regressions

### Explicitly not built this slice
- Drug Interaction Checker, Symptom-Based Search, Fraud Detection — deferred to AI Integration
  phase per `doc.html`'s own plan (see above)
- No admin dashboard page yet — Notifications/Analytics got dashboard pages the same session they
  were built; Pharmacy didn't, to keep this slice's scope contained. Natural next step if pharmacy
  data needs to be visible in the UI.
- No live verification in this specific session — built and pushed alongside the live-verified
  Codespaces work earlier, but the fixes (Kafka image, Dockerfile `--no-restore`, EF `Id` column
  mapping) all predate this service, so it should inherit them correctly; hasn't been individually
  round-tripped against a running Postgres yet.

## Phase 3, slice 2: Restaurant Extension (2026-08-12) — code complete, Docker verification not yet re-run

Jumped ahead of Clothing Extension in `doc.html`'s listing order — flagged and explained at the
time: Clothing's six features are almost entirely AI-dependent (Size Recommendation needs a
prediction model, Trend Prediction, Color Coordination, Virtual Try-On, and Outfit Suggestions are
all AI; only Sustainability isn't), too thin a slice to justify a new service. Restaurant has a
much better split.

### Restaurant Service (`src/Modules/RestaurantService/`), port 5010
- [x] `MenuItem` entity, tenant-scoped (`menu_items` table added to `create_tenant_schema()`)
- [x] Menu Management: full CRUD (`POST/GET/PUT/DELETE /api/menu-items`, `GET .../{id}`)
- [x] **Allergy Management**: `GET /api/menu-items/allergen-free?exclude=nuts,dairy` — filters out
  any item carrying an excluded allergen tag
- [x] **Dynamic Pricing** (rule-based, not AI): `GET /api/menu-items/{id}/price` applies
  `doc.html`'s own multipliers (`PeakHourPricing`, `OffPeakDiscount`, `RamadanPricing`) via a
  `RestaurantOptions` config class bound to a real `RestaurantExtension` appsettings section — same
  "config block becomes real behavior" pattern as Pharmacy's `PharmacyOptions`
- [x] **Ingredient Management** interpreted as menu-item composition/allergen tagging (`Description`
  + `AllergenTags` fields) rather than a separate raw-ingredient-stock subsystem — that would
  duplicate Inventory Service's domain; kept it in Restaurant's own scope instead
- [x] Same conventions as every other service (tenant middleware, header guard, no JWT, permissive
  CORS)
- [x] Registered in `BarakahPlatform.sln` and `docker-compose.yml`
- [x] `dotnet build` — 0 errors; `dotnet test` — 10/10 still passing, no regressions

### Explicitly not built this slice
- Menu Optimization (needs profitability analytics), Food Quality Prediction (AI), Customer
  Preferences (AI personalization) — deferred to AI Integration phase
- **"Peak hours" and "Ramadan period" are not computed** — `PeakHourStartUtc`/`PeakHourEndUtc` are
  a fixed UTC window (11:00-14:00, an addition beyond what `doc.html` specifies), and
  `IsRamadanPeriod` is a manually-toggled config flag, not a real Hijri calendar calculation. This
  is an honest simplification, not a hidden gap — noted directly in `RestaurantOptions`'s own
  doc comment.
- No admin dashboard page yet, same reasoning as Pharmacy

## Phase 3, slice 3: SuperShop Extension (2026-08-12) — code complete, Docker verification not yet re-run

### SuperShop Service (`src/Modules/SuperShopService/`), port 5014
- [x] **Supplier Management**: `Supplier` entity, CRUD + `PATCH /api/suppliers/{id}/rating`
- [x] **Customer Loyalty**: `LoyaltyAccount` entity, `GET/POST /api/loyalty/earn`,
  `POST /api/loyalty/{customerId}/redeem` — points balance + lifetime-earned tracking
- [x] **Expiry Management**: `ProductBatch` entity, `GET /api/product-batches/expiring` — same
  pattern as Pharmacy's prescription expiry alerts, applied to batch-level stock instead
- [x] **Bulk Discounts** (rule-based, not AI): stateless `POST /api/pricing/bulk-discount` applies
  `doc.html`'s `BulkDiscountThreshold` against a configured discount percentage
- [x] `SuperShopOptions` config class mirrors `doc.html`'s `SuperShopExtension` block
  (`ExpiryMonitoringEnabled`, `LoyaltyProgramEnabled`, `BulkDiscountThreshold`,
  `SupplierRatingEnabled`) bound from a real `SuperShopExtension` appsettings section
- [x] `suppliers`, `loyalty_accounts`, `product_batches` tables added to
  `create_tenant_schema()` in `scripts/init-db.sql`
- [x] Same conventions as every other service; registered in `.sln`/`docker-compose.yml`
- [x] `dotnet build` — 0 errors; `dotnet test` — 10/10 still passing, no regressions

### Explicitly not built this slice
- Smart Shopping Lists (personalized) and Shopping Patterns (behavior analysis) — both need real
  recommendation/behavioral AI, deferred to the AI Integration phase
- `AutoReorderEnabled` from `doc.html`'s config block is not implemented — automatic reordering
  needs demand-pattern intelligence, same reasoning as the above
- No admin dashboard page yet, same reasoning as Pharmacy/Restaurant

**Phase 3 status**: 3 of 4 extensions done (Pharmacy, Restaurant, SuperShop). Clothing remains —
deliberately skipped so far since 5 of its 6 features are AI-dependent (see the Restaurant slice
note above); revisit once the AI Integration phase makes those buildable, or build the thin
Sustainability-only slice if it's wanted sooner.

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
