using System;
using System.Linq;
using UnityEngine;
using Vosk;

public class VoskRecognitionManager : MonoBehaviour
{
    public static VoskRecognitionManager Instance { get; private set; }

    [Header("Model")]
    [Tooltip("Path relative to StreamingAssets, e.g. 'vosk-model-small-en-us-0.15'")]
    [SerializeField] string modelPath = "vosk-model-small-en-us-0.15";

    [Header("Grammar")]
    [Tooltip("Used only to constrain Vosk's recognition vocabulary. Toy matching itself happens in ToyCatalog.")]
    [SerializeField] ToyCatalog catalog;

    Model _model; 
    string _grammar;
    int _listenStartPos;
    bool _isListening;

    public event Action<string> OnTranscribed;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Vosk.Vosk.SetLogLevel(0);
        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, modelPath);
        Debug.Log($"[Vosk] Loading model from: {fullPath}");
        _model = new Model(fullPath);
        Debug.Log("[Vosk] Model loaded OK");

        _grammar = BuildGrammar();
    }

    void OnDestroy()
    {
        if (Instance == this)
            _model?.Dispose();
    }

    public void BeginListening()
    {
        if (_isListening) return;
        _listenStartPos = MicCaptureManager.Instance.GetPosition();
        _isListening = true;
    }

    public void EndListeningAndRecognize()
    {
        if (!_isListening) return;
        _isListening = false;

        float[] samples = MicCaptureManager.Instance.GetSamplesSince(_listenStartPos);
        if (samples.Length == 0)
        {
            Debug.LogWarning("[Vosk] Nothing recorded.");
            return;
        }

        short[] pcm = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
            pcm[i] = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);

        var recognizer = new VoskRecognizer(_model, MicCaptureManager.Instance.SampleRate, _grammar);
        recognizer.AcceptWaveform(pcm, pcm.Length);
        string json = recognizer.FinalResult();
        recognizer.Dispose();

        string text = ParseText(json);
        if (text != null)
            OnTranscribed?.Invoke(text);
    }

    string BuildGrammar()
    {
        if (catalog == null) return null; // open-vocabulary recognition if no catalog assigned

        var words = catalog.GetAllTriggerWords().Distinct().Select(w => $"\"{w}\"").ToList();
        words.Add("\"[unk]\"");
        return "[" + string.Join(", ", words) + "]";
    }

    static string ParseText(string json)
    {
        int idx = json.IndexOf("\"text\"", StringComparison.Ordinal);
        if (idx < 0) return null;
        int colon = json.IndexOf(':', idx);
        if (colon < 0) return null;
        int qStart = json.IndexOf('"', colon + 1);
        if (qStart < 0) return null;
        int qEnd = json.IndexOf('"', qStart + 1);
        if (qEnd < 0) return null;
        string text = json.Substring(qStart + 1, qEnd - qStart - 1).Trim();
        return text.Length > 0 ? text : null;
    }
}
