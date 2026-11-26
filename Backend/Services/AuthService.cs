using Ecommerce.Backend.Data;
using Ecommerce.Backend.DTOs;
using Ecommerce.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Ecommerce.Backend.Services;

public class AuthService(AppDbContext db, TokenService tokens, IPasswordHasher<User> hasher)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw ApiException.Conflict("An account with this email already exists.");

        var user = new User { Email = email, FullName = request.FullName.Trim() };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        user.Cart = new Cart(); // every user gets exactly one cart

        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two concurrent registrations with the same email.
            throw ApiException.Conflict("An account with this email already exists.");
        }

        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // Same message for unknown email and wrong password to avoid account enumeration.
        const string invalid = "Invalid email or password.";
        if (user is null)
            throw ApiException.Unauthorized(invalid);

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw ApiException.Unauthorized(invalid);

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(ct);
        }

        return CreateResponse(user);
    }

    public async Task<UserDto> GetProfileAsync(int userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw ApiException.Unauthorized("User no longer exists.");
        return ToDto(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var (token, expiresAt) = tokens.CreateToken(user);
        return new AuthResponse(token, expiresAt, ToDto(user));
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Email, u.FullName, u.CreatedAt);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
