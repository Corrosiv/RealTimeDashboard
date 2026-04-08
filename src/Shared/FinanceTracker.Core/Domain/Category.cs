namespace FinanceTracker.Core.Domain;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? ParentCategoryId { get; set; }
}
