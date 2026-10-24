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

    [Header("Debug")]
    [SerializeField] private bool showDebugHUD = true;

    public GamePhase Phase { get; private set; }
    public int CurrentWave { get; private set; }          // 1-based, 0 before the game starts
    public int TotalWaves => waves.Count;
    public float PrepTimeRemaining { get; private set; }
    public int EnemiesAlive => enemiesAlive;

    public event Action<GamePhase> PhaseChanged;
    public event Action<int> WaveStarted;                 // wave number
    public event Action<int, bool> WaveCompleted;         // wave number, village at 100%?
    public event Action<Enemy> EnemyKilled;               // the resource system uses this for gold
    public event Action GameWon;
    public event Action GameLost;

    private int enemiesAlive;
    private bool skipPrep;
    private Coroutine loop;
    private GUIStyle hudStyle;

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

    // The player will be able to "call the wave early" from a UI button later
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

            yield return SpawnWave(waves[i]);

            while (enemiesAlive > 0)
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

    private IEnumerator SpawnWave(WaveDefinition wave)
    {
        for (int i = 0; i < wave.enemyCount; i++)
        {
            Enemy enemy = spawner.Spawn(wave.enemyPrefab, wave.healthMultiplier);

            if (enemy != null)
            {
                enemiesAlive++;
                enemy.Killed += HandleEnemyKilled;
                enemy.ReachedVillage += HandleEnemyReachedVillage;
            }

            if (i < wave.enemyCount - 1)
            {
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }
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

        if (loop != null) StopCoroutine(loop);
        SetPhase(GamePhase.Lost);
        GameLost?.Invoke();
    }

    private void SetPhase(GamePhase newPhase)
    {
        Phase = newPhase;
        Debug.Log($"Phase: {newPhase} (Wave {CurrentWave}/{TotalWaves})");
        PhaseChanged?.Invoke(newPhase);
    }

    // ---- TEMPORARY debug display, replaced by real UI in a later step ----
    private void OnGUI()
    {
        if (!showDebugHUD) return;

        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            hudStyle.normal.textColor = Color.white;
        }

        string text = Phase switch
        {
            GamePhase.Preparing => $"Wave {CurrentWave}/{TotalWaves} - Prepare: {Mathf.CeilToInt(PrepTimeRemaining)}s",
            GamePhase.WaveActive => $"Wave {CurrentWave}/{TotalWaves} - Enemies left: {enemiesAlive}",
            GamePhase.Won => "VICTORY!",
            GamePhase.Lost => "DEFEAT",
            _ => ""
        };

        GUI.Label(new Rect(15, 10, 600, 35), text, hudStyle);

        if (Phase == GamePhase.Preparing && GUI.Button(new Rect(15, 50, 160, 35), "Start Wave Now"))
        {
            StartWaveNow();
        }
    }
}