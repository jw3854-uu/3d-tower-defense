using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Vosk;

public class VoskKeywordDetector : MonoBehaviour
{
    [Header("Model")]
    [Tooltip("Path relative to StreamingAssets, e.g. 'vosk-model-small-en-us-0.15'")]
    public string modelPath = "vosk-model-small-en-us-0.15";

    [Header("Microphone")]
    public int sampleRate = 16000;
    public int recordMaxSec = 5;

    [Header("Spells")]
    [Tooltip("Made-up spell phrases the recognizer will listen for.")]
    public string[] spells = new[]
    {
        "bee bee bonk",
        "ba ba bonk",
        "bo bo bonk"
    };

    public event Action<string> OnSpellDetected;

    public bool IsRecording { get; private set; }

    Model _model;
    AudioClip _clip;
    string _micDevice;

    void Awake()
    {
        Vosk.Vosk.SetLogLevel(0);
        string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, modelPath);
        Debug.Log($"[Vosk] Loading model from: {fullPath}");
        _model = new Model(fullPath);
        Debug.Log($"[Vosk] Model loaded OK");
    }

    void OnDestroy()
    {
        StopRecording();
        _model?.Dispose();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.rKey.wasPressedThisFrame && !IsRecording)
            StartRecording();
        if (kb.rKey.wasReleasedThisFrame && IsRecording)
            StopAndRecognize();
    }

    void StartRecording()
    {
        _micDevice = null;
        _clip = Microphone.Start(_micDevice, false, recordMaxSec, sampleRate);
        IsRecording = true;
        Debug.Log("[Vosk] Recording... release R to recognize.");
    }

    void StopRecording()
    {
        if (!IsRecording) return;
        if (Microphone.IsRecording(_micDevice))
            Microphone.End(_micDevice);
        _clip = null;
        IsRecording = false;
    }

    void StopAndRecognize()
    {
        int pos = Microphone.GetPosition(_micDevice);
        Microphone.End(_micDevice);
        IsRecording = false;

        if (_clip == null || pos <= 0)
        {
            Debug.LogWarning("[Vosk] Nothing recorded.");
            return;
        }

        float[] samples = new float[pos];
        _clip.GetData(samples, 0);

        float rms = 0f;
        for (int i = 0; i < samples.Length; i++)
            rms += samples[i] * samples[i];
        rms = Mathf.Sqrt(rms / samples.Length);
        // Debug.Log($"[Vosk] Recorded {pos / (float)sampleRate:F1}s, {pos} samples, RMS={rms:F4}");

        // Convert float [-1,1] to short (PCM16) — most reliable Vosk path
        short[] pcm = new short[pos];
        for (int i = 0; i < pos; i++)
            pcm[i] = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);

        string grammar = BuildGrammar();
        var recognizer = new VoskRecognizer(_model, sampleRate, grammar);

        recognizer.AcceptWaveform(pcm, pcm.Length);
        string json = recognizer.FinalResult();
        recognizer.Dispose();

        string text = ParseText(json);
        if (text != null && IsKnownSpell(text))
        {
            Debug.Log($"[Vosk] SPELL MATCH: \"{text}\"");
            OnSpellDetected?.Invoke(text);
        }
        else
        {
            Debug.Log($"[Vosk] No spell matched (heard: \"{text ?? ""}\")");
        }
    }

    bool IsKnownSpell(string text)
    {
        foreach (var spell in spells)
        {
            if (string.Equals(text, spell, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    string BuildGrammar()
    {
        var parts = new List<string>();
        foreach (var spell in spells)
            parts.Add($"\"{spell}\"");
        parts.Add("\"[unk]\"");
        return "[" + string.Join(", ", parts) + "]";
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
