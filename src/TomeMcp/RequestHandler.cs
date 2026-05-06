using System.Text.Json;

namespace TomeMcp;

public class RequestHandler
{
    private readonly string _engineRoot;
    private readonly ClassIndex _classIndex;

    public RequestHandler(string engineRoot, ClassIndex classIndex)
    {
        _engineRoot = engineRoot;
        _classIndex = classIndex;
    }

    public bool Handle(Invocation invocation)
    {
        switch (invocation.Method)
        {
            case MethodType.Shutdown:
                Response.FromMessage("Shutting down.").Send();
                return false;

            case MethodType.Initialize:
                Response.FromMessage("OK").Send();
                Thread.Sleep(3000);
                Response.FromContent(Array.Empty<object>()).Send();
                break;

            case MethodType.ToolsList:
                Response.FromMessage("OK").Send();
                SendToolsList();
                break;

            case MethodType.ToolsCall:
                Response.FromMessage("OK").Send();
                HandleToolsCall(invocation);
                break;
        }

        return true;
    }

    private void HandleToolsCall(Invocation invocation)
    {
        switch (invocation.Params?.Name)
        {
            case "ping":
                Response.FromContent(new object[] { new { type = "text", text = "pong" } }).Send();
                break;

            case "read_class":
                HandleReadClass(invocation);
                break;

            case "list_classes":
                HandleListClasses(invocation);
                break;

            case "class_hierarchy":
                HandleClassHierarchy(invocation);
                break;

            default:
                Response.FromMessage("ERROR: Malformed input.").Send();
                SendToolsList();
                break;
        }
    }

    private void HandleReadClass(Invocation invocation)
    {
        var className = invocation.Params?.ClassName;
        if (string.IsNullOrWhiteSpace(className))
        {
            Response.FromMessage("ERROR: Missing class_name parameter.").Send();
            return;
        }

        if (_classIndex.Classes.TryGetValue(className, out var cached))
        {
            var json = JsonSerializer.Serialize(cached, JsonOptions);
            Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
            return;
        }

        var filePath = ResolveClassPath(className);
        if (!File.Exists(filePath))
        {
            Response.FromMessage($"ERROR: Class not found: {className}").Send();
            return;
        }

        var content = File.ReadAllText(filePath);
        var classInfo = LuaParser.Parse(content, filePath);
        var json2 = JsonSerializer.Serialize(classInfo, JsonOptions);

        Response.FromContent(new object[] { new { type = "text", text = json2 } }).Send();
    }

    private void HandleListClasses(Invocation invocation)
    {
        var filter = invocation.Params?.Filter;

        var classes = _classIndex.Classes.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            classes = classes.Where(c =>
                c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        var result = classes
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                name = c.Name,
                baseClasses = c.BaseClasses,
                methodCount = c.Methods.Count,
                isRootClass = c.IsRootClass,
            })
            .ToArray<object>();

        var json = JsonSerializer.Serialize(result, JsonOptions);
        Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
    }

    private void HandleClassHierarchy(Invocation invocation)
    {
        var className = invocation.Params?.ClassName;
        if (string.IsNullOrWhiteSpace(className))
        {
            Response.FromMessage("ERROR: Missing class_name parameter.").Send();
            return;
        }

        if (!_classIndex.Classes.ContainsKey(className))
        {
            Response.FromMessage($"ERROR: Class not found in index: {className}").Send();
            return;
        }

        var ancestors = _classIndex.GetAncestors(className);
        var descendants = _classIndex.GetDescendants(className);

        var result = new
        {
            @class = className,
            ancestors,
            descendants,
        };

        var json = JsonSerializer.Serialize(result, JsonOptions);
        Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
    }

    private string ResolveClassPath(string className)
    {
        if (File.Exists(className))
            return className;

        var relativePath = className.Replace('.', '/') + ".lua";
        return Path.Combine(_engineRoot, relativePath);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static void SendToolsList()
    {
        var tools = Invocation.AvailableTools
            .Select(t => new
            {
                name = t.Key,
                description = t.Value,
                example = $"{{\"method\": \"tools/call\", \"params\": {{\"name\": \"{t.Key}\"}}}}",
            })
            .ToArray<object>();

        var methods = Invocation.MethodExamples
            .Select(m => new
            {
                method = m.Key,
                example = m.Value,
            })
            .ToArray<object>();

        Response.FromContent(new object[] { new { tools, methods } }).Send();
    }
}
