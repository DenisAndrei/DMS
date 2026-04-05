using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using DeviceManagement.Api.Contracts.Requests;
using DeviceManagement.Api.Contracts.Responses;
using DeviceManagement.Api.Domain.Exceptions;
using DeviceManagement.Api.Domain.Models;
using DeviceManagement.Api.Infrastructure.Repositories;
using DeviceManagement.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DeviceManagement.Api.Services;

public sealed class AuthService : IAuthService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AuthOptions _authOptions;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher,
        IOptions<AuthOptions> authOptions,
        TimeProvider timeProvider)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
        _authOptions = authOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        ValidatePassword(request.Password);

        var existingAccount = await _accountRepository.GetByEmailAsync(email, cancellationToken);
        if (existingAccount is not null)
        {
            throw new ConflictException("An account with this email address already exists.");
        }

        var (passwordHash, passwordSalt) = _passwordHasher.HashPassword(request.Password);
        var userProfile = BuildUserProfile(email);

        var createdAccount = await _accountRepository.CreateAsync(
            email,
            passwordHash,
            passwordSalt,
            userProfile,
            cancellationToken);

        return CreateAuthResponse(createdAccount);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        ValidatePassword(request.Password);

        var account = await _accountRepository.GetByEmailAsync(email, cancellationToken);
        if (account is null ||
            !_passwordHasher.VerifyPassword(request.Password, account.PasswordHash, account.PasswordSalt))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return CreateAuthResponse(account);
    }

    private AuthResponse CreateAuthResponse(AuthAccount account)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAtUtc = now.AddMinutes(_authOptions.TokenLifetimeMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, account.UserId.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.NameIdentifier, account.UserId.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, account.Name),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Role, account.Role),
            new Claim("location", account.Location)
        };

        var signingKey = new SymmetricSecurityKey(_authOptions.GetSigningKeyBytes());
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _authOptions.Issuer,
            audience: _authOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResponse(
            tokenValue,
            expiresAtUtc.UtcDateTime,
            new AuthenticatedUserResponse(
                account.UserId,
                account.Email,
                account.Name,
                account.Role,
                account.Location));
    }

    private static string NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new RequestValidationException(nameof(email), "Email is required.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new RequestValidationException(nameof(password), "Password is required.");
        }

        if (password.Trim().Length < 8)
        {
            throw new RequestValidationException(nameof(password), "Password must be at least 8 characters long.");
        }
    }

    // Build a basic profile from the email so a new account can use the app right away.
    private static UserUpsertModel BuildUserProfile(string email)
    {
        var localPart = email.Split('@', 2)[0];
        var normalizedName = Regex.Replace(localPart, @"[._\-]+", " ").Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            normalizedName = "Employee";
        }

        var textInfo = CultureInfo.InvariantCulture.TextInfo;
        var words = normalizedName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => textInfo.ToTitleCase(word.ToLowerInvariant()));

        var displayName = string.Join(' ', words);

        return new UserUpsertModel(
            displayName,
            "Employee",
            "Remote");
    }
}
