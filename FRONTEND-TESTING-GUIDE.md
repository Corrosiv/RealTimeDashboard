# Frontend Testing Guide

This document provides step-by-step instructions for testing the RealTimeDashboard minimalist frontend.

## Quick Start

### Prerequisites
- .NET 10 SDK installed
- PowerShell or compatible shell
- A modern web browser (Chrome, Edge, Firefox, Safari)

### 1. Start the Backend API

```powershell
cd "C:\Users\Admin\source\repos\RealTimeDashboard"
dotnet run --project src/RealTimeDashboard.API/RealTimeDashboard.API.csproj
```

The API should start and listen on `http://localhost:5000` (or check the console output for the configured URL).

### 2. Open the Frontend

Navigate to **http://localhost:5000/** in your web browser.

You should see:
- **Header** with the Real-Time Dashboard title
- **Connection Status** indicator (should show "Connected ✓" if WebSocket is working)
- **CSV Upload** form
- **Metrics** cards (currently empty)
- **Transaction List** (empty initially)
- **Activity Feed** (empty initially)

---

## Manual Testing Scenarios

### Scenario 1: Page Load & Connection Status

**Objective**: Verify the frontend loads and WebSocket connects successfully.

**Steps**:
1. Open http://localhost:5000/
2. Check the connection status indicator in the top-right

**Expected Result**:
- Page loads without errors
- Connection status shows "Connected ✓" (green dot)
- All sections are visible: upload, metrics, transactions, activity feed

**Console Check** (DevTools → Console):
- No JavaScript errors
- WebSocket message like: `"WebSocket connected"` or similar

---

### Scenario 2: CSV Upload Flow

**Objective**: Verify CSV upload populates the transaction list.

**Prerequisites**:
- A valid CSV file with transaction data
- Sample format:
  ```
  timestamp,amount,description,categoryId,createdBy
  2024-01-15T10:30:00Z,50.00,Groceries,1,user1
  2024-01-15T11:45:00Z,-25.50,Refund,2,user2
  ```

**Steps**:
1. Click **"Choose File"** button in the CSV Upload section
2. Select a valid CSV file
3. Click **"Upload"** button
4. Wait 1-2 seconds for the upload to complete

**Expected Result**:
- Green success message appears: `"✓ Successfully processed X transactions"`
- Transactions appear in the **Transaction List** section
- Activity feed updates with an upload event
- Metrics cards populate (total transactions, total balance, categories)

**If Upload Fails**:
- Red error message appears
- Check browser console for details
- Verify the API is running and `/api/upload/csv` endpoint is accessible

---

### Scenario 3: Transaction List & Pagination

**Objective**: Verify transactions display correctly and pagination works.

**Prerequisites**:
- Several transactions already uploaded (from Scenario 2)

**Steps**:
1. Look at the **Transaction List** section
2. Verify columns display: Timestamp, Amount, Description, Category, Created By
3. If there are more than 20 transactions, a **"Load More"** button appears at the bottom
4. Click **"Load More"** to fetch the next page

**Expected Result**:
- Each transaction row shows all data correctly formatted
- Amounts display as currency (e.g., "$50.00")
- Timestamps show in local time
- "Load More" button appears only when there are more transactions to fetch
- Clicking "Load More" appends new transactions to the list

---

### Scenario 4: Real-Time Updates via WebSocket

**Objective**: Verify new transactions appear in real-time without page refresh.

**Prerequisites**:
- Frontend is open in browser
- Backend API is running
- Connection Status shows "Connected ✓"

**Steps**:
1. In a **separate terminal or API tool** (e.g., Postman, curl), create a new transaction:
   ```bash
   curl -X POST http://localhost:5000/api/transactions \
     -H "Content-Type: application/json" \
     -d '{
       "timestamp": "2024-01-15T12:00:00Z",
       "amount": 99.99,
       "description": "Test transaction",
       "categoryId": 1,
       "createdBy": "test-user",
       "currency": "USD"
     }'
   ```

2. Watch the frontend **Transaction List** and **Activity Feed**

**Expected Result**:
- New transaction appears at the **top** of the Transaction List within 1 second
- Activity feed prepends a new event: "TransactionCreated" with the transaction details
- No page refresh required; updates appear instantly
- Both sections update from the same WebSocket event

---

### Scenario 5: Activity Feed

**Objective**: Verify activity feed displays events and updates in real-time.

**Steps**:
1. Look at the **Activity Feed** section
2. Events should be sorted newest-first (most recent at the top)
3. Each event shows: timestamp, event type, message, and created-by user
4. If there are more than 20 events, a **"Load More"** button appears
5. Perform an action (e.g., upload CSV or create transaction) and watch the feed update

**Expected Result**:
- Events display in reverse-chronological order (newest first)
- New events appear at the **top** of the feed in real-time
- Clicking "Load More" fetches older events and appends them at the bottom
- No page refresh required for real-time updates

---

### Scenario 6: Filtering

**Objective**: Verify search and category filters work.

**Prerequisites**:
- Several transactions with different descriptions and categories

**Steps**:
1. Look for the **filter bar** above the Transaction List
2. Enter text in the **search box** (e.g., "Groceries")
3. Select a category from the **category dropdown**
4. Observe the transaction list update

**Expected Result**:
- Transaction List updates to show only matching items
- Filtering works without page refresh
- "Load More" button resets to load from the first page of filtered results
- Multiple filters work together (AND logic)

---

### Scenario 7: Multi-Tab Synchronization

**Objective**: Verify transactions and activity sync across browser tabs.

**Steps**:
1. Open the frontend in **Tab 1**: http://localhost:5000/
2. Open the same URL in **Tab 2**: http://localhost:5000/
3. In **Tab 1**, upload a CSV or create a transaction via API
4. Switch to **Tab 2** and observe the list

**Expected Result**:
- **Tab 2** receives WebSocket updates from the same event
- New transactions/events appear in **Tab 2** within 1-2 seconds
- Both tabs show identical transaction lists and activity feeds
- Both tabs maintain independent WebSocket connections

**Console Check** (DevTools in Tab 2):
- You should see two separate WebSocket connections (one per tab) with similar activity

---

### Scenario 8: WebSocket Reconnection & Exponential Backoff

**Objective**: Verify the client auto-reconnects after disconnection.

**Steps**:

1. Open http://localhost:5000/ and ensure connection status is **"Connected ✓"**

2. Open DevTools → Network tab

3. Find the **WebSocket** connection (labeled `/ws`)

4. Right-click on it and select **"Close"** (or manually terminate the connection)

5. Watch the connection status indicator

6. Wait 1-10 seconds and observe auto-reconnect

**Expected Result**:
- Status changes to **"Reconnecting…"** (or "Disconnected ✗")
- After 1 second, connection attempts to re-establish
- If first attempt fails, exponential backoff kicks in: 2s, 4s, 8s, 16s, 30s (max)
- Once reconnected, status returns to **"Connected ✓"**
- Any messages queued during disconnect are sent after reconnect

**Timeline**:
- T=0s: Disconnect
- T=1s: First reconnect attempt
- T=3s: Second attempt (if first failed) with 2s delay
- T=7s: Third attempt with 4s delay
- ... (continues with exponential backoff up to 30s max)

**Console Check**:
- Look for logs like: `"Reconnecting in 1000ms (attempt 1/10)"`
- After reconnect: `"WebSocket connected"`

---

### Scenario 9: Queued Messages During Reconnect

**Objective**: Verify messages sent during a disconnect are queued and sent after reconnect.

**Steps**:

1. Open http://localhost:5000/ and ensure **"Connected ✓"**

2. Open DevTools → Network tab and find the WebSocket

3. Close the WebSocket connection

4. Quickly (within 1-2 seconds) upload a CSV or create a transaction

5. Watch the connection status: it should show **"Reconnecting…"**

6. Wait for auto-reconnect to complete

7. Check the Transaction List and Activity Feed

**Expected Result**:
- During disconnect, the upload/transaction request is **queued** (not lost)
- Once reconnection succeeds, queued messages are **sent automatically**
- Transaction appears in the list after reconnect completes
- Activity feed updates reflect the queued action
- No manual retry needed from the user

---

### Scenario 10: Metrics Display

**Objective**: Verify metrics cards calculate and display correctly.

**Prerequisites**:
- Several transactions uploaded

**Steps**:
1. Look at the **Metrics** section (top-right area)
2. Check:
   - **Total Transactions**: Sum of all transactions
   - **Total Balance**: Sum of all transaction amounts
   - **Spending by Category**: Table showing top 5 categories by total amount

3. Upload more transactions or create new ones

**Expected Result**:
- Total Transactions count increases
- Total Balance updates to reflect new transactions
- Category breakdown updates and recalculates
- Metrics update in real-time as transactions are added

---

## Automated Testing (Future: Playwright)

Currently, we have placeholder test scaffolding in `test/RealTimeDashboard.Tests/Integration/FrontendIntegrationTests.cs`.

To enable Playwright-based automated tests in the future:

1. Add NuGet package: `Microsoft.Playwright`
2. Install browsers: `playwright install`
3. Implement the test methods in `FrontendIntegrationTests.cs`
4. Run: `dotnet test --filter "FrontendIntegrationTests"`

See comments in the test file for detailed implementation notes.

---

## Common Issues & Troubleshooting

### Issue: Connection Status shows "Disconnected ✗"

**Cause**: WebSocket endpoint not reachable.

**Solution**:
1. Verify API is running on the correct port (check console output)
2. Check DevTools → Network tab for WebSocket errors
3. Ensure `/ws` endpoint exists in `Program.cs`
4. Check API logs for any WebSocket handler errors

### Issue: CSV Upload shows error

**Cause**: Invalid CSV format or API endpoint not working.

**Solution**:
1. Verify CSV has required columns: `timestamp`, `amount`, `description`, `categoryId`, `createdBy`
2. Verify each row has valid data (no empty required fields)
3. Check API logs for import errors
4. Verify `/api/upload/csv` endpoint is implemented

### Issue: Transactions appear but Activity Feed doesn't update

**Cause**: WebSocket event broadcasting might not be working.

**Solution**:
1. Check DevTools Console for JavaScript errors
2. Verify `ActivityEventPublisher` is wired in the backend
3. Check that `TransactionCreatedEventHandler` is registered
4. Verify WebSocket connection is active (status should show "Connected ✓")

### Issue: "Load More" button doesn't work

**Cause**: Cursor pagination issue.

**Solution**:
1. Check API response includes `nextCursor` and `hasMore` fields
2. Verify `CursorService` is encoding/decoding cursors correctly
3. Check API logs for pagination errors
4. Ensure `limit` parameter is within valid range (1-200)

### Issue: Multi-tab sync not working

**Cause**: WebSocket events not being broadcast to all connected clients.

**Solution**:
1. Verify `WebSocketConnectionManager` tracks all connected clients
2. Check that `ActivityEventPublisher.PublishAsync()` broadcasts to all connections
3. Ensure each tab has an independent WebSocket connection (visible in Network tab)
4. Check API logs for broadcast errors

---

## Performance Notes

### Expected Performance

- **Page Load**: < 1s (depends on number of initial transactions loaded)
- **CSV Upload**: < 5s (depends on file size; sample 100-1000 row files typically < 1s)
- **WebSocket Updates**: < 100ms latency from event creation to UI update
- **Pagination**: < 500ms to load next page

### Optimization Tips

- Disable browser extensions that might slow down the page
- Use a modern browser (Chrome, Edge, Firefox)
- Close DevTools if performance testing (it can add overhead)
- For large datasets (10k+ transactions), pagination keeps the UI responsive

---

## Demo Scenario Summary

For a complete demo of the real-time dashboard:

1. **Start API**: `dotnet run --project src/RealTimeDashboard.API/RealTimeDashboard.API.csproj`
2. **Open Frontend**: http://localhost:5000/
3. **Upload CSV**: Click upload and select a CSV file with 10-50 transactions
4. **Verify Sync**: Transactions and metrics appear within 1-2 seconds
5. **Open Tab 2**: Open frontend in another tab
6. **Create Transaction**: Use API or manual POST to create a new transaction
7. **Both Tabs Update**: New transaction appears in both tabs in real-time
8. **Test Reconnect**: Close WebSocket in DevTools, wait 1-10 seconds for auto-reconnect
9. **Verify Resilience**: Activity feed and transactions remain in sync after reconnect

---

## Feedback & Debugging

### Enabling Verbose Logs

To see detailed JavaScript logs:

1. Open DevTools → Console
2. Look for messages prefixed with `[WebSocket]`, `[API]`, `[App]`
3. Filter by level (Info, Warn, Error) as needed

### Reporting Issues

If you encounter unexpected behavior:

1. **Take a screenshot** of the issue
2. **Check DevTools Console** for JavaScript errors
3. **Check API logs** (terminal running the API)
4. **Try a hard refresh**: `Ctrl+Shift+R` (or `Cmd+Shift+R` on Mac)
5. **Clear local storage** if session state seems corrupted: DevTools → Application → Storage → Clear All
6. **Report with**:
   - Steps to reproduce
   - Expected vs. actual behavior
   - Screenshots or video
   - Browser and OS version
   - API logs and console errors
