using UnityEngine;
using UnityEngine.InputSystem;

public class LaunchManager : MonoBehaviour
{
    [Header("References")]
    public ToyBelt toyBelt;

    [Header("Aim")]
    public float maxPitchDegrees = 60f;
    public float maxYawDegrees = 90f;
    public float aimSpeed = 45f; // degrees per second

    [Header("Launch")]
    public float minLaunchSpeed = 5f;
    public float maxLaunchSpeed = 30f;
    public float maxChargeTime = 2f;

    GameObject _loadedToy;
    Quaternion _initialRotation;
    float _currentPitch;
    float _currentYaw;
    bool _isCharging;
    float _chargeTime;

    void Awake()
    {
        _initialRotation = transform.rotation;
    }

    void OnEnable()
    {
        if (toyBelt != null)
            toyBelt.OnToyArrived += OnToyArrived;
    }

    void OnDisable()
    {
        if (toyBelt != null)
            toyBelt.OnToyArrived -= OnToyArrived;
    }

    void OnToyArrived(GameObject toy)
    {
        _loadedToy = toy;
        // Snap toy to launcher muzzle
        toy.transform.SetParent(transform);
        toy.transform.localPosition = Vector3.zero;
        Rigidbody rb = toy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }
        Debug.Log($"[LaunchManager] Toy loaded: {toy.name}");
    }

    void Update()
    {
        if (_loadedToy == null) return;

        HandleAim();
        HandleCharge();
    }

    void HandleAim()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float pitchInput = (kb.upArrowKey.isPressed ? 1 : 0) - (kb.downArrowKey.isPressed ? 1 : 0);
        float yawInput   = (kb.rightArrowKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed ? 1 : 0);

        _currentPitch = Mathf.Clamp(_currentPitch + pitchInput * aimSpeed * Time.deltaTime, -maxPitchDegrees, maxPitchDegrees);
        _currentYaw   = Mathf.Clamp(_currentYaw   + yawInput   * aimSpeed * Time.deltaTime, -maxYawDegrees,   maxYawDegrees);

        // Yaw around world Y, pitch around initial local X
        Quaternion yawRot = Quaternion.AngleAxis(_currentYaw, Vector3.up);
        Quaternion pitchRot = Quaternion.Euler(-_currentPitch, 0f, 0f);
        transform.rotation = yawRot * _initialRotation * pitchRot;
    }

    void HandleCharge()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _isCharging = true;
            _chargeTime = 0f;
        }

        if (_isCharging && mouse.leftButton.isPressed)
            _chargeTime = Mathf.Min(_chargeTime + Time.deltaTime, maxChargeTime);

        if (_isCharging && mouse.leftButton.wasReleasedThisFrame)
        {
            _isCharging = false;
            Launch();
        }
    }

    void Launch()
    {
        if (_loadedToy == null) return;

        float chargeRatio = _chargeTime / maxChargeTime;
        float speed = Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, chargeRatio);

        _loadedToy.transform.SetParent(null);
        Rigidbody rb = _loadedToy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = transform.forward * speed;
        }

        Debug.Log($"[LaunchManager] Launched at speed {speed:F1} (charge {chargeRatio:P0})");
        _loadedToy = null;
    }
}
