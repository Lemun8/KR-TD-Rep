using System;
using System.Collections.Generic;
using UnityEngine;

public class VillageUpgrades : MonoBehaviour
{
    [SerializeField] private List<VillageUpgradeData> upgrades = new List<VillageUpgradeData>();

    public int Count => upgrades.Count;
    public event Action<int> Upgraded;   // index of the upgrade that was bought

    private int[] levels;

    private void Awake()
    {
        levels = new int[upgrades.Count];
    }

    public VillageUpgradeData GetData(int index) => upgrades[index];
    public int GetLevel(int index) => levels[index];
    public bool IsMaxed(int index) => levels[index] >= upgrades[index].MaxLevel;

    public bool CanUpgrade(int index)
    {
        if (IsMaxed(index) || ResourceManager.Instance == null) return false;

        return ResourceManager.Instance.CanAfford(upgrades[index].GetLevel(levels[index]).cost);
    }

    public bool TryUpgrade(int index)
    {
        if (!CanUpgrade(index)) return false;

        VillageUpgradeData data = upgrades[index];
        UpgradeLevel next = data.GetLevel(levels[index]);

        if (!ResourceManager.Instance.TrySpend(next.cost)) return false;

        // From now on this resource pays more at the end of every wave
        ResourceManager.Instance.AddProduction(data.resource, next.productionIncrease);

        levels[index]++;
        Upgraded?.Invoke(index);
        return true;
    }
}