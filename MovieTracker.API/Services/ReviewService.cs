using MongoDB.Bson;
using MongoDB.Driver;
using MovieTracker.API.DTOs;
using MovieTracker.API.Models;
using MovieTracker.API.Settings;

namespace MovieTracker.API.Services;

public class ReviewService
{
    private readonly IMongoCollection<Review> _reviews;
    private readonly MovieService _movieService;

    public ReviewService(IMongoDatabase database, IConfiguration configuration, MovieService movieService)
    {
        var settings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>() ?? new MongoDbSettings();
        _reviews = database.GetCollection<Review>(settings.ReviewsCollectionName);
        _movieService = movieService;
    }

    public async Task<Review?> GetByIdAsync(string id)
    {
        var review = await _reviews.Find(x => x.Id == id).FirstOrDefaultAsync();
        return review;
    }

    public Task<List<Review>> GetByMovieIdAsync(string movieId) =>
        _reviews.Find(x => x.MovieId == movieId).SortByDescending(x => x.CreatedAt).ToListAsync();

    public Task<List<Review>> GetByUserIdAsync(string userId) =>
        _reviews.Find(x => x.UserId == userId).SortByDescending(x => x.CreatedAt).ToListAsync();

    public async Task<(bool Ok, string? Error, Review? Review)> CreateAsync(Review review)
    {
        var exists = await _reviews.Find(x => x.MovieId == review.MovieId && x.UserId == review.UserId).AnyAsync();
        if (exists)
        {
            return (false, "Korisnik je vec ocenio ovaj film.", null);
        }

        review.CreatedAt = DateTime.UtcNow;
        review.UpdatedAt = null;
        review.Comments = new List<Comment>();

        await _reviews.InsertOneAsync(review);
        await RefreshMovieStatsAsync(review.MovieId);
        return (true, null, review);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(string id, string userId, UpdateReviewDto dto)
    {
        var review = await GetByIdAsync(id);
        if (review is null)
        {
            return (false, "Recenzija ne postoji.");
        }

        if (review.UserId != userId)
        {
            return (false, "Mozes menjati samo svoju recenziju.");
        }

        var update = Builders<Review>.Update
            .Set(x => x.Rating, dto.Rating)
            .Set(x => x.Content, dto.Content)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);

        await _reviews.UpdateOneAsync(x => x.Id == id, update);
        await RefreshMovieStatsAsync(review.MovieId);

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(string id, string userId, bool isAdmin)
    {
        var review = await GetByIdAsync(id);
        if (review is null)
        {
            return (false, "Recenzija ne postoji.");
        }

        if (!isAdmin && review.UserId != userId)
        {
            return (false, "Nemate pravo da obrisete ovu recenziju.");
        }

        await _reviews.DeleteOneAsync(x => x.Id == id);
        await RefreshMovieStatsAsync(review.MovieId);

        return (true, null);
    }

    public async Task<(bool Ok, string? Error, Comment? Comment)> AddCommentAsync(string reviewId, string userId, string username, string content)
    {
        var review = await GetByIdAsync(reviewId);
        if (review is null)
        {
            return (false, "Recenzija ne postoji.", null);
        }

        var comment = new Comment
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = userId,
            Username = username,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        var update = Builders<Review>.Update.Push(x => x.Comments, comment);
        await _reviews.UpdateOneAsync(x => x.Id == reviewId, update);

        return (true, null, comment);
    }

    public async Task<(bool Ok, string? Error)> DeleteCommentAsync(string reviewId, string commentId, string userId, bool isAdmin)
    {
        var review = await GetByIdAsync(reviewId);
        if (review is null)
        {
            return (false, "Recenzija ne postoji.");
        }

        var comment = review.Comments.FirstOrDefault(x => x.Id == commentId);
        if (comment is null)
        {
            return (false, "Komentar ne postoji.");
        }

        if (!isAdmin && comment.UserId != userId)
        {
            return (false, "Nemate pravo da obrisete ovaj komentar.");
        }

        var filter = Builders<Review>.Filter.Eq(x => x.Id, reviewId);
        var update = Builders<Review>.Update.PullFilter(x => x.Comments, x => x.Id == commentId);
        await _reviews.UpdateOneAsync(filter, update);

        return (true, null);
    }

    public async Task DeleteByMovieIdAsync(string movieId)
    {
        await _reviews.DeleteManyAsync(x => x.MovieId == movieId);
    }

    public async Task UpdateUserAvatarSnapshotAsync(string userId, string avatarUrl)
    {
        var update = Builders<Review>.Update.Set(x => x.UserAvatarUrl, avatarUrl ?? string.Empty);
        await _reviews.UpdateManyAsync(x => x.UserId == userId, update);
    }

    private async Task RefreshMovieStatsAsync(string movieId)
    {
        var reviews = await _reviews.Find(x => x.MovieId == movieId).ToListAsync();
        var reviewCount = reviews.Count;
        var average = reviewCount == 0 ? 0 : Math.Round(reviews.Average(x => x.Rating), 2);

        await _movieService.UpdateRatingStatsAsync(movieId, average, reviewCount);
    }
}
