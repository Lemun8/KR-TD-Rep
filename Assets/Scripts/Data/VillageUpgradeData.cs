using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UpgradeLevel
{
    public List<ResourceAmount> cost = new List<ResourceAmount>();
    [Min(0)] public int productionIncrease;   // extra income per wave once bought
}

[CreateAssetMenu(fileName = "NewVillageUpgrade", menuName = "TD/Village Upgrade Data")]
public class VillageUpgradeData : ScriptableObject
{
    public string displayName;
    public ResourceType resource;                              // which resource this upgrade produces
    public List<UpgradeLevel> levels = new List<UpgradeLevel>();

    public int MaxLevel => levels.Count;

    public UpgradeLevel GetLevel(int index)
    {
        return levels[index];
    }
}