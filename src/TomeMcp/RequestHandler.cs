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

            case MethodType.ToolsCall:
                Response.FromMessage("OK").Send();

                if (invocation.Params?.Name == "ping")
                {
                    Response.FromContent(new object[] { new { type = "text", text = "pong" } }).Send();
                }
                else
                {
                    Response.FromMessage("ERROR: Malformed input.").Send();
                }
                break;
        }

        return true;
    }
}
