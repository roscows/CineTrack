using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTracker.API.DTOs;
using MovieTracker.API.Services;

namespace MovieTracker.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class WatchlistController : ControllerBase
{
    private readonly WatchlistService _watchlistService;

    public WatchlistController(WatchlistService watchlistService)
    {
        _watchlistService = watchlistService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var items = await _watchlistService.GetByUserIdAsync(userId);
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddWatchlistItemDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _watchlistService.AddAsync(userId, dto.MovieId);
        if (!result.Ok)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Item);
    }

    [HttpPatch("{id}/watched")]
    public async Task<IActionResult> MarkWatched(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _watchlistService.MarkAsWatchedAsync(id, userId);
        if (!result.Ok)
        {
            if (result.Error == "Stavka ne postoji.")
            {
                return NotFound(result.Error);
            }

            return Forbid();
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _watchlistService.DeleteAsync(id, userId);
        if (!result.Ok)
        {
            if (result.Error == "Stavka ne postoji.")
            {
                return NotFound(result.Error);
            }

            return Forbid();
        }

        return NoContent();
    }
}
