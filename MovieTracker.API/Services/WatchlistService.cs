using MongoDB.Driver;
using MovieTracker.API.Models;
using MovieTracker.API.Settings;

namespace MovieTracker.API.Services;

public class WatchlistService
{
    private readonly IMongoCollection<WatchlistItem> _watchlist;
    private readonly IMongoCollection<Movie> _movies;
    private readonly MovieService _movieService;

    public WatchlistService(IMongoDatabase database, IConfiguration configuration, MovieService movieService)
    {
        var settings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>() ?? new MongoDbSettings();
        _watchlist = database.GetCollection<WatchlistItem>(settings.WatchlistCollectionName);
        _movies = database.GetCollection<Movie>(settings.MoviesCollectionName);
        _movieService = movieService;
    }

    public async Task<List<WatchlistItem>> GetByUserIdAsync(string userId)
    {
        var items = await _watchlist.Find(x => x.UserId == userId).SortByDescending(x => x.AddedAt).ToListAsync();

        if (items.Count == 0)
        {
            return items;
        }

        var movieIds = items.Select(x => x.MovieId).Distinct().ToList();
        var existingMovieIds = await _movies
            .Find(x => movieIds.Contains(x.Id))
            .Project(x => x.Id)
            .ToListAsync();

        var existingSet = existingMovieIds.ToHashSet(StringComparer.Ordinal);
        var staleItemIds = items
            .Where(x => !existingSet.Contains(x.MovieId))
            .Select(x => x.Id)
            .ToList();

        if (staleItemIds.Count > 0)
        {
            await _watchlist.DeleteManyAsync(x => staleItemIds.Contains(x.Id));
            items = items.Where(x => existingSet.Contains(x.MovieId)).ToList();
        }

        return items;
    }

    public async Task<(bool Ok, string? Error, WatchlistItem? Item)> AddAsync(string userId, string movieId)
    {
        var existing = await _watchlist.Find(x => x.UserId == userId && x.MovieId == movieId).FirstOrDefaultAsync();
        if (existing is not null)
        {
            return (false, "Film je vec na listi.", null);
        }

        var movie = await _movieService.GetByIdAsync(movieId);
        if (movie is null)
        {
            return (false, "Film ne postoji.", null);
        }

        var item = new WatchlistItem
        {
            UserId = userId,
            MovieId = movie.Id,
            MovieTitle = movie.Title,
            MoviePosterUrl = movie.PosterUrl,
            MovieYear = movie.Year,
            Watched = false,
            AddedAt = DateTime.UtcNow,
            WatchedAt = null
        };

        await _watchlist.InsertOneAsync(item);
        return (true, null, item);
    }

    public async Task<(bool Ok, string? Error)> MarkAsWatchedAsync(string watchlistId, string userId)
    {
        var item = await _watchlist.Find(x => x.Id == watchlistId).FirstOrDefaultAsync();
        if (item is null)
        {
            return (false, "Stavka ne postoji.");
        }

        if (item.UserId != userId)
        {
            return (false, "Nemate pristup ovoj stavci.");
        }

        var update = Builders<WatchlistItem>.Update
            .Set(x => x.Watched, true)
            .Set(x => x.WatchedAt, DateTime.UtcNow);

        await _watchlist.UpdateOneAsync(x => x.Id == watchlistId, update);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(string watchlistId, string userId)
    {
        var item = await _watchlist.Find(x => x.Id == watchlistId).FirstOrDefaultAsync();
        if (item is null)
        {
            return (false, "Stavka ne postoji.");
        }

        if (item.UserId != userId)
        {
            return (false, "Nemate pristup ovoj stavci.");
        }

        await _watchlist.DeleteOneAsync(x => x.Id == watchlistId);
        return (true, null);
    }

    public async Task DeleteByMovieIdAsync(string movieId)
    {
        await _watchlist.DeleteManyAsync(x => x.MovieId == movieId);
    }
}
