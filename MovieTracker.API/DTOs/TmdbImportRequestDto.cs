using System.ComponentModel.DataAnnotations;

namespace MovieTracker.API.DTOs;

public class TmdbImportRequestDto
{
    [Range(1, 50)]
    public int Pages { get; set; } = 5;

    [Range(1, 1000)]
    public int StartPage { get; set; } = 1;

    public bool IncludeCredits { get; set; }

    [MaxLength(10)]
    public string Language { get; set; } = "en-US";
}
