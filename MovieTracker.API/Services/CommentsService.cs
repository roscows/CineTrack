using MovieTracker.API.Services;

namespace MovieTracker.API.Services;

public class CommentsService
{
    private readonly ReviewService _reviewService;

    public CommentsService(ReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    public Task<(bool Ok, string? Error, Models.Comment? Comment)> AddAsync(string reviewId, string userId, string username, string content) =>
        _reviewService.AddCommentAsync(reviewId, userId, username, content);

    public Task<(bool Ok, string? Error)> DeleteAsync(string reviewId, string commentId, string userId, bool isAdmin) =>
        _reviewService.DeleteCommentAsync(reviewId, commentId, userId, isAdmin);
}
