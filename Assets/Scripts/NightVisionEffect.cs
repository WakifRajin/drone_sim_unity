using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Night vision camera effect for URP - Camera-specific.
/// Only applies to the camera this component is attached to.
/// Automatically enables/disables with camera.
/// </summary>
[RequireComponent(typeof(Camera))]
public class NightVisionEffect : MonoBehaviour
{
    [Header("Night Vision Settings")]
    [Tooltip("Enable night vision on this camera")]
    public bool nightVisionEnabled = true;
    [Tooltip("Toggle key (optional - set to None to disable toggle)")]
    public KeyCode toggleKey = KeyCode.None;
    
    [Header("Effect Parameters")]
    public Color nightVisionTint = new Color(0f, 1f, 0.3f, 1f);
    [Range(0f, 5f)]
    public float brightness = 2.5f;
    [Range(0f, 2f)]
    public float contrast = 1.2f;
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.4f;
    [Range(0f, 0.5f)]
    public float scanLineIntensity = 0.1f;
    [Range(0f, 0.1f)]
    public float noiseAmount = 0.05f;
    
    [Header("URP Volume (Auto-created)")]
    public Volume postProcessVolume;
    
    private Camera cam;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private Texture2D vignetteTexture;
    private Texture2D noiseTexture;
    private bool wasCameraEnabled;

    void Start()
    {
        cam = GetComponent<Camera>();
        wasCameraEnabled = cam.enabled;
        
        SetupPostProcessing();
        CreateVignetteTexture();
        CreateNoiseTexture();
        
        // Apply initial state
        UpdateNightVision();
    }

    void Update()
    {
        // Optional toggle
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
        {
            nightVisionEnabled = !nightVisionEnabled;
            UpdateNightVision();
            Debug.Log($"[{gameObject.name}] Night Vision: {(nightVisionEnabled ? "ON" : "OFF")}");
        }
        
        // Check if camera was enabled/disabled
        if (cam.enabled != wasCameraEnabled)
        {
            wasCameraEnabled = cam.enabled;
            UpdateNightVision();
        }
    }

    void OnEnable()
    {
        UpdateNightVision();
    }

    void OnDisable()
    {
        // Disable volume when component is disabled
        if (postProcessVolume != null)
        {
            postProcessVolume.weight = 0f;
        }
    }

    void SetupPostProcessing()
    {
        // Create a camera-specific volume
        if (postProcessVolume == null)
        {
            GameObject volumeObj = new GameObject($"{gameObject.name}_NightVision_Volume");
            volumeObj.transform.SetParent(transform);
            volumeObj.transform.localPosition = Vector3.zero;
            
            postProcessVolume = volumeObj.AddComponent<Volume>();
            postProcessVolume.isGlobal = false; // Local to this camera
            postProcessVolume.priority = 10; // Higher priority
            
            // Create new profile
            postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }

        // Get or add effects
        if (postProcessVolume.profile != null)
        {
            if (!postProcessVolume.profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = postProcessVolume.profile.Add<ColorAdjustments>();
            }
            
            if (!postProcessVolume.profile.TryGet(out vignette))
            {
                vignette = postProcessVolume.profile.Add<Vignette>();
            }
        }
        
        // Configure the volume layer (optional - for more control)
        // This ensures only this camera sees the effect
        postProcessVolume.weight = 0f;
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
                vignette = Mathf.Pow(vignette, 1.5f);
                
                vignetteTexture.SetPixel(x, y, new Color(1f, 1f, 1f, vignette));
            }
        }
        
        vignetteTexture.Apply();
    }

    void CreateNoiseTexture()
    {
        int size = 128;
        noiseTexture = new Texture2D(size, size, TextureFormat.RGB24, false);
        noiseTexture.filterMode = FilterMode.Point;
        noiseTexture.wrapMode = TextureWrapMode.Repeat;
        
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            float value = Random.value;
            pixels[i] = new Color(value, value, value);
        }
        
        noiseTexture.SetPixels(pixels);
        noiseTexture.Apply();
    }

    void UpdateNightVision()
    {
        if (postProcessVolume == null) return;

        // Only apply if camera is enabled AND night vision is enabled
        bool shouldBeActive = cam.enabled && nightVisionEnabled && enabled;

        if (shouldBeActive)
        {
            // Enable volume
            postProcessVolume.weight = 1f;
            
            // Configure color adjustments
            if (colorAdjustments != null)
            {
                colorAdjustments.active = true;
                colorAdjustments.postExposure.value = brightness;
                colorAdjustments.contrast.value = (contrast - 1f) * 100f;
                colorAdjustments.saturation.value = -100f; // Full desaturation
            }
            
            // Configure vignette
            if (vignette != null)
            {
                vignette.active = true;
                vignette.intensity.value = vignetteIntensity;
                vignette.smoothness.value = 0.4f;
                vignette.color.value = Color.black;
            }
        }
        else
        {
            // Disable volume
            postProcessVolume.weight = 0f;
            
            if (colorAdjustments != null) colorAdjustments.active = false;
            if (vignette != null) vignette.active = false;
        }
    }

    void OnGUI()
    {
        // Only render GUI if THIS camera is active
        if (!cam.enabled || !nightVisionEnabled) return;
        
        // Check if this is the active camera (basic check)
        if (Camera.current != cam) return;
        
        // Draw green tint overlay
        GUI.color = new Color(nightVisionTint.r, nightVisionTint.g, nightVisionTint.b, 0.15f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        
        // Draw noise
        if (noiseAmount > 0)
        {
            GUI.color = new Color(1, 1, 1, noiseAmount);
            float noiseOffset = Time.time * 2f;
            GUI.DrawTextureWithTexCoords(
                new Rect(0, 0, Screen.width, Screen.height),
                noiseTexture,
                new Rect(noiseOffset, noiseOffset, Screen.width / 100f, Screen.height / 100f)
            );
        }
        
        // Draw scan lines
        if (scanLineIntensity > 0)
        {
            GUI.color = new Color(0, 0, 0, scanLineIntensity);
            int lineCount = 100;
            float lineHeight = Screen.height / (float)lineCount;
            
            for (int i = 0; i < lineCount; i += 2)
            {
                float y = i * lineHeight + (Time.time * 50f) % (lineHeight * 2);
                GUI.DrawTexture(new Rect(0, y, Screen.width, 1), Texture2D.whiteTexture);
            }
        }
        
        // Reset color
        GUI.color = Color.white;
        
        // Draw border
        GUI.color = nightVisionTint;
        int borderSize = 3;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, borderSize), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height - borderSize, Screen.width, borderSize), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, 0, borderSize, Screen.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - borderSize, 0, borderSize, Screen.height), Texture2D.whiteTexture);
        
        // Draw HUD
        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = nightVisionTint;
        style.alignment = TextAnchor.UpperLeft;
        style.padding = new RectOffset(15, 10, 15, 10);
        
        GUI.Label(new Rect(0, 0, 300, 40), "◉ NIGHT VISION", style);
        
        GUI.color = Color.white;
    }

    /// <summary>
    /// Programmatically enable/disable night vision
    /// </summary>
    public void SetNightVision(bool enabled)
    {
        nightVisionEnabled = enabled;
        UpdateNightVision();
    }

    /// <summary>
    /// Toggle night vision on/off
    /// </summary>
    public void ToggleNightVision()
    {
        nightVisionEnabled = !nightVisionEnabled;
        UpdateNightVision();
    }

    void OnDestroy()
    {
        if (vignetteTexture != null)
        {
            Destroy(vignetteTexture);
        }
        
        if (noiseTexture != null)
        {
            Destroy(noiseTexture);
        }
        
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            Destroy(postProcessVolume.profile);
        }
    }
}