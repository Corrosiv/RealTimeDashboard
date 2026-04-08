namespace FinanceTracker.Core.Services;

public interface IBudgetCalculator
{
    decimal CalculateRemaining(decimal limit, IEnumerable<decimal> transactions);
}
