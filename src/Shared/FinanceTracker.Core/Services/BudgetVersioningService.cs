namespace FinanceTracker.Core.Services;

using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Interfaces;

/// <summary>
/// Manages optimistic concurrency for budgets.
/// </summary>
public class BudgetVersioningService : IBudgetVersioningService
{
    /// <inheritdoc/>
    public bool IsVersionValid(Budget budget, int clientVersion)
    {
        return budget.Version == clientVersion;
    }

    /// <inheritdoc/>
    public int IncrementVersion(Budget budget)
    {
        budget.Version += 1;
        budget.UpdatedAt = DateTime.UtcNow;
        return budget.Version;
    }

    /// <inheritdoc/>
    public ConcurrencyConflict? DetectConflict(int budgetId, int clientVersion, int currentVersion)
    {
        if (clientVersion != currentVersion)
        {
            return new ConcurrencyConflict
            {
                ClientVersion = clientVersion,
                CurrentVersion = currentVersion,
                Message = $"Version conflict: expected {currentVersion}, got {clientVersion}. " +
                          $"The budget was modified by another client. Please refresh and retry."
            };
        }

        return null;
    }
}
