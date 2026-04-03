using BCrypt.Net;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MovieTracker.API.Models;
using MovieTracker.API.Settings;

namespace MovieTracker.API.Services;

public class MongoDbInitializerHostedService : IHostedService
{
    private readonly IMongoDatabase _database;
    private readonly MongoDbSettings _mongoSettings;
    private readonly SeedSettings _seedSettings;
    private readonly ILogger<MongoDbInitializerHostedService> _logger;

    public MongoDbInitializerHostedService(
        IMongoDatabase database,
        IOptions<MongoDbSettings> mongoOptions,
        IOptions<SeedSettings> seedOptions,
        ILogger<MongoDbInitializerHostedService> logger)
    {
        _database = database;
        _mongoSettings = mongoOptions.Value;
        _seedSettings = seedOptions.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await EnsureIndexesAsync(cancellationToken);

        if (_seedSettings.Enabled)
        {
            await SeedAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureIndexesAsync(CancellationToken cancellationToken)
    {
        var users = _database.GetCollection<User>(_mongoSettings.UsersCollectionName);
        var reviews = _database.GetCollection<Review>(_mongoSettings.ReviewsCollectionName);
        var watchlist = _database.GetCollection<WatchlistItem>(_mongoSettings.WatchlistCollectionName);

        await users.Indexes.CreateManyAsync(
            new[]
            {
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Email), new CreateIndexOptions { Unique = true, Name = "ux_users_email" }),
                new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Username), new CreateIndexOptions { Unique = true, Name = "ux_users_username" })
            },
            cancellationToken);

        await reviews.Indexes.CreateOneAsync(
            new CreateIndexModel<Review>(
                Builders<Review>.IndexKeys.Ascending(x => x.MovieId).Ascending(x => x.UserId),
                new CreateIndexOptions { Unique = true, Name = "ux_reviews_movie_user" }),
            cancellationToken: cancellationToken);

        await watchlist.Indexes.CreateOneAsync(
            new CreateIndexModel<WatchlistItem>(
                Builders<WatchlistItem>.IndexKeys.Ascending(x => x.UserId).Ascending(x => x.MovieId),
                new CreateIndexOptions { Unique = true, Name = "ux_watchlist_user_movie" }),
            cancellationToken: cancellationToken);

        _logger.LogInformation("MongoDB indexes ensured.");
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        var users = _database.GetCollection<User>(_mongoSettings.UsersCollectionName);
        var movies = _database.GetCollection<Movie>(_mongoSettings.MoviesCollectionName);

        var admin = await users.Find(x => x.Email == _seedSettings.AdminEmail).FirstOrDefaultAsync(cancellationToken);
        if (admin is null)
        {
            var seededAdmin = new User
            {
                Username = _seedSettings.AdminUsername,
                Email = _seedSettings.AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(_seedSettings.AdminPassword),
                AvatarUrl = string.Empty,
                IsAdmin = true,
                CreatedAt = DateTime.UtcNow
            };

            await users.InsertOneAsync(seededAdmin, cancellationToken: cancellationToken);
            _logger.LogInformation("Seeded admin user: {Email}", _seedSettings.AdminEmail);
        }

        var movieCount = await movies.CountDocumentsAsync(Builders<Movie>.Filter.Empty, cancellationToken: cancellationToken);
        if (movieCount == 0)
        {
            var now = DateTime.UtcNow;
            var seedMovies = new List<Movie>
            {
                new()
                {
                    Title = "Inception",
                    Description = "A thief who steals corporate secrets through dream-sharing technology.",
                    Year = 2010,
                    Genres = ["Sci-Fi", "Action"],
                    Director = "Christopher Nolan",
                    Cast = ["Leonardo DiCaprio", "Joseph Gordon-Levitt", "Elliot Page"],
                    PosterUrl = "https://example.com/posters/inception.jpg",
                    AverageRating = 0,
                    ReviewCount = 0,
                    CreatedAt = now
                },
                new()
                {
                    Title = "Parasite",
                    Description = "A poor family schemes to become employed by a wealthy household.",
                    Year = 2019,
                    Genres = ["Drama", "Thriller"],
                    Director = "Bong Joon-ho",
                    Cast = ["Song Kang-ho", "Lee Sun-kyun", "Cho Yeo-jeong"],
                    PosterUrl = "https://example.com/posters/parasite.jpg",
                    AverageRating = 0,
                    ReviewCount = 0,
                    CreatedAt = now
                },
                new()
                {
                    Title = "The Dark Knight",
                    Description = "Batman faces the Joker, a criminal mastermind who wants chaos.",
                    Year = 2008,
                    Genres = ["Action", "Crime", "Drama"],
                    Director = "Christopher Nolan",
                    Cast = ["Christian Bale", "Heath Ledger", "Aaron Eckhart"],
                    PosterUrl = "https://example.com/posters/the-dark-knight.jpg",
                    AverageRating = 0,
                    ReviewCount = 0,
                    CreatedAt = now
                }
            };

            await movies.InsertManyAsync(seedMovies, cancellationToken: cancellationToken);
            _logger.LogInformation("Seeded {Count} movies.", seedMovies.Count);
        }
    }
}

