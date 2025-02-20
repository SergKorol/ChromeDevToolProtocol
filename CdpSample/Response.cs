using System.Text.Json;
using System.Text.Json.Serialization;

namespace CdpSample;

public record CdpResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("result")]
    public ResponseResult? Result { get; set; }
    
    public record ResponseResult
    {
        [JsonPropertyName("result")]
        public JsonElement? ResultValue { get; set; }
    }
}