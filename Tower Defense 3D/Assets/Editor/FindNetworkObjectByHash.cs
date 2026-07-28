using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public static class FindNetworkObjectByHash
{
    // Change this if you need to look up a different hash later.
    const uint TargetHash = 3699521173u;

    [MenuItem("Tools/Find NetworkObject By Hash")]
    static void Find()
    {
        var all = Resources.FindObjectsOfTypeAll<NetworkObject>();
        bool found = false;

        foreach (var no in all)
        {
            string scenePath = no.gameObject.scene.IsValid() ? no.gameObject.scene.path : "(prefab asset, not in a scene)";
            Debug.Log($"[FindNetworkObjectByHash] '{no.gameObject.name}' hash={no.PrefabIdHash} scene={scenePath}", no.gameObject);

            if (no.PrefabIdHash == TargetHash)
            {
                found = true;
                Debug.LogWarning($"[FindNetworkObjectByHash] MATCH: {no.gameObject.name}", no.gameObject);
                Selection.activeGameObject = no.gameObject;
                EditorGUIUtility.PingObject(no.gameObject);
            }
        }

        if (!found)
            Debug.LogWarning($"[FindNetworkObjectByHash] No currently-open scene has a NetworkObject with hash {TargetHash}. Try opening every scene that might contain it (Main Menu / Character Select / your test scene) one at a time and re-running this.");
    }
}
