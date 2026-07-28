using UnityEngine;

public static class PitchDetector
{
    public static float Detect(float[] samples, int sampleRate,
        float minFreq = 80f, float maxFreq = 600f,
        float clarityThreshold = 0.25f, float clipLevel = 0.4f)
    {
        if (samples == null || samples.Length < 2) return 0f;

        int minLag = Mathf.Max(1, sampleRate / Mathf.RoundToInt(maxFreq));
        int maxLag = Mathf.Min(samples.Length / 2, sampleRate / Mathf.RoundToInt(minFreq));
        if (minLag >= maxLag) return 0f;

        float energy = 0f;
        for (int i = 0; i < samples.Length; i++)
            energy += samples[i] * samples[i];
        if (energy / samples.Length < 0.0001f) return 0f;

        // Center clipping: suppresses harmonics weaker than the fundamental
        float peak = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);
            if (abs > peak) peak = abs;
        }

        float threshold = peak * clipLevel;
        float[] clipped = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
            clipped[i] = Mathf.Abs(samples[i]) > threshold ? samples[i] : 0f;

        // Normalized autocorrelation
        float[] corrs = new float[maxLag + 1];
        float bestCorr = 0f;

        for (int lag = minLag; lag <= maxLag; lag++)
        {
            float num = 0f, d1 = 0f, d2 = 0f;
            int n = clipped.Length - lag;
            for (int i = 0; i < n; i++)
            {
                num += clipped[i] * clipped[i + lag];
                d1 += clipped[i] * clipped[i];
                d2 += clipped[i + lag] * clipped[i + lag];
            }
            float denom = Mathf.Sqrt(d1 * d2);
            corrs[lag] = denom > 1e-8f ? num / denom : 0f;
            if (corrs[lag] > bestCorr) bestCorr = corrs[lag];
        }

        if (bestCorr < clarityThreshold) return 0f;

        // Scan from high lag (low freq) to bias toward fundamental over harmonics
        float primThreshold = bestCorr * 0.85f;
        for (int lag = maxLag; lag >= minLag; lag--)
        {
            if (corrs[lag] >= primThreshold)
                return (float)sampleRate / lag;
        }

        return 0f;
    }
}
