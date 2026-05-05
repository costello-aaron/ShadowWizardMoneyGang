using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    /// <summary>Animator state tag on melee/punch states built by Tools → SWMG → Build Soldier Walk + Punch Animator.</summary>
    public const string AnimatorAttackTag = "Attack";

    public Transform player;
    public float moveSpeed = 3f;
    public float attackRange = 2f;
    public float damage = 10f;
    public float attackCooldown = 1.5f;

    public RoomController room;

    [Header("Animator (optional humanoid locomotion)")]
    [SerializeField] private Animator enemyAnimator;

    [Tooltip("Animator float parameter name for MoveBlend when using Assets/Animations/SyntyEnemyLocomotor.")]
    [SerializeField] private string moveBlendParameter = "MoveBlend";

    [Tooltip("Animator trigger when dealing melee damage (Soldier_WalkPunch controller uses \"Attack\").")]
    [SerializeField] private string attackTriggerParameter = "Attack";

    [Tooltip("Higher = locomotion reacts more softly to jitter.")]
    [SerializeField] private float locomotionSmoothTime = 0.08f;

    [Header("Wall avoidance")]
    [Tooltip("Layers that block movement — exclude Player (and pickups) via layer mask when possible.")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Tooltip("How far ahead to look when steering (can be shorter than obstacleRayLength once step caps are enforced).")]
    [SerializeField] private float obstacleRayLength = 1.25f;

    [Tooltip("Sample points this far left/right of the body center (approx. capsule radius).")]
    [SerializeField] private float avoidanceProbeRadius = 0.38f;

    [Tooltip("World-space probe heights added to transform.position (feet-ish and torso).")]
    [SerializeField] private float probeFootOffset = 0.12f;

    [SerializeField] private float probeChestOffset = 0.95f;

    [SerializeField] private float skinWidth = 0.04f;

    [Tooltip("Degrees left/right to probe when the direct path is blocked.")]
    [SerializeField] private float avoidAngleStep = 30f;

    [SerializeField] private int avoidAngleSteps = 4;

    [Header("Other enemies")]
    [Tooltip("Radial distance — other EnemyAI rigs add a sideways bias so groups don’t overlap.")]
    [SerializeField]
    float neighborSeparationRadius = 1.35f;

    [Tooltip("[0–1] How strongly repulsion bends the steer away from overlapping allies.")]
    [SerializeField]
    [Range(0f, 1f)]
    float neighborSeparationStrength = 0.42f;

    [Tooltip("Height of the sphere used to find neighboring enemies.")]
    [SerializeField]
    float neighborSeparationProbeYOffset = 0.45f;

    [SerializeField]
    LayerMask neighborSeparationMask = ~0;

    static readonly int MaxNeighborOverlaps = 24;

    private float lastAttackTime;

    private int moveBlendHash;
    private int attackTriggerHash;
    private Vector3 lastPlanarPosition;
    private float smoothedMoveBlend;
    private float locomotionBlendVelocity;

    private EnemyHumanoidClipPlayable humanoidClipPlayable;

    private Collider[] neighborOverlapScratch;

    void Awake()
    {
        if (enemyAnimator == null)
            TryGetComponent(out enemyAnimator);
        if (enemyAnimator == null)
            enemyAnimator = GetComponentInChildren<Animator>(true);

        TryGetComponent(out humanoidClipPlayable);

        neighborOverlapScratch = new Collider[MaxNeighborOverlaps];

        moveBlendHash = Animator.StringToHash(moveBlendParameter);
        attackTriggerHash = Animator.StringToHash(attackTriggerParameter);
    }

    void OnEnable()
    {
        lastPlanarPosition = Flatten(transform.position);
        smoothedMoveBlend = 0f;
        locomotionBlendVelocity = 0f;
    }

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

    void LateUpdate()
    {
        Vector3 xzNow = transform.position;
        xzNow.y = 0f;
        Vector3 delta = xzNow - lastPlanarPosition;
        lastPlanarPosition = xzNow;

        float planarSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 1e-7f);

        float targetBlend = planarSpeed <= 0.01f ? 0f : Mathf.Clamp01(planarSpeed / Mathf.Max(moveSpeed, 1e-3f));

        smoothedMoveBlend = Mathf.SmoothDamp(
            smoothedMoveBlend,
            targetBlend,
            ref locomotionBlendVelocity,
            Mathf.Max(locomotionSmoothTime, 0.01f),
            Mathf.Infinity,
            Time.deltaTime);

        if (humanoidClipPlayable != null && humanoidClipPlayable.IsDriving)
        {
            humanoidClipPlayable.PushLocomotion(smoothedMoveBlend);
            return;
        }

        if (enemyAnimator == null || enemyAnimator.runtimeAnimatorController == null)
            return;

        AnimatorStateInfo info = enemyAnimator.GetCurrentAnimatorStateInfo(0);
        if (enemyAnimator.IsInTransition(0) || info.IsTag(AnimatorAttackTag))
            return;

        enemyAnimator.SetFloat(moveBlendHash, smoothedMoveBlend);
    }

    void MoveTowardPlayer()
    {
        Vector3 toPlayer = Flatten(player.position - transform.position);
        if (toPlayer.sqrMagnitude < 0.0001f)
            return;

        Vector3 desired = toPlayer.normalized;

        float step = Mathf.Max(moveSpeed * Time.deltaTime, 1e-5f);

        float lookAhead = Mathf.Max(obstacleRayLength, Mathf.Max(step * 2.5f, 0.35f));

        Vector3 moveDir = GetSteeringDirection(desired, lookAhead);

        Vector3 planarMove = ApplyBlockedSlideMove(
            BlendWithNeighborSeparation(moveDir),
            step);

        transform.position += planarMove;

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
    Vector3 GetSteeringDirection(Vector3 desired, float lookAheadDistance)
    {
        if (desired.sqrMagnitude < 1e-6f)
            return desired;

        if (IsMovementDirectionOpen(desired, lookAheadDistance))
            return desired;

        float bestScore = -2f;
        Vector3 best = desired;

        for (int s = 1; s <= avoidAngleSteps; s++)
        {
            float ang = s * avoidAngleStep;
            TryDir(desired, ang, ref bestScore, ref best, lookAheadDistance);
            TryDir(desired, -ang, ref bestScore, ref best, lookAheadDistance);
        }

        if (IsMovementDirectionOpen(best.normalized, lookAheadDistance))
            return best.normalized;

        // Last resort: side-step perpendicular to desired (slide along wall).
        Vector3 perp = Vector3.Cross(Vector3.up, desired).normalized;
        if (IsMovementDirectionOpen(perp, lookAheadDistance))
            return perp;
        if (IsMovementDirectionOpen(-perp, lookAheadDistance))
            return -perp;

        return best.normalized;
    }

    void TryDir(Vector3 desired, float yawDeg, ref float bestScore, ref Vector3 best, float lookAheadDistance)
    {
        Vector3 dir = Flatten(Quaternion.AngleAxis(yawDeg, Vector3.up) * desired);
        if (dir.sqrMagnitude < 1e-6f)
            return;
        dir.Normalize();
        if (!IsMovementDirectionOpen(dir, lookAheadDistance))
            return;

        float score = Vector3.Dot(dir, desired);
        if (score > bestScore)
        {
            bestScore = score;
            best = dir;
        }
    }

    Vector3 ApplyBlockedSlideMove(Vector3 planarDir, float stepDistance)
    {
        planarDir.y = 0f;
        if (planarDir.sqrMagnitude < 1e-6f)
            return Vector3.zero;
        planarDir.Normalize();

        float allowed =
            ProbeMaxAdvanceAlong(transform.position, planarDir, stepDistance);

        Vector3 applied = planarDir * allowed;

        float remaining = Mathf.Max(0f, stepDistance - allowed);
        if (remaining < 1e-5f || !TryEstimateBlockingNormal(planarDir, stepDistance + 0.05f, out Vector3 flatNormal))
            return Flatten(applied);

        flatNormal.y = 0f;
        if (flatNormal.sqrMagnitude < 1e-6f)
            return Flatten(applied);
        flatNormal.Normalize();

        Vector3 slide = Flatten(Vector3.ProjectOnPlane(planarDir, flatNormal));
        if (slide.sqrMagnitude < 1e-6f)
            return Flatten(applied);
        slide.Normalize();

        float slideAllow =
            ProbeMaxAdvanceAlong(
                transform.position + applied + Vector3.up * 0.02f,
                slide,
                remaining);

        applied += slide * Mathf.Min(slideAllow, remaining);
        return Flatten(applied);
    }

    Vector3 BlendWithNeighborSeparation(Vector3 steerPlanar)
    {
        steerPlanar.y = 0f;
        if (steerPlanar.sqrMagnitude < 1e-6f)
            return steerPlanar;

        if (neighborSeparationRadius <= 0f || neighborSeparationStrength <= 0f ||
            neighborOverlapScratch == null)
            return steerPlanar.normalized;

        steerPlanar.Normalize();

        Vector3 repel = AccumulateNeighborRepulsion();
        repel.y = 0f;
        if (repel.sqrMagnitude < 1e-6f)
            return steerPlanar;

        repel.Normalize();
        Vector3 blended =
            steerPlanar * (1f - neighborSeparationStrength) +
            repel * neighborSeparationStrength;
        blended.y = 0f;
        return blended.sqrMagnitude > 1e-6f ? blended.normalized : steerPlanar;
    }

    Vector3 AccumulateNeighborRepulsion()
    {
        Vector3 probeCenter = transform.position + Vector3.up * neighborSeparationProbeYOffset;
        int n = Physics.OverlapSphereNonAlloc(
            probeCenter,
            neighborSeparationRadius,
            neighborOverlapScratch,
            neighborSeparationMask,
            QueryTriggerInteraction.Ignore);

        Transform selfT = transform;
        Vector3 acc = Vector3.zero;

        for (int i = 0; i < n; i++)
        {
            Collider colliderNearby = neighborOverlapScratch[i];
            if (colliderNearby == null)
                continue;

            Transform colT = colliderNearby.transform;
            if (colT == selfT || colT.IsChildOf(selfT))
                continue;

            EnemyAI other = colliderNearby.GetComponentInParent<EnemyAI>();
            if (other == null || ReferenceEquals(other, this))
                continue;

            Vector3 away = Flatten(selfT.position - other.transform.position);
            float sqMag = away.sqrMagnitude;
            if (sqMag < 1e-8f)
            {
                Vector3 lateral = Flatten(Vector3.Cross(Vector3.up, selfT.forward));
                if (lateral.sqrMagnitude < 1e-6f)
                    lateral = selfT.right;
                acc += lateral.normalized * (1f / 0.16f);
                continue;
            }

            acc += away / Mathf.Max(sqMag, 1e-3f);
        }

        return Flatten(acc);
    }

    float ProbeMaxAdvanceAlong(Vector3 fromWorld, Vector3 dirPlanar, float castDistance)
    {
        dirPlanar.y = 0f;
        if (dirPlanar.sqrMagnitude < 1e-6f)
            return 0f;
        dirPlanar.Normalize();

        castDistance = Mathf.Max(castDistance, 1e-6f);

        Vector3 right = Flatten(Vector3.Cross(Vector3.up, dirPlanar));
        float r = avoidanceProbeRadius;
        if (right.sqrMagnitude < 1e-6f)
            right = transform.right;
        else
            right.Normalize();

        float[] vy = { probeFootOffset, probeChestOffset };
        float[] lr = { -r, 0f, r };

        float allowedTravel = castDistance;
        foreach (float v in vy)
        {
            foreach (float lateral in lr)
            {
                Vector3 o = new Vector3(fromWorld.x, fromWorld.y + v, fromWorld.z) + right * lateral;

                RaycastHit[] hits =
                    Physics.RaycastAll(o, dirPlanar, castDistance, obstacleMask, QueryTriggerInteraction.Ignore);

                foreach (RaycastHit h in hits)
                {
                    if (!ColliderBlocksMovement(h.collider))
                        continue;
                    float d = Mathf.Max(0f, h.distance - skinWidth);
                    allowedTravel = Mathf.Min(allowedTravel, d);
                }
            }
        }

        return Mathf.Clamp(allowedTravel, 0f, castDistance);
    }

    bool IsMovementDirectionOpen(Vector3 planarDir, float lookAheadDistance)
    {
        planarDir.y = 0f;
        if (planarDir.sqrMagnitude < 1e-6f)
            return false;
        planarDir.Normalize();

        float req = Mathf.Max(lookAheadDistance, 1e-3f);
        float minStep = Mathf.Max(moveSpeed * Time.deltaTime * 2f, 0.06f);

        float clearDepth = ProbeMaxAdvanceAlong(transform.position, planarDir, req);
        return clearDepth >= Mathf.Max(req * 0.9f, minStep);
    }

    bool TryEstimateBlockingNormal(Vector3 planarDir, float probeLength, out Vector3 flattenedNormal)
    {
        planarDir.y = 0f;
        planarDir.Normalize();
        flattenedNormal = Vector3.zero;

        Vector3 chest = transform.position + Vector3.up * probeChestOffset;
        RaycastHit[] hits =
            Physics.RaycastAll(chest, planarDir, probeLength, obstacleMask, QueryTriggerInteraction.Ignore);

        bool any = false;
        RaycastHit bestHit = default;
        foreach (RaycastHit h in hits)
        {
            if (!ColliderBlocksMovement(h.collider))
                continue;
            if (!any || h.distance < bestHit.distance)
            {
                bestHit = h;
                any = true;
            }
        }

        if (!any)
            return false;

        flattenedNormal = Flatten(bestHit.normal);
        return flattenedNormal.sqrMagnitude > 1e-6f;
    }

    bool ColliderBlocksMovement(Collider colliderObj)
    {
        if (colliderObj == null || colliderObj.isTrigger)
            return false;

        Transform colT = colliderObj.transform;
        if (colT == transform || colT.IsChildOf(transform))
            return false;

        if (colliderObj.CompareTag("Player"))
            return false;

        EnemyAI otherEnemy = colliderObj.GetComponentInParent<EnemyAI>();
        if (otherEnemy != null)
            return !ReferenceEquals(otherEnemy, this);

        return true;
    }

    void Attack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            Health playerHealth = player.GetComponent<Health>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damage);

            if (humanoidClipPlayable != null && humanoidClipPlayable.IsDriving && humanoidClipPlayable.SupportsMeleeStrike)
                humanoidClipPlayable.NotifyMeleeStrike();
            else if (enemyAnimator != null && enemyAnimator.isActiveAndEnabled && enemyAnimator.runtimeAnimatorController != null)
                enemyAnimator.SetTrigger(attackTriggerHash);

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
