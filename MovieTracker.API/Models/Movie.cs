using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MovieTracker.API.Models;

public class Movie
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Year { get; set; }
    public List<string> Genres { get; set; } = new();
    public string Director { get; set; } = string.Empty;
    public List<string> Cast { get; set; } = new();
    public string PosterUrl { get; set; } = string.Empty;

    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
