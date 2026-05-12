# Copilot Instructions

## Project Guidelines
- User preferences and workspace: Preferred terminal shell is powershell.exe; Workspace root: C:\Users\Admin\source\repos\RealTimeDashboard\; Projects target .NET 10.
- Use approach A: reuse and modify the existing FinanceTracker project (FinanceTracker.Core) directly in RealTimeDashboard.API; permission is granted to modify FinanceTracker as needed.
- Reuse the existing WebSocket chat implementation (MessageHandler + ConnectionManager) if compatible with RealTimeDashboard; it utilizes raw System.Net.WebSockets, JSON envelope, 4KB buffer, and does not handle EndOfMessage.

## Project Structure & Solution Organization
- Single solution file: `RealTimeDashboard.sln` is the authoritative reference for all projects
- Always use the full solution path when running dotnet commands: `dotnet build RealTimeDashboard.sln` or `dotnet test RealTimeDashboard.sln`
- Production code: All projects live in `src/` (RealTimeDashboard.API, Shared/FinanceTracker.Core)
- Test code: All tests live in `test/RealTimeDashboard.Tests/` (single test project, authoritative)
- **No root-level projects**: Never reference orphaned or root-level projects; always use src/ and test/ subdirectories
- See `DIRECTORY-STRUCTURE.md` for detailed project layout and structure
- When gathering context about tests, always use `get_projects_in_solution` first, then target the test project in `test/RealTimeDashboard.Tests/`