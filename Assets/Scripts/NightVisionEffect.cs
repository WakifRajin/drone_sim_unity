using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Night vision camera effect for URP - Camera-specific.
///Debbugging version with detailed logs.
/// </summary>
[RequireComponent(typeof(Camera))]
public class NightVisionEffect : MonoBehaviour
{
    [Header("Night Vision Settings")]
    [Tooltip("Enable night vision on this camera")]
    public bool nightVisionEnabled = true;
    [Tooltip("Toggle key (optional - set to None to disable toggle)")]
    public KeyCode toggleKey = KeyCode.N;
    
    [Header("Effect Parameters")]
    public Color nightVisionTint = new Color(0f, 1f, 0.3f, 1f);
    [Range(0f, 5f)]
    public float brightness = 2.5f;
    [Range(0f, 2f)]
    public float contrast = 1.2f;
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.8f;
    [Range(0f, 0.5f)]
    public float scanLineIntensity = 0.2f;
    [Range(0f, 0.3f)]
    public float noiseAmount = 0.1f;
    [Range(0f, 1f)]
    public float greenOverlayAlpha = 0.3f;
    
    [Header("URP Volume (Auto-created)")]
    public Volume postProcessVolume;
    
    [Header("Debug Info")]
    public bool showDebugInfo = true;
    
    private Camera cam;
    private UniversalAdditionalCameraData cameraData;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private Bloom bloom;
    private Texture2D noiseTexture;
    private bool wasCameraEnabled;
    private GUIStyle debugStyle;

    void Start()
    {
        cam = GetComponent<Camera>();
        cameraData = cam.GetUniversalAdditionalCameraData();
        wasCameraEnabled = cam.enabled;
        
        // CRITICAL: Enable post processing on this camera
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = true;
            Debug.Log($"[NightVision] Post Processing enabled on {gameObject.name}");
        }
        else
        {
            Debug.LogError($"[NightVision] UniversalAdditionalCameraData not found on {gameObject.name}!");
        }
        
        SetupPostProcessing();
        CreateNoiseTexture();
        
        // Setup debug style
        debugStyle = new GUIStyle();
        debugStyle.fontSize = 14;
        debugStyle.normal.textColor = Color.yellow;
        debugStyle.padding = new RectOffset(10, 10, 10, 10);
        
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
            Debug.Log($"[{gameObject.name}] Camera enabled: {cam.enabled}");
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
            postProcessVolume.isGlobal = true; // Changed to global for testing
            postProcessVolume.priority = 100; // Very high priority
            postProcessVolume.weight = 0f;
            
            // Create new profile
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = $"{gameObject.name}_NightVisionProfile";
            postProcessVolume.profile = profile;
            
            Debug.Log($"[NightVision] Created volume for {gameObject.name}");
        }

        // Get or add effects
        if (postProcessVolume.profile != null)
        {
            if (!postProcessVolume.profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = postProcessVolume.profile.Add<ColorAdjustments>(true);
                Debug.Log("[NightVision] Added ColorAdjustments");
            }
            
            if (!postProcessVolume.profile.TryGet(out vignette))
            {
                vignette = postProcessVolume.profile.Add<Vignette>(true);
                Debug.Log("[NightVision] Added Vignette");
            }
            
            if (!postProcessVolume.profile.TryGet(out bloom))
            {
                bloom = postProcessVolume.profile.Add<Bloom>(true);
                Debug.Log("[NightVision] Added Bloom");
            }
        }
        else
        {
            Debug.LogError("[NightVision] Volume profile is null!");
        }
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
        if (postProcessVolume == null)
        {
            Debug.LogError("[NightVision] Post process volume is null!");
            return;
        }

        // Only apply if camera is enabled AND night vision is enabled
        bool shouldBeActive = cam.enabled && nightVisionEnabled && enabled;

        Debug.Log($"[NightVision] UpdateNightVision - Camera: {cam.enabled}, NV: {nightVisionEnabled}, Component: {enabled}, Should be active: {shouldBeActive}");

        if (shouldBeActive)
        {
            // Enable volume
            postProcessVolume.weight = 1f;
            postProcessVolume.enabled = true;
            
            // Configure color adjustments
            if (colorAdjustments != null)
            {
                colorAdjustments.active = true;
                
                // Post exposure (brightness)
                colorAdjustments.postExposure.overrideState = true;
                colorAdjustments.postExposure.value = brightness;
                
                // Contrast
                colorAdjustments.contrast.overrideState = true;
                colorAdjustments.contrast.value = (contrast - 1f) * 100f;
                
                // Saturation (desaturate)
                colorAdjustments.saturation.overrideState = true;
                colorAdjustments.saturation.value = -100f;
                
                // Color filter (green tint)
                colorAdjustments.colorFilter.overrideState = true;
                colorAdjustments.colorFilter.value = nightVisionTint;
                
                Debug.Log($"[NightVision] ColorAdjustments applied - Exposure: {brightness}, Saturation: -100");
            }
            else
            {
                Debug.LogError("[NightVision] ColorAdjustments is null!");
            }
            
            // Configure vignette
            if (vignette != null)
            {
                vignette.active = true;
                
                vignette.intensity.overrideState = true;
                vignette.intensity.value = vignetteIntensity;
                
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = 0.4f;
                
                vignette.color.overrideState = true;
                vignette.color.value = Color.black;
                
                Debug.Log($"[NightVision] Vignette applied - Intensity: {vignetteIntensity}");
            }
            
            // Configure bloom (for glow effect)
            if (bloom != null)
            {
                bloom.active = true;
                
                bloom.intensity.overrideState = true;
                bloom.intensity.value = 0.5f;
                
                bloom.threshold.overrideState = true;
                bloom.threshold.value = 0.5f;
                
                bloom.tint.overrideState = true;
                bloom.tint.value = nightVisionTint;
            }
        }
        else
        {
            // Disable volume
            postProcessVolume.weight = 0f;
            
            if (colorAdjustments != null) colorAdjustments.active = false;
            if (vignette != null) vignette.active = false;
            if (bloom != null) bloom.active = false;
            
            Debug.Log("[NightVision] Effects disabled");
        }
    }

    void OnGUI()
    {
        // Only render GUI if THIS camera is active
        if (!cam.enabled || !nightVisionEnabled) return;
        
        // Draw green overlay - ALWAYS visible for testing
        GUI.color = new Color(nightVisionTint.r, nightVisionTint.g, nightVisionTint.b, greenOverlayAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        
        // Draw noise
        if (noiseAmount > 0 && noiseTexture != null)
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
        int borderSize = 5;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, borderSize), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height - borderSize, Screen.width, borderSize), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, 0, borderSize, Screen.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - borderSize, 0, borderSize, Screen.height), Texture2D.whiteTexture);
        
        // Draw HUD
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = nightVisionTint;
        style.alignment = TextAnchor.UpperLeft;
        style.padding = new RectOffset(20, 10, 20, 10);
        
        GUI.Label(new Rect(0, 0, 400, 50), "◉ NIGHT VISION ACTIVE", style);
        
        // Debug info
        if (showDebugInfo)
        {
            string debugText = $"Camera: {gameObject.name}\n" +
                             $"Camera Enabled: {cam.enabled}\n" +
                             $"NV Enabled: {nightVisionEnabled}\n" +
                             $"Volume Weight: {(postProcessVolume != null ? postProcessVolume.weight : 0)}\n" +
                             $"Post Processing: {(cameraData != null ? cameraData.renderPostProcessing : false)}";
            
            GUI.Label(new Rect(10, 60, 400, 150), debugText, debugStyle);
        }
        
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