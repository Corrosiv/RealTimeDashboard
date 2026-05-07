using FluentValidation;
using RealTimeDashboard.API.DTOs;

namespace RealTimeDashboard.API.Validators;

/// <summary>
/// Validator for CreateTransactionRequest.
/// Ensures new transactions have valid data before persistence.
/// </summary>
public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        // Timestamp validation
        RuleFor(x => x.Timestamp)
            .NotEmpty().WithMessage("Timestamp is required.")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Timestamp cannot be in the future.");

        // Amount validation
        RuleFor(x => x.Amount)
            .NotEmpty().WithMessage("Amount is required.")
            .NotEqual(0).WithMessage("Amount cannot be zero.");

        // Currency validation
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(2, 5).WithMessage("Currency code must be 2-5 characters (e.g., 'USD', 'EUR').");

        // Description validation
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        // CreatedBy validation
        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy (username) is required.")
            .MinimumLength(1).WithMessage("Username must be at least 1 character.")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.");

        // Source validation
        RuleFor(x => x.Source)
            .MaximumLength(50).WithMessage("Source cannot exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Source));
    }
}
