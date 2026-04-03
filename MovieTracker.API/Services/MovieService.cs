using MongoDB.Driver;
using MovieTracker.API.Models;
using MovieTracker.API.Settings;

namespace MovieTracker.API.Services;

public class MovieService
{
    private readonly IMongoCollection<Movie> _movies;

    public MovieService(IMongoDatabase database, IConfiguration configuration)
    {
        var settings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>() ?? new MongoDbSettings();
        _movies = database.GetCollection<Movie>(settings.MoviesCollectionName);
    }

    public async Task<(List<Movie> Items, long Total)> GetAllAsync(string? search, string? genre, int page, int pageSize)
    {
        var filter = BuildFilter(search, genre);

        var total = await _movies.CountDocumentsAsync(filter);
        var items = await _movies.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Movie?> GetByIdAsync(string id)
    {
        var movie = await _movies.Find(x => x.Id == id).FirstOrDefaultAsync();
        return movie;
    }

    public async Task<Movie> CreateAsync(Movie movie)
    {
        movie.CreatedAt = DateTime.UtcNow;
        movie.AverageRating = 0;
        movie.ReviewCount = 0;
        await _movies.InsertOneAsync(movie);
        return movie;
    }

    public async Task<bool> UpdateAsync(string id, Movie updated)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null)
        {
            return false;
        }

        updated.Id = existing.Id;
        updated.CreatedAt = existing.CreatedAt;
        updated.AverageRating = existing.AverageRating;
        updated.ReviewCount = existing.ReviewCount;

        var result = await _movies.ReplaceOneAsync(x => x.Id == id, updated);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _movies.DeleteOneAsync(x => x.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task UpdateRatingStatsAsync(string movieId, double averageRating, int reviewCount)
    {
        var update = Builders<Movie>.Update
            .Set(x => x.AverageRating, averageRating)
            .Set(x => x.ReviewCount, reviewCount);

        await _movies.UpdateOneAsync(x => x.Id == movieId, update);
    }

    private static FilterDefinition<Movie> BuildFilter(string? search, string? genre)
    {
        var filterBuilder = Builders<Movie>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            var titleFilter = filterBuilder.Regex(x => x.Title, new MongoDB.Bson.BsonRegularExpression(normalized, "i"));
            var directorFilter = filterBuilder.Regex(x => x.Director, new MongoDB.Bson.BsonRegularExpression(normalized, "i"));
            filter &= filterBuilder.Or(titleFilter, directorFilter);
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            filter &= filterBuilder.AnyEq(x => x.Genres, genre.Trim());
        }

        return filter;
    }
}
