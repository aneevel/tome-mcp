using TomeMcp;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: TomeMcp <engine-root-path>");
    Console.Error.WriteLine("  e.g. TomeMcp /path/to/TalesMajEyal/game/engines/src/");
    return 1;
}

var engineRoot = args[0];
if (!Directory.Exists(engineRoot))
{
    Console.Error.WriteLine($"Engine root not found: {engineRoot}");
    return 1;
}

Console.WriteLine("T-Engine4 MCP Server running...");

var handler = new RequestHandler(engineRoot);

while (true)
{
    var line = Console.ReadLine();

    if (line is null)
        break;

    try
    {
        var invocation = Invocation.Deserialize(line);

        if (!handler.Handle(invocation))
            return 0;
    }
    catch
    {
        Response.FromMessage("ERROR: Malformed input.").Send();
        RequestHandler.SendToolsList();
    }
}

return 0;