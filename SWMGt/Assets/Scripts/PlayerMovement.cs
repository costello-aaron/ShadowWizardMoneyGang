using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 100f;

    public Transform playerCamera;
    float xRotation = 0f;
    private float baseMoveSpeed;
    private Coroutine speedBoostRoutine;

    void Awake()
    {
        baseMoveSpeed = moveSpeed;
    }

    void Update()
    {
        Move();
        Look();
    }

    void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        transform.position += move * moveSpeed * Time.deltaTime;
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (multiplier <= 0f || duration <= 0f)
            return;

        if (speedBoostRoutine != null)
            StopCoroutine(speedBoostRoutine);

        speedBoostRoutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    System.Collections.IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        moveSpeed = baseMoveSpeed * multiplier;
        yield return new WaitForSeconds(duration);
        moveSpeed = baseMoveSpeed;
        speedBoostRoutine = null;
    }

    /// <summary>
    /// Instantly moves the player (and resets vertical look) — used after room-door transitions.
    /// </summary>
    public void WarpTo(Transform destination)
    {
        if (destination == null)
            return;

        transform.SetPositionAndRotation(destination.position, destination.rotation);
        xRotation = 0f;
        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.identity;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Physics.SyncTransforms();
    }
}