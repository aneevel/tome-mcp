using System.Text.Json;
using System.Text.Json.Serialization;

namespace TomeMcp;

public enum MethodType
{
    Initialize,
    Shutdown,
    ToolsList,
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
        ["tools/list"] = MethodType.ToolsList,
        ["tools/call"] = MethodType.ToolsCall,
    };

    public static readonly Dictionary<string, string> AvailableTools = new()
    {
        ["ping"] = "Returns pong",
        ["read_class"] = "Parse a T-Engine4 Lua class file and return its structure (inheritance, methods, dependencies)",
        ["list_classes"] = "List all indexed classes, optionally filtered by name substring",
        ["class_hierarchy"] = "Show full inheritance tree for a class (ancestors and descendants)",
        ["search_code"] = "Search all Lua source files for a keyword or regex pattern, with surrounding context. Use path_filter to narrow by directory (e.g. talents/psionic)",
    };

    public static readonly Dictionary<string, string> MethodExamples = new()
    {
        ["initialize"] = "{\"method\": \"initialize\"}",
        ["shutdown"] = "{\"method\": \"shutdown\"}",
        ["tools/list"] = "{\"method\": \"tools/list\"}",
        ["tools/call (ping)"] = "{\"method\": \"tools/call\", \"params\": {\"name\": \"ping\"}}",
        ["tools/call (read_class)"] = "{\"method\": \"tools/call\", \"params\": {\"name\": \"read_class\", \"class_name\": \"engine.Actor\"}}",
        ["tools/call (list_classes)"] = "{\"method\": \"tools/call\", \"params\": {\"name\": \"list_classes\"}}",
        ["tools/call (list_classes filtered)"] = "{\"method\": \"tools/call\", \"params\": {\"name\": \"list_classes\", \"filter\": \"Actor\"}}",
        ["tools/call (class_hierarchy)"] = "{\"method\": \"tools/call\", \"params\": {\"name\": \"class_hierarchy\", \"class_name\": \"engine.Entity\"}}",
        ["tools/call (search_code)"] = "{\"method\": \"tools/call\", \"params\": {\"name\": \"search_code\", \"pattern\": \"knockback\"}}",
        ["tools/call (search_code filtered)"] = "{\"method\": \"tools/call\", \"params\": {\"name\": \"search_code\", \"pattern\": \"DamageType.MIND\", \"path_filter\": \"talents/psionic\"}}",
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

    [JsonPropertyName("class_name")]
    public string? ClassName { get; set; }

    [JsonPropertyName("filter")]
    public string? Filter { get; set; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    [JsonPropertyName("case_sensitive")]
    public bool? CaseSensitive { get; set; }

    [JsonPropertyName("max_results")]
    public int? MaxResults { get; set; }

    [JsonPropertyName("context_lines")]
    public int? ContextLines { get; set; }

    [JsonPropertyName("path_filter")]
    public string? PathFilter { get; set; }
}
