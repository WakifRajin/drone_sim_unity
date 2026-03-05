using UnityEngine;

/// <summary>
/// Adds headlights to the drone for night flying.
/// Attach to drone and configure lights.
/// </summary>
public class DroneHeadlight : MonoBehaviour
{
    [Header("Main Headlight")]
    [Tooltip("Forward-facing spotlight")]
    public Light headlight;
    [Tooltip("Headlight intensity")]
    [Range(0f, 10f)]
    public float headlightIntensity = 5f;
    [Tooltip("Headlight range")]
    public float headlightRange = 50f;
    [Tooltip("Spotlight angle")]
    [Range(10f, 120f)]
    public float spotAngle = 60f;
    [Tooltip("Headlight color")]
    public Color headlightColor = Color.white;
    
    [Header("Navigation Lights")]
    [Tooltip("Red light (left side)")]
    public Light leftNavLight;
    [Tooltip("Green light (right side)")]
    public Light rightNavLight;
    [Tooltip("White strobe (rear)")]
    public Light rearStrobe;
    [Tooltip("Navigation light intensity")]
    [Range(0f, 5f)]
    public float navLightIntensity = 1f;
    
    [Header("Strobe Settings")]
    [Tooltip("Enable strobe effect")]
    public bool strobeEnabled = true;
    [Tooltip("Strobe flash rate (flashes per second)")]
    public float strobeRate = 2f;
    
    [Header("Controls")]
    [Tooltip("Key to toggle headlight")]
    public KeyCode toggleKey = KeyCode.L;
    [Tooltip("Headlight on by default")]
    public bool headlightOn = true;
    
    private float strobeTimer = 0f;
    private bool strobeState = false;

    void Start()
    {
        SetupLights();
    }

    void Update()
    {
        // Toggle headlight
        if (Input.GetKeyDown(toggleKey))
        {
            headlightOn = !headlightOn;
            if (headlight != null)
            {
                headlight.enabled = headlightOn;
            }
            Debug.Log($"Headlight: {(headlightOn ? "ON" : "OFF")}");
        }
        
        // Update strobe
        if (strobeEnabled && rearStrobe != null)
        {
            strobeTimer += Time.deltaTime;
            if (strobeTimer >= 1f / strobeRate)
            {
                strobeTimer = 0f;
                strobeState = !strobeState;
                rearStrobe.enabled = strobeState;
            }
        }
    }

    private void SetupLights()
    {
        // Setup main headlight
        if (headlight == null)
        {
            GameObject headlightObj = new GameObject("Headlight");
            headlightObj.transform.SetParent(transform);
            headlightObj.transform.localPosition = new Vector3(0f, -0.2f, 0.5f);
            headlightObj.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            
            headlight = headlightObj.AddComponent<Light>();
        }
        
        headlight.type = LightType.Spot;
        headlight.intensity = headlightIntensity;
        headlight.range = headlightRange;
        headlight.spotAngle = spotAngle;
        headlight.color = headlightColor;
        headlight.shadows = LightShadows.Soft;
        headlight.enabled = headlightOn;
        
        // Setup navigation lights
        SetupNavLight(ref leftNavLight, "Left Nav Light", new Vector3(-0.3f, 0f, 0f), Color.red);
        SetupNavLight(ref rightNavLight, "Right Nav Light", new Vector3(0.3f, 0f, 0f), Color.green);
        SetupNavLight(ref rearStrobe, "Rear Strobe", new Vector3(0f, 0f, -0.3f), Color.white);
    }

    private void SetupNavLight(ref Light light, string name, Vector3 localPos, Color color)
    {
        if (light == null)
        {
            GameObject lightObj = new GameObject(name);
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = localPos;
            lightObj.transform.localRotation = Quaternion.identity;
            
            light = lightObj.AddComponent<Light>();
        }
        
        light.type = LightType.Point;
        light.intensity = navLightIntensity;
        light.range = 5f;
        light.color = color;
        light.shadows = LightShadows.None;
    }

    /// <summary>
    /// Set headlight state
    /// </summary>
    public void SetHeadlight(bool state)
    {
        headlightOn = state;
        if (headlight != null)
        {
            headlight.enabled = state;
        }
    }
}