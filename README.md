# RealTimeDashboard

A recruiter-ready, real-time financial dashboard demo that shows multi-user interaction, CSV ingestion, live activity feed, and backend design.

## Overview
- Upload financial transactions via CSV
- Track total balance and financial metrics
- Categorize transactions and show category breakdowns
- Generate simple financial insights (spend summaries, alerts)
- Multi-user interaction using simple usernames (no authentication)
- Real-time synchronization across browser tabs and users via WebSockets
- Live Activity Feed that reflects user and system events instantly

## UI structure
- Sidebar
  - Upload CSV
  - Metrics
  - Insights
  - Settings (optional)
- Main Dashboard
  - Total Balance
  - Recent Transactions
  - Category Breakdown (charts)
  - Alerts (e.g., overspending)
- Right Panel
  - Live Activity Feed (real-time)

## Tech stack
- Backend: ASP.NET Core Web API (targets .NET 10)
- Real-time: WebSockets
- Database: SQLite (local/demo)
- Testing: xUnit
- CI: GitHub Actions (build + test + validation)
- Frontend: [TBD] (simple SPA or static client; see /docs for details)

## What this project demonstrates
- Real-time communication patterns and event delivery
- Backend design for streaming updates and state synchronization
- CSV processing and transactional data handling
- Clean, recruiter-friendly code organization and testing
- An activity feed architecture for live UX

## Quick start
1. Clone the repo:
   - `git clone https://github.com/<owner>/RealTimeDashboard.git`
2. Open solution in Visual Studio 2026 (Community or above).
3. In a terminal (PowerShell preferred), from solution root:
   - `cd RealTimeDashboard`
   - `dotnet build`
   - `dotnet run --project RealTimeDashboard.API`
4. Open the frontend (see /docs for client instructions) or navigate to the configured UI host.
5. Use a username (no sign‑in required) and open multiple tabs to observe real-time sync.

## Example usage flow
1. User "Alex" uploads a CSV of transactions via Sidebar → Upload CSV.
2. Backend processes CSV: transactions are parsed, categorized, stored.
3. Backend emits events over WebSockets: new transactions, metrics updates, and activity events.
4. All connected clients receive events and update:
   - Main Dashboard: totals and recent transactions refresh
   - Category charts update
   - Right Panel: "Alex uploaded a CSV" appears in the Live Activity Feed

Real-time behavior: All UI changes triggered by server events are pushed to all connected clients immediately over WebSockets (no page refresh required).

For detailed architecture, endpoints, and developer docs see the `docs/` folder:

- `docs/system-overview.md` — high-level architecture and data flow
- `docs/API-SPEC.md` — REST and WebSocket contracts
- `docs/domain-model.md` — core entities and relationships
- `docs/database-design.md` — SQLite schema and constraints
