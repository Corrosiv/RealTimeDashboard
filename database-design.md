# Database Design (SQLite)

Notes:
- SQLite is used for local/demo persistence.
- Use sensible constraints and indexes for common queries (transactions by timestamp, category aggregations).

## Tables

### Users
- `username` TEXT PRIMARY KEY
- `display_name` TEXT
- `created_at` DATETIME
- Notes:
  - Users are optional to persist. If not persisted, `createdBy` in other tables stores the username as text. Persisting users enables preferences/history. [TBD]

### Transactions
- `id` INTEGER PRIMARY KEY AUTOINCREMENT
- `timestamp` DATETIME NOT NULL
- `amount` NUMERIC NOT NULL
- `currency` TEXT NOT NULL DEFAULT 'USD'
- `description` TEXT
- `category_id` INTEGER NULL
- `source` TEXT NOT NULL -- e.g., 'CSV', 'manual'
- `created_by` TEXT NOT NULL -- username
- `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
- Indexes:
  - INDEX on (`timestamp`) for recent queries
  - INDEX on (`category_id`) for aggregation
  - INDEX on (`created_by`) for user-scoped queries

### Categories
- `id` INTEGER PRIMARY KEY AUTOINCREMENT
- `name` TEXT NOT NULL UNIQUE
- `parent_id` INTEGER NULL REFERENCES Categories(id) -- hierarchical categories optional
- Indexes:
  - UNIQUE(name)
  - INDEX on (`parent_id`) if hierarchy used

### Budgets (optional)
- `id` INTEGER PRIMARY KEY AUTOINCREMENT
- `category_id` INTEGER NOT NULL REFERENCES Categories(id)
- `limit_amount` NUMERIC NOT NULL
- `period` TEXT NOT NULL -- e.g., 'monthly'
- Indexes:
  - INDEX on (`category_id`)

### ActivityEvents
- `id` INTEGER PRIMARY KEY AUTOINCREMENT
- `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
- `actor` TEXT NOT NULL -- username or 'system'
- `type` TEXT NOT NULL -- e.g., 'CsvUploaded'
- `message` TEXT NOT NULL
- `metadata` TEXT -- JSON blob with optional details (transaction id, CSV summary)
- Indexes:
  - INDEX on (`timestamp`) for recent feed retrieval
  - INDEX on (`actor`) for actor-based queries

## Constraints & Notes
- Use CHECK constraints for amounts (e.g., `amount` != NULL); further domain checks can be added.
- Prefer storing JSON in `metadata` as TEXT in SQLite; use an application-level schema for parsing.
- For multi-instance deployments, consider moving from SQLite to a server RDBMS to avoid file locking and concurrency issues.
- Foreign key constraints in SQLite require `PRAGMA foreign_keys = ON`; ensure the app enables this.
- Any uncertain design choices are marked [TBD] (e.g., whether to persist users).
