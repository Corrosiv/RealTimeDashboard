# Copilot Instructions

## Project Guidelines
- User preferences and workspace: Preferred terminal shell is powershell.exe; Workspace root: C:\Users\Admin\source\repos\RealTimeDashboard\; Projects target .NET 10.
- Use approach A: reuse and modify the existing FinanceTracker project (FinanceTracker.Core) directly in RealTimeDashboard.API; permission is granted to modify FinanceTracker as needed.
- Reuse the existing WebSocket chat implementation (MessageHandler + ConnectionManager) if compatible with RealTimeDashboard; it utilizes raw System.Net.WebSockets, JSON envelope, 4KB buffer, and does not handle EndOfMessage.