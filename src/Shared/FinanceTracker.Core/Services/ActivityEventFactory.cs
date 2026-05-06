namespace FinanceTracker.Core.Services;

using System.Text.Json;
using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Interfaces;

/// <summary>
/// Creates properly-formatted ActivityEvents for event sourcing.
/// </summary>
public class ActivityEventFactory : IActivityEventFactory
{
    private const int MaxDescriptionLength = 200;

    /// <inheritdoc/>
    public ActivityEvent CreateTransactionCreatedEvent(Transaction transaction, ActorInfo actor, long sequenceId)
    {
        var summary = TruncateDescription(transaction.Description);
        var message = $"{actor.DisplayName ?? actor.Username} recorded {GetAmountDisplay(transaction.Amount)} {GetCurrencyDisplay(transaction.Currency)}: {summary}";

        var metadata = JsonSerializer.Serialize(new
        {
            amount = transaction.Amount,
            currency = transaction.Currency,
            categoryId = transaction.CategoryId,
            source = transaction.Source
        });

        return CreateEvent(
            eventType: "TransactionCreated",
            entityId: transaction.Id,
            message: message,
            actor: actor,
            sequenceId: sequenceId,
            version: null,
            metadata: metadata
        );
    }

    /// <inheritdoc/>
    public ActivityEvent CreateCsvImportedEvent(int importedCount, ActorInfo actor, long sequenceId, string? metadata = null)
    {
        var message = $"{actor.DisplayName ?? actor.Username} imported {importedCount} transaction{(importedCount != 1 ? "s" : "")}";

        return CreateEvent(
            eventType: "CsvImported",
            entityId: 0,  // No specific entity
            message: message,
            actor: actor,
            sequenceId: sequenceId,
            version: null,
            metadata: metadata
        );
    }

    /// <inheritdoc/>
    public ActivityEvent CreateBudgetExceededEvent(Budget budget, decimal currentSpent, long sequenceId)
    {
        var overage = currentSpent - budget.LimitAmount;
        var message = $"Budget alert: spent {GetAmountDisplay(currentSpent)}, exceeded {GetAmountDisplay(budget.LimitAmount)} by {GetAmountDisplay(overage)}";

        var metadata = JsonSerializer.Serialize(new
        {
            limitAmount = budget.LimitAmount,
            currentSpent = currentSpent,
            period = budget.Period
        });

        return CreateEvent(
            eventType: "BudgetExceeded",
            entityId: budget.Id,
            message: message,
            actor: new ActorInfo { Username = "system", DisplayName = "System", JoinedAt = DateTime.UtcNow },
            sequenceId: sequenceId,
            version: budget.Version,
            metadata: metadata
        );
    }

    /// <inheritdoc/>
    public ActivityEvent CreateEvent(
        string eventType,
        int entityId,
        string message,
        ActorInfo actor,
        long sequenceId,
        int? version = null,
        string? metadata = null)
    {
        return new ActivityEvent
        {
            EventId = GenerateEventId(),
            SequenceId = sequenceId,
            Type = eventType,
            EntityId = entityId,
            Scope = "default",
            Actor = actor,
            Message = message,
            CreatedAt = DateTime.UtcNow,
            Version = version,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Generates a unique EventId (UUID format for reliability).
    /// </summary>
    private string GenerateEventId() => Guid.NewGuid().ToString("D");

    /// <summary>
    /// Truncates long descriptions to protect privacy and keep messages concise.
    /// </summary>
    private string TruncateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "(no description)";

        if (description.Length <= MaxDescriptionLength)
            return description;

        return description[..MaxDescriptionLength] + "…";
    }

    /// <summary>
    /// Formats amount for display (without currency symbol).
    /// </summary>
    private string GetAmountDisplay(decimal amount)
    {
        return amount.ToString("F2");
    }

    /// <summary>
    /// Formats currency for display.
    /// </summary>
    private string GetCurrencyDisplay(string? currency)
    {
        return string.IsNullOrWhiteSpace(currency) ? "USD" : currency.ToUpperInvariant();
    }
}
