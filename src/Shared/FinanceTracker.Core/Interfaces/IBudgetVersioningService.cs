namespace FinanceTracker.Core.Interfaces;

using FinanceTracker.Core.Domain;

/// <summary>
/// Manages optimistic concurrency for budgets.
/// Detects conflicts when multiple clients update the same budget.
/// </summary>
public interface IBudgetVersioningService
{
    /// <summary>
    /// Checks if an update is valid given the current version.
    /// </summary>
    /// <param name="budget">Current budget from database.</param>
    /// <param name="clientVersion">Version the client believes it's updating.</param>
    /// <returns>True if client version matches current; false if conflict.</returns>
    bool IsVersionValid(Budget budget, int clientVersion);

    /// <summary>
    /// Increments the version after a successful update.
    /// </summary>
    /// <param name="budget">Budget to update.</param>
    int IncrementVersion(Budget budget);

    /// <summary>
    /// Detects if a category-specific update would conflict with another in-flight update.
    /// </summary>
    /// <param name="budgetId">Budget being updated.</param>
    /// <param name="clientVersion">Client's version.</param>
    /// <param name="currentVersion">Current version in database.</param>
    /// <returns>Conflict info if versions don't match.</returns>
    ConcurrencyConflict? DetectConflict(int budgetId, int clientVersion, int currentVersion);
}

/// <summary>
/// Details about an optimistic concurrency conflict.
/// </summary>
public class ConcurrencyConflict
{
    /// <summary>
    /// Current version in the database.
    /// </summary>
    public int CurrentVersion { get; set; }

    /// <summary>
    /// Version the client was trying to update.
    /// </summary>
    public int ClientVersion { get; set; }

    /// <summary>
    /// Human-readable message about the conflict.
    /// </summary>
    public string Message { get; set; } = null!;
}
