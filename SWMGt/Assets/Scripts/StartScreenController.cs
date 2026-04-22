using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject startScreenPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("Behavior")]
    [SerializeField] private bool lockCursorWhilePlaying = true;
    [Tooltip("If false, cursor stays visible in the Editor so Play-mode testing is easier.")]
    [SerializeField] private bool lockCursorInEditor = false;
    [SerializeField] private bool showQuitButtonInEditor = false;

    [Header("Gameplay scene")]
    [Tooltip("Name must match a scene in File → Build Settings. If empty, see Load Next In Build below.")]
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [Tooltip("If the named scene is missing/empty, load the next scene in Build Settings (MainMenu=0, gameplay=1).")]
    [SerializeField] private bool loadNextInBuildIfNameEmpty = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging = true;

    private bool gameStarted = false;

    void Log(string message)
    {
        if (verboseLogging)
            Debug.Log($"[StartScreenController on {gameObject.name}] {message}", this);
    }

    void Warn(string message)
    {
        Debug.LogWarning($"[StartScreenController on {gameObject.name}] {message}", this);
    }

    void Awake()
    {
        if (!IsPrimaryInstance())
        {
            enabled = false;
            return;
        }

        Time.timeScale = 1f;
        EnsureEventSystemExists();
        Log($"Awake in scene '{gameObject.scene.name}' (primary start screen).");
    }

    /// <summary>Only one StartScreenController should run. Prefer a GameObject named "UIManager" if present.</summary>
    bool IsPrimaryInstance()
    {
        StartScreenController[] all = Object.FindObjectsByType<StartScreenController>(FindObjectsSortMode.None);
        if (all == null || all.Length <= 1)
            return true;

        StartScreenController keep = null;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.name == "UIManager")
            {
                keep = all[i];
                break;
            }
        }

        if (keep == null)
            keep = all[0];

        if (this != keep)
        {
            Warn($"Disabling this duplicate. Use a single StartScreenController on \"{keep.gameObject.name}\" and remove the extra one from this object.");
            return false;
        }

        return true;
    }

    void Start()
    {
        if (startScreenPanel == null)
            Warn("Start Screen Panel is not assigned — menu pause/unpause will not work as intended.");
        if (startButton == null)
            Warn("Start Button is not assigned — wire it or use OnClick → OnStartButtonPressed on the Button.");

        ValidateButtonRaycast(startButton);

        ShowStartScreen();

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        Log($"Start complete. timeScale={Time.timeScale}, panel active={(startScreenPanel != null && startScreenPanel.activeInHierarchy)}.");
    }

    void ValidateButtonRaycast(Button button)
    {
        if (button == null)
            return;

        var graphic = button.targetGraphic;
        if (graphic != null && !graphic.raycastTarget)
            Warn($"Start button '{button.name}' targetGraphic has Raycast Target off — clicks will not hit the button.");

        Canvas canvas = button.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            Warn($"Canvas '{canvas.name}' has no GraphicRaycaster — UI clicks will not work.");
    }

    static void EnsureEventSystemExists()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<StandaloneInputModule>();
        Debug.LogWarning("[StartScreenController] No EventSystem in scene — created one so UI buttons can receive clicks.", eventSystemGo);
    }

    void Update()
    {
        // Fallback: if the panel gets hidden by an inspector event, still start gameplay.
        if (!gameStarted && startScreenPanel != null && !startScreenPanel.activeInHierarchy)
        {
            Log("Panel was hidden without StartGame — applying fallback unpause.");
            StartGame();
        }
    }

    void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartGame);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(QuitGame);

        Time.timeScale = 1f;
    }

    public void ShowStartScreen()
    {
        Log("ShowStartScreen — pausing (timeScale = 0).");
        gameStarted = false;
        Time.timeScale = 0f;

        if (startScreenPanel != null)
            startScreenPanel.SetActive(true);

        if (quitButton != null)
            quitButton.gameObject.SetActive(showQuitButtonInEditor || !Application.isEditor);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void StartGame()
    {
        if (gameStarted)
        {
            Log("StartGame ignored — already started.");
            return;
        }

        Log("StartGame — unpausing and loading gameplay scene.");
        gameStarted = true;
        Time.timeScale = 1f;

        if (startScreenPanel != null)
            startScreenPanel.SetActive(false);

        if (TryLoadGameplayScene())
            return;

        Warn("Could not load gameplay scene — staying in menu. Set Gameplay Scene Name and add scenes to File → Build Settings.");
        bool shouldLockCursor = lockCursorWhilePlaying && (!Application.isEditor || lockCursorInEditor);
        if (shouldLockCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    bool TryLoadGameplayScene()
    {
        if (!string.IsNullOrWhiteSpace(gameplaySceneName) && Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Log($"Loading scene by name: {gameplaySceneName}");
            SceneManager.LoadScene(gameplaySceneName);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(gameplaySceneName))
            Warn($"Cannot load scene '{gameplaySceneName}'. Check the name and add it in File → Build Settings.");

        if (loadNextInBuildIfNameEmpty)
        {
            int current = SceneManager.GetActiveScene().buildIndex;
            int next = current + 1;
            if (next < SceneManager.sceneCountInBuildSettings)
            {
                Log($"Loading scene by build index: {next}");
                SceneManager.LoadScene(next);
                return true;
            }
        }

        return false;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // Optional inspector event hooks.
    public void OnStartButtonPressed()
    {
        StartGame();
    }

    public void OnQuitButtonPressed()
    {
        QuitGame();
    }

    public bool IsGameStarted()
    {
        return gameStarted;
    }
}
