using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitDoorController : MonoBehaviour
{
    public GameObject[] enemies;   // Assign all enemies in the room
    public bool isUnlocked = false;

    void Update()
    {
        if (!isUnlocked && AllEnemiesDead())
        {
            UnlockDoor();
        }
    }

    bool AllEnemiesDead()
    {
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
                return false;
        }
        return true;
    }

    void UnlockDoor()
    {
        isUnlocked = true;

        Debug.Log("Door Unlocked!");

        // Make door passable
        GetComponent<Collider>().isTrigger = true;

        // Optional: change color to show it's unlocked
        GetComponent<Renderer>().material.color = Color.green;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isUnlocked && other.CompareTag("Player"))
        {
            EndLevel();
        }
    }

    void EndLevel()
    {
        Debug.Log("Level Complete!");

        // Reload scene (simple version)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}