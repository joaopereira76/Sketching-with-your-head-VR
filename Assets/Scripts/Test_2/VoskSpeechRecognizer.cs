using UnityEngine;
using UnityEngine.Android;
using Vosk;
using System.IO;
using System.Collections.Generic;
using System.Collections;

public class VoskSpeechRecognizer : MonoBehaviour
{
    public PainterRaycaster2 painter; // Assign in Inspector
    private VoskRecognizer recognizer;
    private Model model;
    private AudioClip mic;
    private const int SampleRate = 16000;

    void Start()
    {

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
        Vosk.Vosk.SetLogLevel(0);
        string modelPath = Path.Combine(Application.streamingAssetsPath, "models/pt");
        model = new Model(modelPath);
        recognizer = new VoskRecognizer(model, SampleRate);
        StartCoroutine(StartMicrophone());
    }

    IEnumerator StartMicrophone()
    {
        mic = Microphone.Start(null, true, 10, SampleRate);
        yield return new WaitForSeconds(1);
    }

    void Update()
    {
        if (mic == null || recognizer == null) return;

        float[] samples = new float[SampleRate];
        int micPos = Microphone.GetPosition(null);
        if (micPos < 0) return;

        mic.GetData(samples, 0);

        if (recognizer.AcceptWaveform(samples, samples.Length))
        {
            string result = recognizer.Result();
            Debug.Log("Voice Result: " + result);
            
        }
    }

    

    void OnDestroy()
    {
        recognizer?.Dispose();
        model?.Dispose();
    }
}
