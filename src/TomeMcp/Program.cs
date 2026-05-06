using TomeMcp;

Console.WriteLine("T-Engine4 MCP Server running...");

var handler = new RequestHandler();

while (true)
{
    var line = Console.ReadLine();

    if (line is null)
        break;

    try
    {
        var invocation = Invocation.Deserialize(line);

        if (!handler.Handle(invocation))
            return;
    }
    catch
    {
        Response.FromMessage("ERROR: Malformed input.").Send();
    }
}