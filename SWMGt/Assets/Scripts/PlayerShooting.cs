using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public GameObject projectilePrefab;

    [Tooltip("Optional FX at the barrel (e.g. Synty FX_Gunshot_01). Cleans up automatically.")]
    [SerializeField]
    GameObject muzzleFlashPrefab;

    public Transform firePoint;
    public Transform playerCamera;
    public float fireRate = 0.3f;

    private float nextFireTime = 0f;

    void Update()
    {
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        Transform cam = playerCamera;
        if (cam == null && Camera.main != null)
            cam = Camera.main.transform;

        Vector3 bore = firePoint.rotation * Vector3.forward;
        if (cam != null)
            bore = cam.forward;

        bore = bore.sqrMagnitude > 1e-8f ? bore.normalized : transform.forward;

        SpawnMuzzleFlash(bore);

        AudioManager.EnsureExists().PlayGunshot();

        Vector3 planarCarrier = ComputePlanarCarrierVelocity();

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position,
            Quaternion.LookRotation(bore));

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
            proj.Launch(bore, planarCarrier);
    }

    void SpawnMuzzleFlash(Vector3 boreDirection)
    {
        if (muzzleFlashPrefab == null)
            return;

        GameObject fx =
            Instantiate(muzzleFlashPrefab, firePoint.position, Quaternion.LookRotation(boreDirection));

        if (!fx.TryGetComponent<MuzzleFlashBurst>(out _))
            fx.AddComponent<MuzzleFlashBurst>();
    }

    Vector3 ComputePlanarCarrierVelocity()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        PlayerMovement mover = GetComponent<PlayerMovement>();
        float cap = mover != null ? mover.moveSpeed : 0f;

        Vector3 planar = transform.right * h + transform.forward * v;
        planar.y = 0f;
        return planar * cap;
    }
}