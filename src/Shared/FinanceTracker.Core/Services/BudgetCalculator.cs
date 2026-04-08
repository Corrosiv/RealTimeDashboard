namespace FinanceTracker.Core.Services;

public class BudgetCalculator : IBudgetCalculator
{
    public decimal CalculateRemaining(decimal limit, IEnumerable<decimal> transactions)
    {
        return limit - transactions.Sum();
    }
}
