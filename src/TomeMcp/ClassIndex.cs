namespace TomeMcp;

public class ClassIndex
{
    public Dictionary<string, LuaClassInfo> Classes { get; } = new();
    public Dictionary<string, List<string>> ChildrenOf { get; } = new();

    public int Build(string engineRoot, string modulesRoot)
    {
        var searchDirs = new List<string> { engineRoot };
        if (Directory.Exists(modulesRoot))
            searchDirs.Add(modulesRoot);

        foreach (var dir in searchDirs)
        {
            foreach (var filePath in Directory.EnumerateFiles(dir, "*.lua", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(filePath);
                if (!content.Contains("class.make") && !content.Contains("class.inherit"))
                    continue;

                var info = LuaParser.Parse(content, filePath);
                if (string.IsNullOrEmpty(info.Name))
                    continue;

                Classes.TryAdd(info.Name, info);
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
}
