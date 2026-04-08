# Roadmap / TODO

This file lists prioritized enhancements and meaningful next steps.

## Phase 1 — Polish & Reliability (High)
- Improve CSV validation and error reporting (row-level errors)
- Add pagination and filtering for transactions endpoints
- Improve unit and integration test coverage (edge cases)
- Improve UI accessibility and responsive layout
- Add richer activity feed filtering (by user, type, time)

## Phase 2 — User & Data Persistence (Medium)
- Optional persistent user records (username history) and preferences
- Server-side session handling for user display names [TBD]
- Export filtered transaction views (CSV/JSON)
- Import mappings for CSV column templates

## Phase 3 — Security & Multi-user Scale (Low)
- Add authentication & authorization (OAuth / external providers) — optional for demo
- Move from SQLite to a server DB for multi-instance deployments
- Add rate limiting and input sanitization hardening

## Phase 4 — Observability & DevOps (Low)
- Structured logging and request tracing
- Production-ready CI/CD pipeline for deployment
- Performance profiling and benchmarking

Notes:
- Items marked [TBD] require product decisions or stakeholder input.
- Prioritization assumes this repo is used as a portfolio/demo; feature complexity should be balanced with clarity for reviewers.
