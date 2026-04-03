using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MovieTracker.API.DTOs;
using MovieTracker.API.Models;
using MovieTracker.API.Settings;

namespace MovieTracker.API.Services;

public class TmdbImportService
{
    private readonly HttpClient _httpClient;
    private readonly IMongoCollection<Movie> _movies;
    private readonly TmdbSettings _tmdbSettings;
    private readonly ILogger<TmdbImportService> _logger;

    public TmdbImportService(
        HttpClient httpClient,
        IMongoDatabase database,
        IConfiguration configuration,
        IOptions<TmdbSettings> tmdbOptions,
        ILogger<TmdbImportService> logger)
    {
        var mongoSettings = configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>() ?? new MongoDbSettings();

        _httpClient = httpClient;
        _movies = database.GetCollection<Movie>(mongoSettings.MoviesCollectionName);
        _tmdbSettings = tmdbOptions.Value;
        _logger = logger;
    }

    public async Task<TmdbImportResultDto> ImportPopularAsync(TmdbImportRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_tmdbSettings.ApiKey))
        {
            throw new InvalidOperationException("TmdbSettings:ApiKey nije postavljen.");
        }

        var existing = await _movies.Find(Builders<Movie>.Filter.Empty)
            .Project(x => new { x.Title, x.Year })
            .ToListAsync(cancellationToken);

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var movie in existing)
        {
            if (!string.IsNullOrWhiteSpace(movie.Title) && movie.Year > 0)
            {
                keys.Add(BuildDedupKey(movie.Title, movie.Year));
            }
        }

        var result = new TmdbImportResultDto();
        var moviesToInsert = new List<Movie>();

        for (var page = request.StartPage; page < request.StartPage + request.Pages; page++)
        {
            TmdbPageResponse? pageResponse;
            try
            {
                var url = BuildPopularUrl(page, request.Language);
                pageResponse = await _httpClient.GetFromJsonAsync<TmdbPageResponse>(url, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Page {page}: {ex.Message}");
                continue;
            }

            if (pageResponse?.Results is null)
            {
                continue;
            }

            foreach (var item in pageResponse.Results)
            {
                result.Requested++;

                if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.ReleaseDate))
                {
                    result.Failed++;
                    result.Errors.Add($"TMDbId {item.Id}: nedostaje title ili release_date.");
                    continue;
                }

                if (!TryParseYear(item.ReleaseDate, out var year))
                {
                    result.Failed++;
                    result.Errors.Add($"TMDbId {item.Id}: nevalidan datum '{item.ReleaseDate}'.");
                    continue;
                }

                var dedupKey = BuildDedupKey(item.Title, year);
                if (keys.Contains(dedupKey))
                {
                    result.SkippedDuplicates++;
                    continue;
                }

                var genreNames = await ResolveGenresAsync(item.GenreIds, request.Language, cancellationToken);
                var details = request.IncludeCredits
                    ? await TryGetMovieDetailsAsync(item.Id, request.Language, cancellationToken)
                    : null;

                var director = details?.Credits?.Crew?.FirstOrDefault(x => string.Equals(x.Job, "Director", StringComparison.OrdinalIgnoreCase))?.Name
                               ?? "Unknown";

                var cast = details?.Credits?.Cast?
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                    .Take(6)
                    .Select(x => x.Name!)
                    .ToList() ?? new List<string>();

                var description = details?.Overview;
                if (string.IsNullOrWhiteSpace(description))
                {
                    description = item.Overview ?? string.Empty;
                }

                var posterPath = details?.PosterPath;
                if (string.IsNullOrWhiteSpace(posterPath))
                {
                    posterPath = item.PosterPath;
                }

                var movie = new Movie
                {
                    Title = item.Title.Trim(),
                    Description = description ?? string.Empty,
                    Year = year,
                    Genres = genreNames,
                    Director = director,
                    Cast = cast,
                    PosterUrl = BuildPosterUrl(posterPath),
                    AverageRating = 0,
                    ReviewCount = 0,
                    CreatedAt = DateTime.UtcNow
                };

                moviesToInsert.Add(movie);
                keys.Add(dedupKey);
                result.Imported++;
            }
        }

        if (moviesToInsert.Count > 0)
        {
            await _movies.InsertManyAsync(moviesToInsert, cancellationToken: cancellationToken);
        }

        if (result.Errors.Count > 20)
        {
            result.Errors = result.Errors.Take(20).ToList();
        }

        _logger.LogInformation("TMDb import finished. Requested={Requested}, Imported={Imported}, Duplicates={Duplicates}, Failed={Failed}",
            result.Requested,
            result.Imported,
            result.SkippedDuplicates,
            result.Failed);

        return result;
    }

    private readonly Dictionary<int, string> _genreCache = new();
    private string _cachedLanguage = string.Empty;

    private async Task<List<string>> ResolveGenresAsync(List<int>? genreIds, string language, CancellationToken cancellationToken)
    {
        if (genreIds is null || genreIds.Count == 0)
        {
            return new List<string>();
        }

        if (_genreCache.Count == 0 || !string.Equals(_cachedLanguage, language, StringComparison.OrdinalIgnoreCase))
        {
            var response = await _httpClient.GetFromJsonAsync<TmdbGenreResponse>(BuildGenresUrl(language), cancellationToken);
            _genreCache.Clear();
            _cachedLanguage = language;

            if (response?.Genres is not null)
            {
                foreach (var genre in response.Genres)
                {
                    if (!string.IsNullOrWhiteSpace(genre.Name))
                    {
                        _genreCache[genre.Id] = genre.Name;
                    }
                }
            }
        }

        return genreIds
            .Where(_genreCache.ContainsKey)
            .Select(x => _genreCache[x])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<TmdbMovieDetailsResponse?> TryGetMovieDetailsAsync(int tmdbId, string language, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TmdbMovieDetailsResponse>(BuildMovieDetailsUrl(tmdbId, language), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TMDb details fetch failed for TMDbId={TmdbId}", tmdbId);
            return null;
        }
    }

    private static bool TryParseYear(string releaseDate, out int year)
    {
        if (DateTime.TryParseExact(releaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            year = date.Year;
            return true;
        }

        year = 0;
        return false;
    }

    private string BuildPosterUrl(string? posterPath)
    {
        if (string.IsNullOrWhiteSpace(posterPath))
        {
            return string.Empty;
        }

        return $"{_tmdbSettings.ImageBaseUrl.TrimEnd('/')}/{posterPath.TrimStart('/')}";
    }

    private string BuildPopularUrl(int page, string language)
    {
        return $"{_tmdbSettings.BaseUrl.TrimEnd('/')}/movie/popular?api_key={Uri.EscapeDataString(_tmdbSettings.ApiKey)}&language={Uri.EscapeDataString(language)}&page={page}";
    }

    private string BuildMovieDetailsUrl(int id, string language)
    {
        return $"{_tmdbSettings.BaseUrl.TrimEnd('/')}/movie/{id}?api_key={Uri.EscapeDataString(_tmdbSettings.ApiKey)}&language={Uri.EscapeDataString(language)}&append_to_response=credits";
    }

    private string BuildGenresUrl(string language)
    {
        return $"{_tmdbSettings.BaseUrl.TrimEnd('/')}/genre/movie/list?api_key={Uri.EscapeDataString(_tmdbSettings.ApiKey)}&language={Uri.EscapeDataString(language)}";
    }

    private static string BuildDedupKey(string title, int year)
    {
        return $"{title.Trim().ToLowerInvariant()}|{year}";
    }

    private sealed class TmdbPageResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbMovieSummary> Results { get; set; } = new();
    }

    private sealed class TmdbMovieSummary
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("genre_ids")]
        public List<int> GenreIds { get; set; } = new();
    }

    private sealed class TmdbGenreResponse
    {
        [JsonPropertyName("genres")]
        public List<TmdbGenreItem> Genres { get; set; } = new();
    }

    private sealed class TmdbGenreItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TmdbMovieDetailsResponse
    {
        [JsonPropertyName("overview")]
        public string? Overview { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("credits")]
        public TmdbCredits? Credits { get; set; }
    }

    private sealed class TmdbCredits
    {
        [JsonPropertyName("cast")]
        public List<TmdbCastItem> Cast { get; set; } = new();

        [JsonPropertyName("crew")]
        public List<TmdbCrewItem> Crew { get; set; } = new();
    }

    private sealed class TmdbCastItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class TmdbCrewItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("job")]
        public string? Job { get; set; }
    }
}
