using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages multiple cameras and cycles through them with a key press.
/// Press V to switch between cameras.
/// </summary>
public class CameraSwitcher : MonoBehaviour
{
    [Header("Camera Setup")]
    [Tooltip("List of all cameras to cycle through")]
    public List<Camera> cameras = new List<Camera>();
    
    [Tooltip("Which camera index to start with (0 = first camera)")]
    public int startingCameraIndex = 0;
    
    [Header("Controls")]
    [Tooltip("Key to cycle through cameras")]
    public KeyCode switchKey = KeyCode.V;
    
    [Header("Audio (Optional)")]
    [Tooltip("Sound to play when switching cameras")]
    public AudioClip switchSound;
    
    [Header("UI (Optional)")]
    [Tooltip("Display camera name on screen when switching")]
    public bool showCameraName = true;
    [Tooltip("How long to display camera name (seconds)")]
    public float displayDuration = 2f;
    
    // Internal state
    private int currentCameraIndex;
    private AudioSource audioSource;
    private string currentCameraName = "";
    private float displayTimer = 0f;
    
    // GUI Style
    private GUIStyle labelStyle;

    void Start()
    {
        // Validate cameras list
        if (cameras.Count == 0)
        {
            Debug.LogError("CameraSwitcher: No cameras assigned! Please add cameras to the list.");
            enabled = false;
            return;
        }
        
        // Remove null entries
        cameras.RemoveAll(cam => cam == null);
        
        if (cameras.Count == 0)
        {
            Debug.LogError("CameraSwitcher: All camera references are null!");
            enabled = false;
            return;
        }
        
        // Clamp starting index
        startingCameraIndex = Mathf.Clamp(startingCameraIndex, 0, cameras.Count - 1);
        currentCameraIndex = startingCameraIndex;
        
        // Setup audio source for switch sound
        if (switchSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = switchSound;
        }
        
        // Initialize - disable all cameras except starting one
        ActivateCamera(currentCameraIndex);
        
        // Setup GUI style
        labelStyle = new GUIStyle();
        labelStyle.fontSize = 24;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = Color.white;
        labelStyle.alignment = TextAnchor.UpperCenter;
    }

    void Update()
    {
        // Check for camera switch input
        if (Input.GetKeyDown(switchKey))
        {
            SwitchToNextCamera();
        }
        
        // Update display timer
        if (displayTimer > 0f)
        {
            displayTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// Switch to the next camera in the list
    /// </summary>
    public void SwitchToNextCamera()
    {
        // Increment index and wrap around
        currentCameraIndex++;
        if (currentCameraIndex >= cameras.Count)
        {
            currentCameraIndex = 0;
        }
        
        ActivateCamera(currentCameraIndex);
        
        // Play sound effect
        if (audioSource != null && switchSound != null)
        {
            audioSource.PlayOneShot(switchSound);
        }
        
        // Show camera name
        if (showCameraName)
        {
            currentCameraName = cameras[currentCameraIndex].name;
            displayTimer = displayDuration;
            Debug.Log($"Switched to: {currentCameraName}");
        }
    }
    
    /// <summary>
    /// Switch to the previous camera in the list
    /// </summary>
    public void SwitchToPreviousCamera()
    {
        // Decrement index and wrap around
        currentCameraIndex--;
        if (currentCameraIndex < 0)
        {
            currentCameraIndex = cameras.Count - 1;
        }
        
        ActivateCamera(currentCameraIndex);
        
        // Play sound effect
        if (audioSource != null && switchSound != null)
        {
            audioSource.PlayOneShot(switchSound);
        }
        
        // Show camera name
        if (showCameraName)
        {
            currentCameraName = cameras[currentCameraIndex].name;
            displayTimer = displayDuration;
            Debug.Log($"Switched to: {currentCameraName}");
        }
    }

    /// <summary>
    /// Switch to a specific camera by index
    /// </summary>
    public void SwitchToCamera(int index)
    {
        if (index < 0 || index >= cameras.Count)
        {
            Debug.LogWarning($"CameraSwitcher: Invalid camera index {index}");
            return;
        }
        
        currentCameraIndex = index;
        ActivateCamera(currentCameraIndex);
        
        if (showCameraName)
        {
            currentCameraName = cameras[currentCameraIndex].name;
            displayTimer = displayDuration;
        }
    }

    /// <summary>
    /// Switch to a specific camera by name
    /// </summary>
    public void SwitchToCamera(string cameraName)
    {
        int index = cameras.FindIndex(cam => cam.name == cameraName);
        if (index >= 0)
        {
            SwitchToCamera(index);
        }
        else
        {
            Debug.LogWarning($"CameraSwitcher: Camera '{cameraName}' not found in list");
        }
    }

    private void ActivateCamera(int index)
    {
        // Disable all cameras
        foreach (Camera cam in cameras)
        {
            if (cam != null)
            {
                cam.enabled = false;
                
                // Also disable AudioListener if present
                AudioListener listener = cam.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = false;
                }
            }
        }
        
        // Enable selected camera
        if (cameras[index] != null)
        {
            cameras[index].enabled = true;
            
            // Enable AudioListener if present
            AudioListener listener = cameras[index].GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = true;
            }
        }
    }

    /// <summary>
    /// Get the currently active camera
    /// </summary>
    public Camera GetActiveCamera()
    {
        if (currentCameraIndex >= 0 && currentCameraIndex < cameras.Count)
        {
            return cameras[currentCameraIndex];
        }
        return null;
    }

    /// <summary>
    /// Get the index of the currently active camera
    /// </summary>
    public int GetActiveCameraIndex()
    {
        return currentCameraIndex;
    }

    // Display camera name on screen
    void OnGUI()
    {
        if (showCameraName && displayTimer > 0f)
        {
            // Create a background box
            float alpha = Mathf.Clamp01(displayTimer / 0.5f); // Fade out in last 0.5 seconds
            
            // Camera name label
            Color textColor = Color.white;
            textColor.a = alpha;
            labelStyle.normal.textColor = textColor;
            
            // Position at top center of screen
            Rect labelRect = new Rect(0, 30, Screen.width, 50);
            
            // Draw shadow for better readability
            Color shadowColor = Color.black;
            shadowColor.a = alpha * 0.8f;
            labelStyle.normal.textColor = shadowColor;
            GUI.Label(new Rect(labelRect.x + 2, labelRect.y + 2, labelRect.width, labelRect.height), 
                     currentCameraName, labelStyle);
            
            // Draw main text
            labelStyle.normal.textColor = textColor;
            GUI.Label(labelRect, currentCameraName, labelStyle);
        }
    }
}