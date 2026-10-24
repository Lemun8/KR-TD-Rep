using System.Collections.Generic;

public static class ResourceFormat
{
    public static string Abbreviate(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Gold: return "G";
            case ResourceType.Wood: return "W";
            case ResourceType.Rocks: return "R";
            case ResourceType.Steel: return "S";
            case ResourceType.Population: return "P";
            default: return "";
        }
    }

    // Example output: "70 G  40 W  20 R"
    public static string CostText(IReadOnlyList<ResourceAmount> cost)
    {
        List<string> parts = new List<string>();

        foreach (ResourceAmount c in cost)
        {
            if (c.amount <= 0) continue;
            parts.Add($"{c.amount} {Abbreviate(c.type)}");
        }

        return string.Join("  ", parts);
    }
}