using UnityEngine;

/// <summary>
/// Night vision camera effect for low-light conditions.
/// Attach to camera. Press N to toggle.
/// Requires NightVisionShader.shader file in project.
/// </summary>
[RequireComponent(typeof(Camera))]
public class NightVisionEffect : MonoBehaviour
{
    [Header("Night Vision Settings")]
    public bool nightVisionEnabled = false;
    public KeyCode toggleKey = KeyCode.N;
    
    [Header("Effect Parameters")]
    public Color nightVisionTint = new Color(0f, 1f, 0.3f, 1f);
    [Range(0f, 5f)]
    public float brightness = 2.5f;
    [Range(0f, 1f)]
    public float contrast = 0.8f;
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.4f;
    [Range(0f, 0.1f)]
    public float noiseAmount = 0.02f;
    
    [Header("Scan Lines")]
    public bool useScanLines = true;
    [Range(0f, 500f)]
    public float scanLineCount = 200f;
    [Range(0f, 1f)]
    public float scanLineIntensity = 0.1f;
    
    private Material nightVisionMaterial;
    private Camera cam;
    private Texture2D noiseTexture;

    void Start()
    {
        cam = GetComponent<Camera>();
        CreateNightVisionMaterial();
        CreateNoiseTexture();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            nightVisionEnabled = !nightVisionEnabled;
            Debug.Log($"Night Vision: {(nightVisionEnabled ? "ON" : "OFF")}");
        }
    }

    void CreateNightVisionMaterial()
    {
        // Try to find the night vision shader
        Shader shader = Shader.Find("Hidden/NightVisionSimple");
        
        if (shader == null)
        {
            Debug.LogError("NightVisionEffect: Shader 'Hidden/NightVisionSimple' not found! Please create NightVisionShader.shader file.");
            enabled = false;
            return;
        }
        
        nightVisionMaterial = new Material(shader);
        nightVisionMaterial.hideFlags = HideFlags.HideAndDontSave;
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

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (nightVisionEnabled && nightVisionMaterial != null)
        {
            // Set shader parameters
            nightVisionMaterial.SetColor("_Tint", nightVisionTint);
            nightVisionMaterial.SetFloat("_Brightness", brightness);
            nightVisionMaterial.SetFloat("_Contrast", contrast);
            nightVisionMaterial.SetFloat("_Vignette", vignetteIntensity);
            nightVisionMaterial.SetFloat("_NoiseAmount", noiseAmount);
            nightVisionMaterial.SetFloat("_ScanLines", scanLineCount);
            nightVisionMaterial.SetFloat("_ScanLineIntensity", useScanLines ? scanLineIntensity : 0f);
            nightVisionMaterial.SetFloat("_Time", Time.time);
            nightVisionMaterial.SetTexture("_NoiseTex", noiseTexture);
            
            // Apply the effect
            Graphics.Blit(source, destination, nightVisionMaterial);
        }
        else
        {
            // No effect, just copy source to destination
            Graphics.Blit(source, destination);
        }
    }

    void OnDestroy()
    {
        if (nightVisionMaterial != null)
        {
            DestroyImmediate(nightVisionMaterial);
        }
        
        if (noiseTexture != null)
        {
            DestroyImmediate(noiseTexture);
        }
    }
}