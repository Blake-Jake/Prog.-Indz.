using SQLite;
using System.Collections.Generic;
using System.Text.Json;

namespace EventMatch.Models;

public class Profile
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Link to user account (email used here to keep it simple)
    public string UserEmail { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public int RadiusKm { get; set; } = 10;
    public string Description { get; set; } = string.Empty;
    public string PhotoPath { get; set; } = string.Empty;

    // Serialized list of user's preferred tags
    public string PreferredTagsJson { get; set; } = "[]";

    public List<string> GetPreferredTags()
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(PreferredTagsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    public void SetPreferredTags(List<string> tags)
    {
        PreferredTagsJson = JsonSerializer.Serialize(tags ?? new List<string>());
    }
}