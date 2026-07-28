using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ToyCatalogEntry
{
    public ToyScriptableObject data;
}

public class ToyCatalog : MonoBehaviour
{
    // Scene-placed, per-level — same singleton pattern as EnemyPath/LevelManager.
    public static ToyCatalog Instance { get; private set; }

    [SerializeField] ToyCatalogEntry[] entries;

    void Awake()
    {
        Instance = this;
    }

    // Server-side re-lookup by name, so the server never has to trust a client-supplied
    // cost/prefab — it only trusts the type name and re-derives everything itself.
    public ToyScriptableObject FindByTypeName(string typeName)
    {
        if (entries == null) return null;

        foreach (var entry in entries)
        {
            if (entry.data != null && entry.data.TypeName == typeName)
                return entry.data;
        }
        return null;
    }

    public ToyScriptableObject CheckVoiceInput(string text)
    {
        if (entries == null || string.IsNullOrEmpty(text)) return null;

        foreach (var entry in entries)
        {
            if (entry.data == null) continue;
            foreach (var word in entry.data.SpawnTriggerWords)
            {
                if (!string.IsNullOrEmpty(word) && text.Contains(word))
                    return entry.data;
            }
        }
        return null;
    }

    public IEnumerable<string> GetAllTriggerWords()
    {
        if (entries == null) yield break;

        foreach (var entry in entries)
        {
            if (entry.data == null) continue;
            foreach (var word in entry.data.SpawnTriggerWords)
            {
                if (!string.IsNullOrEmpty(word))
                    yield return word;
            }
        }
    }
}
