using FSH.Modules.Identity.Contracts.v1.Tokens.RefreshToken;
using FSH.Modules.Identity.Features.v1.Tokens.RefreshToken;

namespace Identity.Tests.Validators;

/// <summary>
/// Tests for RefreshTokenCommandValidator - validates token refresh requests.
/// </summary>
public sealed class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _sut = new();

    #region Token Validation

    [Fact]
    public void Token_Should_Pass_When_Valid()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-jwt-token", "valid-refresh-token");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "Token");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Token_Should_Fail_When_Empty(string? token)
    {
        // Arrange
        var command = new RefreshTokenCommand(token!, "valid-refresh-token");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Token");
    }

    #endregion

    #region RefreshToken Validation

    [Fact]
    public void RefreshToken_Should_Pass_When_Valid()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-jwt-token", "valid-refresh-token");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.Errors.ShouldNotContain(e => e.PropertyName == "RefreshToken");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void RefreshToken_Should_Fail_When_Empty(string? refreshToken)
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-jwt-token", refreshToken!);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "RefreshToken");
    }

    #endregion

    #region Combined Validation

    [Fact]
    public void Validate_Should_Pass_When_AllFieldsValid()
    {
        // Arrange
        var command = new RefreshTokenCommand(
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U",
            "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4=");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Validate_Should_Fail_When_AllFieldsEmpty()
    {
        // Arrange
        var command = new RefreshTokenCommand("", "");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(2);
    }

    [Fact]
    public void Validate_Should_Fail_When_TokenEmpty_RefreshTokenValid()
    {
        // Arrange
        var command = new RefreshTokenCommand("", "valid-refresh-token");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.ShouldContain(e => e.PropertyName == "Token");
    }

    [Fact]
    public void Validate_Should_Fail_When_TokenValid_RefreshTokenEmpty()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid-jwt-token", "");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors.ShouldContain(e => e.PropertyName == "RefreshToken");
    }

    #endregion
}
