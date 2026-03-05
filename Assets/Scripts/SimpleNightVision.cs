using UnityEngine;

/// <summary>
/// Simple night vision effect that doesn't require a custom shader.
/// Uses Unity's built-in rendering features.
/// </summary>
[RequireComponent(typeof(Camera))]
public class SimpleNightVision : MonoBehaviour
{
    [Header("Night Vision Settings")]
    public bool nightVisionEnabled = false;
    public KeyCode toggleKey = KeyCode.N;
    
    [Header("Effect Parameters")]
    [Tooltip("Tint color for night vision (green recommended)")]
    public Color nightVisionTint = new Color(0f, 1f, 0.3f, 1f);
    [Range(1f, 5f)]
    [Tooltip("Brightness multiplier")]
    public float brightness = 2.5f;
    [Range(0f, 1f)]
    [Tooltip("Vignette darkness")]
    public float vignetteStrength = 0.5f;
    
    [Header("Audio Feedback")]
    public AudioClip toggleSound;
    
    private Camera cam;
    private AudioSource audioSource;
    private Material vignettelMaterial;
    private Texture2D vignetteTexture;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;
        
        CreateVignetteTexture();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            nightVisionEnabled = !nightVisionEnabled;
            Debug.Log($"Night Vision: {(nightVisionEnabled ? "ON" : "OFF")}");
            
            if (audioSource != null && toggleSound != null)
            {
                audioSource.PlayOneShot(toggleSound);
            }
        }
    }

    void CreateVignetteTexture()
    {
        int size = 512;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float vignette = 1f - Mathf.Clamp01(dist / maxDist);
                vignette = Mathf.Pow(vignette, 2f);
                
                vignetteTexture.SetPixel(x, y, new Color(1f, 1f, 1f, vignette));
            }
        }
        
        vignetteTexture.Apply();
    }

    void OnGUI()
    {
        if (!nightVisionEnabled) return;
        
        // Draw the tinted overlay
        Color tintedColor = nightVisionTint * brightness;
        GUI.color = new Color(tintedColor.r, tintedColor.g, tintedColor.b, 0.3f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        
        // Draw vignette
        if (vignetteStrength > 0)
        {
            GUI.color = new Color(0, 0, 0, vignetteStrength);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture);
        }
        
        // Draw night vision border
        GUI.color = nightVisionTint;
        int borderSize = 5;
        // Top
        GUI.DrawTexture(new Rect(0, 0, Screen.width, borderSize), Texture2D.whiteTexture);
        // Bottom
        GUI.DrawTexture(new Rect(0, Screen.height - borderSize, Screen.width, borderSize), Texture2D.whiteTexture);
        // Left
        GUI.DrawTexture(new Rect(0, 0, borderSize, Screen.height), Texture2D.whiteTexture);
        // Right
        GUI.DrawTexture(new Rect(Screen.width - borderSize, 0, borderSize, Screen.height), Texture2D.whiteTexture);
        
        // Reset GUI color
        GUI.color = Color.white;
        
        // Draw HUD text
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = nightVisionTint;
        style.alignment = TextAnchor.UpperLeft;
        style.padding = new RectOffset(10, 10, 10, 10);
        
        GUI.Label(new Rect(10, 10, 200, 30), "NIGHT VISION ACTIVE", style);
    }

    void OnDestroy()
    {
        if (vignetteTexture != null)
        {
            Destroy(vignetteTexture);
        }
    }
}