using System.ComponentModel.DataAnnotations;

namespace MovieTracker.API.DTOs;

public class AddWatchlistItemDto
{
    [Required]
    public string MovieId { get; set; } = string.Empty;
}
