namespace MovieTracker.API.DTOs;

public class TmdbImportResultDto
{
    public int Requested { get; set; }
    public int Imported { get; set; }
    public int SkippedDuplicates { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
