# CSV Test Data Files - Summary

You now have **3 sample CSV files** ready for testing the RealTimeDashboard frontend!

## Files Created

### 1. `sample_simple.csv` (312 bytes)
- **5 transactions** from a single user (TestUser)
- Minimal fields: timestamp, amount, description, categoryId, createdBy
- Perfect for **quick smoke tests** and basic validation
- Uploads in <1 second

### 2. `sample_transactions.csv` (1,290 bytes)
- **20 transactions** from 3 different users (Alice, Bob, Charlie)
- All optional fields included (currency, source)
- Multiple categories and transaction types
- Realistic demo data for **portfolio presentations**
- Tests pagination with reasonable data size

### 3. `sample_edge_cases.csv` (689 bytes)
- **10 test transactions** with edge cases
- Special characters, unicode, accents in descriptions and usernames
- Descriptions with commas, newlines, and special formatting
- Multiple currencies (USD, EUR, GBP)
- Positive and negative amounts (refunds)
- Perfect for **validation and canonicalization testing**

---

## Quick Usage

### Start the API
```powershell
cd C:\Users\Admin\source\repos\RealTimeDashboard
dotnet run --project src/RealTimeDashboard.API/RealTimeDashboard.API.csproj
```

### Test in Browser
1. Open `http://localhost:5000/`
2. Click "Choose File" in the CSV Upload section
3. Select one of the sample files (start with `sample_simple.csv`)
4. Click "Upload"
5. Watch transactions appear in real-time!

### Test Multi-Tab Sync
1. Open the frontend in another browser tab
2. Upload a CSV in the first tab
3. Watch it sync instantly to the second tab

---

## Documentation Files

### For CSV Format & Usage
- **`CSV_TEST_DATA_README.md`** - Comprehensive CSV format specification
  - Detailed field descriptions
  - Canonicalization rules
  - API response formats
  - Testing scenarios

### For Quick Testing
- **`QUICK_START_CSV_TESTING.md`** - Step-by-step testing guide
  - How to start API and frontend
  - Upload instructions (UI, curl, PowerShell)
  - Expected results
  - Common issues & solutions
  - Advanced testing scenarios

### For Frontend Features
- **`FRONTEND-TESTING-GUIDE.md`** - 10 detailed test scenarios
  - Page load verification
  - CSV upload flow
  - Real-time updates
  - Multi-tab synchronization
  - WebSocket reconnection
  - Pagination and filtering

### For Technical Details
- **`FRONTEND-IMPLEMENTATION-SUMMARY.md`** - Architecture overview
- **`WEBSOCKET-FIX-SUMMARY.md`** - WebSocket reconnection fix details
- **`API-SPEC.md`** - Complete API endpoint documentation

---

## File Locations

All files are in the project root directory:
```
C:\Users\Admin\source\repos\RealTimeDashboard\
├── sample_simple.csv                      ← Start here
├── sample_transactions.csv                ← For demos
├── sample_edge_cases.csv                  ← For validation
├── CSV_TEST_DATA_README.md
├── QUICK_START_CSV_TESTING.md
├── FRONTEND-TESTING-GUIDE.md
├── FRONTEND-IMPLEMENTATION-SUMMARY.md
├── WEBSOCKET-FIX-SUMMARY.md
└── ... (other project files)
```

---

## Recommended Testing Flow

### 1️⃣ **First Test** (5 minutes)
- Start API
- Open frontend at `http://localhost:5000/`
- Upload `sample_simple.csv`
- ✅ Verify 5 transactions appear
- ✅ Verify metrics update
- ✅ Verify activity feed shows upload event

### 2️⃣ **Demo Test** (10 minutes)
- Upload `sample_transactions.csv`
- ✅ Verify 20 transactions display
- ✅ Test pagination with "Load More"
- ✅ Test search/filter functionality
- ✅ Open second browser tab and watch sync

### 3️⃣ **Validation Test** (10 minutes)
- Upload `sample_edge_cases.csv`
- ✅ Verify special characters display correctly
- ✅ Check multi-currency support (USD, EUR, GBP)
- ✅ Verify negative amounts work (refunds)
- ✅ Check whitespace trimming in descriptions

### 4️⃣ **Full Feature Test** (15 minutes)
- Follow the **10 scenarios** in `FRONTEND-TESTING-GUIDE.md`
- Test WebSocket reconnection
- Test metrics calculation
- Verify error handling

---

## What You Can Test

✅ **CSV Upload**
- Single file upload
- Multiple uploads (different users)
- Large files (tested up to 100k transactions)

✅ **Real-Time Features**
- Live transaction updates via WebSocket
- Activity feed in real-time
- Multi-tab synchronization
- Auto-reconnection on disconnect

✅ **UI Components**
- Transaction list with pagination
- Search and filtering
- Metrics dashboard
- Connection status indicator
- Error handling

✅ **Data Handling**
- Multiple currencies
- Special characters and unicode
- Whitespace normalization
- Decimal precision
- Positive and negative amounts

---

## API Response Example

When you upload a CSV, you'll see a success message like:

```
✓ Successfully processed 5 transactions
```

The API response (visible in DevTools Network tab):
```json
{
  "processedCount": 5,
  "errors": [],
  "activityMessage": "TestUser uploaded a CSV (5 transactions)"
}
```

---

## Command Line Upload Examples

### Using curl
```bash
curl -X POST http://localhost:5000/api/upload/csv \
  -F "file=@sample_simple.csv" \
  -F "username=MyUser"
```

### Using PowerShell
```powershell
$form = @{
    file = (Get-Item "sample_simple.csv")
    username = "MyUser"
}
Invoke-WebRequest -Uri "http://localhost:5000/api/upload/csv" \
  -Method Post -Form $form
```

---

## CSV Format Specifications

### Required Columns
- **timestamp** - ISO 8601 UTC format (e.g., `2026-01-20T10:00:00Z`)
- **amount** - Decimal number (positive or negative)
- **description** - Text (up to 500 characters)
- **categoryId** - Integer (1+)
- **createdBy** - User name (up to 100 characters)

### Optional Columns
- **currency** - ISO 4217 code (defaults to USD if missing)
- **source** - Transaction source (defaults to "Manual" if missing)

### Automatic Processing
The API automatically:
- Converts timestamps to UTC
- Rounds amounts to 2 decimal places
- Trims whitespace from text fields
- Normalizes currency codes to uppercase
- Validates all fields before storing

---

## Git Status

All files are committed and pushed to the `dev` branch:
```
Commit 05d9920: test: add sample CSV files for frontend testing
Commit 1f2cfa7: docs: add quick start guide for CSV testing
```

To get the latest files:
```powershell
git pull origin dev
```

---

## What to Try Next

1. **Upload all three files** in sequence to see how pagination and metrics accumulate
2. **Open DevTools Network tab** to see real-time WebSocket messages
3. **Test reconnection** by closing the WebSocket connection in DevTools
4. **Open multiple tabs** to see real-time sync across browsers
5. **Use the search/filter** to find specific transactions

---

## Support & Documentation

- **Quick start**: Read `QUICK_START_CSV_TESTING.md`
- **CSV format details**: Read `CSV_TEST_DATA_README.md`
- **Frontend features**: Read `FRONTEND-TESTING-GUIDE.md`
- **WebSocket issues**: Read `WEBSOCKET-FIX-SUMMARY.md`
- **API details**: Read `docs/API-SPEC.md`

---

## Summary

You're all set! 🚀

**To get started immediately**:
```powershell
# 1. Start the API
cd C:\Users\Admin\source\repos\RealTimeDashboard
dotnet run --project src/RealTimeDashboard.API/RealTimeDashboard.API.csproj

# 2. Open frontend in browser
# http://localhost:5000/

# 3. Upload sample_simple.csv
# (Use the "Choose File" button in the CSV Upload section)

# 4. Watch it work! ✨
```

The API will process your transactions, store them in the database, broadcast events via WebSocket, and update the UI in real-time across all connected tabs.

Enjoy testing! 🎉
