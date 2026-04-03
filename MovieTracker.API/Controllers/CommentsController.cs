using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTracker.API.DTOs;
using MovieTracker.API.Services;

namespace MovieTracker.API.Controllers;

[ApiController]
[Route("api/reviews/{reviewId}/comments")]
public class CommentsController : ControllerBase
{
    private readonly CommentsService _commentsService;

    public CommentsController(CommentsService commentsService)
    {
        _commentsService = commentsService;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AddComment(string reviewId, [FromBody] CreateCommentDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        var result = await _commentsService.AddAsync(reviewId, userId, username, dto.Content);
        if (!result.Ok)
        {
            if (result.Error == "Recenzija ne postoji.")
            {
                return NotFound(result.Error);
            }

            return BadRequest(result.Error);
        }

        return Ok(result.Comment);
    }

    [Authorize]
    [HttpDelete("{commentId}")]
    public async Task<IActionResult> DeleteComment(string reviewId, string commentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _commentsService.DeleteAsync(reviewId, commentId, userId, string.Equals(role, "Admin", StringComparison.Ordinal));
        if (!result.Ok)
        {
            if (result.Error == "Recenzija ne postoji." || result.Error == "Komentar ne postoji.")
            {
                return NotFound(result.Error);
            }

            return Forbid();
        }

        return NoContent();
    }
}
