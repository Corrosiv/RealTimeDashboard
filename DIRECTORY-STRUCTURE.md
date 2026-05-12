# Directory Structure

This document describes the organization of the RealTimeDashboard project.

## Overview

```
RealTimeDashboard/
├── src/                              # All production source code
│   ├── RealTimeDashboard.API/       # Main ASP.NET Core Web API
│   │   ├── ActivityFeed/            # Activity feed models and logic
│   │   ├── Controllers/             # API endpoint controllers
│   │   ├── DomainEvents/            # Domain event types
│   │   ├── DTOs/                    # Data transfer objects (request/response)
│   │   ├── EventHandlers/           # Event handlers for domain events
│   │   ├── Extensions/              # Extension methods and configurations
│   │   ├── Infrastructure/          # Cross-cutting concerns (utilities, configs)
│   │   ├── Interfaces/              # Abstraction contracts
│   │   ├── Middleware/              # HTTP middleware
│   │   ├── Realtime/                # WebSocket connection management
│   │   ├── Services/                # Business logic services
│   │   ├── Validators/              # FluentValidation validators
│   │   ├── Program.cs               # Application startup
│   │   ├── appsettings.json         # Configuration
│   │   └── RealTimeDashboard.API.csproj
│   └── Shared/
│       └── FinanceTracker.Core/     # Shared domain library (imported from FinanceTracker)
│           └── FinanceTracker.Core.csproj
├── test/                            # All test code
│   └── RealTimeDashboard.Tests/    # Comprehensive unit and integration tests
│       ├── Unit/                    # Unit tests
│       │   ├── CsvProcessing/       # CSV parsing and import tests
│       │   ├── Domain/              # Domain logic tests
│       │   ├── Realtime/            # WebSocket tests
│       │   └── Services/            # Service tests
│       ├── Integration/             # Integration tests
│       └── RealTimeDashboard.Tests.csproj
├── docs/                            # Documentation (architecture, API, design)
├── .github/                         # GitHub Actions workflows
├── RealTimeDashboard.sln            # Solution file (single source of truth)
├── TODO.md                          # Project roadmap and progress tracker
├── README.md                        # Project overview and quick start
└── DIRECTORY-STRUCTURE.md           # This file
```

## Key Principles

1. **Single Solution File**: All projects are referenced in `RealTimeDashboard.sln` only. No duplicate or orphaned project references.

2. **Clear Separation**:
   - **src/** contains production code (API, services, domain logic)
   - **test/** contains all tests (unit, integration, end-to-end)

3. **No Root-Level Projects**: All projects are in proper subdirectories (`src/`, `test/`, `Shared/`), not at the solution root.

4. **Consistent Naming**: 
   - Test project: `RealTimeDashboard.Tests` (contains all tests)
   - API project: `RealTimeDashboard.API`
   - Shared library: `FinanceTracker.Core`

## Solution Structure

### Production Projects

- **RealTimeDashboard.API** (`src/RealTimeDashboard.API/`)
  - ASP.NET Core Web API (.NET 10)
  - Real-time WebSocket support
  - CSV import and processing
  - Transaction management
  - Activity feed

- **FinanceTracker.Core** (`src/Shared/FinanceTracker.Core/`)
  - Shared domain models (Transaction, Category, etc.)
  - Data access abstraction
  - Business logic utilities
  - Imported from FinanceTracker repository

### Test Projects

- **RealTimeDashboard.Tests** (`test/RealTimeDashboard.Tests/`)
  - Comprehensive unit tests (300+ lines of code)
  - Integration tests for API and WebSocket
  - CSV processing edge cases
  - Data validation and error scenarios
  - 126+ tests, all passing

## Building and Testing

### Build
```powershell
cd RealTimeDashboard
dotnet build RealTimeDashboard.sln
```

### Run Tests
```powershell
dotnet test RealTimeDashboard.sln
```

### Run API
```powershell
dotnet run --project src/RealTimeDashboard.API/RealTimeDashboard.API.csproj
```

## Important Notes

- **No Duplicate Projects**: The orphaned `RealTimeDashboard.API.Tests/` (at root level) has been removed.
- **Solution References**: Always use `RealTimeDashboard.sln` as the authoritative project reference.
- **IntelliSense**: When using VS Code or Visual Studio, the structure should now be clear and consistent.
- **CI/CD**: GitHub Actions workflows reference projects by their solution-relative paths, which are now stable and unambiguous.

## Future Structure

When Phase 1.5 frontend is implemented:
```
RealTimeDashboard/
├── src/
│   ├── RealTimeDashboard.API/
│   └── Shared/FinanceTracker.Core/
├── frontend/                        # NEW: Minimalist HTML/JS frontend
│   ├── index.html
│   ├── client.js
│   └── styles.css
├── test/
│   └── RealTimeDashboard.Tests/
└── ...
```
