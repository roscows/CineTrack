using System.ComponentModel.DataAnnotations;

namespace MovieTracker.API.DTOs;

public class CreateReviewDto
{
    [Required]
    public string MovieId { get; set; } = string.Empty;

    [Range(1, 10)]
    public int Rating { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;
}
