using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class Enemy : NetworkBehaviour
{
    [Header("Stats")]
    [Tooltip("Shared with this creature's tower form — maxHp/speed/killReward/armor all come from here.")]
    [SerializeField] ToyScriptableObject enemyData;
    [SerializeField] float enemyOffset = 0f;

    [Header("Health Bar")]
    [SerializeField] float barWidth = 1f;
    [SerializeField] float barHeight = 0.12f;
    [SerializeField] float barYOffset = 1.3f;

    // Server-writable — the single source of truth. Every client's health bar just
    // reacts to OnValueChanged instead of computing its own HP.
    NetworkVariable<float> _currentHp = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public float CurrentHp => _currentHp.Value;

    List<Vector3> _waypoints;
    int _index;

    Quaternion _baseRotation;
    Image _hpFill;
    Transform _barCanvas;
    Camera _cam;

    public override void OnNetworkSpawn()
    {
        if (enemyData == null)
        {
            Debug.LogError($"[Enemy] {gameObject.name} has no enemyData assigned — drag its ToyScriptableObject onto the Enemy component.", this);
            return;
        }

        // Visual-only setup — every machine builds its own health bar UI.
        _cam = Camera.main;
        _baseRotation = transform.rotation;
        BuildHealthBar();
        _currentHp.OnValueChanged += (oldHp, newHp) => RefreshBar();

        if (!IsServer) return;

        // Authority-only: server owns HP and movement.
        _currentHp.Value = enemyData.MaxHealth;
        _waypoints = EnemyPath.Instance?.Waypoints;
        if (_waypoints == null || _waypoints.Count == 0)
        {
            Debug.LogError("Enemy: No waypoints available.");
            return;
        }
        transform.position = _waypoints[0] + Vector3.up * enemyOffset;
        _index = 1;
    }

    void BuildHealthBar()
    {
        var canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = Vector3.zero;

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();

        var canvasRt = canvasGO.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(barWidth, barHeight);
        _barCanvas = canvasGO.transform;

        // Dark background
        var bg = new GameObject("BG");
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f);
        StretchRect(bg.GetComponent<RectTransform>());

        // Green fill that shrinks left as HP drops
        var fill = new GameObject("Fill");
        fill.transform.SetParent(canvasGO.transform, false);
        _hpFill = fill.AddComponent<Image>();
        _hpFill.color = Color.green;
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillRt.pivot = new Vector2(0f, 0.5f);

        RefreshBar();
    }

    static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void Update()
    {
        // Health bar always faces the camera — purely visual, every machine does its own.
        if (_barCanvas != null && _cam != null)
        {
            _barCanvas.localPosition = transform.InverseTransformDirection(Vector3.up) * barYOffset;
            _barCanvas.LookAt(_cam.transform);
            _barCanvas.Rotate(0f, 180f, 0f);
        }

        if (!IsServer) return; // Only the server moves enemies and decides death/reaching the end
        if (_waypoints == null) return;
        if (_index >= _waypoints.Count)
        {
            LevelManager.Instance?.EnemyReachedEnd();
            NetworkObject.Despawn();
            return;
        }

        Vector3 target = _waypoints[_index] + Vector3.up * enemyOffset;
        transform.position = Vector3.MoveTowards(transform.position, target, enemyData.Speed * Time.deltaTime);

        Vector3 dir = target - transform.position;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized) * _baseRotation;

        if (Vector3.Distance(transform.position, target) < 0.05f)
            _index++;
    }

    // Whoever detects the hit (a Toy) calls this instead of touching HP directly.
    // Only the server actually applies damage — this is the single authority for HP/death.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TakeDamageRpc(float damage, bool ignoresArmor = false)
    {
        float effective = ignoresArmor ? damage : damage * (1f - enemyData.Armor);
        _currentHp.Value = Mathf.Max(0f, _currentHp.Value - Mathf.Max(0f, effective));
        if (_currentHp.Value <= 0f) Die();
    }

    void RefreshBar()
    {
        if (_hpFill == null) return;
        float ratio = _currentHp.Value / enemyData.MaxHealth;
        var rt = _hpFill.GetComponent<RectTransform>();
        rt.anchorMax = new Vector2(ratio, 1f);
        _hpFill.color = Color.Lerp(Color.red, Color.green, ratio);
    }

    // Server-only: award money then despawn, which removes the object on every client.
    void Die()
    {
        LevelManager.Instance?.AddMoney(enemyData.KillReward);
        NetworkObject.Despawn();
    }

    public bool HasReachedEnd() => _waypoints != null && _index >= _waypoints.Count;
}
