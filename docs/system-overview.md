# System Overview

A concise, high-level description of the system architecture and responsibilities.

## Architecture summary
- Single backend service: `RealTimeDashboard.API` (ASP.NET Core Web API).
- Clients: single-page app or browser tabs connecting to backend via HTTP and WebSockets.
- Local persistence: SQLite for demo/local use.
- Real-time transport: WebSockets for event delivery and UI synchronization.

## Backend responsibilities
- Accept and validate CSV uploads, parse transactions, and persist them.
- Manage transaction lifecycle: create, categorize, compute metrics.
- Generate and persist activity feed events for user actions and system alerts.
- Push real-time events to connected clients via WebSockets.
- Provide REST APIs for transaction queries and metrics.

## Real-time communication (WebSockets)
- Single WebSocket endpoint (e.g., `/ws` or `/api/ws`) accepts client connections.
- Clients subscribe to server events and optionally send minimal actions (e.g., add transaction).
- Server broadcasts relevant events (transaction added, CSV processed, metrics updated, alerts) to connected clients.
- Event messages use a lightweight JSON envelope describing event type and payload.

## Data flow
1. CSV upload (HTTP POST) → backend receives file and username.
2. CSV processing: parse rows → map to transactions → categorize (rules or heuristics).
3. Persist transactions to SQLite in a transaction-safe manner.
4. Compute updated metrics and detect alerts (e.g., budget exceeded).
5. Create ActivityEvent entries and push real-time messages to clients.
6. Clients update UI components (dashboard, charts, activity feed) on receipt.

## Multi-user synchronization model
- Username-only identification (no auth).
- All connected clients receive the same broadcast events; clients can locally filter events for display (e.g., show only actions from specific users).
- The server is authoritative for persisted state; clients are thin renderers that react to server events.

## Activity feed generation
- Activity events are created for user-triggered actions (CSV upload, manual transaction add) and notable system events (alerts, batch processing).
- Each ActivityEvent includes: timestamp, actor (username or system), event type, and a short human-friendly message.
- Feed updates are broadcast to all clients in real time; historical feed is available via REST for clients that reconnect.
