using UnityEngine;

public class MicCaptureManager : MonoBehaviour
{
    public static MicCaptureManager Instance { get; private set; }

    [Header("Microphone")]
    [SerializeField] int sampleRate = 16000;
    [Tooltip("Length of the circular recording buffer, in seconds.")]
    [SerializeField] int bufferLengthSec = 30;

    public AudioClip Clip { get; private set; }
    public string Device { get; private set; }
    public int SampleRate => sampleRate;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Device = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
        Clip = Microphone.Start(Device, true, bufferLengthSec, sampleRate);
    }

    void OnDestroy()
    {
        if (Instance == this && Device != null && Microphone.IsRecording(Device))
            Microphone.End(Device);
    }

    public int GetPosition() => Microphone.GetPosition(Device);

    // Returns samples recorded between startPos and the current mic position, handling ring-buffer wraparound.
    public float[] GetSamplesSince(int startPos)
    {
        int currentPos = GetPosition();
        int total = Clip.samples;
        int length = currentPos - startPos;
        if (length < 0) length += total;
        if (length <= 0) return System.Array.Empty<float>();

        var samples = new float[length];

        if (startPos + length <= total)
        {
            Clip.GetData(samples, startPos);
        }
        else
        {
            int firstPart = total - startPos;
            var head = new float[firstPart];
            Clip.GetData(head, startPos);
            System.Array.Copy(head, samples, firstPart);

            var tail = new float[length - firstPart];
            Clip.GetData(tail, 0);
            System.Array.Copy(tail, 0, samples, firstPart, tail.Length);
        }

        return samples;
    }
}
