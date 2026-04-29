using UnityEngine;

[System.Serializable]
public struct ToyTypeDefinition
{
    public string typeName;
    public GameObject prefab;
    [Tooltip("If any of these words appear in the transcription, this toy type is selected.")]
    public string[] triggerWords;
}

public class ToyTypeCatalog : MonoBehaviour
{
    [Tooltip("Ordered by priority — first match wins. Put highest-priority type at index 0.")]
    [SerializeField] ToyTypeDefinition[] toyTypes;

    public GameObject CheckVoiceInput(string text)
    {
        if (toyTypes == null) return null;
        foreach (var toyType in toyTypes)
        {
            if (toyType.triggerWords == null) continue;
            foreach (var word in toyType.triggerWords)
            {
                if (!string.IsNullOrEmpty(word) && text.Contains(word))
                    return toyType.prefab;
            }
        }
        return null;
    }
}
