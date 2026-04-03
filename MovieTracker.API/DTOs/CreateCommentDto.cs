using System.ComponentModel.DataAnnotations;

namespace MovieTracker.API.DTOs;

public class CreateCommentDto
{
    [Required]
    public string Content { get; set; } = string.Empty;
}
