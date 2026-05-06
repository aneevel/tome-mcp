using System.Text.Json.Serialization;

namespace TomeMcp;

public class TalentDef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("internalId")]
    public string InternalId { get; set; } = "";

    [JsonPropertyName("talentType")]
    public string TalentType { get; set; } = "";

    [JsonPropertyName("tier")]
    public int Tier { get; set; }

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "active";

    [JsonPropertyName("cooldown")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Cooldown { get; set; }

    [JsonPropertyName("resource")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Resource { get; set; }

    [JsonPropertyName("resourceCost")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResourceCost { get; set; }

    [JsonPropertyName("effectsApplied")]
    public List<string> EffectsApplied { get; set; } = new();

    [JsonPropertyName("damageTypesUsed")]
    public List<string> DamageTypesUsed { get; set; } = new();

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = "";

    [JsonPropertyName("lineNumber")]
    public int LineNumber { get; set; }
}

public class EffectDef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("effectId")]
    public string EffectId { get; set; } = "";

    [JsonPropertyName("desc")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Desc { get; set; }

    [JsonPropertyName("effectType")]
    public string EffectType { get; set; } = "";

    [JsonPropertyName("subtype")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Subtype { get; set; }

    [JsonPropertyName("damageTypesUsed")]
    public List<string> DamageTypesUsed { get; set; } = new();

    [JsonPropertyName("effectsApplied")]
    public List<string> EffectsApplied { get; set; } = new();

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = "";

    [JsonPropertyName("lineNumber")]
    public int LineNumber { get; set; }
}

public class DamageTypeDef
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("typeId")]
    public string TypeId { get; set; } = "";

    [JsonPropertyName("textColor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TextColor { get; set; }

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; } = "";

    [JsonPropertyName("lineNumber")]
    public int LineNumber { get; set; }
}

public class DataIndex
{
    public Dictionary<string, TalentDef> Talents { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, EffectDef> Effects { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, DamageTypeDef> DamageTypes { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, List<string>> EffectToTalents { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<string>> DamageTypeToTalents { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void Build(string modulesDataRoot)
    {
        var talentsDir = Path.Combine(modulesDataRoot, "talents");
        if (Directory.Exists(talentsDir))
        {
            foreach (var file in Directory.EnumerateFiles(talentsDir, "*.lua", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(file);
                var talents = DataParser.ParseTalents(content, file);
                foreach (var t in talents)
                    Talents.TryAdd(t.Name, t);
            }
        }

        var effectsDir = Path.Combine(modulesDataRoot, "timed_effects");
        if (Directory.Exists(effectsDir))
        {
            foreach (var file in Directory.EnumerateFiles(effectsDir, "*.lua", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(file);
                var effects = DataParser.ParseEffects(content, file);
                foreach (var e in effects)
                    Effects.TryAdd(e.EffectId, e);
            }
        }

        var damageTypesFile = Path.Combine(modulesDataRoot, "damage_types.lua");
        if (File.Exists(damageTypesFile))
        {
            var content = File.ReadAllText(damageTypesFile);
            var types = DataParser.ParseDamageTypes(content, damageTypesFile);
            foreach (var dt in types)
                DamageTypes.TryAdd(dt.TypeId, dt);
        }

        BuildCrossReferences();
    }

    private void BuildCrossReferences()
    {
        foreach (var talent in Talents.Values)
        {
            foreach (var eff in talent.EffectsApplied)
            {
                if (!EffectToTalents.TryGetValue(eff, out var list))
                {
                    list = new List<string>();
                    EffectToTalents[eff] = list;
                }
                if (!list.Contains(talent.Name))
                    list.Add(talent.Name);
            }

            foreach (var dt in talent.DamageTypesUsed)
            {
                if (!DamageTypeToTalents.TryGetValue(dt, out var list))
                {
                    list = new List<string>();
                    DamageTypeToTalents[dt] = list;
                }
                if (!list.Contains(talent.Name))
                    list.Add(talent.Name);
            }
        }
    }
}
