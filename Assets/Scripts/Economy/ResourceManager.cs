using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private WaveManager waveManager;

    [Header("Starting Resources")]
    [SerializeField] private List<ResourceAmount> startingResources = new List<ResourceAmount>();

    [Header("Income (paid when a wave is completed)")]
    [Tooltip("Base income. Mines, settlement, etc. will raise this later.")]
    [SerializeField] private List<ResourceAmount> baseProductionPerWave = new List<ResourceAmount>();
    [SerializeField] private List<ResourceAmount> waveCompletionBonus = new List<ResourceAmount>();
    [Tooltip("Extra bonus if the village is at 100% condition")]
    [SerializeField] private List<ResourceAmount> perfectVillageBonus = new List<ResourceAmount>();

    [Header("Debug")]
    [SerializeField] private bool showDebugHUD = true;
    [SerializeField] private BuildingData debugBuilding;

    public event Action<ResourceType, int> ResourceChanged; // (type, new total)

    private int[] amounts;
    private int[] production;
    private GUIStyle hudStyle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        int typeCount = Enum.GetNames(typeof(ResourceType)).Length;
        amounts = new int[typeCount];
        production = new int[typeCount];

        foreach (ResourceAmount r in startingResources) amounts[(int)r.type] += r.amount;
        foreach (ResourceAmount r in baseProductionPerWave) production[(int)r.type] += r.amount;
    }

    private void Start()
    {
        if (waveManager == null)
        {
            Debug.LogWarning("ResourceManager: no WaveManager assigned, so no kill gold or wave income.", this);
            return;
        }

        waveManager.EnemyKilled += HandleEnemyKilled;
        waveManager.WaveCompleted += HandleWaveCompleted;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (waveManager != null)
        {
            waveManager.EnemyKilled -= HandleEnemyKilled;
            waveManager.WaveCompleted -= HandleWaveCompleted;
        }
    }

    // ---------- Public API (towers, UI and upgrades will use these) ----------

    public int Get(ResourceType type) => amounts[(int)type];

    public int GetProduction(ResourceType type) => production[(int)type];

    public void Add(ResourceType type, int amount)
    {
        if (amount <= 0) return;

        amounts[(int)type] += amount;
        ResourceChanged?.Invoke(type, amounts[(int)type]);
    }

    // Called by village upgrades later (e.g. upgrading the mine raises Rocks income)
    public void AddProduction(ResourceType type, int amount)
    {
        production[(int)type] += amount;
    }

    public bool CanAfford(IReadOnlyList<ResourceAmount> cost)
    {
        int[] needed = new int[amounts.Length];
        foreach (ResourceAmount c in cost) needed[(int)c.type] += c.amount;

        for (int i = 0; i < amounts.Length; i++)
        {
            if (amounts[i] < needed[i]) return false;
        }

        return true;
    }

    // Spends everything or nothing. Returns false (and spends nothing) if the player can't afford it.
    public bool TrySpend(IReadOnlyList<ResourceAmount> cost)
    {
        if (!CanAfford(cost)) return false;

        foreach (ResourceAmount c in cost)
        {
            if (c.amount <= 0) continue;

            amounts[(int)c.type] -= c.amount;
            ResourceChanged?.Invoke(c.type, amounts[(int)c.type]);
        }

        return true;
    }

    // Gives back a percentage of a cost (used when selling buildings)
    public void Refund(IReadOnlyList<ResourceAmount> cost, float percent)
    {
        foreach (ResourceAmount c in cost)
        {
            Add(c.type, Mathf.FloorToInt(c.amount * percent));
        }
    }

    // ---------- Wave events ----------

    private void HandleEnemyKilled(Enemy enemy)
    {
        Add(ResourceType.Gold, enemy.GoldReward);
    }

    private void HandleWaveCompleted(int waveNumber, bool villageIsPerfect)
    {
        for (int i = 0; i < production.Length; i++)
        {
            Add((ResourceType)i, production[i]);
        }

        Grant(waveCompletionBonus);

        if (villageIsPerfect)
        {
            Grant(perfectVillageBonus);
        }

        Debug.Log($"Wave {waveNumber} income paid. Perfect village: {villageIsPerfect}");
    }

    private void Grant(List<ResourceAmount> list)
    {
        foreach (ResourceAmount r in list) Add(r.type, r.amount);
    }

    // ---------- TEMPORARY debug tools ----------

    [ContextMenu("Test: Add 100 of every resource")]
    private void TestAddAll()
    {
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType))) Add(type, 100);
    }

    [ContextMenu("Test: Buy Debug Building")]
    private void TestBuy()
    {
        if (debugBuilding == null)
        {
            Debug.LogWarning("Assign a Debug Building first.");
            return;
        }

        bool bought = TrySpend(debugBuilding.Cost);
        Debug.Log(bought ? $"Bought {debugBuilding.displayName}" : $"Can't afford {debugBuilding.displayName}");
    }

    private void OnGUI()
    {
        if (!showDebugHUD || amounts == null) return;

        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            hudStyle.normal.textColor = Color.yellow;
        }

        string text = "";
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            text += $"{type}: {Get(type)}    ";
        }

        GUI.Label(new Rect(15, 95, 1200, 30), text, hudStyle);
    }
}