using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    public float damage = 10f;
    public float attackCooldown = 1.5f;

    public RoomController room;

    private float lastAttackTime;

    void Log(string message)
    {
        Debug.Log($"[EnemyAI:{name}] {message}");
    }

    void Start()
    {
        Log("Start()");
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                Log($"Player assigned: {player.name}");
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange)
        {
            MoveTowardPlayer();
        }
        else
        {
            Attack();
        }
    }

    void MoveTowardPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        transform.LookAt(player);
    }

    void Attack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Log("Attack()");
            Health playerHealth = player.GetComponent<Health>();

            if (playerHealth != null)
                playerHealth.TakeDamage(damage);

            lastAttackTime = Time.time;
        }
    }

    public void Die()
    {
        Log($"Die() roomAssigned={(room != null)}");
        if (room != null)
            room.EnemyDied();

        Destroy(gameObject);
    }
}