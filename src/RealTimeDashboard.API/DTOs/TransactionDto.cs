namespace RealTimeDashboard.API.DTOs;

public record TransactionDto(int Id, DateTime Timestamp, decimal Amount, string? Description, int? CategoryId, string CreatedBy);
