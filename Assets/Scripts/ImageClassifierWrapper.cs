using System;
using System.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageClassifierWrapper : MonoBehaviour
{
    private AndroidJavaObject javaClass;
    private Texture2D m_LastCameraTexture;
    private string filePath;
    private Texture2D m_Texture;

    public RenderTexture renderTexture;
    public ARCameraBackground m_ARCameraBackground;
    public ARCameraManager cameraManager;
    public string label;

    // Start is called before the first frame update
    void Start()
    {
        javaClass = new AndroidJavaObject("com.frozenwater.imageclassifierplugin.ImageClassifier");
        javaClass.Call("initializeInterpreter", 1);

    }



    unsafe void SaveImageCPU()
    {
        XRCameraImage image;
        if (!cameraManager.TryGetLatestImage(out image))
            return;

        var conversionParams = new XRCameraImageConversionParams
        {
            // Get the entire image.
            inputRect = new RectInt(0, 0, image.width, image.height),

            // Downsample by 2.
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),

            // Choose RGBA format.
            outputFormat = TextureFormat.RGBA32,

            // Flip across the vertical axis (mirror image).
            transformation = CameraImageTransformation.MirrorY
        };

        // See how many bytes you need to store the final image.
        int size = image.GetConvertedDataSize(conversionParams);

        // Allocate a buffer to store the image.
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        // Extract the image data
        image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);

        // The image was converted to RGBA32 format and written into the provided buffer
        // so you can dispose of the XRCameraImage. You must do this or it will leak resources.
        image.Dispose();

        // At this point, you can process the image, pass it to a computer vision algorithm, etc.
        // In this example, you apply it to a texture to visualize it.

        // You've got the data; let's put it into a texture so you can visualize it.
        m_Texture = new Texture2D(
            conversionParams.outputDimensions.x,
            conversionParams.outputDimensions.y,
            conversionParams.outputFormat,
            false);

        m_Texture.LoadRawTextureData(buffer);
        m_Texture.Apply();

        var bytes = m_Texture.EncodeToPNG();
        filePath = Application.persistentDataPath + "/camera_texture.png";
        File.WriteAllBytes(filePath, bytes);

        // Done with your temporary data, so you can dispose it.
        buffer.Dispose();
    }

    private void SaveCameraImage()
    {
        //Copy the camera background to a RenderTexture
        Graphics.Blit(null, renderTexture, m_ARCameraBackground.material);

        if (renderTexture == null)
        {
            print("sad face :(");
        }

        print("are we here?");
        // Copy the RenderTexture from GPU to CPU
        var activeRenderTexture = RenderTexture.active;
        print("there is no active render texture?");

        RenderTexture.active = renderTexture;

        if (m_LastCameraTexture == null)
            m_LastCameraTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, true);

        print("Set up last Camera texture");
        m_LastCameraTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        m_LastCameraTexture.Apply();

        RenderTexture.active = activeRenderTexture;
        print("reset render texture");
        // Write to file
        var bytes = m_LastCameraTexture.EncodeToPNG();
        filePath = Application.persistentDataPath + "/camera_texture.png";
        File.WriteAllBytes(filePath, bytes);

    }

    public string ClassifyCameraImage()
    {
        SaveImageCPU();
        string label = javaClass.Call<string>("callClassifier", filePath);
        print(label);

        //text.text = label.ToString();

        return label;
    }

    private void OnApplicationQuit()
    {
        javaClass.Call("close");
    }

}
