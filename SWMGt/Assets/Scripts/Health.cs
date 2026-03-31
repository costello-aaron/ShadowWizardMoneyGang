using UnityEngine;

public class Health : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    public bool destroyOnDeath = true;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (CompareTag("Player"))
        {
            GameManager.instance.GameOver();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public float GetHealth()
    {
        return currentHealth;
    }
}