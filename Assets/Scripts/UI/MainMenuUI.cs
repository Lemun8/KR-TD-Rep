using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Controls the main menu: Start loads the game, Options shows the volume panel, Quit closes the game.
public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button backButton;

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Scenes")]
    [Tooltip("The exact name of your game scene. It must be in the Build Profiles scene list.")]
    [SerializeField] private string gameSceneName = "GameScene";

    private void Awake()
    {
        Time.timeScale = 1f;   // safety: coming back from a frozen end screen

        startButton.onClick.AddListener(StartGame);
        optionsButton.onClick.AddListener(OpenOptions);
        quitButton.onClick.AddListener(Quit);
        backButton.onClick.AddListener(CloseOptions);

        ShowMain();
    }

    private void StartGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"MainMenuUI: scene '{gameSceneName}' can't be loaded. Check the name, and that the scene is in File > Build Profiles > Scene List.", this);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void OpenOptions()
    {
        AudioManager.Play(SfxEvent.ButtonClick);
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    private void CloseOptions()
    {
        AudioManager.Play(SfxEvent.ButtonClick);
        VolumeSettings.Save();
        ShowMain();
    }

    private void ShowMain()
    {
        mainPanel.SetActive(true);
        optionsPanel.SetActive(false);
    }

    private void Quit()
    {
        VolumeSettings.Save();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}