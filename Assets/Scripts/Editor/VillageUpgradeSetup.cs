using UnityEditor;
using UnityEngine;

public static class VillageUpgradeSetup
{
    private const string ParentFolder = "Assets/Data";
    private const string Folder = "Assets/Data/Village";

    // Creates the four village upgrades, or RESETS them to these values if they already exist.
    // Every upgrade costs gold only. Production numbers are per MINUTE.
    [MenuItem("TD/Create Default Village Upgrades")]
    private static void CreateAll()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder(ParentFolder, "Village");
        }

        // Population: the biggest numbers, because everything needs it
        Create("Settlement", "Settlement", ResourceType.Population,
            Level(15, 80), Level(20, 140), Level(30, 220));

        // Wood: the easiest to collect, so the cheapest upgrades
        Create("Lumber", "Lumber", ResourceType.Wood,
            Level(12, 50), Level(18, 80), Level(25, 130));

        // Rocks: medium
        Create("Mine", "Mine", ResourceType.Rocks,
            Level(8, 90), Level(12, 150), Level(16, 230));

        // Steel: rare, small gains, the most expensive
        Create("Forge", "Forge", ResourceType.Steel,
            Level(2, 150), Level(3, 230), Level(4, 330));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Village upgrades set to gold-only costs in " + Folder);
    }

    private static UpgradeLevel Level(int productionIncrease, int goldCost)
    {
        UpgradeLevel level = new UpgradeLevel { productionIncrease = productionIncrease };
        level.cost.Add(new ResourceAmount { type = ResourceType.Gold, amount = goldCost });
        return level;
    }

    private static void Create(string fileName, string displayName, ResourceType resource, params UpgradeLevel[] levels)
    {
        string path = $"{Folder}/{fileName}.asset";
        VillageUpgradeData data = AssetDatabase.LoadAssetAtPath<VillageUpgradeData>(path);

        if (data == null)
        {
            data = ScriptableObject.CreateInstance<VillageUpgradeData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.displayName = displayName;
        data.resource = resource;
        data.levels.Clear();
        data.levels.AddRange(levels);

        EditorUtility.SetDirty(data);
    }
}