using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen red edge vignette when the player takes damage. Creates its own overlay canvas (DontDestroyOnLoad).
/// </summary>
public class DamageVignetteUI : MonoBehaviour
{
    public static DamageVignetteUI Instance { get; private set; }

    [SerializeField] private int textureSize = 512;
    [SerializeField] private float innerRadius = 0.35f;
    [SerializeField] private float outerRadius = 0.92f;
    [SerializeField] private float maxCornerAlpha = 0.75f;

    [Header("Pulse")]
    [SerializeField] private float flashPerDamage = 0.12f;
    [SerializeField] private float flashCap = 0.95f;
    [SerializeField] private float decayDuration = 0.55f;

    private CanvasGroup canvasGroup;
    private float intensity;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        Health.OnPlayerDamaged += OnPlayerDamaged;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Health.OnPlayerDamaged -= OnPlayerDamaged;
        if (Instance == this)
            Instance = null;
    }

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("DamageVignetteUI");
        go.AddComponent<DamageVignetteUI>();
    }

    void BuildOverlay()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Keep your HUD / inventory Canvas sorting order above this (e.g. 100+).
        canvas.sortingOrder = 40;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject imageGo = new GameObject("Vignette");
        imageGo.transform.SetParent(transform, false);
        RectTransform rect = imageGo.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageGo.AddComponent<Image>();
        image.raycastTarget = false;
        image.sprite = BuildVignetteSprite();
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
    }

    Sprite BuildVignetteSprite()
    {
        int w = Mathf.Max(32, textureSize);
        int h = w;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float inv = 1f / (w - 1);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = x * inv;
                float v = y * inv;
                float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) / 0.7071f;
                float a = Mathf.InverseLerp(innerRadius, outerRadius, d);
                a = Mathf.SmoothStep(0f, 1f, a) * maxCornerAlpha;
                tex.SetPixel(x, y, new Color(1f, 0.08f, 0.08f, a));
            }
        }

        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    void OnPlayerDamaged(float amount)
    {
        if (amount <= 0f)
            return;

        float add = flashPerDamage * amount;
        intensity = Mathf.Min(flashCap, intensity + Mathf.Max(add, flashPerDamage * 1.5f));
    }

    void Update()
    {
        if (canvasGroup == null)
            return;

        float decay = flashCap / Mathf.Max(0.05f, decayDuration);
        intensity = Mathf.MoveTowards(intensity, 0f, decay * Time.unscaledDeltaTime);
        canvasGroup.alpha = intensity;
    }
}
