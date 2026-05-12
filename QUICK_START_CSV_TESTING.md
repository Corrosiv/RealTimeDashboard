# Quick Start: Testing with Sample CSV Files

You now have three sample CSV files ready to test the frontend with real transaction data!

## Files Available

| File | Size | Transactions | Use Case |
|------|------|--------------|----------|
| `sample_simple.csv` | 5 | Quick smoke test, basic validation |
| `sample_transactions.csv` | 20 | Demo with variety, pagination testing |
| `sample_edge_cases.csv` | 10 | Special characters, validation edge cases |

## Detailed Guide

### 1. **Start the API Server**

In PowerShell, from the project root:

```powershell
cd C:\Users\Admin\source\repos\RealTimeDashboard
dotnet run --project src/RealTimeDashboard.API/RealTimeDashboard.API.csproj
```

Wait for the server to start. You should see:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### 2. **Open the Frontend**

Open your browser and navigate to:
```
http://localhost:5000/
```

You should see:
- ✅ "Connected ✓" in the top-right (green dot)
- Empty transaction list
- Empty activity feed
- Empty metrics cards

### 3. **Upload a CSV File**

#### Option A: Using the Frontend UI (Recommended for Testing)

1. In the **CSV Upload** section, click **"Choose File"**
2. Select one of the sample files:
   - Start with `sample_simple.csv` for quick testing
   - Use `sample_transactions.csv` for a demo with more data
   - Use `sample_edge_cases.csv` for validation testing
3. The username field is optional (pre-populated with your current session username)
4. Click **"Upload"** button
5. Wait 1-2 seconds for processing

**Expected Results**:
- ✅ Green success message: `"✓ Successfully processed X transactions"`
- ✅ Transactions appear in the **Transaction List** section
- ✅ **Metrics cards** populate (total count, balance, categories)
- ✅ **Activity Feed** shows upload event

#### Option B: Using curl (For Script Testing)

```bash
# Upload simple CSV
curl -X POST http://localhost:5000/api/upload/csv \
  -F "file=@sample_simple.csv" \
  -F "username=TestUser"

# Upload transactions CSV
curl -X POST http://localhost:5000/api/upload/csv \
  -F "file=@sample_transactions.csv" \
  -F "username=DemoUser"

# Upload edge cases CSV
curl -X POST http://localhost:5000/api/upload/csv \
  -F "file=@sample_edge_cases.csv" \
  -F "username=EdgeCaseUser"
```

#### Option C: Using PowerShell

```powershell
$file = Get-Item "sample_simple.csv"
$form = @{
    file = $file
    username = "TestUser"
}

$response = Invoke-WebRequest -Uri "http://localhost:5000/api/upload/csv" `
  -Method Post `
  -Form $form

$response.Content | ConvertFrom-Json
```

### 4. **Verify the Data**

After uploading, check:

1. **Transaction List** (should show all transactions):
   - Columns: Timestamp, Amount, Description, Category, Created By
   - Amounts formatted as currency
   - Newest transactions at the top
   - "Load More" button if > 20 transactions

2. **Metrics Cards** (top-right):
   - Total Transactions: Should match uploaded count
   - Total Balance: Sum of all amounts
   - Spending by Category: Top 5 categories by amount

3. **Activity Feed** (bottom):
   - Should show an event like: `"TestUser uploaded a CSV"`
   - Newest event at the top

### 5. **Test Additional Features**

#### Filter Transactions
- Enter text in the search box (e.g., "Groceries")
- Select a category from the dropdown
- Transaction list updates instantly without page refresh

#### Pagination
- If you uploaded `sample_transactions.csv` (20 items), click "Load More" to fetch next page
- Each page shows up to 20 transactions

#### Multi-Tab Sync
1. Open the frontend in a **new browser tab**: `http://localhost:5000/`
2. Upload a CSV in **Tab 1**
3. Watch **Tab 2** automatically update within 1-2 seconds (no refresh needed!)
4. Both tabs show identical data

#### Test Reconnection
1. Open DevTools (F12) → Network tab
2. Find the WebSocket connection (labeled `/ws`)
3. Right-click → Close
4. Watch the connection status change to "Reconnecting…"
5. Wait 1-10 seconds (exponential backoff)
6. Status returns to "Connected ✓"

---

## CSV File Details

### `sample_simple.csv` ⚡ Fast Test (5 transactions)

Best for:
- Quick verification that uploads work
- Testing basic UI functionality
- Minimal dataset for fast feedback

Users: `TestUser`
Categories: 1-5
Amounts: $45-$150
Date Range: Jan 20, 2026

### `sample_transactions.csv` 📊 Demo Data (20 transactions)

Best for:
- Portfolio demonstrations
- Showing diverse transaction types
- Testing pagination
- Testing multi-user data

Users: Alice, Bob, Charlie
Categories: 1-10
Amounts: $15-$500
Date Range: Jan 15-17, 2026

### `sample_edge_cases.csv` 🧪 Validation Testing (10 transactions)

Best for:
- Testing special character handling
- Validating multi-currency support
- Text field trimming and normalization
- Negative amounts (refunds)

Users: User1, User2, ÜserNämé
Categories: 1-5
Currencies: USD, EUR, GBP
Amounts: -$50 to $9999.99
Special Features: Commas in descriptions, newlines, accents, unicode

---

## Common Issues & Solutions

### Issue: Upload button is disabled

**Solution**: Make sure you've selected a file first. The button only enables when a CSV file is chosen.

### Issue: "Connection Error" modal appears

**Solution**: 
1. Make sure the API server is running (check terminal for "Now listening on: http://localhost:5000")
2. Refresh the page (F5)
3. Wait for "Connected ✓" status
4. Try uploading again

### Issue: Upload succeeds but no transactions appear

**Solution**:
1. Check the browser console (F12 → Console) for errors
2. Check the API server terminal logs for errors
3. Try uploading `sample_simple.csv` (simplest format)
4. Verify metrics cards update (if they do, data was received)

### Issue: Only some transactions appear

**Solution**: 
1. You might have pagination enabled (only shows 20 per page)
2. Click "Load More" button at bottom of transaction list
3. Check if you have a search filter active (clear it)
4. Check if you have a category filter selected (reset to "All")

### Issue: Metrics don't add up

**Solution**:
1. The balance is the SUM of all amounts (including negative for refunds)
2. Total transactions is COUNT of all transactions
3. Category breakdown shows TOP 5 categories by amount
4. Refresh the page to ensure latest metrics

---

## Advanced Testing

### Generate Your Own Data

If you want more transactions for stress testing:

**Using Python**:
```python
import csv
from datetime import datetime, timedelta

# Generate 100 transactions
transactions = []
for i in range(100):
    timestamp = (datetime(2026, 1, 20) + timedelta(hours=i)).isoformat() + 'Z'
    amount = (i * 10.5) % 500
    transactions.append([
        timestamp,
        f"{amount:.2f}",
        f"Transaction #{i+1}",
        (i % 5) + 1,
        f"User{i % 3 + 1}"
    ])

with open("large_sample.csv", "w", newline="") as f:
    writer = csv.writer(f)
    writer.writerow(["timestamp", "amount", "description", "categoryId", "createdBy"])
    writer.writerows(transactions)
```

### Test with Different Browsers

Upload the same CSV to test in:
- Chrome/Edge (Chromium)
- Firefox
- Safari
- Mobile browsers (iOS Safari, Chrome Mobile)

All should display identically due to the responsive design.

### Measure Performance

1. Open DevTools → Performance tab
2. Click Record
3. Upload `sample_transactions.csv`
4. Stop recording
5. Check timeline for:
   - API request latency (should be <500ms)
   - WebSocket message latency (should be <100ms)
   - DOM update time (should be <100ms)

---

## Next Steps

After testing with sample data:

1. **Read the Full Testing Guide**: See `FRONTEND-TESTING-GUIDE.md` for 10 detailed test scenarios
2. **Check the Implementation Summary**: See `FRONTEND-IMPLEMENTATION-SUMMARY.md` for architecture details
3. **Review the API Spec**: See `docs/API-SPEC.md` for complete endpoint documentation
4. **Prepare for Demo**: Use `sample_transactions.csv` with multiple tabs for impressive multi-user sync demo

---

## File Locations

All sample files are in the root directory of the project:
```
C:\Users\Admin\source\repos\RealTimeDashboard\
├── sample_simple.csv
├── sample_transactions.csv
├── sample_edge_cases.csv
├── CSV_TEST_DATA_README.md          (comprehensive format documentation)
└── QUICK_START_CSV_TESTING.md       (this file)
```

They're also tracked in git, so you can always get the latest versions with:
```powershell
git pull origin dev
```

---

**Good luck testing! 🚀**

For detailed testing procedures, see `FRONTEND-TESTING-GUIDE.md`.
For troubleshooting, see `WEBSOCKET-FIX-SUMMARY.md`.
