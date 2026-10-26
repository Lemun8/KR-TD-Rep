using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private WaveManager waveManager;

    [Header("Starting Resources")]
    [SerializeField] private List<ResourceAmount> startingResources = new List<ResourceAmount>();

    [Header("Income over time")]
    [Tooltip("How often income rolls in, in seconds.")]
    [Min(0.5f)]
    [SerializeField] private float incomeInterval = 5f;
    [Tooltip("Base income PER MINUTE. It rolls in a little at a time, every Income Interval. Village upgrades raise it.")]
    [FormerlySerializedAs("baseProductionPerWave")]
    [SerializeField] private List<ResourceAmount> baseProductionPerMinute = new List<ResourceAmount>();

    [Header("Wave rewards (paid when a wave is completed)")]
    [SerializeField] private List<ResourceAmount> waveCompletionBonus = new List<ResourceAmount>();
    [Tooltip("Extra bonus if the village is at 100% condition")]
    [SerializeField] private List<ResourceAmount> perfectVillageBonus = new List<ResourceAmount>();

    [Header("Debug")]
    [SerializeField] private BuildingData debugBuilding;

    public event Action<ResourceType, int> ResourceChanged; // (type, new total)

    private int[] amounts;
    private int[] production;          // per minute
    private float[] incomeRemainder;   // fractions that did not make a whole unit yet
    private float incomeTimer;

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
        incomeRemainder = new float[typeCount];

        foreach (ResourceAmount r in startingResources) amounts[(int)r.type] += r.amount;
        foreach (ResourceAmount r in baseProductionPerMinute) production[(int)r.type] += r.amount;
    }

    private void Start()
    {
        if (waveManager == null)
        {
            Debug.LogWarning("ResourceManager: no WaveManager assigned, so no kill gold or wave rewards.", this);
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

    private void Update()
    {
        if (amounts == null) return;

        // No more income once the game is over
        if (waveManager != null && (waveManager.Phase == GamePhase.Won || waveManager.Phase == GamePhase.Lost)) return;

        incomeTimer += Time.deltaTime;

        while (incomeTimer >= incomeInterval)
        {
            incomeTimer -= incomeInterval;
            PayIncome();
        }
    }

    // ---------- Public API (towers, UI and upgrades use these) ----------

    public int Get(ResourceType type) => amounts[(int)type];

    // Income per MINUTE
    public int GetProduction(ResourceType type) => production[(int)type];

    public void Add(ResourceType type, int amount)
    {
        if (amount <= 0) return;

        amounts[(int)type] += amount;
        ResourceChanged?.Invoke(type, amounts[(int)type]);
    }

    // Called by village upgrades (e.g. upgrading the mine raises Rocks income per minute)
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

    // ---------- Income ----------

    // One tick: pays this slice of the per-minute income, keeping any leftover fraction for next time
    private void PayIncome()
    {
        float fractionOfMinute = incomeInterval / 60f;

        for (int i = 0; i < production.Length; i++)
        {
            incomeRemainder[i] += production[i] * fractionOfMinute;

            int whole = Mathf.FloorToInt(incomeRemainder[i]);
            if (whole <= 0) continue;

            incomeRemainder[i] -= whole;
            Add((ResourceType)i, whole);
        }
    }

    // ---------- Wave events ----------

    private void HandleEnemyKilled(Enemy enemy)
    {
        Add(ResourceType.Gold, enemy.GoldReward);
    }

    private void HandleWaveCompleted(int waveNumber, bool villageIsPerfect)
    {
        Grant(waveCompletionBonus);

        if (villageIsPerfect)
        {
            Grant(perfectVillageBonus);
        }

        Debug.Log($"Wave {waveNumber} rewards paid. Perfect village: {villageIsPerfect}");
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
}