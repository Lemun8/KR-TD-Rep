using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndScreenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private GameObject screen;          // the overlay, hidden until the game ends
    [SerializeField] private CanvasGroup screenGroup;    // used to fade it in
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    [Header("Timing")]
    [Tooltip("Seconds to wait before the screen appears, so the player sees what happened.")]
    [SerializeField] private float showDelay = 1.5f;
    [SerializeField] private float fadeTime = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color victoryColor = new Color(1f, 0.82f, 0.25f);
    [SerializeField] private Color defeatColor = new Color(0.9f, 0.3f, 0.25f);

    private int enemiesDefeated;
    private int perfectWaves;
    private float endTime;
    private bool ended;

    private void Awake()
    {
        Time.timeScale = 1f;   // safety: never start a run frozen
        screen.SetActive(false);

        restartButton.onClick.AddListener(Restart);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
    }

    private void Start()
    {
        if (waveManager == null)
        {
            Debug.LogWarning("EndScreenUI: no WaveManager assigned.", this);
            return;
        }

        waveManager.EnemyKilled += HandleEnemyKilled;
        waveManager.WaveCompleted += HandleWaveCompleted;
        waveManager.GameWon += HandleGameWon;
        waveManager.GameLost += HandleGameLost;
    }

    private void OnDestroy()
    {
        if (waveManager == null) return;

        waveManager.EnemyKilled -= HandleEnemyKilled;
        waveManager.WaveCompleted -= HandleWaveCompleted;
        waveManager.GameWon -= HandleGameWon;
        waveManager.GameLost -= HandleGameLost;
    }

    // ---------- Run statistics ----------

    private void HandleEnemyKilled(Enemy enemy)
    {
        enemiesDefeated++;
    }

    private void HandleWaveCompleted(int wave, bool villageIsPerfect)
    {
        if (villageIsPerfect) perfectWaves++;
    }

    // ---------- Win / lose ----------

    private void HandleGameWon()
    {
        End(true);
    }

    private void HandleGameLost()
    {
        End(false);
    }

    private void End(bool won)
    {
        if (ended) return;
        ended = true;

        endTime = Time.timeSinceLevelLoad;
        StartCoroutine(ShowRoutine(won));
    }

    private IEnumerator ShowRoutine(bool won)
    {
        yield return new WaitForSeconds(showDelay);

        if (BuildMenuUI.Instance != null) BuildMenuUI.Instance.Close();

        titleText.text = won ? "VICTORY!" : "DEFEAT";
        titleText.color = won ? victoryColor : defeatColor;
        subtitleText.text = won
            ? $"The village survived all {waveManager.TotalWaves} waves."
            : "The village has fallen.";
        statsText.text = BuildStats(won);

        screenGroup.alpha = 0f;
        screen.SetActive(true);

        // Freeze the game behind the screen
        Time.timeScale = 0f;

        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            screenGroup.alpha = t / fadeTime;
            yield return null;
        }

        screenGroup.alpha = 1f;
    }

    private string BuildStats(bool won)
    {
        int survived = won ? waveManager.TotalWaves : Mathf.Max(0, waveManager.CurrentWave - 1);

        string village = "-";
        if (VillageHealth.Instance != null)
        {
            Health health = VillageHealth.Instance.Health;
            village = $"{Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}";
        }

        int minutes = Mathf.FloorToInt(endTime / 60f);
        int seconds = Mathf.FloorToInt(endTime % 60f);

        return $"Waves survived:  {survived} / {waveManager.TotalWaves}\n" +
               $"Enemies defeated:  {enemiesDefeated}\n" +
               $"Perfect waves:  {perfectWaves}\n" +
               $"Village health:  {village}\n" +
               $"Time:  {minutes:00}:{seconds:00}";
    }

    // ---------- Buttons ----------

    private void Restart()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (scene.buildIndex < 0)
        {
            Debug.LogError("Restart needs this scene in the build list: File > Build Profiles > Scene List > Add Open Scenes.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(scene.buildIndex);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}