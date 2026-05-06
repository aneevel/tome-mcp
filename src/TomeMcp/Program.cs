using TomeMcp;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: TomeMcp <game-root-path>");
    Console.Error.WriteLine("  e.g. TomeMcp /path/to/TalesMajEyal/game/");
    return 1;
}

var gameRoot = args[0];
if (!Directory.Exists(gameRoot))
{
    Console.Error.WriteLine($"Game root not found: {gameRoot}");
    return 1;
}

var engineRoot = Path.Combine(gameRoot, "engines", "src");
var modulesRoot = Path.Combine(gameRoot, "modules");

if (!Directory.Exists(engineRoot))
{
    Console.Error.WriteLine($"Engine source not found: {engineRoot}");
    return 1;
}

Console.Error.WriteLine("T-Engine4 MCP Server — building class index...");

var classIndex = new ClassIndex();
var classCount = classIndex.Build(engineRoot, modulesRoot);

Console.Error.WriteLine($"Indexed {classCount} classes, {classIndex.AllFiles.Count} total files.");

Console.Error.WriteLine("Building data index...");

var dataIndex = new DataIndex();
var modulesDataRoot = Path.Combine(modulesRoot, "data");
if (Directory.Exists(modulesDataRoot))
    dataIndex.Build(modulesDataRoot);

Console.Error.WriteLine($"Indexed {dataIndex.Talents.Count} talents, {dataIndex.Effects.Count} effects, {dataIndex.DamageTypes.Count} damage types.");
Console.Error.WriteLine("T-Engine4 MCP Server running.");

var handler = new RequestHandler(engineRoot, classIndex, dataIndex);

while (true)
{
    var line = Console.ReadLine();

    if (line is null)
        break;

    try
    {
        var invocation = Invocation.Deserialize(line);
        handler.Handle(invocation);
    }
    catch
    {
        Response.FromMessage("ERROR: Malformed input.").Send();
        RequestHandler.SendToolsList();
    }
}

return 0;