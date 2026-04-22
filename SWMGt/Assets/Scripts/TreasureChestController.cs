using UnityEngine;

public class TreasureChestController : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private bool isLocked = true;
    [SerializeField] private bool isOpened = false;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;

    [Header("Visuals")]
    [SerializeField] private Renderer chestRenderer;
    [SerializeField] private Color lockedColor = new Color(0.6f, 0.2f, 0.2f);
    [SerializeField] private Color unlockedColor = new Color(0.2f, 0.7f, 0.2f);
    [SerializeField] private Color openedColor = new Color(0.8f, 0.8f, 0.2f);

    private Material runtimeMaterial;
    private bool playerInRange = false;
    private Collider playerCollider;
    private Transform cachedPlayerTransform;

    void Start()
    {
        EnsureRuntimeMaterial();
        RefreshColor();
    }

    void Update()
    {
        if (isLocked || isOpened)
            return;

        if (!Input.GetKeyDown(KeyCode.E))
            return;

        GameObject playerObject = ResolvePlayerForInteraction();
        if (playerObject != null)
            TryOpenChest(playerObject);
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        RefreshColor();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerInRange = true;
        playerCollider = other;
        cachedPlayerTransform = other.GetComponentInParent<PlayerMovement>()?.transform;
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayerCollider(other))
            return;

        playerInRange = false;
        playerCollider = null;
        cachedPlayerTransform = null;
    }

    void TryOpenChest(GameObject player)
    {
        if (player == null || isLocked || isOpened)
            return;

        isOpened = true;
        GiveRandomPotion(player);
        RefreshColor();
    }

    GameObject ResolvePlayerForInteraction()
    {
        if (playerInRange && playerCollider != null)
        {
            PlayerMovement movement = playerCollider.GetComponentInParent<PlayerMovement>();
            if (movement != null)
                return movement.gameObject;

            if (playerCollider.CompareTag("Player"))
                return playerCollider.gameObject;
        }

        if (cachedPlayerTransform == null)
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
                cachedPlayerTransform = taggedPlayer.transform;
        }

        if (cachedPlayerTransform == null)
            return null;

        float distance = Vector3.Distance(transform.position, cachedPlayerTransform.position);
        if (distance <= interactDistance)
            return cachedPlayerTransform.gameObject;

        return null;
    }

    bool IsPlayerCollider(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag("Player"))
            return true;

        return other.GetComponentInParent<PlayerMovement>() != null;
    }

    void GiveRandomPotion(GameObject player)
    {
        PlayerInventory inv = PlayerInventory.EnsureExists();
        bool giveHealthPotion = Random.value < 0.5f;

        if (giveHealthPotion)
            inv.AddHealthPotions(1);
        else
            inv.AddSpeedPotions(1);
    }

    void EnsureRuntimeMaterial()
    {
        Renderer rend = chestRenderer != null ? chestRenderer : GetComponentInChildren<Renderer>();
        if (rend == null || !Application.isPlaying || !gameObject.scene.IsValid())
            return;

        if (runtimeMaterial == null && rend.sharedMaterial != null)
        {
            runtimeMaterial = new Material(rend.sharedMaterial);
            rend.material = runtimeMaterial;
        }
    }

    void RefreshColor()
    {
        EnsureRuntimeMaterial();

        Material mat = runtimeMaterial;
        if (mat == null)
        {
            Renderer rend = chestRenderer != null ? chestRenderer : GetComponentInChildren<Renderer>();
            if (rend != null)
                mat = rend.sharedMaterial;
        }

        if (mat == null)
            return;

        if (isOpened)
            mat.color = openedColor;
        else if (isLocked)
            mat.color = lockedColor;
        else
            mat.color = unlockedColor;
        Debug.Log($"RefreshColor() color={(isOpened ? openedColor : (isLocked ? lockedColor : unlockedColor))}");
    }
}
