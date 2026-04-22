using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public GameObject projectilePrefab;
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

        Quaternion shootRotation = firePoint.rotation;
        if (cam != null)
            shootRotation = Quaternion.LookRotation(cam.forward);

        Instantiate(projectilePrefab, firePoint.position, shootRotation);
    }
}