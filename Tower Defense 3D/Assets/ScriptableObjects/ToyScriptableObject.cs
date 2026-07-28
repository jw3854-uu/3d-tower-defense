using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ToyPrefab
{
    public GameObject prefab;
}

[CreateAssetMenu(fileName = "ToyScriptableObject", menuName = "ScriptableObjects/ToyScriptableObject")]
public class ToyScriptableObject : ScriptableObject
{
    [Header("Toy Types")]
    [SerializeField] string typeName;
    [SerializeField] int maxHealth;
    [SerializeField] ToyPrefab prefab;

    [Header("As Tower")]
    [SerializeField] string[] spawnTriggerWords;
    [SerializeField] string[] shootingTriggerWords;
    [SerializeField] int cost;
    [SerializeField] float attackRange;
    [SerializeField] float attackRate;
    [SerializeField] int attackDamage;

    [Header("As Enemy")]
    // [SerializeField] int damage;
    [SerializeField] float speed;

    public string TypeName => typeName;
    public int MaxHealth => maxHealth;
    public ToyPrefab Prefab => prefab;

    public IReadOnlyList<string> SpawnTriggerWords => spawnTriggerWords;
    public IReadOnlyList<string> ShootingTriggerWords => shootingTriggerWords;
    public int Cost => cost;
    public float AttackRange => attackRange;
    public float AttackRate => attackRate;
    public int AttackDamage => attackDamage;

    // public int Damage => damage;
    public float Speed => speed;
}
