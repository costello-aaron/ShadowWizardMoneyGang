using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitDoorController : MonoBehaviour
{
    public bool isLocked = true;
    [Header("Level Transition")]
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private bool useNextBuildIndex = true;
    [SerializeField] private Renderer targetRenderer;
    private Material runtimeDoorMaterial;

    void Log(string message)
    {
        Debug.Log($"[ExitDoorController:{name}] {message}");
    }

    public void SetLocked(bool locked)
    {
        Log($"SetLocked(locked={locked})");
        isLocked = locked;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = !locked;
            Log($"Collider isTrigger set to {col.isTrigger}");
        }
        else
        {
            Log("No Collider found.");
        }

        Renderer rend = targetRenderer != null ? targetRenderer : GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            Material mat = null;

            // Use a per-instance material at runtime so only this door changes color.
            if (Application.isPlaying && gameObject.scene.IsValid())
            {
                if (runtimeDoorMaterial == null)
                {
                    Material source = rend.sharedMaterial;
                    if (source != null)
                    {
                        runtimeDoorMaterial = new Material(source);
                        rend.material = runtimeDoorMaterial;
                        Log("Created runtime material instance for door.");
                    }
                }

                mat = runtimeDoorMaterial;
            }

            // Fallback path for non-runtime/prefab contexts.
            if (mat == null)
            {
                mat = rend.sharedMaterial;
            }

            if (mat != null)
            {
                mat.color = locked ? Color.red : Color.green;
                Log($"Door color updated to {(locked ? "red" : "green")}");
            }
            else
            {
                Log("No material found on renderer.");
            }
        }
        else
        {
            Log("No child Renderer found.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Log($"OnTriggerEnter(other={other.name}, tag={other.tag}, isLocked={isLocked})");
        if (!isLocked && other.CompareTag("Player"))
        {
            LoadNextLevel();
        }
    }

    void LoadNextLevel()
    {
        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            if (Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Log($"Loading next scene by name: {nextSceneName}");
                SceneManager.LoadScene(nextSceneName);
                return;
            }

            Log($"Scene '{nextSceneName}' is not in Build Settings. Falling back.");
        }

        if (useNextBuildIndex)
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int nextIndex = currentIndex + 1;
            int sceneCount = SceneManager.sceneCountInBuildSettings;

            if (nextIndex < sceneCount)
            {
                Log($"Loading next scene by build index: {nextIndex}");
                SceneManager.LoadScene(nextIndex);
                return;
            }

            Log("No next scene in Build Settings. Reloading current scene.");
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}