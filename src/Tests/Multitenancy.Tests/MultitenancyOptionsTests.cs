using FSH.Modules.Multitenancy;

namespace Multitenancy.Tests;

/// <summary>
/// Tests for MultitenancyOptions - configuration for multitenancy behavior.
/// </summary>
public sealed class MultitenancyOptionsTests
{
    [Fact]
    public void DefaultValues_Should_BeSet()
    {
        // Arrange & Act
        var options = new MultitenancyOptions();

        // Assert
        options.RunTenantMigrationsOnStartup.ShouldBeFalse();
        options.AutoProvisionOnStartup.ShouldBeTrue();
    }

    [Fact]
    public void RunTenantMigrationsOnStartup_Should_BeSettable()
    {
        // Arrange
        var options = new MultitenancyOptions
        {
            RunTenantMigrationsOnStartup = true
        };

        // Assert
        options.RunTenantMigrationsOnStartup.ShouldBeTrue();
    }

    [Fact]
    public void AutoProvisionOnStartup_Should_BeSettable()
    {
        // Arrange
        var options = new MultitenancyOptions
        {
            AutoProvisionOnStartup = false
        };

        // Assert
        options.AutoProvisionOnStartup.ShouldBeFalse();
    }

    [Fact]
    public void BothOptions_Can_BeEnabled()
    {
        // Arrange
        var options = new MultitenancyOptions
        {
            RunTenantMigrationsOnStartup = true,
            AutoProvisionOnStartup = true
        };

        // Assert
        options.RunTenantMigrationsOnStartup.ShouldBeTrue();
        options.AutoProvisionOnStartup.ShouldBeTrue();
    }

    [Fact]
    public void BothOptions_Can_BeDisabled()
    {
        // Arrange
        var options = new MultitenancyOptions
        {
            RunTenantMigrationsOnStartup = false,
            AutoProvisionOnStartup = false
        };

        // Assert
        options.RunTenantMigrationsOnStartup.ShouldBeFalse();
        options.AutoProvisionOnStartup.ShouldBeFalse();
    }
}
