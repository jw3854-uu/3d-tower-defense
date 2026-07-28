using UnityEngine;

public enum PitchDetectionMethod { Autocorrelation, Cepstrum }

// One instance per speaker (each player calls their own tracker to get their own
// live pitch) — reads from the single shared MicCaptureManager mic stream rather
// than opening its own Microphone session, so it doesn't fight Vosk for the device.
public class PitchTracker : MonoBehaviour
{
    [Header("Method")]
    public PitchDetectionMethod method = PitchDetectionMethod.Cepstrum;

    [Header("Pitch Detection (shared)")]
    public float minFreq = 80f;
    public float maxFreq = 600f;

    [Header("Autocorrelation Settings")]
    [Range(0.05f, 0.5f)]
    public float clarityThreshold = 0.2f;
    [Tooltip("Center clipping level (fraction of peak amplitude).")]
    [Range(0.1f, 0.8f)]
    public float clipLevel = 0.4f;

    [Header("Cepstrum Settings")]
    [Tooltip("FFT frame size. Must be power of 2. 512 = 32ms at 16kHz.")]
    public int cepstrumFrameSize = 512;
    [Tooltip("Minimum cepstrum peak height to count as real pitch.")]
    [Range(0.01f, 0.3f)]
    public float cepstrumPeakThreshold = 0.04f;
    [Tooltip("RMS below this triggers automatic noise floor learning.")]
    public float noiseLearnRMS = 0.008f;
    public bool enableDenoise = true;

    [Header("Analysis")]
    public int windowSamples = 2048;
    public float intervalSec = 0.05f;

    [Header("Smoothing")]
    [Tooltip("Number of recent frames to median-filter over.")]
    public int medianWindow = 5;

    public float Pitch { get; private set; }
    public float CurrentRMS { get; private set; }
    public bool IsTracking { get; private set; }
    public bool NoiseCalibrated => _cepstrum != null && _cepstrum.NoiseReady;

    // Derived from the shared mic clip so analysis always matches its actual sample rate.
    public int sampleRate => MicCaptureManager.Instance != null ? MicCaptureManager.Instance.SampleRate : 16000;

    public float[] RawBuffer => _rawBuffer;

    float _nextAnalysis;
    float[] _rawBuffer;

    float[] _pitchHistory;
    int _historyIdx;
    CepstrumAnalyzer _cepstrum;

    int _pitchFrames;
    int _silentFrames;
    float _nextReport;

    public void StartTracking()
    {
        if (IsTracking) StopTracking();

        if (MicCaptureManager.Instance == null)
        {
            Debug.LogError("[PitchTracker] MicCaptureManager.Instance is null — the shared mic capture must exist before tracking starts.");
            return;
        }

        _rawBuffer = new float[windowSamples];

        _pitchHistory = new float[medianWindow];
        _historyIdx = 0;

        _pitchFrames = 0;
        _silentFrames = 0;
        _nextReport = Time.time + 5f;

        if (method == PitchDetectionMethod.Cepstrum)
            _cepstrum = new CepstrumAnalyzer(cepstrumFrameSize, sampleRate, minFreq, maxFreq);

        IsTracking = true;
        Pitch = 0f;
        CurrentRMS = 0f;
    }

    public void StopTracking()
    {
        if (!IsTracking) return;
        IsTracking = false;
        Pitch = 0f;
        CurrentRMS = 0f;
    }

    void OnDisable() => StopTracking();

    void Update()
    {
        if (!IsTracking) return;
        if (Time.time < _nextAnalysis) return;
        _nextAnalysis = Time.time + intervalSec;

        var mic = MicCaptureManager.Instance;
        if (mic == null || mic.Clip == null) return;

        int micPos = mic.GetPosition();
        if (micPos < windowSamples) return;

        int start = micPos - windowSamples;
        if (start < 0) start += mic.Clip.samples;
        mic.Clip.GetData(_rawBuffer, start);

        float rms = 0f;
        for (int i = 0; i < _rawBuffer.Length; i++)
            rms += _rawBuffer[i] * _rawBuffer[i];
        CurrentRMS = Mathf.Sqrt(rms / _rawBuffer.Length);

        float pitch;
        if (method == PitchDetectionMethod.Cepstrum)
        {
            if (CurrentRMS < noiseLearnRMS && _cepstrum != null)
                _cepstrum.LearnNoise(_rawBuffer,
                    Mathf.Max(0, _rawBuffer.Length - cepstrumFrameSize));

            var cr = _cepstrum.AnalyzeBuffer(_rawBuffer, cepstrumPeakThreshold, enableDenoise);
            pitch = cr.Pitch;
        }
        else
        {
            pitch = PitchDetector.Detect(
                _rawBuffer, sampleRate, minFreq, maxFreq, clarityThreshold, clipLevel);
        }

        _pitchHistory[_historyIdx] = pitch;
        _historyIdx = (_historyIdx + 1) % medianWindow;
        Pitch = Median(_pitchHistory);

        if (Pitch > 0f) _pitchFrames++;
        else _silentFrames++;

        if (Time.time >= _nextReport)
        {
            string methodTag = method == PitchDetectionMethod.Cepstrum
                ? $"Cepstrum (noise={(NoiseCalibrated ? "ready" : "learning")})"
                : "Autocorrelation";
            Debug.Log($"[PitchTracker:{methodTag}] Last 5s: detected={_pitchFrames} frames, silent={_silentFrames} frames, current={Pitch:F1} Hz");
            _pitchFrames = 0;
            _silentFrames = 0;
            _nextReport = Time.time + 5f;
        }
    }

    static float Median(float[] values)
    {
        int count = 0;
        for (int i = 0; i < values.Length; i++)
            if (values[i] > 0f) count++;
        if (count == 0) return 0f;

        float[] sorted = new float[count];
        int idx = 0;
        for (int i = 0; i < values.Length; i++)
            if (values[i] > 0f) sorted[idx++] = values[i];
        System.Array.Sort(sorted);
        return sorted[count / 2];
    }
}
