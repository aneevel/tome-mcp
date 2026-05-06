using System.Text.Json;

namespace TomeMcp;

public class RequestHandler
{
    private readonly string _engineRoot;

    public RequestHandler(string engineRoot)
    {
        _engineRoot = engineRoot;
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

        var filePath = ResolveClassPath(className);
        if (!File.Exists(filePath))
        {
            Response.FromMessage($"ERROR: File not found: {filePath}").Send();
            return;
        }

        var content = File.ReadAllText(filePath);
        var classInfo = LuaParser.Parse(content, filePath);
        var json = JsonSerializer.Serialize(classInfo, JsonOptions);

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
