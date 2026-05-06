using System.Text.Json;

namespace TomeMcp;

public class RequestHandler
{
    private readonly string _engineRoot;
    private readonly ClassIndex _classIndex;
    private readonly DataIndex _dataIndex;

    public RequestHandler(string engineRoot, ClassIndex classIndex, DataIndex dataIndex)
    {
        _engineRoot = engineRoot;
        _classIndex = classIndex;
        _dataIndex = dataIndex;
    }

    public void Handle(Invocation invocation)
    {
        switch (invocation.Method)
        {
            case MethodType.ToolsList:
                Response.FromMessage("OK").Send();
                SendToolsList();
                break;

            case MethodType.ToolsCall:
                Response.FromMessage("OK").Send();
                HandleToolsCall(invocation);
                break;
        }
    }

    private void HandleToolsCall(Invocation invocation)
    {
        switch (invocation.Params?.Name)
        {
            case "ping":
                Response.FromContent(new object[] { new { type = "text", text = "pong" } }).Send();
                break;

            case "read_class":
                HandleReadClass(invocation);
                break;

            case "list_classes":
                HandleListClasses(invocation);
                break;

            case "class_hierarchy":
                HandleClassHierarchy(invocation);
                break;

            case "search_code":
                HandleSearchCode(invocation);
                break;

            case "read_talent":
                HandleReadTalent(invocation);
                break;

            case "read_effect":
                HandleReadEffect(invocation);
                break;

            case "query_data":
                HandleQueryData(invocation);
                break;

            default:
                Response.FromMessage("ERROR: Malformed input.").Send();
                SendToolsList();
                break;
        }
    }

    private void HandleReadClass(Invocation invocation)
    {
        var className = invocation.Params?.ClassName;
        if (string.IsNullOrWhiteSpace(className))
        {
            Response.FromMessage("ERROR: Missing class_name parameter.").Send();
            return;
        }

        if (_classIndex.Classes.TryGetValue(className, out var cached))
        {
            var json = JsonSerializer.Serialize(cached, JsonOptions);
            Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
            return;
        }

        var filePath = ResolveClassPath(className);
        if (!File.Exists(filePath))
        {
            Response.FromMessage($"ERROR: Class not found: {className}").Send();
            return;
        }

        var content = File.ReadAllText(filePath);
        var classInfo = LuaParser.Parse(content, filePath);
        var json2 = JsonSerializer.Serialize(classInfo, JsonOptions);

        Response.FromContent(new object[] { new { type = "text", text = json2 } }).Send();
    }

    private void HandleListClasses(Invocation invocation)
    {
        var filter = invocation.Params?.Filter;

        var classes = _classIndex.Classes.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            classes = classes.Where(c =>
                c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        var result = classes
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                name = c.Name,
                baseClasses = c.BaseClasses,
                methodCount = c.Methods.Count,
                isRootClass = c.IsRootClass,
            })
            .ToArray<object>();

        var json = JsonSerializer.Serialize(result, JsonOptions);
        Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
    }

    private void HandleClassHierarchy(Invocation invocation)
    {
        var className = invocation.Params?.ClassName;
        if (string.IsNullOrWhiteSpace(className))
        {
            Response.FromMessage("ERROR: Missing class_name parameter.").Send();
            return;
        }

        if (!_classIndex.Classes.ContainsKey(className))
        {
            Response.FromMessage($"ERROR: Class not found in index: {className}").Send();
            return;
        }

        var ancestors = _classIndex.GetAncestors(className);
        var descendants = _classIndex.GetDescendants(className);

        var result = new
        {
            @class = className,
            ancestors,
            descendants,
        };

        var json = JsonSerializer.Serialize(result, JsonOptions);
        Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
    }

    private void HandleSearchCode(Invocation invocation)
    {
        var pattern = invocation.Params?.Pattern;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            Response.FromMessage("ERROR: Missing pattern parameter.").Send();
            return;
        }

        var caseSensitive = invocation.Params?.CaseSensitive ?? false;
        var maxResults = invocation.Params?.MaxResults ?? 50;
        var contextLines = invocation.Params?.ContextLines ?? 2;
        var pathFilter = invocation.Params?.PathFilter;

        maxResults = Math.Clamp(maxResults, 1, 200);
        contextLines = Math.Clamp(contextLines, 0, 10);

        try
        {
            var matches = _classIndex.Search(pattern, caseSensitive, maxResults, contextLines, pathFilter);

            var result = new
            {
                pattern,
                caseSensitive,
                totalMatches = matches.Count,
                capped = matches.Count >= maxResults,
                matches,
            };

            var json = JsonSerializer.Serialize(result, JsonOptions);
            Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
        }
        catch (System.Text.RegularExpressions.RegexParseException ex)
        {
            Response.FromMessage($"ERROR: Invalid regex pattern: {ex.Message}").Send();
        }
    }

    private void HandleReadTalent(Invocation invocation)
    {
        var talentName = invocation.Params?.TalentName;
        if (string.IsNullOrWhiteSpace(talentName))
        {
            Response.FromMessage("ERROR: Missing talent_name parameter.").Send();
            return;
        }

        if (_dataIndex.Talents.TryGetValue(talentName, out var talent))
        {
            var appliedBy = new List<string>();
            foreach (var eff in talent.EffectsApplied)
            {
                if (_dataIndex.Effects.TryGetValue(eff, out var effDef))
                    appliedBy.Add($"{eff} ({effDef.Desc ?? effDef.Name}, {effDef.EffectType})");
            }

            var result = new
            {
                talent,
                effectDetails = appliedBy,
            };

            var json = JsonSerializer.Serialize(result, JsonOptions);
            Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
            return;
        }

        var match = _dataIndex.Talents.Values
            .FirstOrDefault(t => t.Name.Contains(talentName, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            var json = JsonSerializer.Serialize(match, JsonOptions);
            Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
            return;
        }

        Response.FromMessage($"ERROR: Talent not found: {talentName}").Send();
    }

    private void HandleReadEffect(Invocation invocation)
    {
        var effectName = invocation.Params?.EffectName;
        if (string.IsNullOrWhiteSpace(effectName))
        {
            Response.FromMessage("ERROR: Missing effect_name parameter.").Send();
            return;
        }

        var key = effectName.StartsWith("EFF_", StringComparison.OrdinalIgnoreCase)
            ? effectName
            : "EFF_" + effectName;

        if (!_dataIndex.Effects.TryGetValue(key, out var effect))
        {
            effect = _dataIndex.Effects.Values
                .FirstOrDefault(e => e.Name.Equals(effectName, StringComparison.OrdinalIgnoreCase)
                    || e.Desc?.Equals(effectName, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (effect is null)
        {
            Response.FromMessage($"ERROR: Effect not found: {effectName}").Send();
            return;
        }

        _dataIndex.EffectToTalents.TryGetValue(effect.EffectId, out var appliedByTalents);

        var result = new
        {
            effect,
            appliedByTalents = appliedByTalents ?? new List<string>(),
        };

        var json = JsonSerializer.Serialize(result, JsonOptions);
        Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
    }

    private void HandleQueryData(Invocation invocation)
    {
        var entityType = invocation.Params?.EntityType;
        if (string.IsNullOrWhiteSpace(entityType))
        {
            Response.FromMessage("ERROR: Missing entity_type parameter (talent, effect, or damage_type).").Send();
            return;
        }

        var filter = invocation.Params?.Filter;
        var talentType = invocation.Params?.TalentType;
        var damageType = invocation.Params?.DamageType;
        var effectName = invocation.Params?.EffectName;

        switch (entityType.ToLowerInvariant())
        {
            case "talent":
            {
                var talents = _dataIndex.Talents.Values.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(filter))
                    talents = talents.Where(t => t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(talentType))
                    talents = talents.Where(t => t.TalentType.Contains(talentType, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(damageType))
                    talents = talents.Where(t => t.DamageTypesUsed.Any(d => d.Equals(damageType, StringComparison.OrdinalIgnoreCase)));
                if (!string.IsNullOrWhiteSpace(effectName))
                {
                    var effKey = effectName.StartsWith("EFF_", StringComparison.OrdinalIgnoreCase) ? effectName : "EFF_" + effectName;
                    talents = talents.Where(t => t.EffectsApplied.Any(e => e.Equals(effKey, StringComparison.OrdinalIgnoreCase)));
                }

                var result = talents.OrderBy(t => t.TalentType).ThenBy(t => t.Tier)
                    .Select(t => new { t.Name, t.TalentType, t.Tier, t.Mode, t.DamageTypesUsed, t.EffectsApplied })
                    .ToArray();

                var json = JsonSerializer.Serialize(new { entityType, totalResults = result.Length, results = result }, JsonOptions);
                Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
                break;
            }
            case "effect":
            {
                var effects = _dataIndex.Effects.Values.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(filter))
                    effects = effects.Where(e => e.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                        || (e.Desc?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
                if (!string.IsNullOrWhiteSpace(damageType))
                    effects = effects.Where(e => e.DamageTypesUsed.Any(d => d.Equals(damageType, StringComparison.OrdinalIgnoreCase)));
                if (!string.IsNullOrWhiteSpace(talentType))
                {
                    var talentEffects = _dataIndex.Talents.Values
                        .Where(t => t.TalentType.Contains(talentType, StringComparison.OrdinalIgnoreCase))
                        .SelectMany(t => t.EffectsApplied)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    effects = effects.Where(e => talentEffects.Contains(e.EffectId));
                }

                var result = effects.OrderBy(e => e.EffectType).ThenBy(e => e.Name)
                    .Select(e => new
                    {
                        e.EffectId, e.Desc, e.EffectType, e.Subtype, e.DamageTypesUsed,
                        appliedBy = _dataIndex.EffectToTalents.TryGetValue(e.EffectId, out var talents) ? talents : new List<string>(),
                    })
                    .ToArray();

                var json = JsonSerializer.Serialize(new { entityType, totalResults = result.Length, results = result }, JsonOptions);
                Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
                break;
            }
            case "damage_type":
            {
                var types = _dataIndex.DamageTypes.Values.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(filter))
                    types = types.Where(d => d.TypeId.Contains(filter, StringComparison.OrdinalIgnoreCase)
                        || d.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

                var result = types.OrderBy(d => d.Name)
                    .Select(d => new
                    {
                        d.TypeId, d.Name, d.TextColor,
                        usedByTalentCount = _dataIndex.DamageTypeToTalents.TryGetValue(d.TypeId, out var talents) ? talents.Count : 0,
                    })
                    .ToArray();

                var json = JsonSerializer.Serialize(new { entityType, totalResults = result.Length, results = result }, JsonOptions);
                Response.FromContent(new object[] { new { type = "text", text = json } }).Send();
                break;
            }
            default:
                Response.FromMessage("ERROR: entity_type must be 'talent', 'effect', or 'damage_type'.").Send();
                break;
        }
    }

    private string ResolveClassPath(string className)
    {
        if (File.Exists(className))
            return className;

        var relativePath = className.Replace('.', '/') + ".lua";
        return Path.Combine(_engineRoot, relativePath);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

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
