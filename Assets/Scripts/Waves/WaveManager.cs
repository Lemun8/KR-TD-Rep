using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawner spawner;

    [Header("Loop Settings")]
    [SerializeField] private float prepDuration = 20f;
    [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();

    public GamePhase Phase { get; private set; }
    public int CurrentWave { get; private set; }          // 1-based, 0 before the game starts
    public int TotalWaves => waves.Count;
    public float PrepTimeRemaining { get; private set; }
    public float PrepDuration => prepDuration;
    public int EnemiesAlive => enemiesAlive;
    public int EnemiesRemaining => enemiesAlive + enemiesToSpawn;   // alive + still to spawn this wave

    public event Action<GamePhase> PhaseChanged;
    public event Action<int> WaveStarted;                 // wave number
    public event Action<int, bool> WaveCompleted;         // wave number, village at 100%?
    public event Action<Enemy> EnemyKilled;               // the resource system uses this for gold
    public event Action GameWon;
    public event Action GameLost;

    private int enemiesAlive;
    private int enemiesToSpawn;
    private int groupsSpawning;
    private bool skipPrep;
    private Coroutine loop;

    private void Start()
    {
        if (spawner == null || waves.Count == 0)
        {
            Debug.LogError("WaveManager: assign the spawner and add at least one wave.", this);
            return;
        }

        if (VillageHealth.Instance != null)
        {
            VillageHealth.Instance.Destroyed += HandleVillageDestroyed;
        }
        else
        {
            Debug.LogWarning("WaveManager: no VillageHealth in the scene, so there is no lose condition.");
        }

        loop = StartCoroutine(GameLoop());
    }

    private void OnDestroy()
    {
        if (VillageHealth.Instance != null)
        {
            VillageHealth.Instance.Destroyed -= HandleVillageDestroyed;
        }
    }

    // The player calls the wave early from the HUD button
    public void StartWaveNow()
    {
        if (Phase == GamePhase.Preparing) skipPrep = true;
    }

    private IEnumerator GameLoop()
    {
        for (int i = 0; i < waves.Count; i++)
        {
            CurrentWave = i + 1;

            // ---- PREP PHASE (build time) ----
            SetPhase(GamePhase.Preparing);
            PrepTimeRemaining = prepDuration;
            skipPrep = false;

            while (PrepTimeRemaining > 0f && !skipPrep)
            {
                PrepTimeRemaining -= Time.deltaTime;
                yield return null;
            }

            // ---- WAVE PHASE (threat) ----
            SetPhase(GamePhase.WaveActive);
            WaveStarted?.Invoke(CurrentWave);

            StartWave(waves[i]);

            // The wave ends when every group has finished spawning AND every enemy is gone
            while (groupsSpawning > 0 || enemiesAlive > 0)
            {
                yield return null;
            }

            // ---- WAVE COMPLETE ----
            bool perfect = VillageHealth.Instance != null && VillageHealth.Instance.IsPerfectCondition;
            WaveCompleted?.Invoke(CurrentWave, perfect);
        }

        SetPhase(GamePhase.Won);
        GameWon?.Invoke();
    }

    // Starts every group of the wave at the same time (each one waits for its own Start Delay)
    private void StartWave(WaveDefinition wave)
    {
        if (wave.groups.Count == 0)
        {
            Debug.LogWarning($"WaveManager: wave {CurrentWave} has no spawn groups.", this);
            return;
        }

        enemiesToSpawn = 0;
        foreach (SpawnGroup group in wave.groups) enemiesToSpawn += group.count;

        foreach (SpawnGroup group in wave.groups)
        {
            groupsSpawning++;
            StartCoroutine(SpawnGroupRoutine(group));
        }
    }

    private IEnumerator SpawnGroupRoutine(SpawnGroup group)
    {
        try
        {
            if (group.startDelay > 0f)
            {
                yield return new WaitForSeconds(group.startDelay);
            }

            int spawned = 0;

            while (spawned < group.count)
            {
                // One swarm: its enemies come out close together
                int inThisSwarm = Mathf.Min(group.swarmSize, group.count - spawned);

                for (int i = 0; i < inThisSwarm; i++)
                {
                    SpawnOne(group);
                    spawned++;

                    if (i < inThisSwarm - 1)
                    {
                        yield return new WaitForSeconds(group.swarmSpacing);
                    }
                }

                // Pause before the next swarm (not after the last one)
                if (spawned < group.count)
                {
                    yield return new WaitForSeconds(group.timeBetweenSwarms);
                }
            }
        }
        finally
        {
            groupsSpawning--;
        }
    }

    private void SpawnOne(SpawnGroup group)
    {
        Enemy enemy = spawner.Spawn(group.enemyPrefab, group.healthMultiplier);
        enemiesToSpawn--;

        if (enemy == null) return;

        enemiesAlive++;
        enemy.Killed += HandleEnemyKilled;
        enemy.ReachedVillage += HandleEnemyReachedVillage;
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        ForgetEnemy(enemy);
        EnemyKilled?.Invoke(enemy);
    }

    private void HandleEnemyReachedVillage(Enemy enemy)
    {
        ForgetEnemy(enemy);
    }

    private void ForgetEnemy(Enemy enemy)
    {
        enemy.Killed -= HandleEnemyKilled;
        enemy.ReachedVillage -= HandleEnemyReachedVillage;
        enemiesAlive--;
    }

    private void HandleVillageDestroyed()
    {
        if (Phase == GamePhase.Won || Phase == GamePhase.Lost) return;

        // Stops the game loop AND every spawn group, so nothing new appears after the defeat
        StopAllCoroutines();
        SetPhase(GamePhase.Lost);
        GameLost?.Invoke();
    }

    private void SetPhase(GamePhase newPhase)
    {
        Phase = newPhase;
        Debug.Log($"Phase: {newPhase} (Wave {CurrentWave}/{TotalWaves})");
        PhaseChanged?.Invoke(newPhase);
    }
}