using Xunit;
using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Services;

namespace RealTimeDashboard.Tests.Unit;

/// <summary>
/// Unit tests for BudgetVersioningService.
/// Critical for optimistic concurrency conflict detection.
/// </summary>
public class BudgetVersioningServiceTests
{
    private readonly BudgetVersioningService _versioningService = new();

    [Fact]
    public void IsVersionValid_WithMatchingVersion_ReturnsTrue()
    {
        // Arrange
        var budget = new Budget { Id = 1, CategoryId = 1, LimitAmount = 1000m, Version = 5 };
        var clientVersion = 5;

        // Act
        var isValid = _versioningService.IsVersionValid(budget, clientVersion);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsVersionValid_WithMismatchedVersion_ReturnsFalse()
    {
        // Arrange
        var budget = new Budget { Id = 1, CategoryId = 1, LimitAmount = 1000m, Version = 5 };
        var clientVersion = 4;

        // Act
        var isValid = _versioningService.IsVersionValid(budget, clientVersion);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IncrementVersion_IncrementsVersionByOne()
    {
        // Arrange
        var budget = new Budget { Id = 1, CategoryId = 1, LimitAmount = 1000m, Version = 5 };
        var originalVersion = budget.Version;

        // Act
        var newVersion = _versioningService.IncrementVersion(budget);

        // Assert
        Assert.Equal(originalVersion + 1, newVersion);
        Assert.Equal(newVersion, budget.Version);
    }

    [Fact]
    public void IncrementVersion_UpdatesTimestamp()
    {
        // Arrange
        var budget = new Budget 
        { 
            Id = 1, 
            CategoryId = 1, 
            LimitAmount = 1000m, 
            Version = 1,
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        var oldTimestamp = budget.UpdatedAt;

        // Act
        _versioningService.IncrementVersion(budget);

        // Assert: UpdatedAt should be more recent
        Assert.True(budget.UpdatedAt > oldTimestamp);
    }

    [Fact]
    public void DetectConflict_WithMatchingVersions_ReturnsNull()
    {
        // Arrange
        var budgetId = 1;
        var clientVersion = 5;
        var currentVersion = 5;

        // Act
        var conflict = _versioningService.DetectConflict(budgetId, clientVersion, currentVersion);

        // Assert
        Assert.Null(conflict);
    }

    [Fact]
    public void DetectConflict_WithMismatchedVersions_ReturnConflictInfo()
    {
        // Arrange
        var budgetId = 1;
        var clientVersion = 4;
        var currentVersion = 5;

        // Act
        var conflict = _versioningService.DetectConflict(budgetId, clientVersion, currentVersion);

        // Assert
        Assert.NotNull(conflict);
        Assert.Equal(clientVersion, conflict.ClientVersion);
        Assert.Equal(currentVersion, conflict.CurrentVersion);
        Assert.NotEmpty(conflict.Message);
    }

    [Fact]
    public void DetectConflict_Message_ContainsVersionNumbers()
    {
        // Arrange
        var clientVersion = 3;
        var currentVersion = 7;

        // Act
        var conflict = _versioningService.DetectConflict(1, clientVersion, currentVersion);

        // Assert
        Assert.Contains("7", conflict!.Message);
        Assert.Contains("3", conflict.Message);
    }

    [Fact]
    public void Version_StartsAt1_ByDefault()
    {
        // Arrange & Act
        var budget = new Budget { Id = 1, CategoryId = 1, LimitAmount = 1000m };

        // Assert
        Assert.Equal(1, budget.Version);
    }

    [Fact]
    public void MultipleIncrements_MonotonicallyIncreaseVersion()
    {
        // Arrange
        var budget = new Budget { Id = 1, CategoryId = 1, LimitAmount = 1000m, Version = 1 };

        // Act
        var v1 = _versioningService.IncrementVersion(budget);
        var v2 = _versioningService.IncrementVersion(budget);
        var v3 = _versioningService.IncrementVersion(budget);

        // Assert
        Assert.Equal(2, v1);
        Assert.Equal(3, v2);
        Assert.Equal(4, v3);
        Assert.Equal(4, budget.Version);
    }
}
