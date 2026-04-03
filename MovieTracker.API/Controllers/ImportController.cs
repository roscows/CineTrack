using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTracker.API.DTOs;
using MovieTracker.API.Services;

namespace MovieTracker.API.Controllers;

[ApiController]
[Route("api/admin/import")]
[Authorize(Roles = "Admin")]
public class ImportController : ControllerBase
{
    private readonly TmdbImportService _tmdbImportService;

    public ImportController(TmdbImportService tmdbImportService)
    {
        _tmdbImportService = tmdbImportService;
    }

    [HttpPost("tmdb")]
    public async Task<ActionResult<TmdbImportResultDto>> ImportTmdb([FromBody] TmdbImportRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _tmdbImportService.ImportPopularAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
