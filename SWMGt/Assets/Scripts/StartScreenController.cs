using UnityEngine;
using UnityEngine.UI;

public class StartScreenController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject startScreenPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("Behavior")]
    [SerializeField] private bool lockCursorWhilePlaying = true;
    [SerializeField] private bool showQuitButtonInEditor = false;

    private bool gameStarted = false;

    void Awake()
    {
        Time.timeScale = 1f;
    }

    void Start()
    {
        ShowStartScreen();

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
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
        gameStarted = true;
        Time.timeScale = 1f;

        if (startScreenPanel != null)
            startScreenPanel.SetActive(false);

        if (lockCursorWhilePlaying)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public bool IsGameStarted()
    {
        return gameStarted;
    }
}
