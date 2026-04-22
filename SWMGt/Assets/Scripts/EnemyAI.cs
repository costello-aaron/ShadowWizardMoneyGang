using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    public float damage = 10f;
    public float attackCooldown = 1.5f;

    public RoomController room;

    [Header("Wall avoidance")]
    [Tooltip("Layers that block movement (set to your wall / environment layer for best results).")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    [SerializeField] private float obstacleRayLength = 1.25f;

    [SerializeField] private float rayHeight = 0.6f;

    [Tooltip("Degrees left/right to probe when the direct path is blocked.")]
    [SerializeField] private float avoidAngleStep = 30f;

    [SerializeField] private int avoidAngleSteps = 4;

    private float lastAttackTime;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
        }
    }

    void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > attackRange)
            MoveTowardPlayer();
        else
            Attack();
    }

    void MoveTowardPlayer()
    {
        Vector3 toPlayer = Flatten(player.position - transform.position);
        if (toPlayer.sqrMagnitude < 0.0001f)
            return;

        Vector3 desired = toPlayer.normalized;
        Vector3 moveDir = GetSteeringDirection(desired);
        transform.position += moveDir * (moveSpeed * Time.deltaTime);

        Vector3 look = Flatten(player.position - transform.position);
        if (look.sqrMagnitude > 0.0001f)
            transform.forward = look.normalized;
    }

    static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    /// <summary>
    /// Tries to move toward the player; if a wall is ahead, steers to the best open direction in a small fan.
    /// </summary>
    Vector3 GetSteeringDirection(Vector3 desired)
    {
        Vector3 origin = transform.position + Vector3.up * rayHeight;

        if (IsPathClear(origin, desired))
            return desired;

        float bestScore = -2f;
        Vector3 best = desired;

        for (int s = 1; s <= avoidAngleSteps; s++)
        {
            float ang = s * avoidAngleStep;
            TryDir(origin, desired, ang, ref bestScore, ref best);
            TryDir(origin, desired, -ang, ref bestScore, ref best);
        }

        if (IsPathClear(origin, best))
            return best.normalized;

        // Last resort: side-step perpendicular to desired (slide along wall).
        Vector3 perp = Vector3.Cross(Vector3.up, desired).normalized;
        if (IsPathClear(origin, perp))
            return perp;
        if (IsPathClear(origin, -perp))
            return -perp;

        return best.normalized;
    }

    void TryDir(Vector3 origin, Vector3 desired, float yawDeg, ref float bestScore, ref Vector3 best)
    {
        Vector3 dir = Flatten(Quaternion.AngleAxis(yawDeg, Vector3.up) * desired);
        if (dir.sqrMagnitude < 0.0001f)
            return;
        dir.Normalize();
        if (!IsPathClear(origin, dir))
            return;

        float score = Vector3.Dot(dir, desired);
        if (score > bestScore)
        {
            bestScore = score;
            best = dir;
        }
    }

    bool IsPathClear(Vector3 origin, Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return true;
        direction.Normalize();

        Vector3 rayStart = origin + direction * 0.2f;
        float length = obstacleRayLength;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, direction, length, obstacleMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return true;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i].collider;
            if (c == null)
                continue;
            if (c.transform.IsChildOf(transform))
                continue;
            if (c.isTrigger)
                continue;
            if (c.CompareTag("Player"))
                return true;
            if (c.GetComponentInParent<EnemyAI>() is EnemyAI e && e != this)
                return false;

            return false;
        }

        return true;
    }

    void Attack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);

            lastAttackTime = Time.time;
        }
    }

    public void Die()
    {
        if (room != null)
            room.EnemyDied();

        Destroy(gameObject);
    }
}
