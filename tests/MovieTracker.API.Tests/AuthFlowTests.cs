using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace MovieTracker.API.Tests;

public class AuthFlowTests : IClassFixture<MovieTrackerApiFactory>
{
    private readonly HttpClient _client;

    public AuthFlowTests(MovieTrackerApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Should_Always_Create_NonAdmin_User()
    {
        var payload = new
        {
            username = $"user_{Guid.NewGuid():N}",
            email = $"user_{Guid.NewGuid():N}@test.local",
            password = "User123!",
            avatarUrl = ""
        };

        var response = await _client.PostAsJsonAsync("/api/users/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!["isAdmin"].ToString().Should().Be("False");
    }

    [Fact]
    public async Task Login_Should_Return_Jwt_Token()
    {
        var email = $"user_{Guid.NewGuid():N}@test.local";
        var password = "User123!";

        var registerPayload = new
        {
            username = $"user_{Guid.NewGuid():N}",
            email,
            password,
            avatarUrl = ""
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/users/register", registerPayload);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.Token.Should().NotBeNullOrWhiteSpace();
        auth.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task PostMovie_WithoutToken_Should_Return_Unauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/movies", BuildMoviePayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostMovie_WithNonAdminToken_Should_Return_Forbidden()
    {
        var email = $"user_{Guid.NewGuid():N}@test.local";
        var password = "User123!";

        await _client.PostAsJsonAsync("/api/users/register", new
        {
            username = $"user_{Guid.NewGuid():N}",
            email,
            password,
            avatarUrl = ""
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var response = await _client.PostAsJsonAsync("/api/movies", BuildMoviePayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateAdmin_WithNonAdminToken_Should_Return_Forbidden()
    {
        var email = $"user_{Guid.NewGuid():N}@test.local";
        var password = "User123!";

        await _client.PostAsJsonAsync("/api/users/register", new
        {
            username = $"user_{Guid.NewGuid():N}",
            email,
            password,
            avatarUrl = ""
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/users/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var response = await _client.PostAsJsonAsync("/api/users/admin", new
        {
            username = $"admin_{Guid.NewGuid():N}",
            email = $"admin_{Guid.NewGuid():N}@test.local",
            password = "Admin123!",
            avatarUrl = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static object BuildMoviePayload() => new
    {
        title = "Integration Test Movie",
        description = "Movie for auth integration tests.",
        year = 2024,
        genres = new[] { "Drama" },
        director = "Test Director",
        cast = new[] { "Actor One", "Actor Two" },
        posterUrl = "https://example.com/posters/test-movie.jpg"
    };

    private sealed class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
    }
}
