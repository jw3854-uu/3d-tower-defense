using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaybackPitchTracker : MonoBehaviour
{
    [Header("Pitch Detection")]
    public float minFreqHz = 80f;
    public float maxFreqHz = 600f;
    [Range(0.05f, 0.5f)]
    public float clarityThreshold = 0.2f;

    public int windowSamples = 2048;
    public float intervalSec = 0.05f;

    public float CurrentPitch { get; private set; }

    AudioSource _source;
    float _nextAnalysis;
    float[] _buffer;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _buffer = new float[windowSamples];
    }

    void Update()
    {
        if (_source.clip == null || !_source.isPlaying)
        {
            CurrentPitch = 0f;
            return;
        }
        if (Time.time < _nextAnalysis) return;
        _nextAnalysis = Time.time + intervalSec;

        int clipSamples = _source.clip.samples;
        int channels = _source.clip.channels;
        int pos = _source.timeSamples;

        if (channels == 1)
        {
            int start = pos - windowSamples;
            if (start < 0) start += clipSamples;
            _source.clip.GetData(_buffer, start);
        }
        else
        {
            int rawLen = windowSamples * channels;
            float[] raw = new float[rawLen];
            int start = pos - windowSamples;
            if (start < 0) start += clipSamples;
            _source.clip.GetData(raw, start);

            for (int i = 0; i < windowSamples; i++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++)
                    sum += raw[i * channels + c];
                _buffer[i] = sum / channels;
            }
        }

        CurrentPitch = PitchDetector.Detect(
            _buffer, _source.clip.frequency, minFreqHz, maxFreqHz, clarityThreshold);
    }
}
