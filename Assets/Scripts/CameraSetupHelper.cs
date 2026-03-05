using UnityEngine;

/// <summary>
/// Helper component to mark cameras with descriptive names for the camera switcher.
/// Attach this to each camera GameObject.
/// </summary>
public class CameraSetupHelper : MonoBehaviour
{
    [Header("Camera Info")]
    [Tooltip("Display name for this camera view")]
    public string cameraDisplayName = "Camera View";
    
    [Header("Camera Behavior")]
    [Tooltip("Camera script components that should be enabled/disabled with this camera")]
    public MonoBehaviour[] cameraScripts;
    
    private Camera cam;
    private NightVisionEffect nightVision;
    private bool wasEnabled;

    void Start()
    {
        cam = GetComponent<Camera>();
        nightVision = GetComponent<NightVisionEffect>();
        
        // Rename GameObject to match display name for easier identification
        if (!string.IsNullOrEmpty(cameraDisplayName))
        {
            gameObject.name = cameraDisplayName;
        }
    }

    void Update()
    {
        // Enable/disable camera scripts based on camera state
        if (cam != null && cam.enabled != wasEnabled)
        {
            wasEnabled = cam.enabled;
            
            foreach (MonoBehaviour script in cameraScripts)
            {
                if (script != null)
                {
                    script.enabled = cam.enabled;
                }
            }
            
            // Update night vision effect when camera switches
            if (nightVision != null)
            {
                nightVision.enabled = cam.enabled;
            }
        }
    }
}