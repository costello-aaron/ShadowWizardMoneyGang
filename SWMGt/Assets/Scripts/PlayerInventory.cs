using System;
using UnityEngine;

/// <summary>
/// Run-wide potion storage. Persists across scene loads via DontDestroyOnLoad.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    [Header("Speed potion (used from inventory)")]
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float speedBoostDuration = 20f;

    private int healthPotions;
    private int speedPotions;

    public event Action OnInventoryChanged;

    public int HealthPotions => healthPotions;
    public int SpeedPotions => speedPotions;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static PlayerInventory EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject go = new GameObject("PlayerInventory");
        return go.AddComponent<PlayerInventory>();
    }

    public void AddHealthPotions(int amount = 1)
    {
        if (amount <= 0)
            return;
        healthPotions += amount;
        Notify();
    }

    public void AddSpeedPotions(int amount = 1)
    {
        if (amount <= 0)
            return;
        speedPotions += amount;
        Notify();
    }

    public bool TryUseHealthPotion()
    {
        if (healthPotions <= 0)
            return false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return false;

        Health health = player.GetComponentInChildren<Health>();
        if (health == null)
            health = player.GetComponent<Health>();
        if (health == null)
            return false;

        healthPotions--;
        health.HealToFull();
        Notify();
        DamageVignetteUI.TriggerHealPotionFeedback();
        return true;
    }

    public bool TryUseSpeedPotion()
    {
        if (speedPotions <= 0)
            return false;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return false;

        PlayerMovement movement = player.GetComponentInChildren<PlayerMovement>();
        if (movement == null)
            movement = player.GetComponent<PlayerMovement>();
        if (movement == null)
            return false;

        speedPotions--;
        movement.ApplySpeedBoost(speedMultiplier, speedBoostDuration);
        Notify();
        DamageVignetteUI.TriggerSpeedPotionFeedback();
        return true;
    }

    void Notify()
    {
        OnInventoryChanged?.Invoke();
    }
}
