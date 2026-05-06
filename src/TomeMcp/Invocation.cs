using System.Text.Json;
using System.Text.Json.Serialization;

namespace TomeMcp;

public enum MethodType
{
    Initialize,
    Shutdown,
    ToolsCall,
}

public class Invocation
{
    public MethodType Method { get; set; }
    public InvocationParams? Params { get; set; }

    private static readonly Dictionary<string, MethodType> MethodMap = new()
    {
        ["initialize"] = MethodType.Initialize,
        ["shutdown"] = MethodType.Shutdown,
        ["tools/call"] = MethodType.ToolsCall,
    };

    public static Invocation Deserialize(string json)
    {
        var doc = JsonDocument.Parse(json);
        var methodStr = doc.RootElement.GetProperty("method").GetString()
            ?? throw new JsonException("Missing method.");

        if (!MethodMap.TryGetValue(methodStr, out var methodType))
            throw new JsonException($"Unknown method: {methodStr}");

        var invocation = new Invocation { Method = methodType };

        if (doc.RootElement.TryGetProperty("params", out var paramsElement))
        {
            var paramTypeMap = new Dictionary<MethodType, InvocationParamsType>
            {
                [MethodType.ToolsCall] = InvocationParamsType.ToolsCall,
            };

            var p = JsonSerializer.Deserialize<InvocationParams>(paramsElement.GetRawText());
            if (p is not null && paramTypeMap.TryGetValue(methodType, out var paramsType))
            {
                p.Type = paramsType;
            }
            invocation.Params = p;
        }

        return invocation;
    }
}

public enum InvocationParamsType
{
    ToolsCall,
}

public class InvocationParams
{
    public InvocationParamsType Type { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
