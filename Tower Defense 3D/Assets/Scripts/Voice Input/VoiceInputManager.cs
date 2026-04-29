using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Whisper;
using Whisper.Utils;

[RequireComponent(typeof(MicrophoneRecord))]
public class VoiceInputManager : MonoBehaviour
{
    [Header("References")]
    public WhisperManager whisperManager;

    // Subscribe to this to receive transcription results
    public event Action<string> OnTranscriptionResult;
    public event Action OnRecordingStarted;
    public event Action<string> OnTranscribed;
    private bool _active;
    public bool IsRecording => _microphone != null && _microphone.IsRecording;

    MicrophoneRecord _microphone;
    bool _isTranscribing;

    void Awake()
    {
        _microphone = GetComponent<MicrophoneRecord>();
        _microphone.OnRecordStop += OnRecordStop;
        _active = false;
    }

    public void EnableVoiceInput()
    {
        _active = true;
    }

    public void DisableVoiceInput()
    {
        _active = false;
    }

    void Update()
    {
        if (!_active) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        // Record while pressing R
        if (kb.rKey.wasPressedThisFrame)
            StartRecording();

        if (kb.rKey.wasReleasedThisFrame)
            StopRecording();
    }

    void StartRecording()
    {
        if (_microphone.IsRecording || _isTranscribing) return;
        _microphone.StartRecord();
        OnRecordingStarted?.Invoke();
    }

    void StopRecording()
    {
        if (!_microphone.IsRecording) return;
        _microphone.StopRecord();
        // Debug.Log("Recording stopped, transcribing...");
    }

    async void OnRecordStop(AudioChunk chunk)
    {
        if (_isTranscribing) return;
        _isTranscribing = true;

        WhisperResult result = await whisperManager.GetTextAsync(
            chunk.Data, chunk.Frequency, chunk.Channels
        );

        string text = result?.Result?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            Debug.Log($"Transcription: {text}");
            OnTranscriptionResult?.Invoke(text);
        }

        _isTranscribing = false;
        DisableVoiceInput(); 
        OnTranscribed?.Invoke(text);
    }

    void OnDestroy()
    {
        _microphone.OnRecordStop -= OnRecordStop;
    }
}
