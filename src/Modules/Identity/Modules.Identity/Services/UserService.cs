using FSH.Framework.Shared.Storage;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using System.Security.Claims;

namespace FSH.Modules.Identity.Services;

/// <summary>
/// Facade service that delegates to focused single-responsibility services.
/// Maintained for backward compatibility with existing consumers.
/// </summary>
internal sealed class UserService(
    IUserRegistrationService registrationService,
    IUserProfileService profileService,
    IUserStatusService statusService,
    IUserRoleService roleService,
    IUserPasswordService passwordService,
    IUserPermissionService permissionService) : IUserService
{
<<<<<<< HEAD
    private readonly Uri? _originUrl = originOptions.Value.OriginUrl;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IAuditClient _auditClient = auditClient;
    private readonly IPasswordHistoryService _passwordHistoryService = passwordHistoryService;
    private readonly IPasswordExpiryService _passwordExpiryService = passwordExpiryService;

    private void EnsureValidTenant()
    {
        if (string.IsNullOrWhiteSpace(multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id))
        {
            throw new UnauthorizedException("invalid tenant");
        }
    }

    public async Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken cancellationToken)
    {
        EnsureValidTenant();

        var user = await userManager.Users
            .Where(u => u.Id == userId && !u.EmailConfirmed)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new CustomException("An error occurred while confirming E-Mail.");

        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await userManager.ConfirmEmailAsync(user, code);

        return result.Succeeded
            ? string.Format(CultureInfo.InvariantCulture, "Account Confirmed for E-Mail {0}. You can now use the /api/tokens endpoint to generate JWT.", user.Email)
            : throw new CustomException(string.Format(CultureInfo.InvariantCulture, "An error occurred while confirming {0}", user.Email));
    }

    public async Task<string> AdminConfirmEmailAsync(string userId, string tenant, CancellationToken cancellationToken)
    {
        EnsureValidTenant();

        var user = await userManager.Users
            .Where(u => u.Id == userId && !u.EmailConfirmed)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new CustomException("An error occurred while confirming E-Mail.");

        user.EmailConfirmed = true;

        return string.Format(CultureInfo.InvariantCulture, "Account Confirmed for E-Mail {0}. You can now use the /api/tokens endpoint to generate JWT.", user.Email);
    }

    public Task<string> ConfirmPhoneNumberAsync(string userId, string code)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null)
    {
        EnsureValidTenant();
        return await userManager.FindByEmailAsync(email.Normalize()) is FshUser user && user.Id != exceptId;
    }

    public async Task<bool> ExistsWithNameAsync(string name)
    {
        EnsureValidTenant();
        return await userManager.FindByNameAsync(name) is not null;
    }

    public async Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null)
    {
        EnsureValidTenant();
        return await userManager.Users.FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber) is FshUser user && user.Id != exceptId;
    }

    public async Task<UserDto> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userManager.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException("user not found");

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ImageUrl = ResolveImageUrl(user.ImageUrl),
            IsActive = user.IsActive
        };
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        userManager.Users.AsNoTracking().CountAsync(cancellationToken);

    public async Task<List<UserDto>> GetListAsync(CancellationToken cancellationToken)
    {
        var users = await userManager.Users.AsNoTracking().ToListAsync(cancellationToken);
        var result = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImageUrl = ResolveImageUrl(user.ImageUrl),
                IsActive = user.IsActive
            });
        }

        return result;
    }
=======
    // Registration operations (delegated to IUserRegistrationService)
    public Task<string> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string userName,
        string password,
        string confirmPassword,
        string phoneNumber,
        string origin,
        CancellationToken cancellationToken)
        => registrationService.RegisterAsync(firstName, lastName, email, userName, password, confirmPassword, phoneNumber, origin, cancellationToken);
>>>>>>> develop

    public Task<string> GetOrCreateFromPrincipalAsync(ClaimsPrincipal principal)
        => registrationService.GetOrCreateFromPrincipalAsync(principal);

    public Task<string> ConfirmEmailAsync(string userId, string code, string tenant, CancellationToken cancellationToken)
        => registrationService.ConfirmEmailAsync(userId, code, tenant, cancellationToken);

    public Task<string> ConfirmPhoneNumberAsync(string userId, string code)
        => registrationService.ConfirmPhoneNumberAsync(userId, code);

    // Profile operations (delegated to IUserProfileService)
    public Task<UserDto> GetAsync(string userId, CancellationToken cancellationToken)
        => profileService.GetAsync(userId, cancellationToken);

    public Task<List<UserDto>> GetListAsync(CancellationToken cancellationToken)
        => profileService.GetListAsync(cancellationToken);

    public Task<int> GetCountAsync(CancellationToken cancellationToken)
        => profileService.GetCountAsync(cancellationToken);

    public Task UpdateAsync(string userId, string firstName, string lastName, string phoneNumber, FileUploadRequest image, bool deleteCurrentImage)
        => profileService.UpdateAsync(userId, firstName, lastName, phoneNumber, image, deleteCurrentImage);

    public Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null)
        => profileService.ExistsWithEmailAsync(email, exceptId);

    public Task<bool> ExistsWithNameAsync(string name)
        => profileService.ExistsWithNameAsync(name);

    public Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null)
        => profileService.ExistsWithPhoneNumberAsync(phoneNumber, exceptId);

    // Status operations (delegated to IUserStatusService)
    public Task ToggleStatusAsync(bool activateUser, string userId, CancellationToken cancellationToken)
        => statusService.ToggleStatusAsync(activateUser, userId, cancellationToken);

    public Task DeleteAsync(string userId)
        => statusService.DeleteAsync(userId);

    // Role operations (delegated to IUserRoleService)
    public Task<string> AssignRolesAsync(string userId, List<UserRoleDto> userRoles, CancellationToken cancellationToken)
        => roleService.AssignRolesAsync(userId, userRoles, cancellationToken);

    public Task<List<UserRoleDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
        => roleService.GetUserRolesAsync(userId, cancellationToken);

    // Password operations (delegated to IUserPasswordService)
    public Task ForgotPasswordAsync(string email, string origin, CancellationToken cancellationToken)
        => passwordService.ForgotPasswordAsync(email, origin, cancellationToken);

    public Task ResetPasswordAsync(string email, string password, string token, CancellationToken cancellationToken)
        => passwordService.ResetPasswordAsync(email, password, token, cancellationToken);

    public Task ChangePasswordAsync(string password, string newPassword, string confirmNewPassword, string userId)
        => passwordService.ChangePasswordAsync(password, newPassword, confirmNewPassword, userId);

    // Permission operations (delegated to IUserPermissionService)
    public Task<List<string>?> GetPermissionsAsync(string userId, CancellationToken cancellationToken)
        => permissionService.GetPermissionsAsync(userId, cancellationToken);

<<<<<<< HEAD
        if (!activateUser)
        {
            var activeAdmins = await userManager.GetUsersInRoleAsync(RoleConstants.Admin);
            int activeAdminCount = activeAdmins.Count(u => u.IsActive);
            if (activeAdminCount == 0)
            {
                await AuditPolicyFailureAsync("NoActiveAdmins", cancellationToken);
                throw new CustomException("Tenant must have at least one active administrator.");
            }
        }

        user.IsActive = activateUser;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(error => error.Description).ToList();
            throw new CustomException("Toggle status failed", errors);
        }

        var tenantId = multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id ?? "unknown";
        await _auditClient.WriteActivityAsync(
            ActivityKind.Command,
            name: "ToggleUserStatus",
            statusCode: 204,
            durationMs: 0,
            captured: BodyCapture.None,
            requestSize: 0,
            responseSize: 0,
            requestPreview: new { actorId = actorId.ToString(), targetUserId = userId, action = activateUser ? "activate" : "deactivate", tenant = tenantId },
            responsePreview: new { outcome = "success" },
            severity: AuditSeverity.Information,
            source: "Identity",
            ct: cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(string userId, string firstName, string lastName, string phoneNumber, FileUploadRequest image, bool deleteCurrentImage)
    {
        var user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("user not found");

        Uri imageUri = user.ImageUrl ?? null!;
        if (image?.Data != null || deleteCurrentImage)
        {
            var imageString = await storageService.UploadAsync<FshUser>(image, FileType.Image);
            user.ImageUrl = new Uri(imageString, UriKind.RelativeOrAbsolute);
            if (deleteCurrentImage && imageUri != null)
            {
                await storageService.RemoveAsync(imageUri.ToString());
            }
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        string? currentPhoneNumber = await userManager.GetPhoneNumberAsync(user);
        if (phoneNumber != currentPhoneNumber)
        {
            await userManager.SetPhoneNumberAsync(user, phoneNumber);
        }

        var result = await userManager.UpdateAsync(user);
        await signInManager.RefreshSignInAsync(user);

        if (!result.Succeeded)
        {
            throw new CustomException("Update profile failed");
        }
    }

    public async Task DeleteAsync(string userId)
    {
        FshUser? user = await userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("User Not Found.");

        user.IsActive = false;
        IdentityResult? result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            List<string> errors = result.Errors.Select(error => error.Description).ToList();
            throw new CustomException("Delete profile failed", errors);
        }

        // ✅ Invalidate permission cache for this user
        await InvalidatePermissionCacheAsync(userId, CancellationToken.None);
    }

    private async Task<string> GetEmailVerificationUriAsync(FshUser user, string origin)
    {
        EnsureValidTenant();

        string code = await userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        const string route = "api/v1/identity/confirm-email";
        var endpointUri = new Uri(string.Concat($"{origin}/", route));
        string verificationUri = QueryHelpers.AddQueryString(endpointUri.ToString(), QueryStringKeys.UserId, user.Id);
        verificationUri = QueryHelpers.AddQueryString(verificationUri, QueryStringKeys.Code, code);
        verificationUri = QueryHelpers.AddQueryString(verificationUri,
            MultitenancyConstants.Identifier,
            multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id!);
        return verificationUri;
    }

    private static string BuildConfirmationEmailHtml(string userName, string confirmationUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Confirm Your Email</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc;">
                <table role="presentation" style="width: 100%; border-collapse: collapse;">
                    <tr>
                        <td align="center" style="padding: 40px 0;">
                            <table role="presentation" style="width: 100%; max-width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);">
                                <tr>
                                    <td style="padding: 40px 40px 30px 40px; text-align: center; background-color: #2563eb; border-radius: 8px 8px 0 0;">
                                        <h1 style="margin: 0; color: #ffffff; font-size: 24px; font-weight: 600;">Confirm Your Email Address</h1>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 40px;">
                                        <p style="margin: 0 0 20px 0; color: #334155; font-size: 16px; line-height: 1.6;">
                                            Hi {System.Net.WebUtility.HtmlEncode(userName)},
                                        </p>
                                        <p style="margin: 0 0 20px 0; color: #334155; font-size: 16px; line-height: 1.6;">
                                            Thank you for registering! Please confirm your email address by clicking the button below:
                                        </p>
                                        <table role="presentation" style="width: 100%; border-collapse: collapse;">
                                            <tr>
                                                <td align="center" style="padding: 30px 0;">
                                                    <a href="{System.Net.WebUtility.HtmlEncode(confirmationUrl)}" style="display: inline-block; padding: 14px 32px; background-color: #2563eb; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: 600; border-radius: 6px;">
                                                        Confirm Email Address
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>
                                        <p style="margin: 0 0 20px 0; color: #64748b; font-size: 14px; line-height: 1.6;">
                                            If the button doesn't work, copy and paste this link into your browser:
                                        </p>
                                        <p style="margin: 0 0 20px 0; color: #2563eb; font-size: 14px; line-height: 1.6; word-break: break-all;">
                                            {System.Net.WebUtility.HtmlEncode(confirmationUrl)}
                                        </p>
                                        <p style="margin: 30px 0 0 0; color: #64748b; font-size: 14px; line-height: 1.6;">
                                            If you didn't create an account, you can safely ignore this email.
                                        </p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 20px 40px; background-color: #f1f5f9; border-radius: 0 0 8px 8px; text-align: center;">
                                        <p style="margin: 0; color: #94a3b8; font-size: 12px;">
                                            This is an automated message. Please do not reply to this email.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }

    public async Task<string> AssignRolesAsync(string userId, List<UserRoleDto> userRoles, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException("user not found");

        // Check if the user is an admin for which the admin role is getting disabled
        if (await userManager.IsInRoleAsync(user, RoleConstants.Admin)
            && userRoles.Exists(a => !a.Enabled && a.RoleName == RoleConstants.Admin))
        {
            // Get count of users in Admin Role
            int adminCount = (await userManager.GetUsersInRoleAsync(RoleConstants.Admin)).Count;

            // Check if user is not Root Tenant Admin
            // Edge Case : there are chances for other tenants to have users with the same email as that of Root Tenant Admin. Probably can add a check while User Registration
            if (user.Email == MultitenancyConstants.Root.EmailAddress)
            {
                if (multiTenantContextAccessor?.MultiTenantContext?.TenantInfo?.Id == MultitenancyConstants.Root.Id)
                {
                    throw new CustomException("action not permitted");
                }
            }
            else if (adminCount <= 2)
            {
                throw new CustomException("tenant should have at least 2 admins.");
            }
        }

        foreach (var userRole in userRoles)
        {
            // Check if Role Exists
            if (await roleManager.FindByNameAsync(userRole.RoleName!) is not null)
            {
                if (userRole.Enabled)
                {
                    if (!await userManager.IsInRoleAsync(user, userRole.RoleName!))
                    {
                        await userManager.AddToRoleAsync(user, userRole.RoleName!);
                    }
                }
                else
                {
                    await userManager.RemoveFromRoleAsync(user, userRole.RoleName!);
                }
            }
        }

        // ✅ Invalidate permission cache for this user
        await InvalidatePermissionCacheAsync(userId, cancellationToken);

        return "User Roles Updated Successfully.";
    }

    public async Task<List<UserRoleDto>> GetUserRolesAsync(string userId, CancellationToken cancellationToken)
    {
        var userRoles = new List<UserRoleDto>();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) throw new NotFoundException("user not found");
        var roles = await roleManager.Roles.AsNoTracking().ToListAsync(cancellationToken);
        if (roles is null) throw new NotFoundException("roles not found");
        foreach (var role in roles)
        {
            userRoles.Add(new UserRoleDto
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Description = role.Description,
                Enabled = await userManager.IsInRoleAsync(user, role.Name!)
            });
        }

        return userRoles;
    }

    private string? ResolveImageUrl(Uri? imageUrl)
    {
        if (imageUrl is null)
        {
            return null;
        }

        // Absolute URLs (e.g., S3) pass through unchanged.
        if (imageUrl.IsAbsoluteUri)
        {
            return imageUrl.ToString();
        }

        // For relative paths from local storage, prefix with the API origin and wwwroot.
        if (_originUrl is null)
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is not null && !string.IsNullOrWhiteSpace(request.Scheme) && request.Host.HasValue)
            {
                var baseUri = $"{request.Scheme}://{request.Host.Value}{request.PathBase}";
                var relativePath = imageUrl.ToString().TrimStart('/');
                return $"{baseUri.TrimEnd('/')}/{relativePath}";
            }

            return imageUrl.ToString();
        }

        var originRelativePath = imageUrl.ToString().TrimStart('/');
        return $"{_originUrl.AbsoluteUri.TrimEnd('/')}/{originRelativePath}";
    }
=======
    public Task<bool> HasPermissionAsync(string userId, string permission, CancellationToken cancellationToken = default)
        => permissionService.HasPermissionAsync(userId, permission, cancellationToken);
>>>>>>> develop
}
