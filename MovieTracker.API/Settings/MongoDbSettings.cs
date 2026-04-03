namespace MovieTracker.API.Settings;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "MovieTrackerDb";
    public string MoviesCollectionName { get; set; } = "Movies";
    public string UsersCollectionName { get; set; } = "Users";
    public string ReviewsCollectionName { get; set; } = "Reviews";
    public string WatchlistCollectionName { get; set; } = "Watchlist";
}
