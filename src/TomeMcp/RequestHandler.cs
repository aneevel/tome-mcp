using System.Text.Json;

namespace TomeMcp;

public class RequestHandler
{
    public bool Handle(Invocation invocation)
    {
        switch (invocation.Method)
        {
            case MethodType.Shutdown:
                Respond(new { message = "Shutting down." });
                return false;

            case MethodType.Initialize:
                Respond(new { message = "OK" });
                Thread.Sleep(3000);
                Respond(new { tools = Array.Empty<object>() });
                break;

            case MethodType.ToolsCall:
                Respond(new { message = "OK" });

                if (invocation.Params?.Name == "ping")
                {
                    Respond(new { content = new[] { new { type = "text", text = "pong" } } });
                }
                else
                {
                    Respond(new { message = "ERROR: Malformed input." });
                }
                break;
        }

        return true;
    }

    private void Respond(object result)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { result }));
    }
}
