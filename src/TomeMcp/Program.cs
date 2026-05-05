using System.Text.Json;

Console.WriteLine("T-Engine4 MCP Server running...");

while (true)
{
    var line = Console.ReadLine();

    if (line is null)
        break;

    try
    {
        var doc = JsonDocument.Parse(line);
        var method = doc.RootElement.GetProperty("method").GetString();

        if (method == "shutdown")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { result = new { message = "Shutting down." } }));
            break;
        }
        else if (method == "initialize")
        {
            Console.WriteLine(JsonSerializer.Serialize(new { result = new { message = "OK" } }));
            Thread.Sleep(3000);
            Console.WriteLine(JsonSerializer.Serialize(new { result = new { tools = Array.Empty<object>() } }));
        }
        else
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new { result = new { message = "ERROR: Malformed input; skipping processing."}}));
        }
    }
    catch
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new { result = new { message = "ERROR: Malformed input; skipping processing."}}));
    }
}