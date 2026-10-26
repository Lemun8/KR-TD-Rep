using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveHudUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject prepBarRoot;
    [SerializeField] private RectTransform prepFill;
    [SerializeField] private Button startWaveButton;

    [Header("Banner")]
    [SerializeField] private CanvasGroup banner;
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private float bannerHoldTime = 1.5f;
    [SerializeField] private float bannerFadeTime = 0.6f;

    private Coroutine bannerRoutine;
    private int shownWave = -1;
    private int shownSeconds = -1;
    private int shownEnemies = -1;

    private void Awake()
    {
        startWaveButton.onClick.AddListener(() =>
        {
            if (waveManager != null) waveManager.StartWaveNow();
        });
    }

    private void Start()
    {
        if (waveManager == null)
        {
            Debug.LogWarning("WaveHudUI: no WaveManager assigned.", this);
            return;
        }

        waveManager.WaveStarted += HandleWaveStarted;
        waveManager.WaveCompleted += HandleWaveCompleted;
    }

    private void OnDestroy()
    {
        if (waveManager == null) return;

        waveManager.WaveStarted -= HandleWaveStarted;
        waveManager.WaveCompleted -= HandleWaveCompleted;
    }

    private void Update()
    {
        if (waveManager == null) return;

        GamePhase phase = waveManager.Phase;

        if (waveManager.CurrentWave != shownWave)
        {
            shownWave = waveManager.CurrentWave;
            waveText.text = $"WAVE {Mathf.Max(shownWave, 1)} / {waveManager.TotalWaves}";
        }

        bool preparing = phase == GamePhase.Preparing;
        startWaveButton.gameObject.SetActive(preparing);
        prepBarRoot.SetActive(preparing);

        switch (phase)
        {
            case GamePhase.Preparing:
                int seconds = Mathf.CeilToInt(waveManager.PrepTimeRemaining);
                if (seconds != shownSeconds)
                {
                    shownSeconds = seconds;
                    statusText.text = $"Next wave in {seconds}s";
                }

                float ratio = waveManager.PrepDuration > 0f
                    ? Mathf.Clamp01(waveManager.PrepTimeRemaining / waveManager.PrepDuration)
                    : 0f;
                prepFill.anchorMax = new Vector2(ratio, 1f);
                shownEnemies = -1;
                break;

            case GamePhase.WaveActive:
                int enemies = waveManager.EnemiesRemaining;
                if (enemies != shownEnemies)
                {
                    shownEnemies = enemies;
                    statusText.text = $"Enemies left: {enemies}";
                }

                shownSeconds = -1;
                break;

            case GamePhase.Won:
                statusText.text = "Victory!";
                break;

            case GamePhase.Lost:
                statusText.text = "Defeat";
                break;
        }
    }

    private void HandleWaveStarted(int wave)
    {
        ShowBanner($"WAVE {wave}");
    }

    private void HandleWaveCompleted(int wave, bool villageIsPerfect)
    {
        string message = $"WAVE {wave} CLEARED";
        if (villageIsPerfect) message += "\n<size=50%>Perfect village bonus!</size>";

        ShowBanner(message);
    }

    private void ShowBanner(string message)
    {
        if (banner == null) return;

        bannerText.text = message;

        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine());
    }

    private IEnumerator BannerRoutine()
    {
        banner.alpha = 1f;
        yield return new WaitForSeconds(bannerHoldTime);

        for (float t = 0f; t < bannerFadeTime; t += Time.deltaTime)
        {
            banner.alpha = 1f - t / bannerFadeTime;
            yield return null;
        }

        banner.alpha = 0f;
    }
}