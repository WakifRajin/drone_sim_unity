using UnityEngine;
using Unity.Robotics.ROSTCPConnector;

[DefaultExecutionOrder(-1000)]
public class RosConnectionBootstrap : MonoBehaviour
{
    private const string IP_PREF_KEY = "ROS_IP";
    private const string PORT_PREF_KEY = "ROS_PORT";

    [SerializeField] private string fallbackIP = "127.0.0.1";
    [SerializeField] private int fallbackPort = 10000;

    private void Awake()
    {
        ROSConnection ros = FindFirstObjectByType<ROSConnection>();

        if (ros == null)
        {
            Debug.LogError("ROSConnection not found in scene.");
            return;
        }

        string ip = PlayerPrefs.GetString(IP_PREF_KEY, fallbackIP);
        int port = PlayerPrefs.GetInt(PORT_PREF_KEY, fallbackPort);

        ros.RosIPAddress = ip;
        ros.RosPort = port;

        Debug.Log($"[ROS Bootstrap] Applied {ip}:{port}");
    }
}