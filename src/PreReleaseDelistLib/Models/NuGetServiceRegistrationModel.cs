using System.Text.Json.Serialization;

namespace PreReleaseDelistLib.Models;

public class NuGetServiceRegistrationModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;
}