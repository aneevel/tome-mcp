using System.Text.RegularExpressions;

namespace TomeMcp;

public static partial class DataParser
{
    [GeneratedRegex(@"EFF_(\w+)", RegexOptions.Compiled)]
    private static partial Regex EffectRefRegex();

    [GeneratedRegex(@"DamageType\.([A-Z_]+)", RegexOptions.Compiled)]
    private static partial Regex DamageTypeRefRegex();

    [GeneratedRegex(@"name\s*=\s*""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex NameFieldRegex();

    [GeneratedRegex(@"name\s*=\s*_t\(?\s*""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex NameTranslatedFieldRegex();

    [GeneratedRegex(@"name\s*=\s*_t""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex NameTranslatedShortRegex();

    [GeneratedRegex(@"type\s*=\s*\{\s*""([^""]+)""\s*,\s*(\d+)\s*\}", RegexOptions.Compiled)]
    private static partial Regex TalentTypeFieldRegex();

    [GeneratedRegex(@"mode\s*=\s*""(\w+)""", RegexOptions.Compiled)]
    private static partial Regex ModeFieldRegex();

    [GeneratedRegex(@"cooldown\s*=\s*(\d+)", RegexOptions.Compiled)]
    private static partial Regex CooldownFieldRegex();

    [GeneratedRegex(@"(hate|psi|mana|stamina|vim|positive|negative|paradox|feedback|soul)\s*=\s*(\d+)", RegexOptions.Compiled)]
    private static partial Regex ResourceFieldRegex();

    [GeneratedRegex(@"type\s*=\s*""(\w+)""", RegexOptions.Compiled)]
    private static partial Regex EffectTypeFieldRegex();

    [GeneratedRegex(@"subtype\s*=\s*\{\s*(\w+)\s*=\s*true", RegexOptions.Compiled)]
    private static partial Regex SubtypeFieldRegex();

    [GeneratedRegex(@"desc\s*=\s*_t""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex DescFieldRegex();

    [GeneratedRegex(@"text_color\s*=\s*""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex TextColorFieldRegex();

    [GeneratedRegex(@"type\s*=\s*""([A-Z_]+)""", RegexOptions.Compiled)]
    private static partial Regex DamageTypeIdFieldRegex();

    private static List<(int lineNumber, string body)> ExtractBlocks(string content, string prefix)
    {
        var blocks = new List<(int, string)>();
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            int startLine = i + 1;
            int depth = 0;
            var bodyLines = new List<string>();
            bool foundOpen = false;

            for (int j = i; j < lines.Length; j++)
            {
                var line = lines[j];
                bodyLines.Add(line);

                foreach (char c in line)
                {
                    if (c == '{') { depth++; foundOpen = true; }
                    else if (c == '}') depth--;
                }

                if (foundOpen && depth <= 0)
                    break;
            }

            blocks.Add((startLine, string.Join('\n', bodyLines)));
        }

        return blocks;
    }

    private static string? ExtractName(string body)
    {
        var m = NameFieldRegex().Match(body);
        if (m.Success) return m.Groups[1].Value;

        m = NameTranslatedFieldRegex().Match(body);
        if (m.Success) return m.Groups[1].Value;

        m = NameTranslatedShortRegex().Match(body);
        if (m.Success) return m.Groups[1].Value;

        return null;
    }

    private static string DeriveInternalId(string name, string prefix)
    {
        return prefix + name.ToUpperInvariant()
            .Replace(' ', '_')
            .Replace('-', '_')
            .Replace("'", "");
    }

    private static List<string> ExtractEffectRefs(string body)
    {
        var refs = new HashSet<string>();
        foreach (Match m in EffectRefRegex().Matches(body))
            refs.Add("EFF_" + m.Groups[1].Value);
        return refs.ToList();
    }

    private static List<string> ExtractDamageTypeRefs(string body)
    {
        var refs = new HashSet<string>();
        foreach (Match m in DamageTypeRefRegex().Matches(body))
            refs.Add(m.Groups[1].Value);
        return refs.ToList();
    }

    public static List<TalentDef> ParseTalents(string content, string filePath)
    {
        var results = new List<TalentDef>();

        foreach (var (lineNumber, body) in ExtractBlocks(content, "newTalent{"))
        {
            var name = ExtractName(body);
            if (name is null) continue;

            var talent = new TalentDef
            {
                Name = name,
                InternalId = DeriveInternalId(name, "T_"),
                FilePath = filePath,
                LineNumber = lineNumber,
            };

            var typeMatch = TalentTypeFieldRegex().Match(body);
            if (typeMatch.Success)
            {
                talent.TalentType = typeMatch.Groups[1].Value;
                if (int.TryParse(typeMatch.Groups[2].Value, out var tier))
                    talent.Tier = tier;
            }

            var modeMatch = ModeFieldRegex().Match(body);
            if (modeMatch.Success)
                talent.Mode = modeMatch.Groups[1].Value;
            else if (body.Contains("mode = \"passive\""))
                talent.Mode = "passive";
            else if (body.Contains("mode = \"sustained\""))
                talent.Mode = "sustained";

            var cdMatch = CooldownFieldRegex().Match(body);
            if (cdMatch.Success && int.TryParse(cdMatch.Groups[1].Value, out var cd))
                talent.Cooldown = cd;

            var resMatch = ResourceFieldRegex().Match(body);
            if (resMatch.Success)
            {
                talent.Resource = resMatch.Groups[1].Value;
                if (int.TryParse(resMatch.Groups[2].Value, out var cost))
                    talent.ResourceCost = cost;
            }

            talent.EffectsApplied = ExtractEffectRefs(body);
            talent.DamageTypesUsed = ExtractDamageTypeRefs(body);

            results.Add(talent);
        }

        return results;
    }

    public static List<EffectDef> ParseEffects(string content, string filePath)
    {
        var results = new List<EffectDef>();

        foreach (var (lineNumber, body) in ExtractBlocks(content, "newEffect{"))
        {
            var nameMatch = Regex.Match(body, @"name\s*=\s*""([^""]+)""");
            if (!nameMatch.Success) continue;

            var effectName = nameMatch.Groups[1].Value;
            var effect = new EffectDef
            {
                Name = effectName,
                EffectId = "EFF_" + effectName,
                FilePath = filePath,
                LineNumber = lineNumber,
            };

            var descMatch = DescFieldRegex().Match(body);
            if (descMatch.Success)
                effect.Desc = descMatch.Groups[1].Value;

            var typeMatch = EffectTypeFieldRegex().Match(body);
            if (typeMatch.Success)
                effect.EffectType = typeMatch.Groups[1].Value;

            var subtypeMatch = SubtypeFieldRegex().Match(body);
            if (subtypeMatch.Success)
                effect.Subtype = subtypeMatch.Groups[1].Value;

            effect.DamageTypesUsed = ExtractDamageTypeRefs(body);
            effect.EffectsApplied = ExtractEffectRefs(body);
            effect.EffectsApplied.Remove(effect.EffectId);

            results.Add(effect);
        }

        return results;
    }

    public static List<DamageTypeDef> ParseDamageTypes(string content, string filePath)
    {
        var results = new List<DamageTypeDef>();

        foreach (var (lineNumber, body) in ExtractBlocks(content, "newDamageType{"))
        {
            var name = ExtractName(body);
            if (name is null) continue;

            var dt = new DamageTypeDef
            {
                Name = name,
                FilePath = filePath,
                LineNumber = lineNumber,
            };

            var typeIdMatch = DamageTypeIdFieldRegex().Match(body);
            if (typeIdMatch.Success)
                dt.TypeId = typeIdMatch.Groups[1].Value;

            var colorMatch = TextColorFieldRegex().Match(body);
            if (colorMatch.Success)
                dt.TextColor = colorMatch.Groups[1].Value;

            if (!string.IsNullOrEmpty(dt.TypeId))
                results.Add(dt);
        }

        return results;
    }
}
