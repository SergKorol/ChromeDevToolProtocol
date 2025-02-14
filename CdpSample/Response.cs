using System.Text.Json.Serialization;

namespace CdpSample;

public class CdpResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("result")]
    public ResultWrapper? Result { get; set; }

    public class ResultWrapper
    {
        [JsonPropertyName("result")]
        public InnerResult? HtmlResult { get; set; }
    }

    public class InnerResult
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }
}