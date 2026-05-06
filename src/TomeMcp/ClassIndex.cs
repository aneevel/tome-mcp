using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace TomeMcp;

public class SearchMatch
{
    [JsonPropertyName("className")]
    public string ClassName { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = "";

    [JsonPropertyName("lineNumber")]
    public int LineNumber { get; set; }

    [JsonPropertyName("line")]
    public string Line { get; set; } = "";

    [JsonPropertyName("context")]
    public List<string> Context { get; set; } = new();
}

public class ClassIndex
{
    public Dictionary<string, LuaClassInfo> Classes { get; } = new();
    public Dictionary<string, List<string>> ChildrenOf { get; } = new();
    public List<string> AllFiles { get; } = new();

    private readonly Dictionary<string, string> _fileToClassName = new();
    private string _gameRoot = "";

    public int Build(string engineRoot, string modulesRoot)
    {
        _gameRoot = Path.GetDirectoryName(engineRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            ?? engineRoot;

        var searchDirs = new List<string> { engineRoot };
        if (Directory.Exists(modulesRoot))
            searchDirs.Add(modulesRoot);

        foreach (var dir in searchDirs)
        {
            foreach (var filePath in Directory.EnumerateFiles(dir, "*.lua", SearchOption.AllDirectories))
            {
                AllFiles.Add(filePath);

                var content = File.ReadAllText(filePath);
                if (!content.Contains("class.make") && !content.Contains("class.inherit"))
                    continue;

                var info = LuaParser.Parse(content, filePath);
                if (string.IsNullOrEmpty(info.Name))
                    continue;

                Classes.TryAdd(info.Name, info);
                _fileToClassName[filePath] = info.Name;
            }
        }

        ResolveBaseClassNames();
        BuildChildrenMap();

        return Classes.Count;
    }

    private void ResolveBaseClassNames()
    {
        foreach (var info in Classes.Values)
        {
            for (int i = 0; i < info.BaseClasses.Count; i++)
            {
                var shortName = info.BaseClasses[i];

                if (Classes.ContainsKey(shortName))
                    continue;

                // Match against local require aliases:
                // e.g. local Entity = require "engine.Entity" → "Entity" resolves to "engine.Entity"
                var dep = info.Dependencies.FirstOrDefault(d =>
                    d.LocalName == shortName);

                if (dep is not null && Classes.ContainsKey(dep.ModulePath))
                {
                    info.BaseClasses[i] = dep.ModulePath;
                    continue;
                }

                // Try qualified prefix match (e.g. "engine.Generator" for short name "Generator")
                var qualified = Classes.Keys.FirstOrDefault(k =>
                    k.EndsWith("." + shortName, StringComparison.Ordinal));

                if (qualified is not null)
                    info.BaseClasses[i] = qualified;
            }
        }
    }

    private void BuildChildrenMap()
    {
        ChildrenOf.Clear();

        foreach (var info in Classes.Values)
        {
            foreach (var baseName in info.BaseClasses)
            {
                if (!ChildrenOf.TryGetValue(baseName, out var children))
                {
                    children = new List<string>();
                    ChildrenOf[baseName] = children;
                }
                children.Add(info.Name);
            }
        }
    }

    public List<string> GetAncestors(string className)
    {
        var ancestors = new List<string>();
        var visited = new HashSet<string>();
        CollectAncestors(className, ancestors, visited);
        return ancestors;
    }

    private void CollectAncestors(string className, List<string> ancestors, HashSet<string> visited)
    {
        if (!Classes.TryGetValue(className, out var info))
            return;

        foreach (var baseName in info.BaseClasses)
        {
            if (!visited.Add(baseName))
                continue;
            ancestors.Add(baseName);
            CollectAncestors(baseName, ancestors, visited);
        }
    }

    public List<string> GetDescendants(string className)
    {
        var descendants = new List<string>();
        var visited = new HashSet<string>();
        CollectDescendants(className, descendants, visited);
        return descendants;
    }

    private void CollectDescendants(string className, List<string> descendants, HashSet<string> visited)
    {
        if (!ChildrenOf.TryGetValue(className, out var children))
            return;

        foreach (var child in children)
        {
            if (!visited.Add(child))
                continue;
            descendants.Add(child);
            CollectDescendants(child, descendants, visited);
        }
    }

    public List<SearchMatch> Search(string pattern, bool caseSensitive = false, int maxResults = 50, int contextLines = 2, string? pathFilter = null)
    {
        var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        var regex = new Regex(pattern, options);

        var results = new List<SearchMatch>();

        foreach (var filePath in AllFiles)
        {
            if (pathFilter is not null && !filePath.Contains(pathFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!File.Exists(filePath))
                continue;

            var lines = File.ReadAllLines(filePath);
            _fileToClassName.TryGetValue(filePath, out var className);
            var category = DeriveCategoryFromPath(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (!regex.IsMatch(lines[i]))
                    continue;

                var contextStart = Math.Max(0, i - contextLines);
                var contextEnd = Math.Min(lines.Length - 1, i + contextLines);

                var context = new List<string>();
                for (int c = contextStart; c <= contextEnd; c++)
                {
                    var prefix = c == i ? ">" : " ";
                    context.Add($"{prefix} {c + 1}: {lines[c]}");
                }

                results.Add(new SearchMatch
                {
                    ClassName = className ?? "",
                    Category = category,
                    FilePath = filePath,
                    LineNumber = i + 1,
                    Line = lines[i].TrimStart(),
                    Context = context,
                });

                if (results.Count >= maxResults)
                    return results;
            }
        }

        return results;
    }

    private string DeriveCategoryFromPath(string filePath)
    {
        if (string.IsNullOrEmpty(_gameRoot) || !filePath.StartsWith(_gameRoot, StringComparison.Ordinal))
            return Path.GetFileNameWithoutExtension(filePath);

        var relative = filePath[(_gameRoot.Length + 1)..];
        if (relative.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
            relative = relative[..^4];

        return relative.Replace(Path.DirectorySeparatorChar, '/');
    }
}
