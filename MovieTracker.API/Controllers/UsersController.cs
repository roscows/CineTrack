using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTracker.API.DTOs;
using MovieTracker.API.Models;
using MovieTracker.API.Services;

namespace MovieTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ReviewService _reviewService;

    public UsersController(UserService userService, ReviewService reviewService)
    {
        _userService = userService;
        _reviewService = reviewService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var existingEmail = await _userService.GetByEmailAsync(dto.Email);
        if (existingEmail is not null)
        {
            return BadRequest("Email je vec zauzet.");
        }

        var existingUsername = await _userService.GetByUsernameAsync(dto.Username);
        if (existingUsername is not null)
        {
            return BadRequest("Username je vec zauzet.");
        }

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            AvatarUrl = dto.AvatarUrl,
            IsAdmin = false
        };

        var created = await _userService.CreateAsync(user, dto.Password);
        return Ok(new
        {
            created.Id,
            created.Username,
            created.Email,
            created.AvatarUrl,
            created.IsAdmin,
            created.CreatedAt
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminUserDto dto)
    {
        var existingEmail = await _userService.GetByEmailAsync(dto.Email);
        if (existingEmail is not null)
        {
            return BadRequest("Email je vec zauzet.");
        }

        var existingUsername = await _userService.GetByUsernameAsync(dto.Username);
        if (existingUsername is not null)
        {
            return BadRequest("Username je vec zauzet.");
        }

        var adminUser = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            AvatarUrl = dto.AvatarUrl,
            IsAdmin = true
        };

        var created = await _userService.CreateAsync(adminUser, dto.Password);

        return CreatedAtAction(nameof(Profile), new { id = created.Id }, new
        {
            created.Id,
            created.Username,
            created.Email,
            created.AvatarUrl,
            created.IsAdmin,
            created.CreatedAt
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
    {
        var user = await _userService.ValidateCredentialsAsync(dto.Email, dto.Password);
        if (user is null)
        {
            return Unauthorized("Pogresni kredencijali.");
        }

        var token = _userService.GenerateJwtToken(user);
        var response = new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsAdmin = user.IsAdmin
        };

        return Ok(response);
    }

    [Authorize]
    [HttpPatch("me/avatar")]
    public async Task<IActionResult> UpdateMyAvatar([FromBody] UpdateAvatarDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var updated = await _userService.UpdateAvatarAsync(userId, dto.AvatarUrl);
        if (updated is null)
        {
            return NotFound("Korisnik ne postoji.");
        }

        await _reviewService.UpdateUserAvatarSnapshotAsync(userId, updated.AvatarUrl);

        return Ok(new
        {
            updated.Id,
            updated.Username,
            updated.Email,
            updated.AvatarUrl,
            updated.IsAdmin,
            updated.CreatedAt
        });
    }

    [HttpGet("{id}/profile")]
    public async Task<IActionResult> Profile(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.AvatarUrl,
            user.IsAdmin,
            user.CreatedAt
        });
    }

    [HttpGet("{id}/reviews")]
    public async Task<IActionResult> UserReviews(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound("Korisnik ne postoji.");
        }

        var reviews = await _reviewService.GetByUserIdAsync(id);
        return Ok(reviews);
    }
}
