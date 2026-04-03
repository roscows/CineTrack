using System.ComponentModel.DataAnnotations;

namespace MovieTracker.API.DTOs;

public class UpdateMovieDto
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public int Year { get; set; }
    public List<string> Genres { get; set; } = new();
    public string Director { get; set; } = string.Empty;
    public List<string> Cast { get; set; } = new();
    public string PosterUrl { get; set; } = string.Empty;
}
