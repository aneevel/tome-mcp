using System.Text.RegularExpressions;

namespace TomeMcp;

public static partial class LuaParser
{
    private static readonly Regex ModuleMakeRegex = GenerateModuleMakeRegex();
    private static readonly Regex ModuleInheritRegex = GenerateModuleInheritRegex();
    private static readonly Regex LocalRequireRegex = GenerateLocalRequireRegex();
    private static readonly Regex BareRequireRegex = GenerateBareRequireRegex();
    private static readonly Regex InstanceMethodRegex = GenerateInstanceMethodRegex();
    private static readonly Regex StaticMethodRegex = GenerateStaticMethodRegex();
    private static readonly Regex ClassmodRegex = GenerateClassmodRegex();
    private static readonly Regex LuaDocLineRegex = GenerateLuaDocLineRegex();

    [GeneratedRegex(@"^module\(\.\.\.,\s*package\.seeall,\s*class\.make\)", RegexOptions.Compiled)]
    private static partial Regex GenerateModuleMakeRegex();

    [GeneratedRegex(@"^module\(\.\.\.,\s*package\.seeall,\s*class\.inherit\((.+)\)\)", RegexOptions.Compiled)]
    private static partial Regex GenerateModuleInheritRegex();

    [GeneratedRegex(@"^local\s+(\w+)\s*=\s*require\s+""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex GenerateLocalRequireRegex();

    [GeneratedRegex(@"^require\s+""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex GenerateBareRequireRegex();

    [GeneratedRegex(@"^function\s+_M:(\w+)\s*\(", RegexOptions.Compiled)]
    private static partial Regex GenerateInstanceMethodRegex();

    [GeneratedRegex(@"^function\s+_M\.(\w+)\s*\(", RegexOptions.Compiled)]
    private static partial Regex GenerateStaticMethodRegex();

    [GeneratedRegex(@"@classmod\s+(\S+)", RegexOptions.Compiled)]
    private static partial Regex GenerateClassmodRegex();

    [GeneratedRegex(@"^---\s?(.*)", RegexOptions.Compiled)]
    private static partial Regex GenerateLuaDocLineRegex();

    public static LuaClassInfo Parse(string content, string filePath)
    {
        var info = new LuaClassInfo { FilePath = filePath };
        var lines = content.Split('\n');

        var docBuffer = new List<string>();
        bool moduleFound = false;
        var seenDependencies = new HashSet<string>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var trimmed = line.TrimStart();
            int lineNumber = i + 1;

            // LuaDoc lines (--- comment) — accumulate until consumed
            var luaDocMatch = LuaDocLineRegex.Match(trimmed);
            if (luaDocMatch.Success)
            {
                var docLine = luaDocMatch.Groups[1].Value;

                var classmodMatch = ClassmodRegex.Match(docLine);
                if (classmodMatch.Success)
                {
                    info.Name = classmodMatch.Groups[1].Value;
                }
                else
                {
                    docBuffer.Add(docLine);
                }
                continue;
            }

            // module(..., package.seeall, class.make)
            if (!moduleFound)
            {
                var makeMatch = ModuleMakeRegex.Match(trimmed);
                if (makeMatch.Success)
                {
                    info.IsRootClass = true;
                    moduleFound = true;
                    if (docBuffer.Count > 0)
                        info.Description = string.Join("\n", docBuffer);
                    docBuffer.Clear();
                    continue;
                }

                var inheritMatch = ModuleInheritRegex.Match(trimmed);
                if (inheritMatch.Success)
                {
                    info.IsRootClass = false;
                    var bases = inheritMatch.Groups[1].Value
                        .Split(',')
                        .Select(b => b.Trim())
                        .Where(b => b.Length > 0)
                        .ToList();
                    info.BaseClasses = bases;
                    moduleFound = true;
                    if (docBuffer.Count > 0)
                        info.Description = string.Join("\n", docBuffer);
                    docBuffer.Clear();
                    continue;
                }
            }

            // Non-doc, non-module line clears the doc buffer
            if (!luaDocMatch.Success && trimmed.Length > 0 && !trimmed.StartsWith("--"))
            {
                docBuffer.Clear();
            }

            var localReqMatch = LocalRequireRegex.Match(trimmed);
            if (localReqMatch.Success)
            {
                var modulePath = localReqMatch.Groups[2].Value;
                if (seenDependencies.Add(modulePath))
                {
                    info.Dependencies.Add(new LuaDependency
                    {
                        LocalName = localReqMatch.Groups[1].Value,
                        ModulePath = modulePath,
                    });
                }
                continue;
            }

            var bareReqMatch = BareRequireRegex.Match(trimmed);
            if (bareReqMatch.Success)
            {
                var modulePath = bareReqMatch.Groups[1].Value;
                if (seenDependencies.Add(modulePath))
                {
                    info.Dependencies.Add(new LuaDependency
                    {
                        ModulePath = modulePath,
                    });
                }
                continue;
            }

            // function _M:name(
            var instMatch = InstanceMethodRegex.Match(trimmed);
            if (instMatch.Success)
            {
                info.Methods.Add(new LuaMethodInfo
                {
                    Name = instMatch.Groups[1].Value,
                    IsInstance = true,
                    LineNumber = lineNumber,
                });
                docBuffer.Clear();
                continue;
            }

            // function _M.name(
            var staticMatch = StaticMethodRegex.Match(trimmed);
            if (staticMatch.Success)
            {
                info.Methods.Add(new LuaMethodInfo
                {
                    Name = staticMatch.Groups[1].Value,
                    IsInstance = false,
                    LineNumber = lineNumber,
                });
                docBuffer.Clear();
                continue;
            }
        }

        // Fallback: derive name from file path if @classmod wasn't found
        if (string.IsNullOrEmpty(info.Name))
        {
            info.Name = DeriveClassNameFromPath(filePath);
        }

        return info;
    }

    private static string DeriveClassNameFromPath(string filePath)
    {
        var engineMarker = "/engine/";
        var idx = filePath.LastIndexOf(engineMarker, StringComparison.Ordinal);
        if (idx >= 0)
        {
            var relative = filePath[(idx + 1)..];
            if (relative.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
                relative = relative[..^4];
            return relative.Replace('/', '.');
        }

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        return fileName;
    }
}
