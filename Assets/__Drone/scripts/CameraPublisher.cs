using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;

[RequireComponent(typeof(Camera))]
public class CameraPublisher : MonoBehaviour
{
    private ROSConnection ros;
    private Camera droneCamera;

    [Header("ROS Topic")]
    public string imageTopic = "drone/image_raw";

    [Header("Publish Settings")]
    public int width = 640;
    public int height = 480;
    public int fps = 15;

    private float timer = 0f;
    private float publishInterval;

    private RenderTexture renderTexture;
    private Texture2D texture;
    private byte[] imageDataBuffer;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        droneCamera = GetComponent<Camera>();
        publishInterval = 1f / fps;
        ros.RegisterPublisher<ImageMsg>(imageTopic);

        renderTexture = new RenderTexture(width, height, 24);
        texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        imageDataBuffer = new byte[width * height * 3];
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= publishInterval)
        {
            timer = 0f;
            PublishCameraImage();
        }
    }

    void PublishCameraImage()
    {
        // Render the camera view into the reusable render texture
        droneCamera.targetTexture = renderTexture;
        droneCamera.Render();
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();

        // Copy to pre-allocated buffer using NativeArray overload (avoids intermediate byte[] allocation)
        texture.GetRawTextureData<byte>().CopyTo(imageDataBuffer);

        // Cleanup
        droneCamera.targetTexture = null;
        RenderTexture.active = null;

        // Build Image message
        ImageMsg msg = new ImageMsg
        {
            header = new HeaderMsg
            {
                frame_id = "drone_camera",
                stamp = new TimeMsg
                {
                    sec = (int)Time.time,
                    nanosec = (uint)((Time.time - (int)Time.time) * 1e9)
                }
            },
            height = (uint)height,
            width = (uint)width,
            encoding = "rgb8",
            step = (uint)(width * 3), // 3 bytes per pixel (RGB)
            data = imageDataBuffer
        };

        ros.Publish(imageTopic, msg);
    }

    void OnDestroy()
    {
        if (renderTexture != null) Destroy(renderTexture);
        if (texture != null) Destroy(texture);
    }
}