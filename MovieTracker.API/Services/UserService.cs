using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using MovieTracker.API.Models;
using MovieTracker.API.Settings;

namespace MovieTracker.API.Services;

public class UserService
{
    private readonly IMongoCollection<User> _users;
    private readonly JwtSettings _jwtSettings;

    public UserService(IMongoDatabase database, IConfiguration configuration, IOptions<JwtSettings> jwtOptions)
    {
        var settings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>() ?? new MongoDbSettings();
        _users = database.GetCollection<User>(settings.UsersCollectionName);
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        var user = await _users.Find(x => x.Id == id).FirstOrDefaultAsync();
        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var user = await _users.Find(x => x.Email == email).FirstOrDefaultAsync();
        return user;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var user = await _users.Find(x => x.Username == username).FirstOrDefaultAsync();
        return user;
    }

    public async Task<User> CreateAsync(User user, string plainPassword)
    {
        user.PasswordHash = HashPassword(plainPassword);
        user.CreatedAt = DateTime.UtcNow;
        await _users.InsertOneAsync(user);
        return user;
    }

    public async Task<User?> ValidateCredentialsAsync(string email, string password)
    {
        var user = await GetByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        if (!IsBcryptHash(user.PasswordHash))
        {
            user.PasswordHash = HashPassword(password);
            await _users.ReplaceOneAsync(x => x.Id == user.Id, user);
        }

        return user;
    }

    public async Task<User?> UpdateAvatarAsync(string userId, string avatarUrl)
    {
        var update = Builders<User>.Update.Set(x => x.AvatarUrl, avatarUrl ?? string.Empty);
        var options = new FindOneAndUpdateOptions<User>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updated = await _users.FindOneAndUpdateAsync(x => x.Id == userId, update, options);
        return updated;
    }

    public string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        if (IsBcryptHash(passwordHash))
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        var legacyHash = ComputeLegacySha256Hash(password);
        return string.Equals(passwordHash, legacyHash, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBcryptHash(string passwordHash)
    {
        return passwordHash.StartsWith("$2a$", StringComparison.Ordinal)
               || passwordHash.StartsWith("$2b$", StringComparison.Ordinal)
               || passwordHash.StartsWith("$2y$", StringComparison.Ordinal);
    }

    private static string ComputeLegacySha256Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}
