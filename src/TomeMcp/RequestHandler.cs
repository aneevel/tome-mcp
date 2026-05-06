namespace TomeMcp;

public class RequestHandler
{
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

                if (invocation.Params?.Name == "ping")
                {
                    Response.FromContent(new object[] { new { type = "text", text = "pong" } }).Send();
                }
                else
                {
                    Response.FromMessage("ERROR: Malformed input.").Send();
                    SendToolsList();
                }
                break;
        }

        return true;
    }

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
