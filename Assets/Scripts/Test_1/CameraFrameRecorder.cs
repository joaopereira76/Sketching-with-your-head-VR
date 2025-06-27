using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Collections;

public class CameraFrameRecorder : MonoBehaviour
{
    [SerializeField] private Camera secondaryCamera;
    [SerializeField] private string saveFolder = "CapturedFrames";
    [SerializeField] private int frameWidth = 1920;
    [SerializeField] private int frameHeight = 1080;
    [SerializeField] private KeyCode startCaptureKey = KeyCode.P;
    [SerializeField] private KeyCode stopCaptureAndGenerateKey = KeyCode.M;

    private RenderTexture renderTexture;
    private int frameCount = 0;
    private Coroutine captureCoroutine;

    void Start()
    {
        EnsureFolderExists();
        renderTexture = new RenderTexture(frameWidth, frameHeight, 24);
        secondaryCamera.targetTexture = renderTexture;
        print($"[CameraFrameRecorder] Initialized. Frames will be saved in: {saveFolder}");
    }

    void Update()
    {
        if (Input.GetKeyDown(startCaptureKey))
        {
            if (captureCoroutine == null)
            {
                print("[CameraFrameRecorder] Started automatic capturing.");
                captureCoroutine = StartCoroutine(CaptureFramesEverySecond());
            }
        }
        else if (Input.GetKeyDown(stopCaptureAndGenerateKey))
        {
            if (captureCoroutine != null)
            {
                print("[CameraFrameRecorder] Stopping capture and generating video.");
                StopCoroutine(captureCoroutine);
                captureCoroutine = null;
                StartCoroutine(GenerateVideo());
            }
        }
    }

    IEnumerator CaptureFramesEverySecond()
    {
        while (true)
        {
            CaptureFrame();
            yield return new WaitForSeconds(1f); // Capture every 1 second
        }
    }

    void CaptureFrame()
    {
        EnsureFolderExists();

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D frameTexture = new Texture2D(frameWidth, frameHeight, TextureFormat.RGB24, false);
        frameTexture.ReadPixels(new Rect(0, 0, frameWidth, frameHeight), 0, 0);
        frameTexture.Apply();

        RenderTexture.active = currentRT;

        string filePath = Path.Combine(saveFolder, $"frame_{frameCount:D4}.png");
        File.WriteAllBytes(filePath, frameTexture.EncodeToPNG());

        Destroy(frameTexture);

        print($"[CameraFrameRecorder] Captured frame {frameCount} -> {filePath}");
        frameCount++;
    }

    IEnumerator GenerateVideo()
    {
        print("[CameraFrameRecorder] Starting video generation...");

        string videoPath = Path.Combine(saveFolder, "output.mp4");
        string ffmpegCommand = $"-framerate 1 -i {saveFolder}/frame_%04d.png -c:v libx264 -pix_fmt yuv420p {videoPath}";

        yield return StartCoroutine(RunFFmpeg(ffmpegCommand));

        print($"[CameraFrameRecorder] Video saved to: {videoPath}");
    }

    IEnumerator RunFFmpeg(string arguments)
    {
        print($"[CameraFrameRecorder] Running FFmpeg: ffmpeg {arguments}");

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = new Process { StartInfo = psi })
        {
            process.Start();
            yield return new WaitUntil(() => process.HasExited);

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(output))
                print("[FFmpeg Output] " + output);
            if (!string.IsNullOrEmpty(error))
                print("[FFmpeg Error] " + error);
        }
    }

    void EnsureFolderExists()
    {
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
            print($"[CameraFrameRecorder] Created folder: {saveFolder}");
        }
    }
}
