# RealTimeDashboard Unit Test Plan

## Overview
This test plan guides the TDD implementation for RealTimeDashboard, focusing on:
- **Correctness-critical** services tested in isolation with unit tests (mocked dependencies)
- **Integration** tests for WebSocket flows and end-to-end scenarios
- **Architecture**: Single group/family dashboard, all users see same data, optimistic concurrency, event sourcing with bounded replay log

## Test Scope & Priorities

### Tier 1: Correctness-Critical (Heavily Isolated Unit Tests)
These services require rigorous unit testing with mocked dependencies:

#### 1.1 Transaction Deduplication Logic
**Service**: `TransactionDeduplicationService` (to be created)
**Purpose**: Detect duplicate transactions based on configurable keys (date, amount, description)

**Test Cases**:
- `ShouldIdentifyExactDuplicates()` - same date, amount, description
- `ShouldIdentifyDuplicatesIgnoringWhitespace()` - descriptions differ only in whitespace
- `ShouldNotFlagSimilarTransactionsAsDuplicates()` - same amount, different date
- `ShouldHandleNullDescriptions()` - two transactions with null descriptions
- `ShouldReportDuplicateWithLineNumbers()` - CSV import context (row N is duplicate of row M)
- `ShouldDetectDuplicatesAcrossPreviousImports()` - vs. existing DB transactions
- `ShouldUseCustomKeyFunction()` - configurable deduplication key

#### 1.2 Hash Generation (Idempotency Key)
**Service**: `TransactionHashGenerator` (to be created)
**Purpose**: Generate deterministic hash for transactions to ensure idempotent operations

**Test Cases**:
- `ShouldGenerateSameHashForIdenticalTransactions()` - deterministic output
- `ShouldGenerateDifferentHashesForDifferentTransactions()` - change one field → different hash
- `ShouldHandleNullsConsistently()` - null descriptions produce same hash
- `ShouldGenerateUrlSafeHash()` - safe for URLs/headers
- `ShouldIgnoreTransientFields()` - hash ignores Id, Timestamp precision, etc.

#### 1.3 Budget Versioning (Optimistic Concurrency)
**Service**: `BudgetVersioningService` (to be created)
**Purpose**: Track budget versions and detect concurrent modifications

**Test Cases**:
- `ShouldReturnVersionAfterBudgetCreation()` - initial version = 1
- `ShouldIncrementVersionOnUpdate()` - each update bumps version
- `ShouldDetectConflictOnStaleVersion()` - update with old version → conflict
- `ShouldAllowConcurrentReadsWithoutConflict()` - multiple readers, no contention
- `ShouldMergeNonConflictingUpdates()` - update different fields concurrently
- `ShouldRejectConflictingCategoryChanges()` - category updated twice → reject second
- `ShouldProvideNewVersionAfterResolvedConflict()` - failed update yields current version

#### 1.4 Event Creation & Sequencing
**Service**: `ActivityEventFactory` (to be created)
**Purpose**: Create activity events with correct sequence IDs, payload shape, timestamps

**Test Cases**:
- `ShouldCreateEventWithCorrectSeqId()` - auto-incremented sequence
- `ShouldFormatPayloadAsJson()` - correct JSON envelope structure
- `ShouldIncludeActorAndTimestamp()` - required fields present
- `ShouldGenerateSummaryNotFullTransaction()` - no IDs, family-friendly summary
- `ShouldHandleSpecialCharactersInDescription()` - escaping/sanitization
- `ShouldTruncateLongDescriptions()` - description has max length
- `ShouldMaintainSeqIdOrderingUnderConcurrency()` - seq IDs monotonic

### Tier 2: Integration & Behavioral (Unit + Mocked Infrastructure)
These are tested primarily via unit tests with mocked WebSocket/DB, plus integration tests:

#### 2.1 ConnectionManager
**Service**: `WebSocketConnectionManager`
**Tests**:
- `ShouldAddAndRetrieveSocket()` - add/get/remove operations
- `ShouldHandleMultipleConnections()` - concurrent adds
- `ShouldRemoveSocketOnDisconnect()` - remove from tracking
- `ShouldReturnEmptyWhenNoConnections()` - idempotent removes
- `ShouldAllowSubscriptionFiltering()` - clients opt into event types

#### 2.2 MessageHandler
**Service**: `MessageHandler` (routing/deserialization)
**Tests**:
- `ShouldDeserializeJsonEnvelope()` - parse { type, payload }
- `ShouldRouteToTransactionHandler()` - type=CreateTransaction
- `ShouldRouteToSubscriptionHandler()` - type=Subscribe
- `ShouldRejectMalformedMessages()` - invalid JSON → error response
- `ShouldValidateActorField()` - username required
- `ShouldIgnoreUnknownMessageTypes()` - graceful fallback

#### 2.3 Event Log & Replay
**Service**: `EventLog` (to be created)
**Purpose**: Bounded log for reliable replay on reconnect

**Tests**:
- `ShouldAppendEventToLog()` - new event added with seq_id
- `ShouldReturnEventsAfterSeqId()` - client request since: seq_5 → returns 6+
- `ShouldEnforceBoundedLogSize()` - max 10k events, oldest pruned
- `ShouldReturnErrorWhenSeqIdOutsideBound()` - replay window exceeded
- `ShouldMaintainOrderingAfterPrune()` - seq IDs still monotonic

### Tier 3: End-to-End Integration Tests
These test the full flow with real HTTP/WebSocket:

#### 3.1 WebSocket Reconnect & Replay Flow
**Scenario**: Client disconnects after event #5, reconnects, requests replay
- `ShouldReplayMissedEventsOnReconnect()` - client gets events 6-N
- `ShouldHandleReconnectDuringMassTransaction()` - high event volume
- `ShouldBroadcastToAllClientsExceptOriginator()` - correct recipient list

#### 3.2 Concurrent Transaction Creation
**Scenario**: Two clients create transactions simultaneously
- `ShouldPersistBothTransactions()` - no loss of data
- `ShouldBroadcastBothEventsInOrder()` - correct seq_id order
- `ShouldRecipientsSeeConsistentOrder()` - all clients see same order

#### 3.3 Optimistic Concurrency Conflict
**Scenario**: Client A and B update same budget concurrently
- `ShouldRejectSecondUpdate()` - B gets conflict error
- `ShouldProvideCurrentVersionToRetry()` - B can retry with new version
- `ShouldNotPersistConflictingChange()` - DB consistent

## Test File Structure

```
test/RealTimeDashboard.Tests/
├── Unit/
│   ├── Deduplication/
│   │   └── TransactionDeduplicationServiceTests.cs
│   ├── Hashing/
│   │   └── TransactionHashGeneratorTests.cs
│   ├── Versioning/
│   │   └── BudgetVersioningServiceTests.cs
│   ├── Events/
│   │   └── ActivityEventFactoryTests.cs
│   │   └── EventLogTests.cs
│   ├── Realtime/
│   │   ├── WebSocketConnectionManagerTests.cs
│   │   └── MessageHandlerTests.cs
│   └── TransactionProcessorTests.cs (existing, will extend)
└── Integration/
    ├── WebSocketFlowTests.cs (reconnect, replay)
    ├── ConcurrentTransactionTests.cs (parallel creates)
    ├── OptimisticConcurrencyTests.cs (conflict resolution)
    └── Helpers/
        └── TestWebSocketClient.cs (existing, will enhance)
```

## Dependencies & Mocking Strategy

### Mocks Required
- `ITransactionStore` - return configured transactions
- `IActivityFeedService` - capture published events
- `IBudgetCalculator` - return predetermined remainings
- `WebSocket` - mock send/receive for MessageHandler tests
- `FinanceDbContext` - use in-memory SQLite for integration tests

### Real vs. Mocked
| Component | Unit | Integration |
|-----------|------|-------------|
| `TransactionDeduplicationService` | Real | Real |
| `TransactionHashGenerator` | Real | Real |
| `BudgetVersioningService` | Real | Real |
| `ActivityEventFactory` | Real | Real |
| `WebSocketConnectionManager` | Real (simple) | Real |
| `MessageHandler` | Mocked `WebSocket` | Real |
| `ITransactionStore` | Mocked | Real (in-memory DB) |
| `FinanceDbContext` | N/A | In-memory SQLite |

## Test Data & Fixtures

### Fixture: Sample Transactions
```csharp
var tx1 = new Transaction 
{ 
    Timestamp = new(2024, 1, 15), 
    Amount = 50.00m, 
    Description = "Groceries", 
    CreatedBy = "Alice" 
};
```

### Fixture: Budget with Version
```csharp
var budget = new Budget 
{ 
    Limit = 1000m, 
    Version = 1, 
    CategoryId = 1 
};
```

## Acceptance Criteria

1. **All Tier 1 tests pass** - deduplication, hashing, versioning, events
2. **80%+ branch coverage** for correctness-critical services (Tier 1)
3. **At least one happy-path + one failure path** per Tier 2 test
4. **End-to-end WebSocket test** confirms reconnect + broadcast + ordering
5. **No placeholder tests** - all tests exercise real logic, not just `Assert.True(true)`

## Notes

- Tests use **Xunit** (existing framework)
- Mocking via **Moq** (already in dependencies)
- Database tests use **in-memory SQLite** (existing in `.csproj`)
- All tests should be isolated: no shared mutable state, use factories/fixtures
- Integration tests may be slower; mark with `[Trait("Category", "Integration")]` for optional filtering

