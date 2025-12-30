using FluentValidation;

namespace FSH.Modules.Identity.Features.v1.Tokens.Impersonation;

public sealed class ImpersonateUserCommandValidator : AbstractValidator<Contracts.v1.Tokens.Impersonation.ImpersonateUserCommand>
{
    public ImpersonateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required")
            .Must(BeValidGuid).WithMessage("User ID must be a valid GUID");
    }

    private static bool BeValidGuid(string userId)
    {
        return Guid.TryParse(userId, out _);
    }
}
