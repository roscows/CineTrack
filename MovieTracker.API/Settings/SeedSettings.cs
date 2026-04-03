namespace MovieTracker.API.Settings;

public class SeedSettings
{
    public bool Enabled { get; set; } = true;
    public string AdminUsername { get; set; } = "admin";
    public string AdminEmail { get; set; } = "admin@movietracker.local";
    public string AdminPassword { get; set; } = "Admin123!";
}
