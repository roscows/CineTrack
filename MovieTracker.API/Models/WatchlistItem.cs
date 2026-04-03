using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MovieTracker.API.Models;

public class WatchlistItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string MovieId { get; set; } = string.Empty;

    public string MovieTitle { get; set; } = string.Empty;
    public string MoviePosterUrl { get; set; } = string.Empty;
    public int MovieYear { get; set; }

    public bool Watched { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? WatchedAt { get; set; }
}
