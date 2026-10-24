using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "TD/Building Data")]
public class BuildingData : ScriptableObject
{
    public string displayName;
    public GameObject prefab;                                  // what gets spawned on the build spot
    [Range(0f, 1f)] public float sellRefundPercent = 0.6f;

    [SerializeField] private List<ResourceAmount> cost = new List<ResourceAmount>();

    public IReadOnlyList<ResourceAmount> Cost => cost;

    // Example output: "70 G  40 W  20 R  1 P"
    public string GetCostText(float multiplier = 1f)
    {
        List<string> parts = new List<string>();

        foreach (ResourceAmount c in cost)
        {
            int amount = Mathf.FloorToInt(c.amount * multiplier);
            if (amount <= 0) continue;

            parts.Add($"{amount} {Abbreviate(c.type)}");
        }

        return string.Join("  ", parts);
    }

    private static string Abbreviate(ResourceType type)
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
}