using System.Text.Json.Serialization;

namespace TomeMcp;

public class LuaMethodInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("isInstance")]
    public bool IsInstance { get; set; }

    [JsonPropertyName("lineNumber")]
    public int LineNumber { get; set; }
}

public class LuaDependency
{
    [JsonPropertyName("modulePath")]
    public string ModulePath { get; set; } = "";

    [JsonPropertyName("localName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LocalName { get; set; }
}

public class LuaClassInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("baseClasses")]
    public List<string> BaseClasses { get; set; } = new();

    [JsonPropertyName("isRootClass")]
    public bool IsRootClass { get; set; }

    [JsonPropertyName("methods")]
    public List<LuaMethodInfo> Methods { get; set; } = new();

    [JsonPropertyName("dependencies")]
    public List<LuaDependency> Dependencies { get; set; } = new();

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = "";
}
