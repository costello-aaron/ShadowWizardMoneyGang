using UnityEngine;
using System.Collections.Generic;

public class RoomController : MonoBehaviour
{
    public GameObject[] enemies;
    public GameObject[] doors;
    public TreasureChestController[] chests;
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int enemyCount = 3;
    private bool roomStarted = false;
    private bool roomCompleted = false;

    private int enemiesAlive = 0;
    private bool roomActive = false;

    /// <summary>True after all enemies in this room are defeated (doors/chests unlocked).</summary>
    public bool IsCleared => roomCompleted;

    void Log(string message)
    {
        Debug.Log($"[RoomController:{name}] {message}");
    }

    void Start()
    {
        Log("Start()");
        // Room starts open; lock it only once combat begins.
        SetDoorsClosed(false);
        SetChestsLocked(true);
        SetEnemiesActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        Log($"OnTriggerEnter(other={other.name}, tag={other.tag})");
        OnPlayerEnter(other);
    }

    public void OnPlayerEnter(Collider other)
    {
        Log($"OnPlayerEnter(other={other.name}, tag={other.tag}, roomStarted={roomStarted}, roomCompleted={roomCompleted})");
        if (!other.CompareTag("Player"))
            return;

        if (!roomStarted && !roomCompleted)
        {
            ActivateRoom();
        }
    }

    void ActivateRoom()
    {
        Log($"ActivateRoom() roomActive={roomActive}, roomCompleted={roomCompleted}");
        if (roomActive || roomCompleted)
            return;

        roomStarted = true;
        roomActive = true;

        SetDoorsClosed(true);
        SetChestsLocked(true);
        SpawnEnemies();

        Debug.Log("Room Started");
    }

    void SpawnEnemies()
    {
        Log($"SpawnEnemies() enemyCount={enemyCount}");
        enemiesAlive = 0;

        if (enemyPrefab == null)
        {
            Debug.LogWarning("RoomController: enemyPrefab is not assigned.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("RoomController: no spawn points assigned.");
            return;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject enemy = Instantiate(enemyPrefab, spawn.position, Quaternion.identity);
            Log($"Spawned enemy {i + 1}/{enemyCount} at {spawn.position}");

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.room = this;
            }
            else
            {
                Log("Spawned enemy missing EnemyAI component.");
            }

            enemiesAlive++;
        }

        Log($"SpawnEnemies complete. enemiesAlive={enemiesAlive}");

        if (enemiesAlive == 0)
        {
            CompleteRoom();
        }
    }

    public void EnemyDied()
    {
        Log($"EnemyDied() before decrement: enemiesAlive={enemiesAlive}, roomActive={roomActive}, roomCompleted={roomCompleted}");
        if (!roomActive || roomCompleted)
            return;

        enemiesAlive--;
        Log($"EnemyDied() after decrement: enemiesAlive={enemiesAlive}");

        if (enemiesAlive <= 0)
        {
            CompleteRoom();
        }
    }

    void CompleteRoom()
    {
        Log($"CompleteRoom() roomActive={roomActive}, roomCompleted={roomCompleted}, enemiesAlive={enemiesAlive}");
        if (!roomActive || roomCompleted)
            return;

        roomCompleted = true;
        roomActive = false;

        SetDoorsClosed(false);
        SetChestsLocked(false);

        Log("Room Cleared");
    }

    void SetDoorsClosed(bool closed)
    {
        Log($"SetDoorsClosed(closed={closed})");
        List<ExitDoorController> doorControllers = new List<ExitDoorController>();

        if (doors != null && doors.Length > 0)
        {
            foreach (GameObject door in doors)
            {
                if (door == null)
                {
                    Log("Door entry is null; skipping.");
                    continue;
                }

                // Ignore prefab assets accidentally assigned in the array.
                if (!door.scene.IsValid())
                {
                    Log($"Door {door.name} is not a scene instance; skipping explicit reference.");
                    continue;
                }

                ExitDoorController doorController = door.GetComponent<ExitDoorController>();

                if (doorController != null)
                {
                    doorControllers.Add(doorController);
                }
                else
                {
                    Log($"Door {door.name} has no ExitDoorController.");
                }
            }
        }

        if (doorControllers.Count == 0)
        {
            Log("No valid scene door references found. Falling back to child ExitDoorController components.");
            ExitDoorController[] fallbackDoors = GetComponentsInChildren<ExitDoorController>(true);
            foreach (ExitDoorController fallbackDoor in fallbackDoors)
            {
                if (fallbackDoor != null)
                    doorControllers.Add(fallbackDoor);
            }
        }

        if (doorControllers.Count == 0)
        {
            Log("No door controllers found to update.");
            return;
        }

        foreach (ExitDoorController doorController in doorControllers)
        {
            if (doorController == null)
                continue;

            Log($"Applying lock={closed} to door {doorController.name}");
            doorController.SetLocked(closed);
        }
    }

    void SetEnemiesActive(bool active)
    {
        Log($"SetEnemiesActive(active={active})");
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
                enemy.SetActive(active);
        }
    }

    void SetChestsLocked(bool locked)
    {
        Log($"SetChestsLocked(locked={locked})");
        if (chests == null || chests.Length == 0)
            return;

        foreach (TreasureChestController chest in chests)
        {
            if (chest == null)
                continue;

            chest.SetLocked(locked);
        }
    }
}