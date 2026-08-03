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

    [Header("As Tower")]
    [Tooltip("Spawned by LaunchManager when A summons this type and B throws it — carries Toy + ToyManager.")]
    [SerializeField] ToyPrefab towerPrefab;
    [SerializeField] string[] spawnTriggerWords;
    [SerializeField] string[] shootingTriggerWords;
    [SerializeField] int cost;
    [SerializeField] float attackRange;
    [SerializeField] float attackRate;
    [SerializeField] int attackDamage;

    [Header("As Enemy")]
    [Tooltip("Spawned by EnemyManager for waves — carries Enemy.")]
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] float speed;
    [SerializeField] int killReward;
    [Range(0f, 1f)]
    [SerializeField] float armor; // fraction: 0 = no reduction, 0.5 = 50%

    public string TypeName => typeName;
    public int MaxHealth => maxHealth;
    public ToyPrefab TowerPrefab => towerPrefab;
    public GameObject EnemyPrefab => enemyPrefab;

    public IReadOnlyList<string> SpawnTriggerWords => spawnTriggerWords;
    public IReadOnlyList<string> ShootingTriggerWords => shootingTriggerWords;
    public int Cost => cost;
    public float AttackRange => attackRange;
    public float AttackRate => attackRate;
    public int AttackDamage => attackDamage;

    public float Speed => speed;
    public int KillReward => killReward;
    public float Armor => armor;
}
