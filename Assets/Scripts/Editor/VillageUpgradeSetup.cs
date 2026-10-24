using UnityEditor;
using UnityEngine;

public static class VillageUpgradeSetup
{
    private const string ParentFolder = "Assets//Data";
    private const string Folder = "Assets/Data/Village";

    [MenuItem("TD/Create Default Village Upgrades")]
    private static void CreateAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder(ParentFolder, "Village");
        }

        // Population: the biggest numbers, because everything needs it
        Create("Settlement", "Settlement", ResourceType.Population,
            Level(15, (ResourceType.Gold, 60), (ResourceType.Wood, 30)),
            Level(20, (ResourceType.Gold, 100), (ResourceType.Wood, 50), (ResourceType.Rocks, 30)),
            Level(30, (ResourceType.Gold, 160), (ResourceType.Wood, 80), (ResourceType.Rocks, 50)));

        // Wood: the easiest to collect, so the cheapest upgrades
        Create("Lumber", "Lumber", ResourceType.Wood,
            Level(12, (ResourceType.Gold, 40)),
            Level(18, (ResourceType.Gold, 70), (ResourceType.Wood, 20)),
            Level(25, (ResourceType.Gold, 110), (ResourceType.Wood, 40), (ResourceType.Rocks, 20)));

        // Rocks: medium
        Create("Mine", "Mine", ResourceType.Rocks,
            Level(8, (ResourceType.Gold, 70), (ResourceType.Wood, 40)),
            Level(12, (ResourceType.Gold, 110), (ResourceType.Wood, 60), (ResourceType.Rocks, 20)),
            Level(16, (ResourceType.Gold, 170), (ResourceType.Wood, 90), (ResourceType.Rocks, 40)));

        // Steel: rare, tiny gains, expensive
        Create("Forge", "Forge", ResourceType.Steel,
            Level(1, (ResourceType.Gold, 120), (ResourceType.Wood, 50), (ResourceType.Rocks, 40)),
            Level(2, (ResourceType.Gold, 180), (ResourceType.Wood, 80), (ResourceType.Rocks, 70)),
            Level(3, (ResourceType.Gold, 260), (ResourceType.Wood, 110), (ResourceType.Rocks, 100)));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Village upgrades created in " + Folder);
    }

    private static UpgradeLevel Level(int productionIncrease, params (ResourceType type, int amount)[] cost)
    {
        UpgradeLevel level = new UpgradeLevel { productionIncrease = productionIncrease };

        foreach (var c in cost)
        {
            level.cost.Add(new ResourceAmount { type = c.type, amount = c.amount });
        }

        return level;
    }

    private static void Create(string fileName, string displayName, ResourceType resource, params UpgradeLevel[] levels)
    {
        string path = $"{Folder}/{fileName}.asset";

        if (AssetDatabase.LoadAssetAtPath<VillageUpgradeData>(path) != null)
        {
            Debug.Log($"{fileName} already exists, skipping.");
            return;
        }

        VillageUpgradeData data = ScriptableObject.CreateInstance<VillageUpgradeData>();
        data.displayName = displayName;
        data.resource = resource;
        data.levels.AddRange(levels);

        AssetDatabase.CreateAsset(data, path);
    }
}