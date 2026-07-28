using UnityEngine;
using UnityEngine.InputSystem;

public class VoiceAimTest : MonoBehaviour
{
    [Header("Pitch Tracker")]
    public PitchTracker pitchTracker;

    [Header("Yaw (Keyboard A/D)")]
    public float yawSpeed = 90f;
    public float maxYaw = 120f;

    [Header("Pitch → Elevation (Voice)")]
    [Tooltip("Voice pitch (Hz) that maps to minimum elevation.")]
    public float pitchMin = 100f;
    [Tooltip("Voice pitch (Hz) that maps to maximum elevation.")]
    public float pitchMax = 400f;
    public float maxElevation = 60f;
    public float elevationSmoothing = 5f;

    float _yaw;
    float _elevation;
    float _targetElevation;
    Quaternion _baseRotation;

    float _dbgPitch;
    bool _active;

    void Awake()
    {
        _baseRotation = transform.rotation;
    }

    void Start()
    {
        StartAiming();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.tabKey.wasPressedThisFrame)
        {
            if (_active) StopAiming();
            else StartAiming();
            return;
        }

        if (!_active) return;

        float yawInput = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        _yaw = Mathf.Clamp(_yaw + yawInput * yawSpeed * Time.deltaTime, -maxYaw, maxYaw);

        float hz = pitchTracker.Pitch;
        _dbgPitch = hz;

        if (hz > 0f)
        {
            float t = Mathf.InverseLerp(pitchMin, pitchMax, hz);
            _targetElevation = Mathf.Lerp(-maxElevation, maxElevation, t);
        }

        _elevation = Mathf.Lerp(_elevation, _targetElevation, elevationSmoothing * Time.deltaTime);

        Quaternion yawRot = Quaternion.AngleAxis(_yaw, Vector3.up);
        Quaternion pitchRot = Quaternion.Euler(-_elevation, 0f, 0f);
        transform.rotation = yawRot * _baseRotation * pitchRot;

        Debug.DrawRay(transform.position, transform.forward * 5f, Color.red);
    }

    void StartAiming()
    {
        _active = true;
        pitchTracker.StartTracking();
        Debug.Log("[VoiceAimTest] AIM ON  |  A/D = yaw  |  Voice pitch = elevation  |  Tab = toggle");
    }

    void StopAiming()
    {
        _active = false;
        pitchTracker.StopTracking();
        Debug.Log("[VoiceAimTest] AIM OFF  |  Tab = toggle");
    }

    void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
        GUILayout.BeginArea(new Rect(10, 10, 420, 240));

        string status = _active ? "<color=cyan>AIM ON</color>" : "<color=grey>AIM OFF</color>";
        GUILayout.Label($"<b>Status:</b> {status}", style);

        GUILayout.Space(8);

        string pitchColor = _dbgPitch > 0f ? "lime" : "yellow";
        GUILayout.Label($"Pitch: <color={pitchColor}><b>{_dbgPitch:F1} Hz</b></color>", style);

        GUILayout.Space(4);
        GUILayout.Label($"Yaw: {_yaw:F1}°    Elevation: {_elevation:F1}°", style);

        if (pitchTracker != null && pitchTracker.IsTracking)
        {
            float rms = pitchTracker.CurrentRMS;
            int barLen = Mathf.Clamp(Mathf.RoundToInt(rms * 200f), 0, 40);
            string bar = new string('|', barLen);
            GUILayout.Label($"Mic Level: <color=lime>{bar}</color>", style);

            if (!pitchTracker.NoiseCalibrated)
                GUILayout.Label("<color=yellow>Calibrating noise floor — stay quiet...</color>", style);
        }

        GUILayout.Space(12);
        GUILayout.Label(_active ? "A/D=yaw  Sing/hum=elevation  Tab=stop" : "Tab=start", style);

        GUILayout.EndArea();
    }
}
