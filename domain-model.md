# Domain Model (Conceptual)

Defines the core entities and their relationships. This is conceptual and not tied to specific DB types.

## User
- Identifier: `username` (string)
- Notes:
  - No authentication; username is a lightweight way to attribute actions.
  - May be ephemeral (not necessarily persisted) depending on deployment [TBD].

## Transaction
- Properties (conceptual):
  - `id` (unique)
  - `timestamp` (date/time)
  - `amount` (decimal)
  - `currency` (string) — use consistent currency for demo
  - `description` (string)
  - `categoryId` (reference to Category)
  - `source` (e.g., "CSV" or "manual")
  - `createdBy` (username)
- Description:
  - Represents a single financial activity (expense or income).
  - Core for computing balances and metrics.

## Category
- Properties:
  - `id` (unique)
  - `name` (string)
  - `parentCategoryId` (optional) — hierarchy [TBD]
- Description:
  - Categories group transactions for breakdowns and budget tracking.

## Budget (optional)
- Properties:
  - `id` (unique)
  - `categoryId` (reference)
  - `limitAmount` (decimal)
  - `period` (e.g., monthly)
- Description:
  - Used to detect alerts (e.g., overspending).
  - Marked optional: only implemented if budget feature is enabled.

## ActivityEvent
- Properties:
  - `id` (unique)
  - `timestamp` (date/time)
  - `actor` (username or "system")
  - `type` (e.g., `CsvUploaded`, `TransactionCreated`, `BudgetExceeded`)
  - `message` (human-friendly string)
  - `metadata` (freeform JSON for details, e.g., transaction id)
- Description:
  - Captures user/system events for the live activity feed and historical lookup.

## Relationships
- User → Transaction: `createdBy` attribute links actions to a username (non-enforced FK if users are ephemeral).
- Transaction → Category: many-to-one; transactions belong to a single category.
- Category → Budget: optional one-to-one per category if budgets exist.
- ActivityEvent may reference Transactions or Categories via metadata.
