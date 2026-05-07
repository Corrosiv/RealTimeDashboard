# API Specification

This document lists the main REST endpoints and WebSocket behavior. Use these as the contract for frontend and backend integration.

Base URL: `https://{host}/` or `http://localhost:{port}/`

## Request Validation & Canonicalization

All API requests are validated and canonicalized according to these rules:

### DateTime / Timestamp Handling
- **Format**: All timestamps must be ISO 8601 (e.g., `2026-04-07T15:00:00Z` or `2026-04-07T15:00:00+02:00`)
- **Timezone**: If a timestamp includes timezone information, it will be converted to UTC during processing
- **Unspecified Timezones**: If no timezone is specified, the timestamp is assumed to be UTC
- **Returned Format**: All timestamps in API responses are returned in UTC
- **Future Timestamps**: Transaction creation rejects timestamps in the future

### Currency Handling
- **Format**: ISO 4217 three-letter currency code (case-insensitive, e.g., "USD", "usd", "Usd")
- **Validation**: Currency code must be valid according to ISO 4217 standard
- **Normalization**: Currency codes are normalized to uppercase during canonicalization
- **Supported Codes**: See `CurrencyCodes.GetAllCodes` for complete list of valid codes
- **Example**: Request with `"currency": "eur"` will be canonicalized to `"EUR"`

### Decimal Precision (Amounts)
- **Precision**: All monetary amounts are rounded to 2 decimal places (currency precision)
- **Rounding**: Uses banker's rounding (round to nearest, ties to even)
- **Example**: `99.996` → `100.00`, `99.994` → `99.99`
- **Returned Format**: Amounts in responses are always 2 decimal places

### Text Field Canonicalization
- **Trimming**: Leading and trailing whitespace is removed from all text fields
- **Whitespace Normalization**: Multiple consecutive spaces/tabs are collapsed to single spaces
- **Control Characters**: Non-printable control characters are removed
- **Max Lengths**: 
  - `CreatedBy`: Maximum 100 characters
  - `Description`: Maximum 500 characters (newlines preserved)
  - `Source`: Maximum 50 characters
  - `Search` (query parameter): Maximum 200 characters
- **Null Handling**: If a field is whitespace-only after trimming, it becomes null

### Server-Controlled Fields
- **CreatedAt**: Always set by the server to `DateTime.UtcNow` at the moment of persistence
  - Clients cannot provide or override this value
  - If provided in a request, it will be silently ignored
- **Id**: Assigned by the database; clients cannot specify this

### Default Values
- **Source**: Defaults to `"Manual"` if not provided (or if null/empty after canonicalization)
- **Currency**: Defaults to `"USD"` if not provided

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

### Transactions
- GET `/api/transactions`
  - Query params: `from`, `to`, `category`, `limit`, `offset`
  - Returns paginated list of transactions
- GET `/api/transactions/{id}`
  - Returns a single transaction
- POST `/api/transactions`
  - Body: JSON { `timestamp`, `amount`, `currency`, `description`, `categoryId`, `createdBy` }
  - Creates a transaction and emits real-time events

### Metrics
- GET `/api/metrics`
- Description: Returns computed metrics (total balance, category totals, alerts)

### Activity Feed (historical)
- GET `/api/activity`
  - Query params: `limit`, `since`
  - Returns recent ActivityEvent entries for clients that reconnect or need history.

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
