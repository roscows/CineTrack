using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTracker.API.DTOs;
using MovieTracker.API.Models;
using MovieTracker.API.Services;

namespace MovieTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly MovieService _movieService;
    private readonly ReviewService _reviewService;
    private readonly WatchlistService _watchlistService;

    public MoviesController(MovieService movieService, ReviewService reviewService, WatchlistService watchlistService)
    {
        _movieService = movieService;
        _reviewService = reviewService;
        _watchlistService = watchlistService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? genre, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest("Nevalidna paginacija.");
        }

        var (items, total) = await _movieService.GetAllAsync(search, genre, page, pageSize);
        return Ok(new
        {
            items,
            total,
            page,
            pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var movie = await _movieService.GetByIdAsync(id);
        return movie is null ? NotFound() : Ok(movie);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMovieDto dto)
    {
        var movie = new Movie
        {
            Title = dto.Title,
            Description = dto.Description,
            Year = dto.Year,
            Genres = dto.Genres,
            Director = dto.Director,
            Cast = dto.Cast,
            PosterUrl = dto.PosterUrl
        };

        var created = await _movieService.CreateAsync(movie);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateMovieDto dto)
    {
        var movie = new Movie
        {
            Title = dto.Title,
            Description = dto.Description,
            Year = dto.Year,
            Genres = dto.Genres,
            Director = dto.Director,
            Cast = dto.Cast,
            PosterUrl = dto.PosterUrl
        };

        var ok = await _movieService.UpdateAsync(id, movie);
        return ok ? NoContent() : NotFound();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var ok = await _movieService.DeleteAsync(id);
        if (!ok)
        {
            return NotFound();
        }

        await _reviewService.DeleteByMovieIdAsync(id);
        await _watchlistService.DeleteByMovieIdAsync(id);

        return NoContent();
    }

    [HttpGet("{id}/reviews")]
    public async Task<IActionResult> GetMovieReviews(string id)
    {
        var movie = await _movieService.GetByIdAsync(id);
        if (movie is null)
        {
            return NotFound("Film ne postoji.");
        }

        var reviews = await _reviewService.GetByMovieIdAsync(id);
        return Ok(reviews);
    }
}
