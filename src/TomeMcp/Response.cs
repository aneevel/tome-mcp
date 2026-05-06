using System.Text.Json;
using System.Text.Json.Serialization;

namespace TomeMcp;

public enum ResponseType
{
    Message,
    ArrayContent,
}

public class Response
{
    [JsonIgnore]
    public ResponseType Type { get; set; }

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object[]? Content { get; set; }

    public static Response FromMessage(string message)
    {
        return new Response
        {
            Type = ResponseType.Message,
            Message = message,
        };
    }

    public static Response FromContent(object[] content)
    {
        return new Response
        {
            Type = ResponseType.ArrayContent,
            Content = content,
        };
    }

    public void Send()
    {
        Console.WriteLine(JsonSerializer.Serialize(new { result = this }, SerializeOptions));
    }

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
