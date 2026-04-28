using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] float maxHp = 50f;
    [SerializeField] float speed = 0.5f;    // tiles per second
    [SerializeField] float armor = 0f;
    [SerializeField] int killReward = 10;

    [Header("Health Bar")]
    [SerializeField] float barWidth = 1f;
    [SerializeField] float barHeight = 0.12f;
    [SerializeField] float barYOffset = 1.3f;

    float _currentHp;
    public float CurrentHp => _currentHp;

    List<Vector3> _waypoints;
    int _index;

    Image _hpFill;
    Transform _barCanvas;
    Camera _cam;

    void Start()
    {
        _cam = Camera.main;
        _currentHp = maxHp;
        BuildHealthBar();

        _waypoints = EnemyPath.Instance?.Waypoints;
        if (_waypoints == null || _waypoints.Count == 0)
        {
            Debug.LogError("Enemy: No waypoints available.");
            return;
        }
        transform.position = _waypoints[0];
        _index = 1;
    }

    void BuildHealthBar()
    {
        var canvasGO = new GameObject("HealthBarCanvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = Vector3.up * barYOffset;

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
        // Health bar always faces the camera
        if (_barCanvas != null && _cam != null)
        {
            _barCanvas.LookAt(_cam.transform);
            _barCanvas.Rotate(0f, 180f, 0f);
        }

        if (_waypoints == null) return;
        if (_index >= _waypoints.Count)
        {
            GameManager.Instance?.EnemyReachedEnd();
            Destroy(gameObject);
            return;
        }

        Vector3 target = _waypoints[_index];
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        Vector3 dir = target - transform.position;
        if (dir.sqrMagnitude > 0.001f)
            transform.forward = dir.normalized;

        if (Vector3.Distance(transform.position, target) < 0.05f)
            _index++;
    }

    public void TakeDamage(float damage)
    {
        float effective = Mathf.Max(0f, damage - armor);
        _currentHp = Mathf.Max(0f, _currentHp - effective);
        RefreshBar();
        if (_currentHp <= 0f) Die();
    }

    void RefreshBar()
    {
        if (_hpFill == null) return;
        float ratio = _currentHp / maxHp;
        var rt = _hpFill.GetComponent<RectTransform>();
        rt.anchorMax = new Vector2(ratio, 1f);
        _hpFill.color = Color.Lerp(Color.red, Color.green, ratio);
    }

    void Die()
    {
        GameManager.Instance?.AddMoney(killReward);
        Destroy(gameObject);
    }

    public bool HasReachedEnd() => _waypoints != null && _index >= _waypoints.Count;
}
