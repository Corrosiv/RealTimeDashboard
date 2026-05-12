# Sample CSV Test Data

This directory contains sample CSV files for testing the RealTimeDashboard frontend and API.

## Files

### `sample_transactions.csv`
**Use case**: General demo and testing with realistic transaction data

**Contents**:
- 20 transactions across 3 users (Alice, Bob, Charlie)
- Multiple categories (groceries, utilities, food, rent, fuel, etc.)
- Various amounts and timestamps spread over 3 days
- All fields populated: timestamp, amount, description, categoryId, createdBy, currency, source

**When to use**:
- Demo presentations (shows diverse transaction types)
- Testing pagination (20 transactions is a good medium size)
- Testing filtering and search functionality
- Testing real-time updates and WebSocket synchronization

---

### `sample_simple.csv`
**Use case**: Quick testing with minimal required fields

**Contents**:
- 5 transactions from a single user (TestUser)
- Simple transaction descriptions
- Only required fields: timestamp, amount, description, categoryId, createdBy
- No currency or source fields (uses defaults: USD, Manual)

**When to use**:
- Quick smoke tests
- Testing basic upload functionality
- Minimal API validation testing
- First-time setup verification

---

### `sample_edge_cases.csv`
**Use case**: Testing edge cases and data validation

**Contents**:
- Positive and negative amounts
- Very small (0.01) and very large (9999.99) amounts
- Descriptions with special characters (commas, newlines, accents, emoji)
- Multiple currencies (USD, EUR, GBP)
- Whitespace handling (leading/trailing spaces)
- Unicode characters in user names and descriptions
- Refunds (negative amounts)

**When to use**:
- Validation and canonicalization testing
- Ensuring API handles special characters correctly
- Multi-currency transaction testing
- Text field trimming and normalization testing
- Edge case verification before production

---

## CSV Format Specification

### Required Columns
- **timestamp**: ISO 8601 format (e.g., `2026-01-20T10:00:00Z`)
  - Must be in UTC or include timezone
  - Cannot be in the future
- **amount**: Decimal number (positive or negative)
  - Positive for income/additions
  - Negative for expenses/refunds
  - Will be rounded to 2 decimal places
- **description**: Text description (up to 500 characters)
  - Can contain spaces, commas, newlines, special characters
  - Leading/trailing whitespace will be trimmed
- **categoryId**: Integer (1+)
  - Identifies the transaction category
  - Examples: 1=Groceries, 2=Utilities, 3=Food, etc.
- **createdBy**: User name (up to 100 characters)
  - Identifies who created the transaction
  - Leading/trailing whitespace will be trimmed

### Optional Columns
- **currency**: ISO 4217 three-letter code (default: USD)
  - Examples: USD, EUR, GBP, JPY, CAD
  - Case-insensitive; normalized to uppercase
- **source**: Transaction source (default: Manual)
  - Examples: "Manual", "Import", "API"
  - Maximum 50 characters

### Data Canonicalization Rules
The API automatically:
- Converts timestamps to UTC
- Rounds amounts to 2 decimal places (banker's rounding)
- Trims leading/trailing whitespace from text fields
- Removes control characters from text fields
- Normalizes currency codes to uppercase
- Sets default currency to USD if not specified
- Sets default source to "Manual" if not specified

---

## How to Use in the Frontend

1. **Start the API**:
   ```powershell
   cd C:\Users\Admin\source\repos\RealTimeDashboard
   dotnet run --project src/RealTimeDashboard.API/RealTimeDashboard.API.csproj
   ```

2. **Open the Frontend**:
   - Navigate to `http://localhost:5000/`
   - You should see "Connected ✓" in the top-right

3. **Upload a CSV File**:
   - Click "Choose File" in the CSV Upload section
   - Select one of the sample files (e.g., `sample_simple.csv`)
   - Enter a username (or leave for default)
   - Click "Upload"

4. **Observe Results**:
   - Transaction list should populate within 1-2 seconds
   - Metrics cards should update
   - Activity feed should show the upload event
   - Open a second browser tab to see real-time sync

---

## Testing Scenarios

### Quick Smoke Test
1. Upload `sample_simple.csv`
2. Verify 5 transactions appear in the list
3. Verify metrics show: 5 total, correct balance, categories populated

### Demo with Variety
1. Upload `sample_transactions.csv`
2. Show 20 transactions across multiple users
3. Test filtering by user name or description
4. Test pagination with "Load More" button
5. Open second tab to demonstrate sync

### Edge Case Validation
1. Upload `sample_edge_cases.csv`
2. Verify special characters display correctly
3. Verify multi-currency transactions are accepted
4. Check that whitespace is properly trimmed
5. Verify negative amounts (refunds) work correctly

### Bulk Test
1. Upload `sample_transactions.csv` multiple times (with different usernames)
2. Verify metrics accumulate correctly
3. Test pagination with 40+ transactions
4. Verify search and filtering across all transactions

---

## API Response Format

When you upload a CSV, the API returns:
```json
{
  "processedCount": 5,
  "errors": [],
  "activityMessage": "TestUser uploaded a CSV (5 transactions)"
}
```

If there are any validation errors, they would appear in the `errors` array:
```json
{
  "processedCount": 3,
  "errors": [
    {
      "rowNumber": 2,
      "field": "amount",
      "message": "Amount must be a valid decimal number"
    },
    {
      "rowNumber": 5,
      "field": "timestamp",
      "message": "Timestamp cannot be in the future"
    }
  ],
  "activityMessage": "TestUser uploaded a CSV (3 transactions, 2 errors)"
}
```

---

## Sample Commands

### Upload via curl
```bash
curl -X POST http://localhost:5000/api/upload/csv \
  -F "file=@sample_simple.csv" \
  -F "username=TestUser"
```

### Upload via PowerShell
```powershell
$file = Get-Item "sample_simple.csv"
$form = @{
    file = $file
    username = "TestUser"
}

Invoke-WebRequest -Uri "http://localhost:5000/api/upload/csv" `
  -Method Post `
  -Form $form
```

---

## Generating More Test Data

### Using Excel/Spreadsheet
1. Create a new spreadsheet
2. Add columns: timestamp, amount, description, categoryId, createdBy
3. Fill in test data
4. Export as CSV

### Using Python (if needed)
```python
import csv
from datetime import datetime, timedelta

# Generate sample data
transactions = []
for i in range(10):
    timestamp = (datetime.utcnow() + timedelta(days=i)).isoformat() + 'Z'
    amount = 50.00 + (i * 10)
    transactions.append([
        timestamp,
        f"{amount:.2f}",
        f"Test transaction {i+1}",
        (i % 5) + 1,  # categoryId 1-5
        "GeneratedUser"
    ])

# Write to CSV
with open("generated_sample.csv", "w", newline="") as f:
    writer = csv.writer(f)
    writer.writerow(["timestamp", "amount", "description", "categoryId", "createdBy"])
    writer.writerows(transactions)
```

---

## Notes

- All sample files use future dates (January 2026) for consistent testing
- The `createdBy` field represents who uploaded/created the transaction, not who it's from
- Categories are numeric IDs; you can use any positive integer
- The API does NOT validate that categories exist - it just stores the ID
- Timestamps MUST be valid ISO 8601 format; the API will reject invalid formats
- Amounts can be positive (income) or negative (refunds/expenses)
