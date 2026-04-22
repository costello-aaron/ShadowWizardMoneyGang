using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public bool destroyOnDeath = true;

    void Log(string message)
    {
        Debug.Log($"[Health:{name}] {message}");
    }

    void Start()
    {
        Log($"Start() maxHealth={maxHealth}");
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        Log($"TakeDamage(amount={amount}) currentHealth(before)={currentHealth}");
        currentHealth -= amount;
        Log($"TakeDamage currentHealth(after)={currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Log($"Die() tag={tag}");
        if (CompareTag("Player"))
        {
            Log("Player died. Triggering GameOver.");
            GameManager.instance.GameOver();
        }
        else
        {
            EnemyAI enemyAI = GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                Log("EnemyAI found. Forwarding death to EnemyAI.Die().");
                enemyAI.Die();
                return;
            }

            Log("No EnemyAI found. Destroying object directly.");
            Destroy(gameObject);
        }
    }

    public float GetHealth()
    {
        return currentHealth;
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        Log($"Heal(amount={amount}) currentHealth(after)={currentHealth}");
    }

    public void HealToFull()
    {
        currentHealth = maxHealth;
        Log($"HealToFull() currentHealth(after)={currentHealth}");
    }
}