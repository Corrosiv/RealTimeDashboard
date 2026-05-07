
# Roadmap / TODO

This file contains a prioritized, actionable roadmap for the RealTimeDashboard portfolio project. Each task includes a status indicator and clear acceptance criteria so progress is verifiable. The roadmap preserves phased priorities and focuses on realistic, testable improvements appropriate for a portfolio/demo application.

Solution audit summary
- Status: quick review of the repository and open files indicates the project already has: a backend API, WebSocket-based real-time updates, SQLite persistence, integration tests, and a CI workflow.
- Key gaps identified (recommended to address in Phase 1–2):
  - CSV import edge cases (row-level errors, quoted fields, embedded newlines, duplicates, mapping templates).
  - API input validation, consistent error payloads and OpenAPI contract coverage.
  - Real-time resiliency (reconnect replay, event ordering, rate-limited replay).
  - Test coverage gaps around failure scenarios, concurrency, and end-to-end WebSocket flows.
  - Lightweight DB migrations and documentation for schema changes (SQLite→server DB readiness).

## Phase 1 — Polish & Reliability (High)
- [x] Baseline platform: backend API, WebSockets, SQLite persistence, integration tests, CI
  - Description: Existing implemented baseline for the demo app.
  - Acceptance Criteria:
    - README documents current capabilities and how to run locally.
    - Integration tests exercising CSV upload and WebSocket flows are present and passing.
    - CI builds and runs tests (existing `.github/workflows/ci.yml`).

- [x] CSV import — row-level validation & structured error reporting
  - Description: Provide per-row validation results and a structured API response for import failures.
  - Acceptance Criteria:
    - `CsvImportResult` includes per-row objects with `lineNumber`, `field`, and `message`/`code`.
    - Integration test uploads a CSV with multiple bad rows and asserts all row errors are reported and no invalid transactions are persisted.
    - HTTP API returns 400 with structured payload for malformed files and 200 with partial results when configured.

- [x] CSV import — robust parsing (quoted fields, embedded newlines, missing columns)
  - Description: Harden CSV parser to handle common malformed formats and large files safely.
  - Acceptance Criteria:
    - Unit tests cover quoted fields with commas, embedded newlines, and missing/extra columns.
    - Rows missing required columns are marked as errors in import results (not silently dropped).
    - Streaming parser handles large files (e.g., 50k rows) without excessive memory usage; document threshold in README.

- [x] CSV import — duplicate detection & idempotency
  - Description: Detect duplicates on import and support skip/merge behavior to make imports idempotent.
  - Acceptance Criteria:
    - Import result reports duplicates detected using configurable key(s) (e.g., date+amount+description).
    - Endpoint supports `onDuplicate=skip|merge|error` and behavior validated by unit/integration tests.
    - Re-uploading the same file with `skip` does not create duplicate transactions (integration test).

- [x] Tests: increase coverage for edge cases and failure modes
  - Description: Add targeted unit and integration tests for parsing, concurrency, and WebSocket reconnections.
  - Acceptance Criteria:
    - New tests cover CSV edge cases, concurrent uploads (TransactionStore), and WebSocket reconnect/replay.
    - Test coverage goals documented (e.g., critical services target >=80% branch coverage) and tracked.

- [x] Transactions API — pagination and filtering
  - Description: Add cursor-based pagination and server-side filtering (date range, amount, category, text search).
  - Status: ✅ COMPLETE - All 57 tests passing
  - Implementation:
    - Cursor-based pagination using base64(timestamp:id)
    - Filtering: dateFrom, dateTo, minAmount, maxAmount, categoryId, type, search
    - Fixed sorting: timestamp:asc|desc
    - FluentValidation with field-level error reporting
    - Domain event pattern for TransactionCreatedEvent
    - Event handlers create activity feed entries and broadcast WebSocket events
    - Comprehensive integration and unit tests

- [ ] Transactions API — request validation & canonicalization
  - Description: Centralize validation for transaction DTOs and normalize date/time to UTC.
  - Acceptance Criteria:
    - FluentValidation rules applied; tests assert bad payloads yield 400 with structured errors.
    - Transaction creation is atomic; returned resource matches persisted DB state in an integration test.
    - API docs state date/time handling (UTC) and currency expectations.

- [ ] Real-time (WebSocket) resilience & ordering
  - Description: Ensure clients can recover from disconnects and request missed events in order.
  - Acceptance Criteria:
    - Reconnect protocol: client can request events since last-seen event id; server provides delta (bounded to protect memory).
    - Integration test simulates disconnect/reconnect and verifies no events are lost and ordering is preserved.
    - Server enforces a replay window/limit and returns a clear error when replay window is exceeded.

- [ ] Activity feed — server-side filtering & paging
  - Description: Support filtering by user, event type, and time range, and provide paging for the feed.
  - Acceptance Criteria:
    - Activity endpoints accept filters and return `nextCursor` for paging.
    - Tests verify filter combinations and ordering; entries include consistent schema and timestamps.

## Phase 2 — User & Data Persistence (Medium)
- [ ] Persistent user records & simple preferences
  - Description: Store optional users and lightweight preferences (displayName history, preferredCurrency).
  - Acceptance Criteria:
    - `User` table/migration exists with `displayName`, `preferredCurrency`, `createdAt`; migration script included.
    - Endpoints to read/update preferences exist and are covered by unit/integration tests.
    - Display name history retained and retrievable via a simple endpoint.

- [ ] Server-side session for display names (ephemeral identities)
  - Description: Back display name state with server sessions (cookie or short-lived token) to attribute actions reliably.
  - Acceptance Criteria:
    - Session issuance when client sets a display name; subsequent requests attribute actions to that session.
    - Integration test sets a display name, performs actions, and verifies feed attribution.
    - Sessions expire after configurable idle timeout; tests assert expiry behavior.

- [ ] Export filtered transaction views (CSV / JSON) with streaming
  - Description: Allow exporting current filtered views as downloadable CSV or JSON, using streaming for large exports.
  - Acceptance Criteria:
    - Endpoint supports export formats and returns streamed responses for large datasets.
    - Export schema documented and aligns with import schema where appropriate.
    - Integration tests verify export respects filters and pagination semantics.

- [ ] CSV import — mapping templates (import profiles)
  - Description: Save and apply column-mapping templates to support provider-specific CSV layouts.
  - Acceptance Criteria:
    - CRUD endpoints for import templates exist and are covered by unit tests.
    - Import accepts a template id and correctly applies mappings (integration test with sample provider CSVs).

- [ ] Schema versioning & migrations (SQLite-safe)
  - Description: Add migration tooling and scripts to manage schema changes safely for local and server DBs.
  - Acceptance Criteria:
    - EF migrations or lightweight migration runner included; migration history table present.
    - Dev doc describes how to run migrations locally and in CI.
    - Integration test runs migrations from baseline to current schema without data loss.

## Phase 3 — Security & Multi-user Scale (Low)
- [ ] Authentication (demo-friendly, pluggable)
  - Description: Add pluggable authentication with an in-memory/demo provider and clear extension points for OAuth.
  - Acceptance Criteria:
    - Auth middleware added; demo provider works locally and is covered by integration tests.
    - Protected endpoints return 401 for unauthenticated requests; activity feed associates identity when authenticated.

- [ ] Authorization & resource scoping
  - Description: Implement simple policies controlling who can view/export/edit transactions.
  - Acceptance Criteria:
    - Policy checks or role assertions protect critical endpoints; unit tests assert enforcement.
    - Integration tests show unauthorized users cannot mutate or export resources.

- [ ] Prepare for server DB (planning + optional validation)
  - Description: Make data access provider-agnostic and document steps to switch to PostgreSQL/SQL Server.
  - Acceptance Criteria:
    - Connection/provider abstraction exists (EF Core providers); switching provider is documented.
    - Optional CI job or script validates migrations against a PostgreSQL container (if enabled).

- [ ] Concurrency & conflict handling
  - Description: Add optimistic concurrency tokens or explicit conflict detection for multi-instance scenarios.
  - Acceptance Criteria:
    - Entities include concurrency tokens; conflicts yield 409 with retry guidance.
    - Integration test simulates concurrent writes and asserts conflict behavior.

- [ ] Basic rate limiting and input sanitization
  - Description: Deploy basic request throttling and sanitize user inputs to reduce injection risks.
  - Acceptance Criteria:
    - Rate limiting middleware configurable per-IP/session; tests assert throttling behavior.
    - Sanitization applied to text fields with unit tests checking suspicious payloads are cleaned or rejected.

## Phase 4 — Observability & DevOps (Low)
- [ ] Structured logging & request correlation
  - Description: Emit structured logs including trace/request id and session/user id to aid debugging.
  - Acceptance Criteria:
    - Logs include `traceId`, `requestId` and are emitted in structured format (JSON) by default.
    - Correlation id propagates across HTTP and WebSocket messages; integration test demonstrates end-to-end correlation.

- [ ] Health checks, metrics, and lightweight tracing
  - Description: Add `/health`, `/metrics` endpoints and basic tracing hooks for local diagnostics.
  - Acceptance Criteria:
    - `/health` reports DB connectivity and WebSocket subsystem state.
    - `/metrics` exposes request/error counts and active WS connections; Prometheus format is optional.
    - Integration test polls health and metrics and validates expected fields.

- [ ] CI improvements for reliability (migrations, integration tests)
  - Description: Harden CI to run migrations and integration tests and build artifacts reproducibly.
  - Acceptance Criteria:
    - CI pipeline runs migrations, unit/integration tests, and builds an artifact on success.
    - Pipeline avoids exposing secrets in logs; steps documented in repo.

- [ ] Benchmarks & profiling guidance
  - Description: Add small benchmark or load-test scripts for critical operations (CSV import, WS fanout).
  - Acceptance Criteria:
    - Benchmarks or simple load scripts included with documented thresholds and steps to run locally.
    - README documents how to profile locally and interpret results.

## Cross-cutting & Maintenance Tasks
- [ ] Standardize error responses and API contract
  - Description: Define a consistent error response schema and ensure controllers use it.
  - Acceptance Criteria:
    - Error schema documented and enforced by integration tests.
    - Map exceptions to HTTP status codes consistently with examples in docs.

- [ ] OpenAPI / contract tests
  - Description: Keep Swagger/OpenAPI spec accurate and add a contract test to detect accidental breaking changes.
  - Acceptance Criteria:
    - OpenAPI covers public endpoints with examples; CI step validates runtime vs spec.
    - Contract test fails if API surface changes without updating spec.

- [ ] Failure & resilience testing
  - Description: Add tests that simulate transient failures (DB down, broken WS) and verify graceful behavior.
  - Acceptance Criteria:
    - Integration tests simulate transient DB and WS failures and assert graceful degradation and retry behavior.
    - Application logs errors (non-sensitive info) and surfaces user-friendly messages.

- [ ] Documentation & onboarding
  - Description: Keep `README.md`, `API-SPEC.md`, `database-design.md`, and developer onboarding docs up to date.
  - Acceptance Criteria:
    - Each public-facing or infra change updates docs in the same PR.
    - README contains local dev steps for running the app, tests, and migrations.

Notes and prioritization guidance
- Focus the next 2–4 weeks on Phase 1 tasks: CSV robustness, import idempotency, transactions pagination, and targeted tests for WebSocket resilience.
- Pick small, testable tasks per sprint (2–3 items) that include at least one integration test to demonstrate correctness.
- Avoid overengineering: prefer simple, documented solutions (demo auth provider, EF migrations) rather than enterprise tooling.