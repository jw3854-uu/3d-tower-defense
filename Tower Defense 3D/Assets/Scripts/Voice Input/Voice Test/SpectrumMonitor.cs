using UnityEngine;

public class SpectrumMonitor : MonoBehaviour
{
    [Header("Source")]
    public PitchTracker pitchTracker;

    [Header("Display")]
    public int textureWidth = 512;
    public int textureHeight = 256;
    public Color backgroundColor = Color.black;
    public Color waveformColor = Color.green;
    public Color spectrumColor = Color.cyan;
    public Color clipLineColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    [Header("Center Clipping Preview")]
    [Range(0.1f, 0.8f)]
    public float clipLevel = 0.4f;
    public bool showClipThreshold = true;

    public float updateInterval = 0.03f;

    Texture2D _texture;
    Color[] _clearPixels;
    float _nextUpdate;

    void Start()
    {
        _texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        _clearPixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < _clearPixels.Length; i++)
            _clearPixels[i] = backgroundColor;
    }

    void OnDestroy()
    {
        if (_texture != null)
            Destroy(_texture);
    }

    void Update()
    {
        if (pitchTracker == null || !pitchTracker.IsTracking) return;
        if (pitchTracker.RawBuffer == null) return;
        if (Time.time < _nextUpdate) return;
        _nextUpdate = Time.time + updateInterval;

        DrawMonitor(pitchTracker.RawBuffer, pitchTracker.sampleRate);
    }

    void DrawMonitor(float[] buffer, int sampleRate)
    {
        _texture.SetPixels(_clearPixels);

        int halfH = textureHeight / 2;

        float peak = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            float abs = Mathf.Abs(buffer[i]);
            if (abs > peak) peak = abs;
        }

        // --- Top half: waveform ---
        if (peak > 0f)
        {
            if (showClipThreshold)
            {
                int clipPixelUp = halfH + Mathf.RoundToInt(clipLevel * (halfH - 1));
                int clipPixelDn = halfH - Mathf.RoundToInt(clipLevel * (halfH - 1));
                clipPixelUp = Mathf.Clamp(clipPixelUp, 0, textureHeight - 1);
                clipPixelDn = Mathf.Clamp(clipPixelDn, 0, textureHeight - 1);
                for (int x = 0; x < textureWidth; x++)
                {
                    _texture.SetPixel(x, clipPixelUp, clipLineColor);
                    _texture.SetPixel(x, clipPixelDn, clipLineColor);
                }
            }

            for (int x = 0; x < textureWidth; x++)
                _texture.SetPixel(x, halfH, new Color(0.3f, 0.3f, 0.3f));

            int prevY = halfH;
            for (int x = 0; x < textureWidth; x++)
            {
                int sampleIdx = (int)((float)x / textureWidth * buffer.Length);
                sampleIdx = Mathf.Clamp(sampleIdx, 0, buffer.Length - 1);

                float norm = buffer[sampleIdx] / peak;
                int y = halfH + Mathf.RoundToInt(norm * (halfH - 2));
                y = Mathf.Clamp(y, 0, textureHeight - 1);

                int yMin = Mathf.Min(prevY, y);
                int yMax = Mathf.Max(prevY, y);
                for (int py = yMin; py <= yMax; py++)
                    _texture.SetPixel(x, py, waveformColor);

                prevY = y;
            }
        }

        // --- Bottom half: frequency spectrum ---
        float[] windowed = new float[buffer.Length];
        for (int i = 0; i < buffer.Length; i++)
        {
            float w = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (buffer.Length - 1)));
            windowed[i] = buffer[i] * w;
        }

        int fftBins = textureWidth;
        float maxDisplayFreq = sampleRate / 2f;
        float[] mags = new float[fftBins];
        float maxMag = 0f;

        int step = Mathf.Max(1, windowed.Length / 512);
        for (int k = 0; k < fftBins; k++)
        {
            float freq = (float)k / fftBins * maxDisplayFreq;
            float omega = 2f * Mathf.PI * freq / sampleRate;

            float re = 0f, im = 0f;
            for (int n = 0; n < windowed.Length; n += step)
            {
                float angle = omega * n;
                re += windowed[n] * Mathf.Cos(angle);
                im -= windowed[n] * Mathf.Sin(angle);
            }
            mags[k] = Mathf.Sqrt(re * re + im * im);
            if (mags[k] > maxMag) maxMag = mags[k];
        }

        if (maxMag > 0f)
        {
            for (int x = 0; x < fftBins && x < textureWidth; x++)
            {
                float normMag = mags[x] / maxMag;
                int barH = Mathf.RoundToInt(normMag * (halfH - 4));
                for (int y = 0; y < barH; y++)
                    _texture.SetPixel(x, y, spectrumColor);
            }

            float[] markers = { 100f, 200f, 300f, 400f, 500f, 1000f, 2000f };
            foreach (float f in markers)
            {
                int mx = Mathf.RoundToInt(f / maxDisplayFreq * fftBins);
                if (mx >= 0 && mx < textureWidth)
                {
                    for (int y = 0; y < halfH; y += 3)
                        _texture.SetPixel(mx, y, new Color(0.5f, 0.5f, 0.5f, 0.5f));
                }
            }
        }

        _texture.Apply();
    }

    void OnGUI()
    {
        if (_texture == null || pitchTracker == null || !pitchTracker.IsTracking) return;

        float scale = 2f;
        float w = textureWidth * scale;
        float h = textureHeight * scale;
        float x = Screen.width - w - 10;
        float y = 10;

        GUI.DrawTexture(new Rect(x, y, w, h), _texture);

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = Color.white }
        };

        float labelY = y + h + 2;
        GUI.Label(new Rect(x, labelY, w, 20),
            $"Top: waveform (red = clip {clipLevel:P0})  |  Bottom: spectrum (0 ~ {pitchTracker.sampleRate / 2} Hz)", style);
        GUI.Label(new Rect(x, labelY + 14, w, 20),
            "Markers: 100  200  300  400  500  1k  2k Hz", style);
        GUI.Label(new Rect(x, labelY + 28, w, 20),
            $"Mic RMS: {pitchTracker.CurrentRMS:F4}  |  Pitch: {pitchTracker.Pitch:F1} Hz", style);
    }
}
