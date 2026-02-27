using UnityEngine;
using TMPro;
using Unity.Robotics.ROSTCPConnector;

public class RosConnectionSettings : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_InputField portInputField;

    [Header("Defaults")]
    [SerializeField] private string defaultIP = "172.26.249.253";
    [SerializeField] private int defaultPort = 10000;

    private const string IP_PREF_KEY = "ROS_IP";
    private const string PORT_PREF_KEY = "ROS_PORT";

    private ROSConnection rosConnection;

    private void Awake()
    {
        rosConnection = ROSConnection.GetOrCreateInstance();
        LoadAndApplySettings();
    }

    private void LoadAndApplySettings()
    {
        string ip = PlayerPrefs.GetString(IP_PREF_KEY, defaultIP);
        int port = PlayerPrefs.GetInt(PORT_PREF_KEY, defaultPort);

        // Update UI
        if (ipInputField != null)
            ipInputField.text = ip;

        if (portInputField != null)
            portInputField.text = port.ToString();

        // Apply to ROS connection
        rosConnection.RosIPAddress = ip;
        rosConnection.RosPort = port;
    }

    // Called by "Save / Apply" button
    public void SaveAndApply()
    {
        string ip = ipInputField.text.Trim();
        int port;

        if (!int.TryParse(portInputField.text, out port))
        {
            Debug.LogError("Invalid ROS port.");
            return;
        }

        PlayerPrefs.SetString(IP_PREF_KEY, ip);
        PlayerPrefs.SetInt(PORT_PREF_KEY, port);
        PlayerPrefs.Save();

        rosConnection.RosIPAddress = ip;
        rosConnection.RosPort = port;

        Debug.Log($"ROS connection updated: {ip}:{port}");
    }
}