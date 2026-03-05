using UnityEngine;

/// <summary>
/// Manages night scene lighting and atmosphere.
/// Creates realistic low-light conditions with moon, stars, and ambient lighting.
/// </summary>
public class NightSceneManager : MonoBehaviour
{
    [Header("Directional Light (Moon)")]
    [Tooltip("Main directional light representing moonlight")]
    public Light moonLight;
    [Tooltip("Moon light intensity")]
    [Range(0f, 1f)]
    public float moonIntensity = 0.1f;
    [Tooltip("Moon color (cool blue-ish)")]
    public Color moonColor = new Color(0.6f, 0.7f, 1f, 1f);
    [Tooltip("Moon angle (rotation)")]
    public Vector3 moonRotation = new Vector3(50f, -30f, 0f);
    
    [Header("Ambient Lighting")]
    [Tooltip("Skybox ambient intensity")]
    [Range(0f, 2f)]
    public float ambientIntensity = 0.2f;
    [Tooltip("Ambient sky color")]
    public Color ambientSkyColor = new Color(0.1f, 0.1f, 0.15f, 1f);
    [Tooltip("Ambient equator color")]
    public Color ambientEquatorColor = new Color(0.05f, 0.05f, 0.08f, 1f);
    [Tooltip("Ambient ground color")]
    public Color ambientGroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
    
    [Header("Fog Settings")]
    [Tooltip("Enable fog for atmosphere")]
    public bool useFog = true;
    [Tooltip("Fog color (dark blue/gray)")]
    public Color fogColor = new Color(0.05f, 0.05f, 0.1f, 1f);
    [Tooltip("Fog mode")]
    public FogMode fogMode = FogMode.ExponentialSquared;
    [Tooltip("Fog density (for exponential mode)")]
    [Range(0f, 0.1f)]
    public float fogDensity = 0.01f;
    [Tooltip("Fog start distance (for linear mode)")]
    public float fogStart = 10f;
    [Tooltip("Fog end distance (for linear mode)")]
    public float fogEnd = 100f;
    
    [Header("Skybox")]
    [Tooltip("Night skybox material (with stars)")]
    public Material nightSkybox;
    [Tooltip("Skybox exposure")]
    [Range(0f, 2f)]
    public float skyboxExposure = 0.3f;
    
    [Header("Post Processing")]
    [Tooltip("Enable bloom for lights")]
    public bool useBloom = true;
    [Tooltip("Enable color grading")]
    public bool useColorGrading = true;
    [Tooltip("Overall scene brightness adjustment")]
    [Range(0f, 2f)]
    public float exposureAdjustment = 0.8f;
    
    [Header("Time of Day")]
    [Tooltip("Simulate time progression")]
    public bool simulateTimeProgression = false;
    [Tooltip("Current time (0-24 hours)")]
    [Range(0f, 24f)]
    public float currentTime = 22f; // 10 PM
    [Tooltip("Time speed multiplier")]
    public float timeSpeed = 1f;

    void Start()
    {
        ApplyNightSettings();
    }

    void Update()
    {
        if (simulateTimeProgression)
        {
            UpdateTimeOfDay();
        }
        
        // Toggle fog with F key
        if (Input.GetKeyDown(KeyCode.F))
        {
            useFog = !useFog;
            RenderSettings.fog = useFog;
            Debug.Log($"Fog: {useFog}");
        }
    }

    [ContextMenu("Apply Night Settings")]
    public void ApplyNightSettings()
    {
        SetupMoonLight();
        SetupAmbientLighting();
        SetupFog();
        SetupSkybox();
        SetupPostProcessing();
    }

    private void SetupMoonLight()
    {
        if (moonLight == null)
        {
            // Try to find existing directional light
            moonLight = FindAnyObjectByType<Light>();
            
            if (moonLight == null)
            {
                // Create new directional light
                GameObject moonObj = new GameObject("Moon Light");
                moonLight = moonObj.AddComponent<Light>();
                moonLight.type = LightType.Directional;
            }
        }
        
        moonLight.type = LightType.Directional;
        moonLight.intensity = moonIntensity;
        moonLight.color = moonColor;
        moonLight.transform.rotation = Quaternion.Euler(moonRotation);
        moonLight.shadows = LightShadows.Soft;
        moonLight.shadowStrength = 0.5f;
    }

    private void SetupAmbientLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.ambientIntensity = ambientIntensity;
    }

    private void SetupFog()
    {
        RenderSettings.fog = useFog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        
        if (fogMode == FogMode.Linear)
        {
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }
        else
        {
            RenderSettings.fogDensity = fogDensity;
        }
    }

    private void SetupSkybox()
    {
        if (nightSkybox != null)
        {
            RenderSettings.skybox = nightSkybox;
        }
        
        if (RenderSettings.skybox != null)
        {
            RenderSettings.skybox.SetFloat("_Exposure", skyboxExposure);
        }
        
        DynamicGI.UpdateEnvironment();
    }

    private void SetupPostProcessing()
    {
        // This requires Unity Post Processing Stack v2
        // You'll need to set this up manually or via Volume component
        Debug.Log("Configure Post Processing manually: Bloom, Color Grading, Vignette");
    }

    private void UpdateTimeOfDay()
    {
        currentTime += Time.deltaTime * timeSpeed / 3600f; // Convert to hours
        
        if (currentTime >= 24f)
        {
            currentTime -= 24f;
        }
        
        // Update moon position based on time
        float normalizedTime = currentTime / 24f;
        float angle = normalizedTime * 360f - 90f; // -90 to start at midnight
        
        moonRotation.x = Mathf.Sin(angle * Mathf.Deg2Rad) * 70f + 20f;
        moonRotation.y = angle;
        
        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(moonRotation);
            
            // Adjust intensity based on time (darker at midnight, lighter at dusk/dawn)
            if (currentTime >= 18f && currentTime <= 24f) // 6 PM to midnight
            {
                float t = (currentTime - 18f) / 6f;
                moonIntensity = Mathf.Lerp(0.3f, 0.1f, t);
            }
            else if (currentTime >= 0f && currentTime <= 6f) // Midnight to 6 AM
            {
                float t = currentTime / 6f;
                moonIntensity = Mathf.Lerp(0.1f, 0.3f, t);
            }
            
            moonLight.intensity = moonIntensity;
        }
    }

    /// <summary>
    /// Quickly switch between day and night
    /// </summary>
    public void ToggleDayNight()
    {
        if (currentTime >= 6f && currentTime <= 18f)
        {
            // Switch to night
            SetTimeOfDay(22f);
        }
        else
        {
            // Switch to day
            SetTimeOfDay(12f);
        }
    }

    /// <summary>
    /// Set specific time of day
    /// </summary>
    public void SetTimeOfDay(float hour)
    {
        currentTime = Mathf.Clamp(hour, 0f, 24f);
        UpdateTimeOfDay();
        ApplyNightSettings();
    }
}