using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Tooltip("Projectile speed relative to aiming direction.")]
    public float speed = 72f;

    public float damage = 20f;
    public float lifeTime = 3f;

    [Tooltip("Player bullets skip the Player layer so sprinting forwards does not instantly self-hit.")]
    [SerializeField] private bool ignoresPlayerCollider = true;

    [Header("Trail")]
    [Tooltip("Draw a motion trail behind the projectile (TrailRenderer).")]
    [SerializeField] private bool useBulletTrail = true;

    [Tooltip("Optional material (URP Particles/Unlit or similar). Leave empty to pick a simple default at runtime.")]
    [SerializeField] private Material trailMaterialOverride;

    [SerializeField] private float trailTime = 0.09f;

    [SerializeField] private float trailMinVertexDistance = 0.025f;

    [SerializeField] private float trailWidthStart = 0.065f;

    [SerializeField] private float trailWidthEnd = 0.008f;

    private Rigidbody rigidBody;
    private Vector3 worldVelocity;
    private TrailRenderer trailRenderer;

    private static Material defaultTrailSharedMaterial;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        if (rigidBody != null)
        {
            rigidBody.collisionDetectionMode =
                CollisionDetectionMode.ContinuousSpeculative;
            rigidBody.useGravity = false;
        }

        EnsureTrail();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    /// <summary>Adds planar shooter velocity (e.g. sprint) so bullets keep pace instead of spawning inside the player capsule.</summary>
    public void Launch(Vector3 boreWorldDirection, Vector3 planarShooterVelocity)
    {
        Vector3 bore = boreWorldDirection.sqrMagnitude > 1e-6f
            ? boreWorldDirection.normalized
            : transform.forward;

        transform.rotation = Quaternion.LookRotation(bore);

        planarShooterVelocity.y = 0f;
        worldVelocity = bore * speed + planarShooterVelocity;

        if (trailRenderer != null)
            trailRenderer.Clear();
    }

    void EnsureTrail()
    {
        if (!useBulletTrail)
            return;

        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
            trailRenderer = gameObject.AddComponent<TrailRenderer>();

        TrailRenderer tr = trailRenderer;
        tr.emitting = true;
        tr.time = Mathf.Max(0.01f, trailTime);
        tr.minVertexDistance = Mathf.Max(0.001f, trailMinVertexDistance);
        tr.numCapVertices = 3;
        tr.numCornerVertices = 2;
        tr.autodestruct = false;
        tr.generateLightingData = false;

        AnimationCurve width = new AnimationCurve();
        width.AddKey(0f, Mathf.Max(1e-4f, trailWidthStart));
        width.AddKey(1f, Mathf.Max(1e-4f, trailWidthEnd));
        tr.widthCurve = width;
        tr.widthMultiplier = 1f;

        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 0.9f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.08f), 0.45f),
                new GradientColorKey(new Color(0.55f, 0.55f, 0.6f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(0.55f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });

        tr.colorGradient = g;

        Material mat =
            trailMaterialOverride != null ? trailMaterialOverride : GetOrCreateDefaultTrailMaterial();
        if (mat != null)
            tr.sharedMaterial = mat;
    }

    static Material GetOrCreateDefaultTrailMaterial()
    {
        if (defaultTrailSharedMaterial != null)
            return defaultTrailSharedMaterial;

        Shader sh =
            Shader.Find("Universal Render Pipeline/Particles/Unlit") ??
            Shader.Find("Universal Render Pipeline/Unlit") ??
            Shader.Find("Unlit/Transparent") ??
            Shader.Find("Sprites/Default");

        if (sh == null)
            return null;

        defaultTrailSharedMaterial = new Material(sh);

        if (defaultTrailSharedMaterial.HasProperty("_BaseColor"))
        {
            defaultTrailSharedMaterial.SetColor(
                "_BaseColor",
                new Color(1f, 0.78f, 0.25f, 0.7f));
            if (defaultTrailSharedMaterial.HasProperty("_Surface"))
                defaultTrailSharedMaterial.SetFloat("_Surface", 1f);
        }
        else if (defaultTrailSharedMaterial.HasProperty("_Color"))
        {
            defaultTrailSharedMaterial.SetColor("_Color", new Color(1f, 0.78f, 0.25f, 0.7f));
        }

        defaultTrailSharedMaterial.renderQueue = 3000;
        defaultTrailSharedMaterial.enableInstancing = false;
        return defaultTrailSharedMaterial;
    }

    private void Update()
    {
        if (worldVelocity.sqrMagnitude < 1e-8f)
            worldVelocity = transform.forward * speed;

        Vector3 delta = worldVelocity * Time.deltaTime;

        if (rigidBody != null)
            rigidBody.MovePosition(rigidBody.position + delta);
        else
            transform.position += delta;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ignoresPlayerCollider && other.CompareTag("Player"))
            return;

        Health target = other.GetComponent<Health>()
            ?? other.GetComponentInParent<Health>();

        if (target != null)
            target.TakeDamage(damage);

        Destroy(gameObject);
    }
}
