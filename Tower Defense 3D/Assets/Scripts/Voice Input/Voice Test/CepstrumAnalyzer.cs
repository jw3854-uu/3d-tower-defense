using UnityEngine;
using System;

public class CepstrumAnalyzer
{
    public struct Result
    {
        public float Pitch;
        public float Strength;
    }

    readonly int _fftSize;
    readonly int _sampleRate;
    readonly float _minFreq;
    readonly float _maxFreq;

    float[] _noiseFloor;
    int _noiseFrames;

    float[] _windowed;
    float[] _real;
    float[] _imag;
    float[] _mag;

    public bool NoiseReady => _noiseFrames >= 10;

    public CepstrumAnalyzer(int fftSize, int sampleRate,
        float minFreq = 80f, float maxFreq = 600f)
    {
        _fftSize = fftSize;
        _sampleRate = sampleRate;
        _minFreq = minFreq;
        _maxFreq = maxFreq;

        _noiseFloor = new float[fftSize];
        _noiseFrames = 0;

        _windowed = new float[fftSize];
        _real = new float[fftSize];
        _imag = new float[fftSize];
        _mag = new float[fftSize];
    }

    public void LearnNoise(float[] samples, int offset)
    {
        if (offset < 0 || offset + _fftSize > samples.Length) return;

        ApplyWindow(samples, offset);
        Array.Copy(_windowed, _real, _fftSize);
        Array.Clear(_imag, 0, _fftSize);
        FFT(_real, _imag, false);

        for (int i = 0; i < _fftSize; i++)
        {
            float m = Mathf.Sqrt(_real[i] * _real[i] + _imag[i] * _imag[i]);
            _noiseFloor[i] = (_noiseFloor[i] * _noiseFrames + m) / (_noiseFrames + 1);
        }
        _noiseFrames++;
    }

    public void ResetNoise()
    {
        Array.Clear(_noiseFloor, 0, _fftSize);
        _noiseFrames = 0;
    }

    public Result AnalyzeFrame(float[] samples, int offset,
        float peakThreshold, bool denoise)
    {
        var result = new Result();
        if (offset < 0 || offset + _fftSize > samples.Length) return result;

        ApplyWindow(samples, offset);

        float energy = 0f;
        for (int i = 0; i < _fftSize; i++)
            energy += _windowed[i] * _windowed[i];
        if (energy / _fftSize < 0.0001f) return result;

        // FFT → magnitude spectrum
        Array.Copy(_windowed, _real, _fftSize);
        Array.Clear(_imag, 0, _fftSize);
        FFT(_real, _imag, false);

        for (int i = 0; i < _fftSize; i++)
            _mag[i] = Mathf.Sqrt(_real[i] * _real[i] + _imag[i] * _imag[i]);

        // Spectral subtraction
        if (denoise && _noiseFrames > 0)
        {
            for (int i = 0; i < _fftSize; i++)
                _mag[i] = Mathf.Max(_mag[i] - _noiseFloor[i] * 1.5f, _mag[i] * 0.01f);
        }

        // Log magnitude → IFFT → cepstrum
        for (int i = 0; i < _fftSize; i++)
        {
            _real[i] = Mathf.Log(Mathf.Max(_mag[i], 1e-10f));
            _imag[i] = 0f;
        }
        FFT(_real, _imag, true);

        // Find the strongest peak in the quefrency range for human pitch
        int minQ = Mathf.Max(2, _sampleRate / Mathf.RoundToInt(_maxFreq));
        int maxQ = Mathf.Min(_fftSize / 2 - 1, _sampleRate / Mathf.RoundToInt(_minFreq));

        float bestVal = 0f;
        int bestQ = 0;

        for (int q = minQ; q <= maxQ; q++)
        {
            float v = _real[q];
            if (v <= 0f) continue;
            if (q > minQ && _real[q - 1] >= v) continue;
            if (q < maxQ && _real[q + 1] >= v) continue;

            if (v > bestVal) { bestVal = v; bestQ = q; }
        }

        if (bestVal < peakThreshold || bestQ == 0) return result;

        result.Pitch = (float)_sampleRate / bestQ;
        result.Strength = bestVal;
        return result;
    }

    public Result AnalyzeBuffer(float[] buffer, float peakThreshold, bool denoise)
    {
        int hop = _fftSize / 2;
        int count = Mathf.Max(1, (buffer.Length - _fftSize) / hop + 1);

        if (count == 1)
            return AnalyzeFrame(buffer, buffer.Length - _fftSize, peakThreshold, denoise);

        float[] pitches = new float[count];
        for (int f = 0; f < count; f++)
        {
            var r = AnalyzeFrame(buffer, f * hop, peakThreshold, denoise);
            pitches[f] = r.Pitch;
        }

        return new Result { Pitch = MedianPositive(pitches) };
    }

    void ApplyWindow(float[] src, int offset)
    {
        for (int i = 0; i < _fftSize; i++)
        {
            float w = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (_fftSize - 1)));
            _windowed[i] = src[offset + i] * w;
        }
    }

    static float MedianPositive(float[] v)
    {
        int n = 0;
        for (int i = 0; i < v.Length; i++)
            if (v[i] > 0f) n++;
        if (n == 0) return 0f;
        float[] s = new float[n];
        int idx = 0;
        for (int i = 0; i < v.Length; i++)
            if (v[i] > 0f) s[idx++] = v[i];
        Array.Sort(s);
        return s[n / 2];
    }

    static void FFT(float[] re, float[] im, bool inverse)
    {
        int n = re.Length;
        int j = 0;
        for (int i = 0; i < n - 1; i++)
        {
            if (i < j)
            {
                (re[i], re[j]) = (re[j], re[i]);
                (im[i], im[j]) = (im[j], im[i]);
            }
            int k = n >> 1;
            while (k <= j) { j -= k; k >>= 1; }
            j += k;
        }

        float dir = inverse ? 1f : -1f;
        for (int step = 2; step <= n; step <<= 1)
        {
            int half = step >> 1;
            float ang = dir * Mathf.PI / half;
            float wR = Mathf.Cos(ang), wI = Mathf.Sin(ang);
            for (int g = 0; g < n; g += step)
            {
                float tR = 1f, tI = 0f;
                for (int p = 0; p < half; p++)
                {
                    int a = g + p, b = a + half;
                    float bR = re[b] * tR - im[b] * tI;
                    float bI = re[b] * tI + im[b] * tR;
                    re[b] = re[a] - bR;
                    im[b] = im[a] - bI;
                    re[a] += bR;
                    im[a] += bI;
                    float nR = tR * wR - tI * wI;
                    tI = tR * wI + tI * wR;
                    tR = nR;
                }
            }
        }

        if (inverse)
            for (int i = 0; i < n; i++)
            { re[i] /= n; im[i] /= n; }
    }
}
