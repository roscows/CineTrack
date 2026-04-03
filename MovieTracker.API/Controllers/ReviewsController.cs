using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTracker.API.DTOs;
using MovieTracker.API.Models;
using MovieTracker.API.Services;

namespace MovieTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ReviewService _reviewService;
    private readonly MovieService _movieService;
    private readonly UserService _userService;

    public ReviewsController(ReviewService reviewService, MovieService movieService, UserService userService)
    {
        _reviewService = reviewService;
        _movieService = movieService;
        _userService = userService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var review = await _reviewService.GetByIdAsync(id);
        return review is null ? NotFound() : Ok(review);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = User.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(username))
        {
            return Unauthorized();
        }

        var movie = await _movieService.GetByIdAsync(dto.MovieId);
        if (movie is null)
        {
            return NotFound("Film ne postoji.");
        }

        var user = await _userService.GetByIdAsync(userId);

        var review = new Review
        {
            MovieId = dto.MovieId,
            UserId = userId,
            Username = username,
            UserAvatarUrl = user?.AvatarUrl ?? string.Empty,
            Rating = dto.Rating,
            Content = dto.Content
        };

        var result = await _reviewService.CreateAsync(review);
        if (!result.Ok)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Review!.Id }, result.Review);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateReviewDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _reviewService.UpdateAsync(id, userId, dto);
        if (!result.Ok)
        {
            if (result.Error == "Recenzija ne postoji.")
            {
                return NotFound(result.Error);
            }

            return Forbid();
        }

        return NoContent();
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _reviewService.DeleteAsync(id, userId, string.Equals(role, "Admin", StringComparison.Ordinal));
        if (!result.Ok)
        {
            if (result.Error == "Recenzija ne postoji.")
            {
                return NotFound(result.Error);
            }

            return Forbid();
        }

        return NoContent();
    }
}
