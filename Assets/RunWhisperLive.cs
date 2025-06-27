using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.Text;
using Unity.Collections;


public class RunWhisperLive : MonoBehaviour
{
    public ModelAsset audioDecoder1, audioDecoder2;
    public ModelAsset audioEncoder;
    public ModelAsset logMelSpectro;
    public TextAsset jsonFile;
    public PainterRaycaster2 painterScript;
    public List<UniColorPicker.TapPositionDetector> colorPickerTap;

    Worker decoder1, decoder2, encoder, spectrogram, argmax;
    Tensor<float> encodedAudio;
    Tensor<float> audioInput;
    Tensor<int> tokensTensor;
    Tensor<int> lastTokenTensor;
    NativeArray<int> lastToken;
    NativeArray<int> outputTokens;

    string[] tokens;
    string outputString = "";
    bool transcribing = false;

    const int maxTokens = 100;
    const int END_OF_TEXT = 50257;
    const int START_OF_TRANSCRIPT = 50258;
    const int ENGLISH = 50259;
    const int TRANSCRIBE = 50359;
    const int NO_TIME_STAMPS = 50363;
    const int sampleRate = 16000;
    const int chunkDurationSec = 3;
    const int modelInputSamples = 480000; // 30 seconds * 16kHz

    AudioClip micClip;
    string micDevice;
    int lastSamplePosition = 0;
    float[] micBuffer;

    int tokenCount = 0;

    void Start()
    {
        SetupTokenizer();
        SetupWorkers();

        micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        if (string.IsNullOrEmpty(micDevice))
        {
            Debug.LogError("No microphone found.");
            return;
        }

        micClip = Microphone.Start(micDevice, true, 30, sampleRate);
        micBuffer = new float[sampleRate * chunkDurationSec];
        Debug.Log("Microphone started: " + micDevice);
    }

    void Update()
    {
        if (!Microphone.IsRecording(micDevice)) return;

        int currentPosition = Microphone.GetPosition(micDevice);
        int sampleDelta = currentPosition - lastSamplePosition;
        if (sampleDelta < 0) sampleDelta += micClip.samples;

        float sum = 0f;
        for (int i = 0; i < micBuffer.Length; i++) sum += Mathf.Abs(micBuffer[i]);
        

        if (sampleDelta >= micBuffer.Length)
        {
            micClip.GetData(micBuffer, lastSamplePosition);
            lastSamplePosition = currentPosition;
            ProcessAudioChunk(micBuffer);
        }
    }

    void ProcessAudioChunk(float[] chunk)
    {
        if (transcribing) return;
        transcribing = true;

        float[] padded = new float[modelInputSamples];
        System.Array.Copy(chunk, padded, chunk.Length);

        audioInput?.Dispose();
        audioInput = new Tensor<float>(new TensorShape(1, modelInputSamples), padded);

        spectrogram.Schedule(audioInput);
        var logmel = spectrogram.PeekOutput() as Tensor<float>;

        encoder.Schedule(logmel);
        encodedAudio = encoder.PeekOutput() as Tensor<float>;

        SetupInitialTokens();
        tokensTensor = new Tensor<int>(new TensorShape(1, maxTokens));
        ComputeTensorData.Pin(tokensTensor);
        tokensTensor.Reshape(new TensorShape(1, tokenCount));
        tokensTensor.dataOnBackend.Upload<int>(outputTokens, tokenCount);

        lastToken = new NativeArray<int>(1, Allocator.Persistent); lastToken[0] = NO_TIME_STAMPS;
        lastTokenTensor = new Tensor<int>(new TensorShape(1, 1), new[] { NO_TIME_STAMPS });

        StartCoroutine(RunInferenceLoop());
    }

    System.Collections.IEnumerator RunInferenceLoop()
    {
        while (tokenCount < maxTokens)
        {
            yield return InferenceStep();

            if (!transcribing) break;
        }

        

        painterScript.ReceiveCommand(outputString);

        foreach (UniColorPicker.TapPositionDetector tap in colorPickerTap)
        {
            tap.ReceiveCommand(outputString);
        }

        outputString = "";
        transcribing = false;
    }

    System.Collections.IEnumerator InferenceStep()
    {
        decoder1.SetInput("input_ids", tokensTensor);
        decoder1.SetInput("encoder_hidden_states", encodedAudio);
        decoder1.Schedule();
        yield return null;

        var pkv = new Dictionary<string, Tensor<float>>();
        for (int i = 0; i < 4; i++)
        {
            pkv[$"decoder{i}k"] = decoder1.PeekOutput($"present.{i}.decoder.key") as Tensor<float>;
            pkv[$"decoder{i}v"] = decoder1.PeekOutput($"present.{i}.decoder.value") as Tensor<float>;
            pkv[$"encoder{i}k"] = decoder1.PeekOutput($"present.{i}.encoder.key") as Tensor<float>;
            pkv[$"encoder{i}v"] = decoder1.PeekOutput($"present.{i}.encoder.value") as Tensor<float>;
        }

        decoder2.SetInput("input_ids", lastTokenTensor);
        for (int i = 0; i < 4; i++)
        {
            decoder2.SetInput($"past_key_values.{i}.decoder.key", pkv[$"decoder{i}k"]);
            decoder2.SetInput($"past_key_values.{i}.decoder.value", pkv[$"decoder{i}v"]);
            decoder2.SetInput($"past_key_values.{i}.encoder.key", pkv[$"encoder{i}k"]);
            decoder2.SetInput($"past_key_values.{i}.encoder.value", pkv[$"encoder{i}v"]);
        }

        decoder2.Schedule();
        yield return null;

        var logits = decoder2.PeekOutput("logits") as Tensor<float>;
        argmax.Schedule(logits);
        yield return null;

        using var t_Token = argmax.PeekOutput().ReadbackAndClone() as Tensor<int>;
        int index = t_Token[0];

        outputTokens[tokenCount] = lastToken[0];
        lastToken[0] = index;
        tokenCount++;

        tokensTensor.Reshape(new TensorShape(1, tokenCount));
        tokensTensor.dataOnBackend.Upload(outputTokens, tokenCount);
        lastTokenTensor.dataOnBackend.Upload(lastToken, 1);

        if (index == END_OF_TEXT)
        {
            transcribing = false;
        }
        else if (index < tokens.Length)
        {
            outputString += DecodeToken(tokens[index]);
        }
    }

    void SetupTokenizer()
    {
        var vocab = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonFile.text);
        tokens = new string[vocab.Count];
        foreach (var kvp in vocab)
            tokens[kvp.Value] = kvp.Key;
    }

    void SetupWorkers()
    {
        decoder1 = new Worker(ModelLoader.Load(audioDecoder1), BackendType.GPUCompute);
        decoder2 = new Worker(ModelLoader.Load(audioDecoder2), BackendType.GPUCompute);

        var graph = new FunctionalGraph();
        var input = graph.AddInput(DataType.Float, new DynamicTensorShape(1, 1, 51865));
        var amax = Functional.ArgMax(input, -1, false);
        argmax = new Worker(graph.Compile(amax), BackendType.GPUCompute);

        encoder = new Worker(ModelLoader.Load(audioEncoder), BackendType.GPUCompute);
        spectrogram = new Worker(ModelLoader.Load(logMelSpectro), BackendType.GPUCompute);
    }

    void SetupInitialTokens()
    {
        outputTokens.Dispose();
        outputTokens = new NativeArray<int>(maxTokens, Allocator.Persistent);

        outputTokens[0] = START_OF_TRANSCRIPT;
        outputTokens[1] = ENGLISH;
        outputTokens[2] = TRANSCRIBE;
        tokenCount = 3;
    }

    string DecodeToken(string text)
    {
        byte[] bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(text);
        return Encoding.UTF8.GetString(bytes);
    }

    void OnDestroy()
    {
        decoder1?.Dispose();
        decoder2?.Dispose();
        encoder?.Dispose();
        spectrogram?.Dispose();
        argmax?.Dispose();

        audioInput?.Dispose();
        lastTokenTensor?.Dispose();
        tokensTensor?.Dispose();
        lastToken.Dispose();
        outputTokens.Dispose();
    }
}
