# API Specification

This document lists the main REST endpoints and WebSocket behavior. Use these as the contract for frontend and backend integration.

Base URL: `https://{host}/` or `http://localhost:{port}/`

## REST Endpoints

### CSV Upload
- POST `/api/upload/csv`
- Description: Accepts a CSV file and username; parses and persists transactions.
- Request:
  - Content-Type: `multipart/form-data`
  - Fields:
    - `file` — CSV file
    - `username` — string
- Response (200 OK):
  - JSON:
    - `processedCount`: number
    - `errors`: array of row-level errors (may be empty)
    - `activityMessage`: string
- Notes:
  - Server emits a WebSocket event after processing: `CsvUploaded` and `TransactionCreated` messages.
  - For large files, endpoint may return 202 Accepted and process asynchronously [TBD].

Example request (conceptual):
POST /api/upload/csv (multipart):
- file: transactions.csv
- username: "Alex"

Example response:
{
  "processedCount": 42,
  "errors": [],
  "activityMessage": "Alex uploaded a CSV (42 transactions)"
}

---

### Transactions
- GET `/api/transactions`
  - Query params: `from`, `to`, `category`, `limit`, `offset`
  - Returns paginated list of transactions
- GET `/api/transactions/{id}`
  - Returns a single transaction
- POST `/api/transactions`
  - Body: JSON { `timestamp`, `amount`, `currency`, `description`, `categoryId`, `createdBy` }
  - Creates a transaction and emits real-time events

Example GET response:
[
  {
    "id": 123,
    "timestamp": "2026-04-01T12:34:00Z",
    "amount": -50.00,
    "currency": "USD",
    "description": "Dinner",
    "categoryId": 5,
    "createdBy": "Maria"
  }
]

---

### Metrics
- GET `/api/metrics`
- Description: Returns computed metrics (total balance, category totals, alerts)
- Response example:
{
  "totalBalance": 1250.75,
  "categoryTotals": [
    { "categoryId": 1, "name": "Food", "total": 400.00 },
    { "categoryId": 2, "name": "Income", "total": 1650.75 }
  ],
  "alerts": [
    { "type": "BudgetExceeded", "message": "Budget exceeded (Food)" }
  ]
}

Notes:
- Metrics are recomputed on data changes; server pushes `MetricsUpdated` events via WebSockets.

---

### Activity Feed (historical)
- GET `/api/activity`
  - Query params: `limit`, `since`
  - Returns recent ActivityEvent entries for clients that reconnect or need history.

Example response:
[
  { "id": 987, "timestamp": "...", "actor": "Alex", "type": "CsvUploaded", "message": "Alex uploaded a CSV" },
  ...
]

## WebSocket Behavior

Endpoint: `ws://{host}/ws` or `wss://{host}/ws`

- Connection:
  - Clients open a WebSocket and send an initial `Subscribe` message with username and optional filters.
  - Server sends a welcome/ack message and then pushes events.

- Client -> Server messages (examples):
  - Subscribe
    {
      "type": "Subscribe",
      "payload": { "username": "Maria", "filters": { "categories": null } }
    }
  - CreateTransaction (optional)
    {
      "type": "CreateTransaction",
      "payload": {
        "timestamp": "2026-04-07T15:00:00Z",
        "amount": -50.00,
        "currency": "USD",
        "description": "Lunch",
        "categoryId": 3,
        "createdBy": "Maria"
      }
    }

- Server -> Clients messages (examples):
  - CsvUploaded
    {
      "type": "CsvUploaded",
      "payload": {
        "username": "Alex",
        "processedCount": 42,
        "summary": { "inserted": 40, "skipped": 2 }
      }
    }
  - TransactionCreated
    {
      "type": "TransactionCreated",
      "payload": {
        "id": 124,
        "timestamp": "2026-04-07T15:00:00Z",
        "amount": -50.00,
        "description": "Lunch",
        "categoryId": 3,
        "createdBy": "Maria"
      }
    }
  - MetricsUpdated
    {
      "type": "MetricsUpdated",
      "payload": {
        "totalBalance": 1200.75,
        "categoryTotals": [ { "categoryId":3, "total": 250.00 } ]
      }
    }
  - ActivityEvent
    {
      "type": "ActivityEvent",
      "payload": {
        "id": 555,
        "timestamp": "2026-04-07T15:01:00Z",
        "actor": "Maria",
        "type": "TransactionCreated",
        "message": "Maria added a $50 expense (Food)",
        "metadata": { "transactionId": 124 }
      }
    }

Notes:
- Event envelope uses `type` string and `payload` object.
- Clients should be resilient to duplicate events and handle idempotency when needed.
- Message schemas above are representative; exact fields and routes should match implementation. Any unspecified details are [TBD].
