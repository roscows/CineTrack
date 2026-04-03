namespace MovieTracker.API.Settings;

public class JwtSettings
{
    public string SecretKey { get; set; } = "SuperSecretKeyForDev_ChangeMe_AtLeast32Chars";
    public string Issuer { get; set; } = "MovieTracker.API";
    public string Audience { get; set; } = "MovieTracker.Client";
    public int ExpiresInMinutes { get; set; } = 120;
}
