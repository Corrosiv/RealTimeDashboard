using FluentValidation;
using RealTimeDashboard.API.DTOs;

namespace RealTimeDashboard.API.Validators;

/// <summary>
/// Validator for ActivityFeedQueryRequest.
/// Ensures all filter and pagination parameters are valid.
/// </summary>
public class ActivityFeedQueryRequestValidator : AbstractValidator<ActivityFeedQueryRequest>
{
    public ActivityFeedQueryRequestValidator()
    {
        // Validate Limit
        RuleFor(r => r.Limit)
            .GreaterThanOrEqualTo(1).WithMessage("Limit must be at least 1.")
            .LessThanOrEqualTo(200).WithMessage("Limit cannot exceed 200.");

        // Validate Cursor (if provided, should be non-empty)
        RuleFor(r => r.Cursor)
            .Must(c => string.IsNullOrWhiteSpace(c) || !string.IsNullOrEmpty(c))
            .WithMessage("Cursor must be a valid base64-encoded string.");

        // Validate Since/Until (Since should be before Until if both provided)
        RuleFor(r => new { r.Since, r.Until })
            .Must(pair => !pair.Since.HasValue || !pair.Until.HasValue || pair.Since.Value <= pair.Until.Value)
            .WithMessage("Since must be before or equal to Until.");

        // Validate CreatedBy (max length)
        RuleFor(r => r.CreatedBy)
            .MaximumLength(100).WithMessage("CreatedBy cannot exceed 100 characters.")
            .When(r => !string.IsNullOrWhiteSpace(r.CreatedBy));

        // Validate EventType (max length)
        RuleFor(r => r.EventType)
            .MaximumLength(500).WithMessage("EventType cannot exceed 500 characters.")
            .When(r => !string.IsNullOrWhiteSpace(r.EventType));

        // Validate ResourceId (positive integer)
        RuleFor(r => r.ResourceId)
            .GreaterThan(0).WithMessage("ResourceId must be a positive integer.")
            .When(r => r.ResourceId.HasValue);
    }
}
