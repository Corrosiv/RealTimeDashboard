using FluentValidation;
using RealTimeDashboard.API.DTOs;

namespace RealTimeDashboard.API.Validators;

/// <summary>
/// Validator for TransactionQueryRequest.
/// Ensures filtering, pagination, and sorting parameters are valid.
/// </summary>
public class TransactionQueryRequestValidator : AbstractValidator<TransactionQueryRequest>
{
    public TransactionQueryRequestValidator()
    {
        // DateFrom and DateTo validation
        RuleFor(x => x.DateFrom)
            .LessThanOrEqualTo(x => x.DateTo)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("DateFrom must be on or before DateTo.");

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("DateTo must be on or after DateFrom.");

        // Amount filtering
        RuleFor(x => x.MinAmount)
            .LessThanOrEqualTo(x => x.MaxAmount)
            .When(x => x.MinAmount.HasValue && x.MaxAmount.HasValue)
            .WithMessage("MinAmount must be less than or equal to MaxAmount.");

        // Pagination
        RuleFor(x => x.Limit)
            .GreaterThan(0).WithMessage("Limit must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Limit cannot exceed 100.");

        // Sort validation
        RuleFor(x => x.Sort)
            .NotEmpty().WithMessage("Sort cannot be empty.")
            .Matches(@"^timestamp:(asc|desc)$")
            .WithMessage("Sort must be in format 'timestamp:asc' or 'timestamp:desc'.");

        // Search text
        RuleFor(x => x.Search)
            .MaximumLength(200).WithMessage("Search text cannot exceed 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        // Cursor (if present, should be non-empty)
        RuleFor(x => x.Cursor)
            .NotEmpty().WithMessage("Cursor, if provided, cannot be empty.")
            .When(x => x.Cursor != null);
    }
}
