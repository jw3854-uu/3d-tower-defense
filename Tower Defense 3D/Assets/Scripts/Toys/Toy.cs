using UnityEngine;

public class Toy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] float damage = 20f;
    [SerializeField] float attacksPerSecond = 1f;
    [SerializeField] float attackRange = 3f;    // world units; 1 unit = 1 tile
    [SerializeField] int price = 50;

    float _cooldown;
    bool _active;

    // Called by ToyManager once the toy has successfully landed on a valid tile
    public void Activate()
    {
        _active = true;
        Debug.Log($"[Toy] {gameObject.name} activated.");
    }

    void Update()
    {
        if (!_active) return;

        _cooldown -= Time.deltaTime;
        if (_cooldown > 0f) return;

        Enemy target = FindLowestHpInRange();
        if (target == null) return;

        target.TakeDamage(damage);
        _cooldown = 1f / attacksPerSecond;
    }

    Enemy FindLowestHpInRange()
    {
        Enemy[] all = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy best = null;
        float lowestHp = float.MaxValue;

        foreach (Enemy e in all)
        {
            if (Vector3.Distance(transform.position, e.transform.position) > attackRange) continue;
            if (e.CurrentHp < lowestHp)
            {
                lowestHp = e.CurrentHp;
                best = e;
            }
        }

        return best;
    }

    public int Price => price;
}
