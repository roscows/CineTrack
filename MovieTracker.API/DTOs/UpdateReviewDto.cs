using System.ComponentModel.DataAnnotations;

namespace MovieTracker.API.DTOs;

public class UpdateReviewDto
{
    [Range(1, 10)]
    public int Rating { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;
}
